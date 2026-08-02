using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using UltraShield.Services.Models;

namespace UltraShield.Core.Converters
{
    public class VerdictToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ScanVerdict verdict)
                return Brushes.Gray;

            return verdict switch
            {
                ScanVerdict.Clean => (Brush)Application.Current.Resources["BrushSafe"],
                ScanVerdict.Suspicious => (Brush)Application.Current.Resources["BrushSuspicious"],
                ScanVerdict.Malicious => (Brush)Application.Current.Resources["BrushDanger"],
                _ => (Brush)Application.Current.Resources["BrushTextSecondary"]
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
