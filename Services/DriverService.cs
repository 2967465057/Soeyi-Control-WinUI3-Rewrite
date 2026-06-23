using System.Diagnostics;
using System.Runtime.InteropServices;
using NLog;

namespace SoeyiWinUI_v2.Services;

/// <summary>
/// Checks and installs the libusb0 kernel driver required for USB secondary display.
/// </summary>
public static class DriverService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string DriverSysPath = @"C:\Windows\System32\drivers\libusb0.sys";
    private const string DeviceId = @"USB\VID_33C3&PID_F101&MI_02";

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
    /// Installs the libusb0 driver using DPInst64.exe.
    /// Launches UAC prompt if not already elevated.
    /// Returns true if installation succeeded.
    /// </summary>
    public static bool InstallDriver()
    {
        if (IsDriverInstalled())
        {
            Logger.Info("Driver already installed, skipping");
            return true;
        }

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
                Verb = "runas", // triggers UAC elevation
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                Logger.Error("Driver install: failed to start process (UAC declined?)");
                return false;
            }

            proc.WaitForExit(120_000); // 2 min timeout
            int exitCode = proc.ExitCode;
            Logger.Info("Driver install: DPInst64 exit code = {0}", exitCode);

            // DPInst returns 0 on success, 0x40000000+ on reboot needed
            bool ok = exitCode == 0 || (exitCode & 0x40000000) != 0;

            if (ok)
            {
                // Verify driver file was actually copied
                Thread.Sleep(500);
                bool installed = IsDriverInstalled();
                Logger.Info("Driver install: verification = {0}", installed);
                return installed;
            }

            Logger.Error("Driver install: DPInst64 failed (exit {0})", exitCode);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Driver install: exception");
            return false;
        }
    }

    /// <summary>
    /// Returns the directory containing libusb driver files.
    /// </summary>
    private static string? FindLibusbDir()
    {
        var baseDir = AppContext.BaseDirectory;

        // Try: appDir\libusb
        var dir = Path.Combine(baseDir, "libusb");
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        // Try: appDir\..\libusb (for dotnet run from project dir)
        dir = Path.GetFullPath(Path.Combine(baseDir, "..", "libusb"));
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        // Try: appDir\..\..\..\..\libusb (for Debug/netX/win-x64 nesting)
        dir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "libusb"));
        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "DPInst64.exe")))
            return dir;

        return null;
    }
}
