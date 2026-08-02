namespace UltraShield.Services
{
    /// <summary>
    /// Small, manually-curated seed list of package names that have been
    /// publicly documented (by Sonatype, SecurityScorecard, etc.) as malicious
    /// or brandjacked. This is NOT a substitute for a real feed - it's a
    /// starting point so the scanner has something useful offline, before
    /// Services/ is wired up to a live feed (e.g. Sonatype OSS Index, OSV.dev,
    /// or a maintained community blocklist).
    ///
    /// Update this list from public security advisories only. Never add a
    /// name without a public source - false positives here would wrongly
    /// flag legitimate developers' packages.
    /// </summary>
    public static class KnownMaliciousPackages
    {
        // (package name, ecosystem, short public-advisory reference)
        public static readonly (string Name, string Ecosystem, string Reference)[] Entries = new[]
        {
            ("buffer-utilities", "npm", "Sonatype sonatype-2026-003558 - npm brandjacking, Lazarus Group, June 2026"),
            ("rollup-plugin-polyfill-route", "npm", "Panther Labs - DPRK npm campaign, BeaverTail/OtterCookie payload, March 2026"),
            ("event-stream", "npm", "2018 - malicious dependency injected via compromised maintainer account (historical)"),
            ("ua-parser-js", "npm", "2021 - compromised versions published cryptominer/credential stealer (historical)"),
        };

        public static bool TryFind(string packageName, string ecosystem, out string? reference)
        {
            foreach (var entry in Entries)
            {
                if (string.Equals(entry.Name, packageName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entry.Ecosystem, ecosystem, StringComparison.OrdinalIgnoreCase))
                {
                    reference = entry.Reference;
                    return true;
                }
            }
            reference = null;
            return false;
        }
    }
}
