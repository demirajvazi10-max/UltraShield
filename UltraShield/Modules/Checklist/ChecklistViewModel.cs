using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using UltraShield.Core;
using UltraShield.Services;

namespace UltraShield.Modules.Checklist
{
    public class ChecklistItem : ViewModelBase
    {
        /// <summary>English text - also the key the done/not-done state is saved under, so it survives a language switch.</summary>
        public string Key { get; set; } = "";

        /// <summary>What is shown and read: the key in the current language.</summary>
        public string Title => L.T(Key);

        public void RefreshLanguage() => OnPropertyChanged(nameof(Title));

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
            new ChecklistItem { Key = "2FA enabled on primary email" },
            new ChecklistItem { Key = "2FA enabled on GitHub / package registry accounts" },
            new ChecklistItem { Key = "Dependencies audited for known-malicious packages" },
            new ChecklistItem { Key = "Recent, tested backups exist" },
            new ChecklistItem { Key = "No plaintext credentials in repos or scripts" },
            new ChecklistItem { Key = "Verified recruiter/job offers through an official company channel before running any code they sent" },
            new ChecklistItem { Key = "Browser extensions reviewed - none installed from outside the official store" },
        };

        public string ProgressText => L.F("{0} of {1} completed", Items.Count(i => i.IsDone), Items.Count);

        public ChecklistViewModel()
        {
            foreach (var item in Items)
            {
                item.PropertyChanged += OnItemChanged;
            }

            L.Changed += () =>
            {
                foreach (var item in Items) item.RefreshLanguage();
                OnPropertyChanged(nameof(ProgressText));
            };

            _ = LoadStateAsync();
        }

        private async System.Threading.Tasks.Task LoadStateAsync()
        {
            var saved = await _persistence.LoadAsync();
            foreach (var item in Items)
            {
                if (saved.TryGetValue(item.Key, out var done))
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
            var state = Items.ToDictionary(i => i.Key, i => i.IsDone);
            await _persistence.SaveAsync(state);
        }
    }
}
