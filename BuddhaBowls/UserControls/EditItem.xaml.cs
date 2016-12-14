using System.Windows.Controls;

namespace BuddhaBowls.UserControls
{
    /// <summary>
    /// Interaction logic for EditItem.xaml
    /// </summary>
    public partial class EditItem : UserControl
    {
        public EditItem()
        {
            InitializeComponent();
        }

        public EditItem(object context) : this()
        {
            DataContext = context;
        }

        private void FieldValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((InventoryTabVM)DataContext).ClearErrors();
        }
    }
}
