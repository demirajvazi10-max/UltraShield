using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UltraShield.Services.Models;

namespace UltraShield.Services
{
    public class NpmPackageService
    {
        private readonly HttpClient _http;

        public NpmPackageService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PackageCheckResult> CheckAsync(string packageName)
        {
            var result = new PackageCheckResult { PackageName = packageName, Ecosystem = "npm" };

            // 1. Local seed list of publicly-documented bad actors first - cheap and offline.
            if (KnownMaliciousPackages.TryFind(packageName, "npm", out var reference))
            {
                result.ExistsInRegistry = true;
                result.Verdict = ScanVerdict.Malicious;
                result.Warnings.Add($"Matches known-malicious package list: {reference}");
                return result;
            }

            // 2. Live registry lookup for existence + basic metadata heuristics.
            try
            {
                var response = await _http.GetAsync($"https://registry.npmjs.org/{Uri.EscapeDataString(packageName)}");
                if (!response.IsSuccessStatusCode)
                {
                    result.ExistsInRegistry = false;
                    result.Verdict = ScanVerdict.Unknown;
                    result.Notes.Add("Package not found in the npm registry.");
                    return result;
                }

                result.ExistsInRegistry = true;
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                // Heuristics - none of these are proof of malice on their own,
                // they're signals worth a human looking closer at.
                if (!root.TryGetProperty("repository", out _))
                {
                    result.Warnings.Add("No repository field in package metadata.");
                }

                if (root.TryGetProperty("time", out var time) && time.TryGetProperty("created", out var created))
                {
                    if (DateTime.TryParse(created.GetString(), out var createdDate))
                    {
                        var age = DateTime.UtcNow - createdDate;
                        if (age.TotalDays < 14)
                        {
                            result.Warnings.Add($"Package was first published only {(int)age.TotalDays} day(s) ago.");
                        }
                    }
                }

                if (root.TryGetProperty("versions", out var versions))
                {
                    var versionCount = 0;
                    foreach (var _ in versions.EnumerateObject()) versionCount++;
                    if (versionCount <= 1)
                    {
                        result.Warnings.Add("Package has only a single published version.");
                    }
                }

                result.Verdict = result.Warnings.Count > 0 ? ScanVerdict.Suspicious : ScanVerdict.Clean;
                result.Notes.Add("Heuristic checks only - this does not scan the package's actual code.");
            }
            catch (Exception ex)
            {
                result.Verdict = ScanVerdict.Error;
                result.Notes.Add($"Lookup failed: {ex.Message}");
            }

            return result;
        }
    }
}
