using System.Runtime.InteropServices;

namespace SoeyiWinUI_v2.Native;

/// <summary>
/// P/Invoke wrapper for MSDISPLAYSDKWRRAPER.dll (USB secondary display SDK).
/// All functions use Cdecl calling convention, matching the native C++ DLL.
/// </summary>
public static class DeviceApi
{
    private const string DllName = "MSDISPLAYSDKWRRAPER.dll";

    // ── Lifecycle ──
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Wrraper_MSDisplayStart(int logLevel, IntPtr edid);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Wrraper_MSDisplayStop();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Wrraper_MSDisplayRegisterCallback(IntPtr attachFunc, IntPtr detachFunc);

    // ── Device I/O ──
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayReadSN")]
    public static extern int MSDisplayReadSN(uint handle, IntPtr data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplaySetVideoParam")]
    public static extern int MSDisplaySetVideoParam(uint handle, IntPtr resolution);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplaySendPicture")]
    public static extern int MSDisplaySendPicture(uint handle, IntPtr pic, bool b);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayReadFlash")]
    public static extern int MSDisplayReadFlash(uint handle, uint addr, int len, IntPtr data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayReadXdata")]
    public static extern int MSDisplayReadXdata(uint handle, uint addr, int len, IntPtr data);

    // ── Display Settings ──
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayGetBrightness")]
    public static extern int MSDisplayGetBrightness(uint handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplaySetBrightness")]
    public static extern int MSDisplaySetBrightness(uint handle, int brightness);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayGetVolume")]
    public static extern int MSDisplayGetVolume(uint handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplaySetVolume")]
    public static extern int MSDisplaySetVolume(uint handle, int volume);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplaySetRotation")]
    public static extern int MSDisplaySetRotation(uint handle, int rotation);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayGetRotation")]
    public static extern int MSDisplayGetRotation(uint handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayReadEDID")]
    public static extern int MSDisplayReadEDID(uint handle, IntPtr edid);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayWriteEDID")]
    public static extern int MSDisplayWriteEDID(uint handle, IntPtr edid);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayGetFirmwareVer")]
    public static extern int MSDisplayGetFirmwareVer(uint handle, IntPtr version);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Wrraper_MSDisplayUpdateFirmware")]
    public static extern int MSDisplayUpdateFirmware(uint handle, IntPtr data, int len);

    // ── Callback Delegates ──
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AttachProcHandler(uint handle, IntPtr resolution, int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DetachProcHandler(uint handle);

    // ── Structures ──
    [StructLayout(LayoutKind.Sequential)]
    public struct MSDisplayPicture
    {
        public int Width;
        public int Height;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSDisplayResolution
    {
        public int Width;
        public int Height;
        public int Refresh;

        public readonly bool IsValid => Width > 0 && Height > 0 && Refresh > 0;

        public override readonly string ToString() => $"{Width}x{Height}@{Refresh}Hz";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSDisplayEdid
    {
        public int Mode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string EdidReplace;

        public int AppendCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public MSDisplayTiming[] TimingAppend;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSDisplayTiming
    {
        public uint Vic;
        public uint Polarity;
        public uint HTotal;
        public uint VTotal;
        public uint HActive;
        public uint VActive;
        public uint PixClk;
        public uint VFreq;
        public uint HOffset;
        public uint VOffset;
        public uint HSyncWidth;
        public uint VSyncWidth;
    }

    // ── Predefined Display Timings (from decompiled SOEYI) ──
    public static readonly SortedDictionary<int, MSDisplayTiming> KnownTimings = new()
    {
        [143] = new() { Vic = 143, Polarity = 7, HTotal = 600, VTotal = 490, HActive = 480, VActive = 480,
                        PixClk = 1764, VFreq = 6000, HOffset = 100, VOffset = 8, HSyncWidth = 50, VSyncWidth = 4 },
        [147] = new() { Vic = 147, Polarity = 7, HTotal = 452, VTotal = 568, HActive = 240, VActive = 320,
                        PixClk = 1540, VFreq = 6000, HOffset = 190, VOffset = 206, HSyncWidth = 5, VSyncWidth = 20 },
        [148] = new() { Vic = 148, Polarity = 7, HTotal = 568, VTotal = 452, HActive = 320, VActive = 240,
                        PixClk = 1540, VFreq = 6000, HOffset = 206, VOffset = 190, HSyncWidth = 20, VSyncWidth = 5 },
        [156] = new() { Vic = 156, Polarity = 7, HTotal = 640, VTotal = 352, HActive = 240, VActive = 240,
                        PixClk = 1350, VFreq = 6000, HOffset = 190, VOffset = 51, HSyncWidth = 10, VSyncWidth = 10 },
        [159] = new() { Vic = 159, Polarity = 7, HTotal = 600, VTotal = 375, HActive = 480, VActive = 272,
                        PixClk = 1350, VFreq = 6000, HOffset = 100, VOffset = 90, HSyncWidth = 50, VSyncWidth = 5 },
        [160] = new() { Vic = 160, Polarity = 7, HTotal = 472, VTotal = 996, HActive = 360, VActive = 960,
                        PixClk = 2820, VFreq = 6000, HOffset = 88, VOffset = 33, HSyncWidth = 32, VSyncWidth = 10 },
        [171] = new() { Vic = 171, Polarity = 7, HTotal = 488, VTotal = 996, HActive = 376, VActive = 960,
                        PixClk = 2916, VFreq = 6000, HOffset = 312, VOffset = 66, HSyncWidth = 112, VSyncWidth = 10 },
    };
}

