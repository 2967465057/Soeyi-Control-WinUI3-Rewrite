using Grpc.Core;
using NLog;
using FanControl.IPC;

namespace SoeyiWinUI_v2.Services;

/// <summary>
/// Reads sensor data from Fan Control via its gRPC named-pipe API.
/// Uses IPCFactory.GetSensorClient() which connects to \\.\pipe\FanControl.
/// Fan Control must be running.
/// </summary>
public class FanControlSensorReader : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private SensorsRPC.SensorsRPCClient? _client;
    private bool _available;
    private float _cpuTemp, _cpuUsage, _gpuTemp, _gpuUsage;

    public bool Available => _available;
    public float CpuTemperature => _cpuTemp;
    public float CpuUsage => _cpuUsage;
    public float GpuTemperature => _gpuTemp;
    public float GpuUsage => _gpuUsage;

    public bool Connect()
    {
        try
        {
            _client = IPCFactory.GetSensorClient();
            var reply = _client.GetAllSensors(new GetAllSensorsRequest(), new CallOptions());
            _available = reply?.Sensors?.Count > 0;
            Logger.Info("FanControl IPC connected ({0} sensors)", reply?.Sensors?.Count ?? 0);
            return _available;
        }
        catch (Exception ex)
        {
            Logger.Info("FanControl IPC unavailable: {0}", ex.Message);
            _available = false;
            return false;
        }
    }

    /// <summary>Poll all sensors from FanControl, update cached CPU/GPU values.</summary>
    public void Poll()
    {
        if (!_available || _client == null) return;
        try
        {
            var reply = _client.GetAllSensors(new GetAllSensorsRequest(), new CallOptions());
            if (reply?.Sensors == null) return;

            foreach (var s in reply.Sensors)
            {
                if (!s.HasValue) continue;
                var v = s.Value;
                var name = s.Name ?? "";

                switch (s.Type)
                {
                    case SensorMessageType.Temperature:
                        if (name.Contains("Package") || (name.Contains("Core") && name.Contains("CPU")))
                            _cpuTemp = v;
                        else if (name.Contains("GPU") || name.Contains("Hot Spot"))
                            _gpuTemp = v;
                        break;
                    case SensorMessageType.UsagePercent:
                        if (name.Contains("CPU")) _cpuUsage = v;
                        else if (name.Contains("GPU")) _gpuUsage = v;
                        break;
                }
            }
        }
        catch { /* FanControl may have disconnected */ }
    }

    public void Dispose() { _available = false; _client = null; }
}
