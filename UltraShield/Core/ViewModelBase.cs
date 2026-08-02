using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UltraShield.Core
{
    /// <summary>
    /// Shared base for every module ViewModel (Education, Scanner, Checklist).
    /// Keeping this in Core means all three modules stay consistent without
    /// depending on each other.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
