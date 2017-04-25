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
    /// Interaction logic for PrepListControl.xaml
    /// </summary>
    public partial class PrepListControl : UserControl
    {
        public PrepListControl()
        {
            InitializeComponent();
        }

        public PrepListControl(InventoryTabVM context) : this()
        {
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((InventoryTabVM)DataContext).FilterItems(textBox.Text);
        }
    }
}
