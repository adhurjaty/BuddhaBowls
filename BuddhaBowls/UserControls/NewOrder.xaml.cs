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
    /// Interaction logic for NewOrder.xaml
    /// </summary>
    public partial class NewOrder : UserControl
    {
        public NewOrder()
        {
            InitializeComponent();
        }

        public NewOrder(object context) : this()
        {
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterOrderItemBox;

            ((OrderTabVM)DataContext).FilterInventoryItems(textBox.Text);
        }

        private void OrderList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SetBlankToZero((TextBox)e.EditingElement);
            ((OrderTabVM)DataContext).InventoryOrderAmountChanged();
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }
    }
}
