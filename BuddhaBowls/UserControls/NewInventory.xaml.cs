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
    }
}
