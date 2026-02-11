using System.Text.Json;

namespace PDFtoPS;

internal sealed class AppConfiguration
{
    public int MaxParallelConversions { get; set; } = 2;

    public Dictionary<string, string> Profiles { get; set; } = new()
    {
        { "Standard (PS Level 3)", "-sDEVICE=ps2write -dLanguageLevel=3" },
        { "Legacy (PS Level 2)", "-sDEVICE=ps2write -dLanguageLevel=2" },
        { "Grayscale", "-sDEVICE=ps2write -dLanguageLevel=3" }
    };

    public static AppConfiguration Load(string? path = null)
    {
        string configPath = path ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        AppConfiguration fallback = new AppConfiguration();

        if (!File.Exists(configPath))
        {
            return fallback;
        }

        try
        {
            string json = File.ReadAllText(configPath);
            AppConfiguration? parsed = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                return fallback;
            }

            if (parsed.MaxParallelConversions < 1)
            {
                parsed.MaxParallelConversions = 1;
            }

            if (parsed.Profiles == null || parsed.Profiles.Count == 0)
            {
                parsed.Profiles = fallback.Profiles;
            }

            return parsed;
        }
        catch
        {
            return fallback;
        }
    }
}
