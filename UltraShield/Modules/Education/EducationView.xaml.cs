using System.Windows.Controls;

namespace UltraShield.Modules.Education
{
    public partial class EducationView : UserControl
    {
        public EducationView()
        {
            InitializeComponent();
            DataContext = new EducationViewModel();
        }
    }
}
