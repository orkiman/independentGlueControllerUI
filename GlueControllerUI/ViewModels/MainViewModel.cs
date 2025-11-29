using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlueControllerUI.Models;
using GlueControllerUI.Services;

namespace GlueControllerUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SerialService _serialService;
    private readonly ProfileService _profileService;

    [ObservableProperty]
    private ControllerConfig _config = new();

    [ObservableProperty]
    private string _selectedPage = "Connection";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Connection
    [ObservableProperty]
    private string[] _availablePorts = [];

    [ObservableProperty]
    private string? _selectedPort;

    // Calibration
    [ObservableProperty]
    private int _calibrationPageLength = 1000;

    [ObservableProperty]
    private bool _isCalibrating;

    [ObservableProperty]
    private string _calibrationResult = "";

    // Test mode
    [ObservableProperty]
    private bool _testGun1;

    [ObservableProperty]
    private bool _testGun2;

    [ObservableProperty]
    private bool _testGun3;

    [ObservableProperty]
    private bool _testGun4;

    // Profiles
    [ObservableProperty]
    private List<Profile> _profiles = [];

    [ObservableProperty]
    private string _newProfileName = "";

    public MainViewModel()
    {
        _serialService = new SerialService();
        _profileService = new ProfileService();

        _serialService.ConnectionChanged += OnConnectionChanged;
        _serialService.MessageReceived += OnMessageReceived;
        _serialService.ErrorOccurred += OnErrorOccurred;

        RefreshPorts();
        LoadProfiles();
    }

    private void OnConnectionChanged(object? sender, bool connected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = connected;
            ConnectionStatus = connected ? $"Connected: {_serialService.CurrentPort}" : "Disconnected";
            StatusMessage = connected ? "Connected to controller" : "Disconnected";
        });
    }

    private void OnMessageReceived(object? sender, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (message.Contains("calibration_result"))
                {
                    var json = System.Text.Json.JsonDocument.Parse(message);
                    var pulsesPerPage = json.RootElement.GetProperty("pulsesPerPage").GetInt32();
                    var pulsesPerMm = pulsesPerPage / (CalibrationPageLength * 10.0);
                    
                    CalibrationResult = $"Pulses per page: {pulsesPerPage}\nCalculated: {pulsesPerMm:F2} pulses/mm";
                    Config.EncoderPulsesPerMm = pulsesPerMm;
                    IsCalibrating = false;
                    StatusMessage = "Calibration complete";
                }
            }
            catch
            {
                StatusMessage = $"Received: {message}";
            }
        });
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = error;
        });
    }

    // === Connection Commands ===

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts = _serialService.GetAvailablePorts();
        if (AvailablePorts.Length > 0 && SelectedPort == null)
            SelectedPort = AvailablePorts[0];
    }

    [RelayCommand]
    private void Connect()
    {
        if (string.IsNullOrEmpty(SelectedPort))
        {
            StatusMessage = "Please select a port";
            return;
        }

        if (_serialService.Connect(SelectedPort))
        {
            StatusMessage = $"Connected to {SelectedPort}";
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _serialService.Disconnect();
    }

    // === Config Commands ===

    [RelayCommand]
    private void SendConfig()
    {
        if (!IsConnected)
        {
            StatusMessage = "Not connected";
            return;
        }

        _serialService.SendConfig(Config);
        StatusMessage = "Configuration sent to controller";
    }

    // === Calibration Commands ===

    [RelayCommand]
    private void StartCalibration()
    {
        if (!IsConnected)
        {
            StatusMessage = "Not connected";
            return;
        }

        IsCalibrating = true;
        CalibrationResult = "Waiting for sensor trigger...";
        _serialService.SendCalibrate(CalibrationPageLength);
        StatusMessage = "Calibration started - pass page through sensor";
    }

    [RelayCommand]
    private void ApplyCalibration()
    {
        OnPropertyChanged(nameof(Config));
        StatusMessage = "Calibration applied to configuration";
    }

    // === Test Commands ===

    partial void OnTestGun1Changed(bool value) => SendTestCommand(1, value);
    partial void OnTestGun2Changed(bool value) => SendTestCommand(2, value);
    partial void OnTestGun3Changed(bool value) => SendTestCommand(3, value);
    partial void OnTestGun4Changed(bool value) => SendTestCommand(4, value);

    private void SendTestCommand(int gun, bool on)
    {
        if (!IsConnected) return;
        _serialService.SendTest(gun, on);
    }

    [RelayCommand]
    private void TestAllOn()
    {
        TestGun1 = TestGun2 = TestGun3 = TestGun4 = true;
    }

    [RelayCommand]
    private void TestAllOff()
    {
        TestGun1 = TestGun2 = TestGun3 = TestGun4 = false;
    }

    // === Gun Zone Commands ===

    [RelayCommand]
    private void AddZone(GunConfig gun)
    {
        gun.Rows.Add(new GlueZone(0, 100, 0));
    }

    [RelayCommand]
    private void RemoveZone(object? parameter)
    {
        if (parameter is not object[] args || args.Length != 2) return;
        if (args[0] is GunConfig gun && args[1] is GlueZone zone)
        {
            gun.Rows.Remove(zone);
        }
    }

    // === Profile Commands ===

    private void LoadProfiles()
    {
        Profiles = _profileService.GetAllProfiles();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            StatusMessage = "Please enter a profile name";
            return;
        }

        var profile = new Profile
        {
            Name = NewProfileName,
            CreatedAt = DateTime.Now,
            Config = Config.Clone()
        };

        _profileService.SaveProfile(profile);
        LoadProfiles();
        NewProfileName = "";
        StatusMessage = $"Profile '{profile.Name}' saved";
    }

    [RelayCommand]
    private void LoadProfile(Profile profile)
    {
        Config = profile.Config.Clone();
        StatusMessage = $"Profile '{profile.Name}' loaded";
    }

    [RelayCommand]
    private void DeleteProfile(Profile profile)
    {
        _profileService.DeleteProfile(profile.Name);
        LoadProfiles();
        StatusMessage = $"Profile '{profile.Name}' deleted";
    }

    [RelayCommand]
    private void ExportProfile(Profile profile)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = profile.Name,
            DefaultExt = ".json",
            Filter = "JSON files (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            _profileService.ExportProfile(profile, dialog.FileName);
            StatusMessage = $"Profile exported to {dialog.FileName}";
        }
    }

    [RelayCommand]
    private void ImportProfile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = ".json",
            Filter = "JSON files (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var profile = _profileService.ImportProfile(dialog.FileName);
            if (profile != null)
            {
                _profileService.SaveProfile(profile);
                LoadProfiles();
                StatusMessage = $"Profile '{profile.Name}' imported";
            }
            else
            {
                StatusMessage = "Failed to import profile";
            }
        }
    }

    // === Navigation ===

    [RelayCommand]
    private void NavigateTo(string page)
    {
        SelectedPage = page;
    }
}
