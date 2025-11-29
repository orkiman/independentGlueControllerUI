using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GlueControllerUI.Models;

public partial class GunConfig : ObservableObject
{
    [ObservableProperty]
    private int _gunId;      // 0-3

    [ObservableProperty]
    private bool _enabled = true;

    public ObservableCollection<GlueZone> Rows { get; set; } = [];

    public GunConfig() { }

    public GunConfig(int gunId)
    {
        GunId = gunId;
    }

    public GunConfig Clone()
    {
        var clone = new GunConfig(GunId) { Enabled = Enabled };
        foreach (var row in Rows)
            clone.Rows.Add(row.Clone());
        return clone;
    }
}
