using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using UltraShield.Core;
using UltraShield.Services;
using UltraShield.Services.Models;

namespace UltraShield.Modules.Scanner
{
    public class ScannerViewModel : ViewModelBase
    {
        private static readonly HttpClient SharedHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly NpmPackageService _npmService = new(SharedHttpClient);
        private readonly PyPiPackageService _pypiService = new(SharedHttpClient);
        private readonly MalwareBazaarService _malwareBazaarService = new(SharedHttpClient);
        private readonly VirusTotalService _virusTotalService = new(SharedHttpClient);
        private readonly AppSettingsService _settingsService = new();
        private readonly QuarantineService _quarantineService = new();
        private readonly FolderScannerService _folderScannerService;

        private AppSettings _settings = new();
        private CancellationTokenSource? _scanCts;

        private string _target = "";
        public string Target
        {
            get => _target;
            set => SetField(ref _target, value);
        }

        // ----- Mode selection (mutually exclusive) -----

        private ScanMode _mode = ScanMode.Npm;

        public bool IsNpmMode
        {
            get => _mode == ScanMode.Npm;
            set { if (value) Mode = ScanMode.Npm; }
        }
        public bool IsPyPiMode
        {
            get => _mode == ScanMode.PyPi;
            set { if (value) Mode = ScanMode.PyPi; }
        }
        public bool IsFileMode
        {
            get => _mode == ScanMode.File;
            set { if (value) Mode = ScanMode.File; }
        }
        public bool IsFolderMode
        {
            get => _mode == ScanMode.Folder;
            set { if (value) Mode = ScanMode.Folder; }
        }

