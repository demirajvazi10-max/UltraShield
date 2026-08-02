using System.Windows.Controls;

namespace UltraShield.Modules.Checklist
{
    public partial class ChecklistView : UserControl
    {
        public ChecklistView()
        {
            InitializeComponent();
            DataContext = new ChecklistViewModel();
        }
    }
}
