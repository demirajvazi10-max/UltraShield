using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UltraShield.Services.Models;

namespace UltraShield.Services
{
    /// <summary>
    /// Looks up a file hash against VirusTotal. Requires the user's own,
    /// personal API key (free tier available at virustotal.com) - UltraShield
    /// never ships or bundles a shared key. The key is stored locally via
    /// AppSettingsService and sent only to VirusTotal's own API.
    /// </summary>
    public class VirusTotalService
    {
        private readonly HttpClient _http;

        public VirusTotalService(HttpClient http)
        {
            _http = http;
        }

        public async Task<HashCheckResult> LookupAsync(string hash, string apiKey)
        {
            var result = new HashCheckResult { Hash = hash, Source = "VirusTotal" };

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                result.Verdict = ScanVerdict.Unknown;
                result.Notes.Add("No VirusTotal API key configured - add your own free key in Settings to enable this check.");
                return result;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.virustotal.com/api/v3/files/{hash}");
                request.Headers.Add("x-apikey", apiKey);

                var response = await _http.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Verdict = ScanVerdict.Unknown;
                    result.Notes.Add("Not present in VirusTotal's database.");
                    return result;
                }

                if (!response.IsSuccessStatusCode)
                {
                    result.Verdict = ScanVerdict.Error;
                    result.Notes.Add($"VirusTotal returned status {(int)response.StatusCode}.");
                    return result;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var stats = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("last_analysis_stats");

                var malicious = stats.GetProperty("malicious").GetInt32();
                var suspicious = stats.GetProperty("suspicious").GetInt32();
                var total = stats.EnumerateObject().Sum(p => p.Value.GetInt32());

                result.DetectionCount = malicious + suspicious;
                result.TotalEngines = total;
                result.Verdict = malicious > 0 ? ScanVerdict.Malicious
                                : suspicious > 0 ? ScanVerdict.Suspicious
                                : ScanVerdict.Clean;
                result.Notes.Add($"{malicious + suspicious} of {total} engines flagged this file.");
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
