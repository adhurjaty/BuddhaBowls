using BuddhaBowls.Models;
using System.Windows.Controls;

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

        public NewOrder(NewOrderVM context) : this()
        {
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterOrderItemBox;

            ((NewOrderVM)DataContext).FilterInventoryItems(textBox.Text);
        }

        private void OrderList_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            //SetBlankToZero((TextBox)e.EditingElement);
            if (this.dataGrid.SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= OrderList_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).Items.Refresh();
                ((DataGrid)sender).RowEditEnding += OrderList_RowEditEnding;
            }
            ((NewOrderVM)DataContext).InventoryOrderAmountChanged((InventoryItem)((DataGrid)sender).CurrentItem);
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }
    }
}