        private ScanMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                OnPropertyChanged(nameof(IsNpmMode));
                OnPropertyChanged(nameof(IsPyPiMode));
                OnPropertyChanged(nameof(IsFileMode));
                OnPropertyChanged(nameof(IsFolderMode));
                OnPropertyChanged(nameof(ShowBrowseButton));
                OnPropertyChanged(nameof(ShowSingleResultPanel));
            }
        }

        // Computed, read-only - kept as plain properties (not converters) so
        // the XAML binds directly with no hidden converter-parameter logic.
        public bool ShowBrowseButton => IsFileMode || IsFolderMode;
        public bool ShowSingleResultPanel => !IsFolderMode;

        private enum ScanMode { Npm, PyPi, File, Folder }

        // ----- Single-item scan result -----

        private string _resultText = "No scan run yet.";
        public string ResultText
        {
            get => _resultText;
            set => SetField(ref _resultText, value);
        }

        // ----- Folder scan state -----

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetField(ref _isScanning, value))
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _folderScanStatus = "";
        public string FolderScanStatus
        {
            get => _folderScanStatus;
            set => SetField(ref _folderScanStatus, value);
        }

        private double _folderScanPercent;
        public double FolderScanPercent
        {
            get => _folderScanPercent;
            set => SetField(ref _folderScanPercent, value);
        }

        public ObservableCollection<FindingItemViewModel> Findings { get; } = new();

        // Set from code-behind when the PasswordBox changes (see ScannerView.xaml.cs).
        public string PendingApiKey { get; set; } = "";

        public RelayCommand ScanCommand { get; }
        public RelayCommand BrowseCommand { get; }
        public RelayCommand SaveApiKeyCommand { get; }
        public RelayCommand StartFolderScanCommand { get; }
        public RelayCommand CancelFolderScanCommand { get; }

        public ScannerViewModel()
        {
            _folderScannerService = new FolderScannerService(_malwareBazaarService, _virusTotalService, new HashResultCache());

            ScanCommand = new RelayCommand(async _ => await RunScanAsync());
            BrowseCommand = new RelayCommand(_ => Browse());
            SaveApiKeyCommand = new RelayCommand(async _ => await SaveApiKeyAsync());
            StartFolderScanCommand = new RelayCommand(async _ => await StartFolderScanAsync(), _ => !IsScanning);
            CancelFolderScanCommand = new RelayCommand(_ => _scanCts?.Cancel(), _ => IsScanning);

            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            _settings = await _settingsService.LoadAsync();
        }

        private async Task SaveApiKeyAsync()
        {
            _settings.VirusTotalApiKey = PendingApiKey;
            await _settingsService.SaveAsync(_settings);
            ResultText = "VirusTotal API key saved locally.";
        }

        private void Browse()
        {
            if (IsFolderMode)
            {
                var dialog = new OpenFolderDialog { Title = "Select a folder to scan" };
                if (dialog.ShowDialog() == true)
                {
                    Target = dialog.FolderName;
                }
            }
            else
            {
                var dialog = new OpenFileDialog { Title = "Select a file to check" };
                if (dialog.ShowDialog() == true)
                {
                    Target = dialog.FileName;
                }
            }
        }

        // ----- Single package / single file scan -----

        private async Task RunScanAsync()
        {
            if (string.IsNullOrWhiteSpace(Target))
            {
                ResultText = "Enter a package name or file path first.";
                return;
            }

            ResultText = "Scanning...";

            try
            {
                if (IsNpmMode)
                {
                    var result = await _npmService.CheckAsync(Target.Trim());
                    ResultText = FormatPackageResult(result);
                }
                else if (IsPyPiMode)
                {
                    var result = await _pypiService.CheckAsync(Target.Trim());
                    ResultText = FormatPackageResult(result);
                }
                else if (IsFileMode)
                {
                    await RunFileScanAsync(Target.Trim());
                }
            }
            catch (Exception ex)
            {
                ResultText = $"Scan failed: {ex.Message}";
            }
        }

        private async Task RunFileScanAsync(string path)
        {
            if (!File.Exists(path))
            {
                ResultText = "That file path doesn't exist.";
                return;
            }

            var hash = await FileHasher.ComputeSha256Async(path);

            var mbResult = await _malwareBazaarService.LookupAsync(hash);

            if (mbResult.Verdict == ScanVerdict.Malicious)
            {
                ResultText = FormatHashResult(hash, mbResult);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_settings.VirusTotalApiKey))
            {
                var vtResult = await _virusTotalService.LookupAsync(hash, _settings.VirusTotalApiKey);
                ResultText = FormatHashResult(hash, mbResult) + "\n\n" + FormatHashResult(hash, vtResult);
            }
            else
            {
                ResultText = FormatHashResult(hash, mbResult) +
                    "\n\nAdd a VirusTotal API key below for a second opinion from ~70 antivirus engines.";
            }
        }

        // ----- Full folder scan (the "Malwarebytes-style" on-demand scan) -----

        private async Task StartFolderScanAsync()
        {
            if (string.IsNullOrWhiteSpace(Target) || !Directory.Exists(Target))
            {
                FolderScanStatus = "Choose a valid folder first.";
                return;
            }

            Findings.Clear();
            IsScanning = true;
            FolderScanPercent = 0;
            FolderScanStatus = "Starting scan...";
            _scanCts = new CancellationTokenSource();

            var progress = new Progress<FolderScanProgress>(p =>
            {
                FolderScanPercent = p.TotalFiles > 0 ? (double)p.FilesScanned / p.TotalFiles * 100 : 0;
                FolderScanStatus = $"Scanned {p.FilesScanned} of {p.TotalFiles} files - {p.ThreatsFound} threat(s) found so far. Checking: {Path.GetFileName(p.CurrentFile)}";
            });

            try
            {
                var results = await _folderScannerService.ScanAsync(
                    Target.Trim(),
                    string.IsNullOrWhiteSpace(_settings.VirusTotalApiKey) ? null : _settings.VirusTotalApiKey,
                    progress,
                    _scanCts.Token);

                foreach (var finding in results)
                {
                    Findings.Add(new FindingItemViewModel(finding, _quarantineService));
                }

                FolderScanStatus = results.Count == 0
                    ? "Scan complete - no threats found."
                    : $"Scan complete - {results.Count} threat(s) found. Review the list below.";
            }
            catch (OperationCanceledException)
            {
                FolderScanStatus = "Scan cancelled.";
            }
            catch (Exception ex)
            {
                FolderScanStatus = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
                _scanCts = null;
            }
        }

        // ----- Formatting -----

        private static string FormatPackageResult(PackageCheckResult r)
        {
            var lines = new List<string>
            {
                $"{r.Ecosystem} package '{r.PackageName}': {r.Verdict}",
                r.ExistsInRegistry ? "Found in the registry." : "Not found in the registry."
            };
            lines.AddRange(r.Warnings.Select(w => $"Warning: {w}"));
            lines.AddRange(r.Notes);
            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatHashResult(string hash, HashCheckResult r)
        {
            var lines = new List<string>
            {
                $"[{r.Source}] SHA-256 {hash}: {r.Verdict}"
            };
            if (r.ThreatLabel != null) lines.Add($"Threat label: {r.ThreatLabel}");
            if (r.DetectionCount != null && r.TotalEngines != null)
                lines.Add($"Detections: {r.DetectionCount}/{r.TotalEngines}");
            lines.AddRange(r.Notes);
            return string.Join(Environment.NewLine, lines);
        }
    }
}
