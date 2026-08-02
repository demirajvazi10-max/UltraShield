using System.IO;
using System.Threading;
using UltraShield.Services.Models;

namespace UltraShield.Services
{
    public class FolderScanProgress
    {
        public int FilesScanned { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFile { get; set; } = "";
        public int ThreatsFound { get; set; }
    }

    public class FolderScanFinding
    {
        public string FilePath { get; set; } = "";
        public string Hash { get; set; } = "";
        public ScanVerdict Verdict { get; set; }
        public string? ThreatLabel { get; set; }
        public string Source { get; set; } = "";
    }

    /// <summary>
    /// On-demand (not real-time) folder/drive scan - the "Malwarebytes-style"
    /// scanner. Walks a directory tree, hashes every file, and checks each
    /// hash against MalwareBazaar (free) and, if a key is configured,
    /// VirusTotal. Results are cached locally (see HashResultCache) so a
    /// repeat scan of the same tree is fast and doesn't re-hit rate limits.
    ///
    /// This is explicitly NOT real-time protection - it only looks at files
    /// when a scan is run, the same way a Malwarebytes on-demand scan does.
    /// </summary>
    public class FolderScannerService
    {
        // VirusTotal's public free tier allows ~4 requests/minute - stay
        // comfortably under that when a VT key is configured.
        private static readonly TimeSpan VirusTotalThrottle = TimeSpan.FromSeconds(16);

        private static readonly HashSet<string> DefaultSkipFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", "node_modules", "bin", "obj", "$RECYCLE.BIN", "System Volume Information"
        };

        private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500 MB - skip huge files by default, they're rarely the payload and slow the scan a lot

        private readonly MalwareBazaarService _malwareBazaar;
        private readonly VirusTotalService _virusTotal;
        private readonly HashResultCache _cache;

        public FolderScannerService(MalwareBazaarService malwareBazaar, VirusTotalService virusTotal, HashResultCache cache)
        {
            _malwareBazaar = malwareBazaar;
            _virusTotal = virusTotal;
            _cache = cache;
        }

        public async Task<List<FolderScanFinding>> ScanAsync(
            string rootPath,
            string? virusTotalApiKey,
            IProgress<FolderScanProgress>? progress,
            CancellationToken cancellationToken,
            HashSet<string>? extraSkipFolders = null)
        {
            var skipFolders = new HashSet<string>(DefaultSkipFolders, StringComparer.OrdinalIgnoreCase);
            if (extraSkipFolders != null)
            {
                foreach (var f in extraSkipFolders) skipFolders.Add(f);
            }

            await _cache.EnsureLoadedAsync();

            var files = EnumerateFiles(rootPath, skipFolders).ToList();
            var findings = new List<FolderScanFinding>();
            var scanned = 0;
            var lastVirusTotalCall = DateTime.MinValue;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanned++;
                progress?.Report(new FolderScanProgress
                {
                    FilesScanned = scanned,
                    TotalFiles = files.Count,
                    CurrentFile = file,
                    ThreatsFound = findings.Count
                });

                try
                {
                    var fileInfo = new FileInfo(file);
                    if (!fileInfo.Exists || fileInfo.Length > MaxFileSizeBytes) continue;

                    var hash = await FileHasher.ComputeSha256Async(file);
                    var cached = _cache.TryGet(hash);

                    ScanVerdict verdict;
                    string? label;
                    string source;

                    if (cached != null)
                    {
                        verdict = cached.Verdict;
                        label = cached.ThreatLabel;
                        source = cached.Source + " (cached)";
                    }
                    else
                    {
                        var mbResult = await _malwareBazaar.LookupAsync(hash);

                        if (mbResult.Verdict == ScanVerdict.Malicious)
                        {
                            verdict = ScanVerdict.Malicious;
                            label = mbResult.ThreatLabel;
                            source = "MalwareBazaar";
                        }
                        else if (!string.IsNullOrWhiteSpace(virusTotalApiKey))
                        {
                            var waitFor = VirusTotalThrottle - (DateTime.UtcNow - lastVirusTotalCall);
                            if (waitFor > TimeSpan.Zero)
                                await Task.Delay(waitFor, cancellationToken);

                            var vtResult = await _virusTotal.LookupAsync(hash, virusTotalApiKey);
                            lastVirusTotalCall = DateTime.UtcNow;

                            verdict = vtResult.Verdict;
                            label = vtResult.ThreatLabel;
                            source = "VirusTotal";
                        }
                        else
                        {
                            verdict = mbResult.Verdict;
                            label = null;
                            source = "MalwareBazaar";
                        }

                        _cache.Set(hash, verdict, label, source);
                    }

                    if (verdict == ScanVerdict.Malicious || verdict == ScanVerdict.Suspicious)
                    {
                        findings.Add(new FolderScanFinding
                        {
                            FilePath = file,
                            Hash = hash,
                            Verdict = verdict,
                            ThreatLabel = label,
                            Source = source
                        });
                        progress?.Report(new FolderScanProgress
                        {
                            FilesScanned = scanned,
                            TotalFiles = files.Count,
                            CurrentFile = file,
                            ThreatsFound = findings.Count
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Unreadable file (locked, permission denied, race with deletion, etc.) -
                    // skip it rather than aborting the whole scan.
                }
            }

            await _cache.SaveAsync();
            return findings;
        }

        private static IEnumerable<string> EnumerateFiles(string root, HashSet<string> skipFolders)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                IEnumerable<string> subDirs = Array.Empty<string>();
                IEnumerable<string> files = Array.Empty<string>();

                try
                {
                    subDirs = Directory.EnumerateDirectories(dir);
                    files = Directory.EnumerateFiles(dir);
                }
                catch
                {
                    // No access to this folder - skip it, don't abort the whole scan.
                    continue;
                }

                foreach (var f in files) yield return f;

                foreach (var sd in subDirs)
                {
                    if (!skipFolders.Contains(Path.GetFileName(sd)))
                        stack.Push(sd);
                }
            }
        }
    }
}
