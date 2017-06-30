using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BuddhaBowls.UserControls
{
    /// <summary>
    /// Interaction logic for PeriodSelectorControl.xaml
    /// </summary>
    public partial class PeriodSelectorControl : UserControl
    {
        public PeriodSelectorControl()
        {
            InitializeComponent();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext != null)
            {
                if (e.NewSize.Width > 640)
                    ((PeriodSelectorVM)DataContext).CurWeekVisibility = Visibility.Visible;
                else
                    ((PeriodSelectorVM)DataContext).CurWeekVisibility = Visibility.Hidden;
            }
        }
    }
}
