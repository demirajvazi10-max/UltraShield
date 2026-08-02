using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UltraShield.Services
{
    public class AppSettings
    {
        public string VirusTotalApiKey { get; set; } = "";
    }

    /// <summary>
    /// Reads/writes settings.json under %LocalAppData%\UltraShield\.
    /// Kept deliberately simple (plain JSON, no encryption) for the skeleton -
    /// worth revisiting with DPAPI protection for the API key before a real release,
    /// since anyone with local file access could otherwise read it.
    /// </summary>
    public class AppSettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltraShield", "settings.json");

        public async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = await File.ReadAllTextAsync(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public async Task SaveAsync(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
        }
    }
}
