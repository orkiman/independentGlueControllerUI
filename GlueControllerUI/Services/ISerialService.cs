using GlueControllerUI.Models;

namespace GlueControllerUI.Services;

public interface ISerialService
{
    event EventHandler<string>? MessageReceived;
    event EventHandler<bool>? ConnectionChanged;
    event EventHandler<string>? ErrorOccurred;

    bool IsConnected { get; }
    string? CurrentPort { get; }

    string[] GetAvailablePorts();
    bool Connect(string portName, int baudRate = 115200);
    void Disconnect();

    void SendConfig(ControllerConfig config);
    void SendCalibrate(int pageLength);
    void SendTest(int gun, bool on);
    void SendHeartbeat();
}
