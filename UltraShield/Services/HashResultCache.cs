using System.IO;
using System.Text.Json;
using UltraShield.Services.Models;

namespace UltraShield.Services
{
    public class CachedHashResult
    {
        public string Hash { get; set; } = "";
        public ScanVerdict Verdict { get; set; }
        public string? ThreatLabel { get; set; }
        public string Source { get; set; } = "";
        public DateTime CheckedAtUtc { get; set; }
    }

    /// <summary>
    /// Persists hash-lookup results locally so a repeat folder scan doesn't
    /// re-query MalwareBazaar/VirusTotal for files it already checked.
    /// Entries older than <see cref="MaxAgeDays"/> are treated as stale and
    /// re-checked, since a hash that was "clean" a year ago isn't guaranteed
    /// clean forever (databases grow).
    /// </summary>
    public class HashResultCache
    {
        private const int MaxAgeDays = 30;

        private static readonly string CachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltraShield", "hash-cache.json");

        private Dictionary<string, CachedHashResult> _entries = new();
        private bool _loaded;

        public async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            try
            {
                if (File.Exists(CachePath))
                {
                    var json = await File.ReadAllTextAsync(CachePath);
                    _entries = JsonSerializer.Deserialize<Dictionary<string, CachedHashResult>>(json)
                               ?? new Dictionary<string, CachedHashResult>();
                }
            }
            catch
            {
                _entries = new Dictionary<string, CachedHashResult>();
            }
            _loaded = true;
        }

        public CachedHashResult? TryGet(string hash)
        {
            if (_entries.TryGetValue(hash, out var entry) &&
                (DateTime.UtcNow - entry.CheckedAtUtc).TotalDays < MaxAgeDays)
            {
                return entry;
            }
            return null;
        }

        public void Set(string hash, ScanVerdict verdict, string? threatLabel, string source)
        {
            _entries[hash] = new CachedHashResult
            {
                Hash = hash,
                Verdict = verdict,
                ThreatLabel = threatLabel,
                Source = source,
                CheckedAtUtc = DateTime.UtcNow
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
                var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CachePath, json);
            }
            catch
            {
                // Cache is a performance optimization, not critical state - don't blow up the scan over it.
            }
        }
    }
}
