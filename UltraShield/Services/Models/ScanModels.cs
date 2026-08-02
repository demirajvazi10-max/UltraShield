using System.Collections.Generic;

namespace UltraShield.Services.Models
{
    public enum ScanVerdict
    {
        Unknown,
        Clean,
        Suspicious,
        Malicious,
        Error
    }

    public class PackageCheckResult
    {
        public string PackageName { get; set; } = "";
        public string Ecosystem { get; set; } = ""; // "npm" or "PyPI"
        public bool ExistsInRegistry { get; set; }
        public ScanVerdict Verdict { get; set; } = ScanVerdict.Unknown;
        public List<string> Warnings { get; set; } = new();
        public List<string> Notes { get; set; } = new();
    }

    public class HashCheckResult
    {
        public string Hash { get; set; } = "";
        public ScanVerdict Verdict { get; set; } = ScanVerdict.Unknown;
        public string? ThreatLabel { get; set; }
        public int? DetectionCount { get; set; }
        public int? TotalEngines { get; set; }
        public string Source { get; set; } = ""; // "MalwareBazaar" or "VirusTotal"
        public List<string> Notes { get; set; } = new();
    }
}
