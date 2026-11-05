using System.Windows;

namespace UltraShield
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            L.UpdateLanguageButton(LanguageButton);
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e) => L.Toggle(LanguageButton);

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }
    }
}
