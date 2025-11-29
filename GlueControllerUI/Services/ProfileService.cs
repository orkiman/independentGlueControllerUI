using System.IO;
using System.Text.Json;
using GlueControllerUI.Models;

namespace GlueControllerUI.Services;

public class ProfileService
{
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfileService()
    {
        _profilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GlueControllerUI",
            "Profiles"
        );

        Directory.CreateDirectory(_profilesDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public List<Profile> GetAllProfiles()
    {
        var profiles = new List<Profile>();

        foreach (var file in Directory.GetFiles(_profilesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch
            {
                // Skip invalid files
            }
        }

        return profiles.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public void SaveProfile(Profile profile)
    {
        var fileName = SanitizeFileName(profile.Name) + ".json";
        var filePath = Path.Combine(_profilesDirectory, fileName);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public void DeleteProfile(string name)
    {
        var fileName = SanitizeFileName(name) + ".json";
        var filePath = Path.Combine(_profilesDirectory, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void ExportProfile(Profile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public Profile? ImportProfile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
