using CommunityToolkit.Mvvm.ComponentModel;

namespace GlueControllerUI.Models;

public partial class GlueZone : ObservableObject
{
    [ObservableProperty]
    private double _from;    // mm

    [ObservableProperty]
    private double _to;      // mm

    [ObservableProperty]
    private double _space;   // mm (0 = continuous)

    public GlueZone() { }

    public GlueZone(double from, double to, double space = 0)
    {
        From = from;
        To = to;
        Space = space;
    }

    public GlueZone Clone() => new(From, To, Space);
}
