using System.IO.Ports;
using System.Text;
using System.Text.Json;
using GlueControllerUI.Models;

namespace GlueControllerUI.Services;

public class SerialService : ISerialService, IDisposable
{
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();
    private const byte STX = 0x02;
    private const byte ETX = 0x03;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<bool>? ConnectionChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsConnected => _port?.IsOpen ?? false;
    public string? CurrentPort => _port?.PortName;

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    public bool Connect(string portName, int baudRate = 115200)
    {
        try
        {
            Disconnect();

            _port = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true
            };

            _port.DataReceived += OnDataReceived;
            _port.ErrorReceived += OnErrorReceived;
            _port.Open();

            ConnectionChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        if (_port != null)
        {
            try
            {
                if (_port.IsOpen)
                    _port.Close();

                _port.DataReceived -= OnDataReceived;
                _port.ErrorReceived -= OnErrorReceived;
                _port.Dispose();
            }
            catch { }
            finally
            {
                _port = null;
                ConnectionChanged?.Invoke(this, false);
            }
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_port == null || !_port.IsOpen) return;

            string data = _port.ReadExisting();
            foreach (char c in data)
            {
                if (c == (char)STX)
                {
                    _buffer.Clear();
                }
                else if (c == (char)ETX)
                {
                    string message = _buffer.ToString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                    _buffer.Clear();
                }
                else
                {
                    _buffer.Append(c);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Read error: {ex.Message}");
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        ErrorOccurred?.Invoke(this, $"Serial error: {e.EventType}");
    }

    private void Send(object message)
    {
        if (_port == null || !_port.IsOpen)
        {
            ErrorOccurred?.Invoke(this, "Not connected");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var data = new byte[] { STX }
                .Concat(Encoding.ASCII.GetBytes(json))
                .Concat(new byte[] { ETX })
                .ToArray();

            _port.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Send error: {ex.Message}");
        }
    }

    public void SendConfig(ControllerConfig config)
    {
        var payload = new
        {
            type = "controller_setup",
            controllerType = config.ControllerType,
            enabled = config.Enabled,
            encoder = config.EncoderPulsesPerMm,
            sensorOffset = config.SensorOffset,
            dotSize = config.DotSize,
            startCurrent = config.StartCurrent,
            startDuration = config.StartDuration,
            holdCurrent = config.HoldCurrent,
            minimumSpeed = config.MinimumSpeed,
            guns = config.Guns.Select(g => new
            {
                gunId = g.GunId,
                enabled = g.Enabled,
                rows = g.Rows.Select(r => new
                {
                    from = r.From,
                    to = r.To,
                    space = r.Space
                })
            })
        };
        Send(payload);
    }

    public void SendCalibrate(int pageLength)
    {
        Send(new { type = "calibrate", pageLength });
    }

    public void SendTest(int gun, bool on)
    {
        Send(new { type = "test", gun, state = on ? "on" : "off" });
    }

    public void SendHeartbeat()
    {
        Send(new { type = "heartbeat" });
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
