using System.Runtime.InteropServices;

namespace SoeyiWinUI_v2.Native;

/// <summary>
/// P/Invoke wrapper for AicUsbDisplay.dll (secondary USB display chipset support).
/// </summary>
public static class AicDeviceApi
{
    private const string DllName = "AicUsbDisplay.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AICDispSendPicture(uint handle, IntPtr pic);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AICDispStart();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AICDispStop();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void AICDispRegisterCallback(IntPtr attachFunc, IntPtr detachFunc);

    // ── Callback Delegates ──
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AttachProcHandler(uint handle, IntPtr resolution, int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DetachProcHandler(uint handle, IntPtr resolution);

    // ── Structures ──
    [StructLayout(LayoutKind.Sequential)]
    public struct AICDispResolution
    {
        public int Width;
        public int Height;
        public int Refresh;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AICDispPicture
    {
        public int Width;
        public int Height;
        public IntPtr Data;
    }
}

