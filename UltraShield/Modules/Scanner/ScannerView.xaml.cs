using System.Windows.Controls;

namespace UltraShield.Modules.Scanner
{
    public partial class ScannerView : UserControl
    {
        private readonly ScannerViewModel _viewModel;

        public ScannerView()
        {
            InitializeComponent();
            _viewModel = new ScannerViewModel();
            DataContext = _viewModel;
        }

        // PasswordBox intentionally isn't data-bound (WPF disallows binding
        // Password directly for security reasons) - so it's synced to the
        // ViewModel here in code-behind instead.
        private void ApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.PendingApiKey = ApiKeyBox.Password;
        }
    }
}
