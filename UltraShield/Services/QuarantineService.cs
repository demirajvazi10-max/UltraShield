using System.IO;

namespace UltraShield.Services
{
    public class QuarantinedFile
    {
        public string OriginalPath { get; set; } = "";
        public string QuarantinePath { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime QuarantinedAtUtc { get; set; }
    }

    /// <summary>
    /// Moves a flagged file out of harm's way instead of deleting it outright -
    /// deleting is a one-way door, and a false positive (or a file the user
    /// needs for evidence/reporting) shouldn't be unrecoverable. Files land in
    /// %LocalAppData%\UltraShield\Quarantine, renamed with a GUID so two files
    /// with the same name from different folders never collide, with a log
    /// (quarantine-log.json) mapping back to the original path.
    /// </summary>
    public class QuarantineService
    {
        private static readonly string QuarantineDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltraShield", "Quarantine");

        private static readonly string LogPath = Path.Combine(QuarantineDir, "quarantine-log.json");

        public async Task<QuarantinedFile> QuarantineAsync(string filePath, string reason)
        {
            Directory.CreateDirectory(QuarantineDir);

            var quarantineFileName = $"{Guid.NewGuid()}.quarantined";
            var quarantinePath = Path.Combine(QuarantineDir, quarantineFileName);

            File.Move(filePath, quarantinePath);

            var entry = new QuarantinedFile
            {
                OriginalPath = filePath,
                QuarantinePath = quarantinePath,
                Reason = reason,
                QuarantinedAtUtc = DateTime.UtcNow
            };

            var log = await LoadLogAsync();
            log.Add(entry);
            await SaveLogAsync(log);

            return entry;
        }

        public async Task RestoreAsync(QuarantinedFile entry)
        {
            if (!File.Exists(entry.QuarantinePath))
                throw new FileNotFoundException("Quarantined file no longer exists.", entry.QuarantinePath);

            Directory.CreateDirectory(Path.GetDirectoryName(entry.OriginalPath)!);
            File.Move(entry.QuarantinePath, entry.OriginalPath, overwrite: false);

            var log = await LoadLogAsync();
            log.RemoveAll(e => e.QuarantinePath == entry.QuarantinePath);
            await SaveLogAsync(log);
        }

        public async Task<List<QuarantinedFile>> LoadLogAsync()
        {
            try
            {
                if (!File.Exists(LogPath)) return new List<QuarantinedFile>();
                var json = await File.ReadAllTextAsync(LogPath);
                return System.Text.Json.JsonSerializer.Deserialize<List<QuarantinedFile>>(json) ?? new List<QuarantinedFile>();
            }
            catch
            {
                return new List<QuarantinedFile>();
            }
        }

        private async Task SaveLogAsync(List<QuarantinedFile> log)
        {
            Directory.CreateDirectory(QuarantineDir);
            var json = System.Text.Json.JsonSerializer.Serialize(log, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(LogPath, json);
        }
    }
}
