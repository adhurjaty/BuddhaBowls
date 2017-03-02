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
        public InventoryListControl(InventoryListVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        //public void HideArrowColumn()
        //{
        //    arrowColumn.Visibility = Visibility.Collapsed;
        //}

        //public void ShowArrowColumn()
        //{
        //    arrowColumn.Visibility = Visibility.Visible;
        //}

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((InventoryListVM)DataContext).FilterItems(textBox.Text);
        }

        private void MasterList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SetBlankToZero((TextBox)e.EditingElement);
            ((InventoryListVM)DataContext).InventoryItemCountChanged();
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryListVM)DataContext).MoveUp((InventoryItem)((Button)sender).CommandParameter);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryListVM)DataContext).MoveDown((InventoryItem)((Button)sender).CommandParameter);

        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            ((VendorInventoryItem)((ComboBox)sender).DataContext).SelectedVendor = (Vendor)((ComboBox)sender).SelectedItem;
        }
    }
}
