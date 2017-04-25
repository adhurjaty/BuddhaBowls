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

        //public void VendorList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        //{
        //    ((VendorTabVM)DataContext).AlterVendorCanExecute = true;
        //}
    }
}
