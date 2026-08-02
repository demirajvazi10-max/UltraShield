using UltraShield.Core;
using UltraShield.Services;
using UltraShield.Services.Models;

namespace UltraShield.Modules.Scanner
{
    public class FindingItemViewModel : ViewModelBase
    {
        private readonly QuarantineService _quarantineService;
        private readonly FolderScanFinding _finding;

        public string FilePath => _finding.FilePath;
        public string Hash => _finding.Hash;
        public ScanVerdict Verdict => _finding.Verdict;
        public string Summary =>
            $"{Verdict} - {_finding.FilePath}" +
            (_finding.ThreatLabel != null ? $" ({_finding.ThreatLabel})" : "") +
            $" [{_finding.Source}]";

        private string _status = "Not quarantined";
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        private bool _isQuarantined;
        public bool IsQuarantined
        {
            get => _isQuarantined;
            set
            {
                if (SetField(ref _isQuarantined, value))
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public RelayCommand QuarantineCommand { get; }

        public FindingItemViewModel(FolderScanFinding finding, QuarantineService quarantineService)
        {
            _finding = finding;
            _quarantineService = quarantineService;
            QuarantineCommand = new RelayCommand(async _ => await QuarantineAsync(), _ => !IsQuarantined);
        }

        private async System.Threading.Tasks.Task QuarantineAsync()
        {
            try
            {
                await _quarantineService.QuarantineAsync(_finding.FilePath, $"{Verdict}: {_finding.ThreatLabel ?? _finding.Source}");
                IsQuarantined = true;
                Status = "Quarantined";
            }
            catch (Exception ex)
            {
                Status = $"Quarantine failed: {ex.Message}";
            }
        }
    }
}
