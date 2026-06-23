using System.Diagnostics;
using System.Runtime.InteropServices;
using NLog;

namespace SoeyiWinUI_v2.Services;

/// <summary>
/// Checks and installs the libusb0 kernel driver required for USB secondary display.
/// Falls back to PnP device reset when driver catalog signature doesn't match.
/// </summary>
public static class DriverService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string DriverSysPath = @"C:\Windows\System32\drivers\libusb0.sys";

    /// <summary>
    /// Returns true if the libusb0 kernel driver is installed on this system.
    /// </summary>
    public static bool IsDriverInstalled()
    {
        if (File.Exists(DriverSysPath))
        {
            Logger.Info("Driver check: {0} exists", DriverSysPath);
            return true;
        }
        Logger.Info("Driver check: {0} missing", DriverSysPath);
        return false;
    }

    /// <summary>
    /// Ensures the USB display is ready. Tries DPInst first, then PnP device reset as fallback.
    /// Returns true if the device should be functional (driver installed OR device reset OK).
    /// This is non-fatal — DLS serial path works even without the libusb kernel driver.
    /// </summary>
    public static bool EnsureDeviceReady()
    {
        if (IsDriverInstalled())
        {
            Logger.Info("Driver already installed, skipping");
            return true;
        }

        // Try DPInst (may need UAC, may fail due to catalog mismatch)
        bool dpOk = TryInstallWithDpInst();

        if (dpOk && IsDriverInstalled())
        {
            Logger.Info("DPInst succeeded, driver installed");
            return true;
        }

        if (!dpOk)
            Logger.Info("DPInst failed — falling back to PnP device reset");

        // Fallback: reset USB serial device via PnP (fixes Error state after driver uninstall)
        bool pnpOk = ResetUsbSerialDevice();
        Logger.Info("PnP device reset: {0}", pnpOk ? "OK" : "failed");

        // Return true — DLS serial path doesn't need libusb kernel driver,
        // and PnP reset may have restored the COM port.
        // Let DeviceService decide if it can connect.
        return true;
    }

    private static bool TryInstallWithDpInst()
    {
        var libusbDir = FindLibusbDir();
        if (libusbDir == null)
        {
            Logger.Error("Driver install: libusb/ dir not found in app directory");
            return false;
        }

        var dpInst = Path.Combine(libusbDir, "DPInst64.exe");
        if (!File.Exists(dpInst))
        {
            Logger.Error("Driver install: DPInst64.exe not found at {0}", dpInst);
            return false;
        }

        Logger.Info("Driver install: running {0} /SW /PATH {1}", dpInst, libusbDir);

        try
        {
            var psi = new ProcessStartInfo(dpInst, $"/SW /PATH \"{libusbDir}\"")
            {
                WorkingDirectory = libusbDir,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                Logger.Error("Driver install: failed to start (UAC declined?)");
                return false;
            }

            proc.WaitForExit(120_000);
            int exitCode = proc.ExitCode;
            Logger.Info("Driver install: DPInst64 exit code = 0x{0:X}", exitCode);

            // DPInst returns 0 on success, 0x40000000+ on reboot needed
            return exitCode == 0 || (exitCode & 0x40000000) != 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Driver install: exception");
            return false;
        }
    }

    /// <summary>
    /// Resets the USB serial device (VID_33C3&PID_F101&MI_00) via PnP disable/enable cycle.
    /// Fixes "Error" state that occurs after original driver is uninstalled.
    /// </summary>
    private static bool ResetUsbSerialDevice()
    {
        try
        {
            // Find all child devices under VID_33C3&PID_F101
            var psi = new ProcessStartInfo("pnputil", "/enum-devices /class Ports /connected")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return false;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);

            // Find instance IDs matching our device
            var lines = output.Split('\n');
            List<string> deviceIds = new();
            foreach (var line in lines)
            {
                if (line.Contains("VID_33C3", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("VID_345F", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line,
                        @"USB\\VID_\w+&PID_\w+&MI_\w+\\.+?(?=\s*$|$)");
                    if (match.Success)
                        deviceIds.Add(match.Value.Trim());
                }
            }

            if (deviceIds.Count == 0)
            {
                Logger.Info("PnP reset: no matching USB serial devices found");
                return false;
            }

            foreach (var id in deviceIds)
            {
                Logger.Info("PnP reset: cycling {0}", id);
                // Disable
                var disable = new ProcessStartInfo("pnputil", $"/disable-device \"{id}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var dp = Process.Start(disable);
                dp?.WaitForExit(8000);

                Thread.Sleep(2000);

                // Enable
                var enable = new ProcessStartInfo("pnputil", $"/enable-device \"{id}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var ep = Process.Start(enable);
                ep?.WaitForExit(8000);

                Thread.Sleep(2000);
            }

            Logger.Info("PnP reset: cycled {0} device(s)", deviceIds.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "PnP reset: exception");
            return false;
        }
    }

    private static string? FindLibusbDir()
    {
        var baseDir = AppContext.BaseDirectory;

        var dir = Path.Combine(baseDir, "libusb");
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        dir = Path.GetFullPath(Path.Combine(baseDir, "..", "libusb"));
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        dir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "libusb"));
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        return null;
    }
}
