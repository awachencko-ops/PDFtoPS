using System.Text.Json;

namespace PDFtoPS.Tests;

public class AppConfigurationTests
{
    [Fact]
    public void Load_ReturnsFallback_WhenFileMissing()
    {
        AppConfiguration cfg = AppConfiguration.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        Assert.True(cfg.MaxParallelConversions >= 1);
        Assert.NotEmpty(cfg.Profiles);
    }

    [Fact]
    public void Load_ClampsParallelism_AndRestoresProfiles()
    {
        string temp = Path.GetTempFileName();
        try
        {
            var payload = new
            {
                maxParallelConversions = 0,
                profiles = new Dictionary<string, string>()
            };
            File.WriteAllText(temp, JsonSerializer.Serialize(payload));

            AppConfiguration cfg = AppConfiguration.Load(temp);

            Assert.Equal(1, cfg.MaxParallelConversions);
            Assert.NotEmpty(cfg.Profiles);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
