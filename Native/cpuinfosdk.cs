using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

namespace SoeyiWinUI_v2.Native;

/// <summary>
/// CPU identification and monitoring using cpuid instruction.
/// Replaces the obfuscated CPUIDSDK from original SOEYI.
/// </summary>
public static class CpuInfoSdk
{
    private const string DllName = "cpuidsdk.dll";

    // ── cpuidsdk.dll exports (verified from SOEYI binary) ──
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int cpuidsdk_Init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int cpuidsdk_GetCpuTemperature(int coreIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int cpuidsdk_GetCpuClock();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void cpuidsdk_Close();

    private static bool _initialized;
    private static string? _cpuBrand;
    private static int _coreCount;
    private static int _threadCount;

    /// <summary>
    /// CPU brand string (e.g. "Intel(R) Core(TM) i7-13700K")
    /// </summary>
    public static string Brand
    {
        get
        {
            if (_cpuBrand == null) ReadCpuBrand();
            return _cpuBrand ?? "Unknown CPU";
        }
    }

    /// <summary>
    /// Physical core count.
    /// </summary>
    public static int CoreCount
    {
        get
        {
            if (_coreCount == 0) ReadCpuTopology();
            return _coreCount > 0 ? _coreCount : Environment.ProcessorCount / 2;
        }
    }

    /// <summary>
    /// Logical thread count.
    /// </summary>
    public static int ThreadCount
    {
        get
        {
            if (_threadCount == 0) ReadCpuTopology();
            return _threadCount > 0 ? _threadCount : Environment.ProcessorCount;
        }
    }

    /// <summary>
    /// Initialize cpuidsdk native library.
    /// </summary>
    public static bool Initialize()
    {
        if (_initialized) return true;
        try { _initialized = cpuidsdk_Init() == 0; } catch { }
        return _initialized;
    }

    /// <summary>
    /// Get CPU temperature for a specific core.
    /// </summary>
    public static float GetTemperature(int coreIndex = 0)
    {
        if (!_initialized) Initialize();
        try { return cpuidsdk_GetCpuTemperature(coreIndex); }
        catch { return 0f; }
    }

    /// <summary>
    /// Get current CPU clock speed in MHz.
    /// </summary>
    public static int GetClockSpeed()
    {
        if (!_initialized) Initialize();
        try { return cpuidsdk_GetCpuClock(); }
        catch { return 0; }
    }

    /// <summary>
    /// Cleanup native resources.
    /// </summary>
    public static void Shutdown()
    {
        if (!_initialized) return;
        try { cpuidsdk_Close(); } catch { }
        _initialized = false;
    }

    /// <summary>
    /// Detect supported CPU features (SSE, AVX, AVX2, etc.)
    /// </summary>
    public static CpuFeatures GetFeatures()
    {
        return new CpuFeatures
        {
            Sse = Sse.IsSupported,
            Sse2 = Sse2.IsSupported,
            Sse3 = Sse3.IsSupported,
            Ssse3 = Ssse3.IsSupported,
            Sse41 = Sse41.IsSupported,
            Sse42 = Sse42.IsSupported,
            Avx = Avx.IsSupported,
            Avx2 = Avx2.IsSupported,
            Avx512F = Avx512F.IsSupported,
            Aes = Aes.IsSupported,
            Bmi1 = Bmi1.IsSupported,
            Bmi2 = Bmi2.IsSupported,
            Fma = Fma.IsSupported,
            Lzcnt = Lzcnt.IsSupported,
            Popcnt = Popcnt.IsSupported,
        };
    }

    private static void ReadCpuBrand()
    {
        if (!X86Base.IsSupported) { _cpuBrand = "Unknown (non-x86)"; return; }
        Span<int> regs = stackalloc int[12];
        unsafe
        {
            fixed (int* p = regs)
            {
                for (int i = 0; i < 3; i++)
                {
                    var res = X86Base.CpuId((int)(0x80000002 + (uint)i), 0);
                    regs[i * 4] = (int)res.Eax;
                    regs[i * 4 + 1] = (int)res.Ebx;
                    regs[i * 4 + 2] = (int)res.Ecx;
                    regs[i * 4 + 3] = (int)res.Edx;
                }
            }
        }
        string? brand = null;
        unsafe
        {
            brand = Marshal.PtrToStringAnsi((IntPtr)Unsafe.AsPointer(ref regs[0]), 48)?.Trim();
        }
        _cpuBrand = brand ?? "Unknown";
    }

    private static void ReadCpuTopology()
    {
        if (!X86Base.IsSupported) return;
        // Leaf 0xB: Extended Topology Enumeration
        var r = X86Base.CpuId(0xB, 0);
        _threadCount = (int)(r.Ebx & 0xFFFF);
        r = X86Base.CpuId(0xB, 1);
        _coreCount = (int)(r.Ebx & 0xFFFF);
    }
}

public record struct CpuFeatures
{
    public bool Sse, Sse2, Sse3, Ssse3, Sse41, Sse42;
    public bool Avx, Avx2, Avx512F;
    public bool Aes, Bmi1, Bmi2, Fma, Lzcnt, Popcnt;
}

