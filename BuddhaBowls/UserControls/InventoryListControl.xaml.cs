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
        public InventoryListControl(TabVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        public void HideArrowColumn()
        {
            arrowColumn.Visibility = Visibility.Collapsed;
        }

        public void ShowArrowColumn()
        {
            arrowColumn.Visibility = Visibility.Visible;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((TabVM)DataContext).FilterItems(textBox.Text);
        }

        private void MasterList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SetBlankToZero((TextBox)e.EditingElement);
            ((NewInventoryVM)DataContext).InventoryItemCountChanged();
        }

        private void SetBlankToZero(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryTabVM)DataContext).MoveUp((InventoryItem)((Button)sender).CommandParameter);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ((InventoryTabVM)DataContext).MoveDown((InventoryItem)((Button)sender).CommandParameter);

        }

        private void dataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            HideArrowColumn();
        }
    }
}
