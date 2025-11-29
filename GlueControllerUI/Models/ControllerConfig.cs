using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GlueControllerUI.Models;

public partial class ControllerConfig : ObservableObject
{
    [ObservableProperty]
    private string _controllerType = "dots";  // "dots" | "lines"

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private double _encoderPulsesPerMm = 1.0;

    [ObservableProperty]
    private int _sensorOffset = 10;           // mm

    [ObservableProperty]
    private string _dotSize = "medium";       // small | medium | large

    [ObservableProperty]
    private double _startCurrent = 1.0;       // A

    [ObservableProperty]
    private double _startDuration = 500;      // ms

    [ObservableProperty]
    private double _holdCurrent = 0.5;        // A

    [ObservableProperty]
    private double _minimumSpeed = 0.0;       // mm/s (0 = disabled)

    public ObservableCollection<GunConfig> Guns { get; set; } = [];

    public ControllerConfig()
    {
        // Initialize 4 guns
        for (int i = 0; i < 4; i++)
            Guns.Add(new GunConfig(i));
    }

    public ControllerConfig Clone()
    {
        var clone = new ControllerConfig
        {
            ControllerType = ControllerType,
            Enabled = Enabled,
            EncoderPulsesPerMm = EncoderPulsesPerMm,
            SensorOffset = SensorOffset,
            DotSize = DotSize,
            StartCurrent = StartCurrent,
            StartDuration = StartDuration,
            HoldCurrent = HoldCurrent,
            MinimumSpeed = MinimumSpeed
        };
        clone.Guns.Clear();
        foreach (var gun in Guns)
            clone.Guns.Add(gun.Clone());
        return clone;
    }
}
