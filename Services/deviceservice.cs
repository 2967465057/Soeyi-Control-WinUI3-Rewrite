using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using NLog;
using SkiaSharp;
using SoeyiWinUI_v2.Native;

namespace SoeyiWinUI_v2.Services;

public class DeviceService : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HashSet<uint> _attachedHandles = new();
    private CancellationTokenSource? _streamCts;
    private bool _started;
    private bool _streaming;
    private readonly ThemeService _theme;
    private readonly HardwareMonitorService? _hwMon;

    // Pinned delegates so GC doesn't collect callback delegates
    private DeviceApi.AttachProcHandler? _onAttachDel;
    private DeviceApi.DetachProcHandler? _onDetachDel;
    private AicDeviceApi.AttachProcHandler? _onAicAttachDel;
    private AicDeviceApi.DetachProcHandler? _onAicDetachDel;

    public DeviceService(ThemeService theme, HardwareMonitorService? hwMon = null)
    {
        _theme = theme;
        _hwMon = hwMon;
    }

    public event EventHandler<DeviceInfo>? DeviceAttached;
    public event EventHandler<uint>? DeviceDetached;
    public IReadOnlyCollection<uint> AttachedHandles => _attachedHandles;

    private const int DisplayOutW = 320;
    private const int DisplayOutH = 1480;
    private const int TargetFps = 30;
    private const int JpegQuality = 50;

    private int _currentBrightness = 80;
    private int _currentVolume = 50;
    private int _currentRotation = 180;

    // ── Start / Stop ──

    private int _startLock; // 0=idle, 1=starting, 2=started

    public int Start(int logLevel = 0)
    {
        if (Interlocked.CompareExchange(ref _startLock, 1, 0) != 0)
        {
            Logger.Info("Start already in progress or completed (state={0})", _startLock);
            return _started ? 0 : -1;
        }

        Logger.Info("=== DeviceService.Start ===");

        // Check for DLS (serial) devices first via WMI
        var dlsDevices = ScanDlsDevices();
        Logger.Info("DLS scan: found {0} devices", dlsDevices.Count);

        // If DLS device found, try serial directly (skip native drivers which may lock COM port)
        if (dlsDevices.Count > 0)
        {
            Logger.Info("DLS device detected — using serial path");
            int dlsRc = StartDls();
            if (dlsRc >= 0) { _started = true; _startLock = 2; _startLock = 2; return 0; }
            Logger.Warn("DLS connect failed ({0}), falling back to native drivers", dlsRc);
        }

        // Try MSDisplay driver (USB video display)
        int rc = DeviceApi.Wrraper_MSDisplayStart(logLevel, IntPtr.Zero);
        Logger.Info("MSDisplayStart returned {0}", rc);

        if (rc >= 0)
        {
            _onAttachDel = OnMsAttach;
            _onDetachDel = OnMsDetach;
            var attachPtr = Marshal.GetFunctionPointerForDelegate(_onAttachDel);
            var detachPtr = Marshal.GetFunctionPointerForDelegate(_onDetachDel);
            DeviceApi.Wrraper_MSDisplayRegisterCallback(attachPtr, detachPtr);
            Logger.Info("MSDisplay callbacks registered");
            _started = true; _startLock = 2;
            return 0;
        }

        // Try AIC driver
        int rc2 = AicDeviceApi.AICDispStart();
        Logger.Info("AICDispStart returned {0}", rc2);
        if (rc2 < 0)
        {
            Logger.Warn("No display driver available");
            return -100;
        }

        _onAicAttachDel = OnAicAttach;
        _onAicDetachDel = OnAicDetach;
        var aAttach = Marshal.GetFunctionPointerForDelegate(_onAicAttachDel);
        var aDetach = Marshal.GetFunctionPointerForDelegate(_onAicDetachDel);
        AicDeviceApi.AICDispRegisterCallback(aAttach, aDetach);
        Logger.Info("AIC callbacks registered");
        _started = true; _startLock = 2;
        return 0;
    }

    public int Stop()
    {
        if (!_started) return 0;
        StopStream();
        try { DeviceApi.Wrraper_MSDisplayStop(); } catch { }
        try { AicDeviceApi.AICDispStop(); } catch { }
        if (_dlsSerialPort != null)
        {
            try { _dlsSerialPort.Close(); _dlsSerialPort.Dispose(); } catch { }
            _dlsSerialPort = null;
        }
        _isDls = false;
        _started = false;
        _startLock = 0;
        _attachedHandles.Clear();
        return 0;
    }

    // ── MSDisplay Callbacks ──

    private void OnMsAttach(uint handle, IntPtr resolutionPtr, int count)
    {
        Logger.Info("MSDisplay attached: handle=0x{0:X}, resolutions={1}", handle, count);

        var resolutions = new List<ResolutionInfo>();
        int size = Marshal.SizeOf<DeviceApi.MSDisplayResolution>();
        for (int i = 0; i < count; i++)
        {
            var r = Marshal.PtrToStructure<DeviceApi.MSDisplayResolution>(resolutionPtr + i * size);
            if (r.IsValid)
                resolutions.Add(new ResolutionInfo(r.Width, r.Height, r.Refresh));
        }

        // Read serial number
        string sn = "";
        try
        {
            var snPtr = Marshal.AllocHGlobal(256);
            try
            {
                if (DeviceApi.MSDisplayReadSN(handle, snPtr) >= 0)
                    sn = Marshal.PtrToStringAnsi(snPtr) ?? "";
            }
            finally { Marshal.FreeHGlobal(snPtr); }
        }
        catch { }

        // Read firmware version
        string fw = "Unknown";
        try
        {
            var fwPtr = Marshal.AllocHGlobal(64);
            try
            {
                if (DeviceApi.MSDisplayGetFirmwareVer(handle, fwPtr) >= 0)
                    fw = Marshal.PtrToStringAnsi(fwPtr) ?? "Unknown";
            }
            finally { Marshal.FreeHGlobal(fwPtr); }
        }
        catch { }

        string name = string.IsNullOrEmpty(sn) ? $"MSDisplay (0x{handle:X})" : $"SOEYI-{sn}";
        var curRes = resolutions.Count > 0 ? resolutions[0] : new ResolutionInfo(DisplayOutW, DisplayOutH, 60);

        _attachedHandles.Add(handle);
        var info = new DeviceInfo(handle, sn, name, fw, curRes,
            resolutions.Count > 0 ? resolutions : new[] { new ResolutionInfo(DisplayOutW, DisplayOutH, 60) },
            _currentBrightness, _currentVolume, _currentRotation, true);
        DeviceAttached?.Invoke(this, info);

        // Start streaming when a device attaches
        StartStream();
    }

    private void OnMsDetach(uint handle)
    {
        Logger.Info("MSDisplay detached: handle=0x{0:X}", handle);
        _attachedHandles.Remove(handle);
        DeviceDetached?.Invoke(this, handle);
        if (_attachedHandles.Count == 0) StopStream();
    }

    // ── AIC Callbacks ──

    private void OnAicAttach(uint handle, IntPtr resolutionPtr, int count)
    {
        Logger.Info("AIC attached: handle=0x{0:X}", handle);
        _attachedHandles.Add(handle);
        string name = $"AIC Display (0x{handle:X})";
        var info = new DeviceInfo(handle, "", name, "AIC",
            new ResolutionInfo(DisplayOutW, DisplayOutH, 60),
            new[] { new ResolutionInfo(DisplayOutW, DisplayOutH, 60) },
            _currentBrightness, _currentVolume, _currentRotation, true);
        DeviceAttached?.Invoke(this, info);
        StartStream();
    }

    private void OnAicDetach(uint handle, IntPtr resolution)
    {
        Logger.Info("AIC detached: handle=0x{0:X}", handle);
        _attachedHandles.Remove(handle);
        DeviceDetached?.Invoke(this, handle);
        if (_attachedHandles.Count == 0) StopStream();
    }

    // ── DLS (Serial-based USB Display) ──

    private static readonly HashSet<string> DlsHardwareIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "VID_33C3&PID_F101", "VID_33C3&PID_F100", "VID_345F&PID_9132"
    };

    private SerialPort? _dlsSerialPort;
    private const int DlsBaudRate = 921600;

    private int StartDls()
    {
        var devices = ScanDlsDevices();
        if (devices.Count == 0)
        {
            Logger.Warn("No DLS device found via WMI");
            return -100;
        }

        var dev = devices[0];

        // Retry up to 3 times with 1s gap (port may be transiently locked)
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Logger.Info("DLS: attempting {0} on {1} @ {2} (attempt {3})", dev.Name, dev.ComPort, DlsBaudRate, attempt);
            try
            {
                _dlsSerialPort = new SerialPort(dev.ComPort, DlsBaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000, WriteTimeout = 2000,
                    DtrEnable = false, RtsEnable = false
                };
                _dlsSerialPort.Open();

                uint handle = 1;
                _attachedHandles.Add(handle);
                _isDls = true;

                var info = new DeviceInfo(handle, "", dev.Name, "DLS/Serial",
                    new ResolutionInfo(DisplayOutW, DisplayOutH, 60),
                    new[] { new ResolutionInfo(DisplayOutW, DisplayOutH, 60) },
                    _currentBrightness, _currentVolume, _currentRotation, true);
                DeviceAttached?.Invoke(this, info);

                StartStream();
                Logger.Info("DLS: connected {0} @ {1}", dev.ComPort, DlsBaudRate);
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "DLS attempt {0} failed: {1}", attempt, ex.Message);
                try { _dlsSerialPort?.Dispose(); } catch { }
                _dlsSerialPort = null;
                if (attempt < 3) { Thread.Sleep(1000); }
            }
        }
        Logger.Error("DLS: all attempts exhausted for {0}", dev.ComPort);
        return -101;
    }

    private bool _isDls;
    private int _frameCount;

    private static List<(string ComPort, string Name, string PnpId)> ScanDlsDevices()
    {
        var results = new List<(string, string, string)>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, PNPDeviceID FROM Win32_SerialPort");
            foreach (ManagementObject port in searcher.Get())
            {
                var c = port["DeviceID"]?.ToString();
                var n = port["Name"]?.ToString();
                var p = port["PNPDeviceID"]?.ToString();
                if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(p)) continue;
                if (DlsHardwareIds.Any(h => p.Contains(h, StringComparison.OrdinalIgnoreCase)))
                {
                    var name = (n?.Replace($"({c})", "").Trim()) ?? $"DLS ({c})";
                    if (string.IsNullOrEmpty(name)) name = $"DLS ({c})";
                    Logger.Info("DLS: {0} matched ({1})", c, p);
                    results.Add((c, name, p));
                }
            }
        }
        catch (Exception ex) { Logger.Error(ex, "DLS WMI scan failed"); }
        return results;
    }

    // ── Frame Streaming ──

    private void StartStream()
    {
        if (_streaming) return;
        _streaming = true;
        _streamCts = new CancellationTokenSource();
        var ct = _streamCts.Token;
        Task.Run(() => StreamLoop(ct), ct);
        Logger.Info("Frame stream started @ {0}fps", TargetFps);
    }

    private void StopStream()
    {
        _streaming = false;
        try { _streamCts?.Cancel(); } catch { }
        _streamCts?.Dispose(); _streamCts = null;
    }

    private async Task StreamLoop(CancellationToken ct)
    {
        const int interval = 1000 / TargetFps;
        try
        {
            while (!ct.IsCancellationRequested && _streaming)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try { SendFrame(); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Logger.Error(ex, "Frame error"); }
                var delay = Math.Max(1, interval - (int)sw.ElapsedMilliseconds);
                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally { Logger.Info("Stream loop exited"); }
    }

    public void SendFrame()
    {
        if (_attachedHandles.Count == 0) return;
        try
        {
            // Save first frame to disk for verification
            if (_frameCount == 0)
            {
                Logger.Info("Sending first frame to {0} handles", _attachedHandles.Count);
            }
            _frameCount++;
            if (_frameCount % 100 == 0)
                Logger.Debug("Frame #{0} — {1} devices", _frameCount, _attachedHandles.Count);
            int outW = DisplayOutW, outH = DisplayOutH;
            var hw = _hwMon?.GetSnapshot() ?? new HwSnapshot(0, 0, 0, 0, 0, 0, 0, 0);
            var theme = _theme.CurrentTheme;

            // Render theme to bitmap at display resolution
            using var surface = SKSurface.Create(new SKImageInfo(outW, outH));
            var canvas = surface.Canvas;
            Rendering.ThemeRenderer.Render(canvas, outW, outH, hw, theme);
            using var srcImg = surface.Snapshot();

            // MSDisplay handles rotation at the driver level — don't double-rotate
            using var finalImg = srcImg;

            if (_isDls)
            {
                // DLS: software rotation + JPEG over serial
                using var outSurface = SKSurface.Create(new SKImageInfo(outW, outH));
                var outCanvas = outSurface.Canvas;
                outCanvas.Clear(SKColors.Black);
                if (_currentRotation != 0)
                    outCanvas.RotateDegrees(_currentRotation, outW / 2f, outH / 2f);
                outCanvas.DrawImage(srcImg, 0, 0);
                using var rotImg = outSurface.Snapshot();

                // Save diagnostic frame
                if (_frameCount == 1)
                {
                    using var diag = rotImg.Encode(SKEncodedImageFormat.Png, 100);
                    var diagPath = Path.Combine(AppContext.BaseDirectory, "diag_frame.png");
                    using var fs = File.Create(diagPath);
                    diag.SaveTo(fs);
                    Logger.Info("DLS diag frame saved to {0}", diagPath);
                }

                using var jpeg = rotImg.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                var jpegBytes = jpeg.ToArray();
                foreach (uint handle in _attachedHandles.ToArray())
                    SendJpegDls(jpegBytes);
            }
            else
            {
                // MSDisplay/AIC: raw BGR pixel data (24bpp), rotation handled by hardware
                using var bmp = SKBitmap.FromImage(srcImg);

                // Save diagnostic frame
                if (_frameCount == 1)
                {
                    using var diag = srcImg.Encode(SKEncodedImageFormat.Png, 100);
                    var diagPath = Path.Combine(AppContext.BaseDirectory, "diag_frame.png");
                    using var fs = File.Create(diagPath);
                    diag.SaveTo(fs);
                    Logger.Info("Diagnostic frame saved to {0} — {1}x{2}", diagPath, outW, outH);
                }
                int outStride = outW * 3;
                byte[] pixelData = new byte[outH * outStride];
                IntPtr px = bmp.GetPixels();
                int srcRow = bmp.RowBytes;
                unsafe
                {
                    byte* src = (byte*)px.ToPointer();
                    fixed (byte* dst = pixelData)
                    {
                        for (int y = 0; y < outH; y++)
                        {
                            byte* s = src + y * srcRow;
                            byte* d = dst + y * outStride;
                            byte* sEnd = s + outW * 4;
                            while (s < sEnd)
                            {
                                *d++ = *s++; // B
                                *d++ = *s++; // G
                                *d++ = *s++; // R
                                s++;        // skip A
                            }
                        }
                    }
                }
                foreach (uint handle in _attachedHandles.ToArray())
                    SendPixelDataToDevice(handle, pixelData, outW, outH);
            }
        }
        catch (Exception ex) { Logger.Error(ex, "SendFrame failed"); }
    }

    private void SendPixelDataToDevice(uint handle, byte[] pixelData, int w, int h)
    {
        GCHandle gch = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            var pic = new DeviceApi.MSDisplayPicture { Width = w, Height = h, Data = gch.AddrOfPinnedObject() };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<DeviceApi.MSDisplayPicture>());
            try
            {
                Marshal.StructureToPtr(pic, ptr, false);
                int rc = DeviceApi.MSDisplaySendPicture(handle, ptr, false);
                if (rc < 0)
                {
                    var aicPic = new AicDeviceApi.AICDispPicture { Width = w, Height = h, Data = gch.AddrOfPinnedObject() };
                    Marshal.StructureToPtr(aicPic, ptr, false);
                    AicDeviceApi.AICDispSendPicture(handle, ptr);
                }
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        finally { gch.Free(); }
    }

    private void SendJpegDls(byte[] jpegData)
    {
        if (_dlsSerialPort != null && _dlsSerialPort.IsOpen)
        {
            try { _dlsSerialPort.BaseStream.Write(jpegData, 0, jpegData.Length); _dlsSerialPort.BaseStream.Flush(); }
            catch (Exception ex) { Logger.Error(ex, "DLS write failed"); }
        }
    }

    // ── Device Controls ──

    public int GetBrightness(uint h) { try { return DeviceApi.MSDisplayGetBrightness(h); } catch { return _currentBrightness; } }
    public int SetBrightness(uint h, int v)
    {
        _currentBrightness = Math.Clamp(v, 0, 100);
        try { return DeviceApi.MSDisplaySetBrightness(h, v); } catch { return 0; }
    }
    public int GetVolume(uint h) { try { return DeviceApi.MSDisplayGetVolume(h); } catch { return _currentVolume; } }
    public int SetVolume(uint h, int v)
    {
        _currentVolume = Math.Clamp(v, 0, 100);
        try { return DeviceApi.MSDisplaySetVolume(h, v); } catch { return 0; }
    }
    public int SetRotation(uint h, int v)
    {
        _currentRotation = v % 360;
        try { return DeviceApi.MSDisplaySetRotation(h, v); } catch { return 0; }
    }
    public int GetRotation(uint h) { try { return DeviceApi.MSDisplayGetRotation(h); } catch { return _currentRotation; } }

    public string? ReadSerial(uint h)
    {
        try
        {
            var p = Marshal.AllocHGlobal(256);
            try { return DeviceApi.MSDisplayReadSN(h, p) >= 0 ? Marshal.PtrToStringAnsi(p) : null; }
            finally { Marshal.FreeHGlobal(p); }
        }
        catch { return null; }
    }

    public string? GetFirmware(uint h)
    {
        try
        {
            var p = Marshal.AllocHGlobal(64);
            try { return DeviceApi.MSDisplayGetFirmwareVer(h, p) >= 0 ? Marshal.PtrToStringAnsi(p) : null; }
            finally { Marshal.FreeHGlobal(p); }
        }
        catch { return null; }
    }

    public int SetResolution(uint h, ResolutionInfo res)
    {
        try
        {
            var r = new DeviceApi.MSDisplayResolution { Width = res.Width, Height = res.Height, Refresh = res.RefreshRate };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<DeviceApi.MSDisplayResolution>());
            try
            {
                Marshal.StructureToPtr(r, ptr, false);
                return DeviceApi.MSDisplaySetVideoParam(h, ptr);
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { return -1; }
    }

    public void Dispose() => Stop();
}

public record DeviceInfo(uint Handle, string SerialNumber, string Model, string FirmwareVersion,
    ResolutionInfo CurrentResolution, IReadOnlyList<ResolutionInfo> SupportedResolutions,
    int Brightness, int Volume, int Rotation, bool IsAttached);

public record struct ResolutionInfo(int Width, int Height, int RefreshRate)
{
    public readonly bool IsValid => Width > 0 && Height > 0 && RefreshRate > 0;
    public override readonly string ToString() => $"{Width}\u00d7{Height}@{RefreshRate}Hz";
}

