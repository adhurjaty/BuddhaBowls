using BuddhaBowls.Models;
using System.Windows.Controls;

namespace BuddhaBowls.UserControls
{
    /// <summary>
    /// Interaction logic for VendorTabControl.xaml
    /// </summary>
    public partial class VendorTabControl : UserControl
    {
        public VendorTabControl()
        {
            InitializeComponent();
        }

        public void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((VendorTabVM)DataContext).FilterVendors(FilterVendorBox.Text);
        }

        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= DataGrid_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).RowEditEnding += DataGrid_RowEditEnding;
                ((VendorTabVM)DataContext).VendorItemChanged(((InventoryItem)e.Row.Item));
            }
        }
    }
}
