using BuddhaBowls.Models;
using System.Windows;
using System.Windows.Controls;

namespace BuddhaBowls.UserControls
{
    /// <summary>
    /// Interaction logic for InventoryTabControl.xaml
    /// </summary>
    public partial class NewInventory : UserControl
    {
        public NewInventory()
        {
            InitializeComponent();
        }

        public NewInventory(NewInventoryVM viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.InitializeControl(this);
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

            ((NewInventoryVM)DataContext).FilterInventoryItems(textBox.Text);
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
            ((NewInventoryVM)DataContext).MoveUp((InventoryItem)((Button)sender).CommandParameter);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ((NewInventoryVM)DataContext).MoveDown((InventoryItem)((Button)sender).CommandParameter);

        }

        private void dataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            HideArrowColumn();
        }
    }
}
