namespace GlueControllerUI.Models;

public class Profile
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ControllerConfig Config { get; set; } = new();
}
