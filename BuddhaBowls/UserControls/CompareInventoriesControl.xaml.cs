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
    /// Interaction logic for CompareInventoriesControl.xaml
    /// </summary>
    public partial class CompareInventoriesControl : UserControl
    {
        public CompareInventoriesControl(CompareInvVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        public void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((CompareInvVM)DataContext).FilterItems(FilterVendorBox.Text);
        }

    }
}
