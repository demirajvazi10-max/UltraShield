using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using UltraShield.Core;
using UltraShield.Services;

namespace UltraShield.Modules.Checklist
{
    public class ChecklistItem : ViewModelBase
    {
        public string Title { get; set; } = "";

        private bool _isDone;
        public bool IsDone
        {
            get => _isDone;
            set => SetField(ref _isDone, value);
        }
    }

    public class ChecklistViewModel : ViewModelBase
    {
        private readonly ChecklistPersistenceService _persistence = new();
        private bool _isLoaded;

        public ObservableCollection<ChecklistItem> Items { get; } = new()
        {
            new ChecklistItem { Title = "2FA enabled on primary email" },
            new ChecklistItem { Title = "2FA enabled on GitHub / package registry accounts" },
            new ChecklistItem { Title = "Dependencies audited for known-malicious packages" },
            new ChecklistItem { Title = "Recent, tested backups exist" },
            new ChecklistItem { Title = "No plaintext credentials in repos or scripts" },
            new ChecklistItem { Title = "Verified recruiter/job offers through an official company channel before running any code they sent" },
            new ChecklistItem { Title = "Browser extensions reviewed - none installed from outside the official store" },
        };

        public string ProgressText => $"{Items.Count(i => i.IsDone)} of {Items.Count} completed";

        public ChecklistViewModel()
        {
            foreach (var item in Items)
            {
                item.PropertyChanged += OnItemChanged;
            }

            _ = LoadStateAsync();
        }

        private async System.Threading.Tasks.Task LoadStateAsync()
        {
            var saved = await _persistence.LoadAsync();
            foreach (var item in Items)
            {
                if (saved.TryGetValue(item.Title, out var done))
                {
                    item.IsDone = done;
                }
            }
            _isLoaded = true;
        }

        private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChecklistItem.IsDone))
            {
                OnPropertyChanged(nameof(ProgressText));
                if (_isLoaded)
                {
                    _ = SaveStateAsync();
                }
            }
        }

        private async System.Threading.Tasks.Task SaveStateAsync()
        {
            var state = Items.ToDictionary(i => i.Title, i => i.IsDone);
            await _persistence.SaveAsync(state);
        }
    }
}
