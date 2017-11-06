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
            ((NewOrderVM)DataContext).RowEdited((VendorInventoryItem)e.Row.Item);
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }
    }
}
