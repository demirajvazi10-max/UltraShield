using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UltraShield.Services.Models;

namespace UltraShield.Services
{
    public class PyPiPackageService
    {
        private readonly HttpClient _http;

        public PyPiPackageService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PackageCheckResult> CheckAsync(string packageName)
        {
            var result = new PackageCheckResult { PackageName = packageName, Ecosystem = "PyPI" };

            if (KnownMaliciousPackages.TryFind(packageName, "PyPI", out var reference))
            {
                result.ExistsInRegistry = true;
                result.Verdict = ScanVerdict.Malicious;
                result.Warnings.Add($"Matches known-malicious package list: {reference}");
                return result;
            }

            try
            {
                var response = await _http.GetAsync($"https://pypi.org/pypi/{Uri.EscapeDataString(packageName)}/json");
                if (!response.IsSuccessStatusCode)
                {
                    result.ExistsInRegistry = false;
                    result.Verdict = ScanVerdict.Unknown;
                    result.Notes.Add("Package not found on PyPI.");
                    return result;
                }

                result.ExistsInRegistry = true;
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var info = doc.RootElement.GetProperty("info");

                var hasHomepage = info.TryGetProperty("home_page", out var hp) && !string.IsNullOrWhiteSpace(hp.GetString());
                var hasProjectUrls = info.TryGetProperty("project_urls", out var pu) && pu.ValueKind == JsonValueKind.Object;
                if (!hasHomepage && !hasProjectUrls)
                {
                    result.Warnings.Add("No homepage or project URLs in package metadata.");
                }

                if (doc.RootElement.TryGetProperty("releases", out var releases))
                {
                    var releaseCount = 0;
                    foreach (var _ in releases.EnumerateObject()) releaseCount++;
                    if (releaseCount <= 1)
                    {
                        result.Warnings.Add("Package has only a single published release.");
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
