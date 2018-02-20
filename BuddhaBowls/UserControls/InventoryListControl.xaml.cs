using BuddhaBowls.Models;
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
    /// Interaction logic for InventoryListControl.xaml
    /// </summary>
    public partial class InventoryListControl : UserControl
    {
        private bool _isUserInteraction = false;
        
        public InventoryListControl()
        {
            InitializeComponent();
        }

        public InventoryListControl(InventoryListVM context) : this()
        {
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((InventoryListVM)DataContext).FilterItems(textBox.Text);
        }

        private void MasterList_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= MasterList_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).RowEditEnding += MasterList_RowEditEnding;
                ((InventoryListVM)DataContext).RowEdited(((VendorInventoryItem)e.Row.Item));
            }
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryListVM)DataContext).MoveUp((VendorInventoryItem)((Button)sender).CommandParameter);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryListVM)DataContext).MoveDown((VendorInventoryItem)((Button)sender).CommandParameter);

        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            VendorInventoryItem item = (VendorInventoryItem)((ComboBox)sender).DataContext;
            Vendor selectedVendor = (Vendor)((ComboBox)sender).SelectedItem;
            item.SelectedVendor = selectedVendor;
            if (item.SelectedVendor != null && _isUserInteraction)
                ((InventoryListVM)DataContext).RowEdited(item);
            _isUserInteraction = false;
        }

        private void ComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isUserInteraction = true;
        }
    }
}
