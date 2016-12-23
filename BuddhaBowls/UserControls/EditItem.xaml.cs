using System.Windows.Controls;

namespace BuddhaBowls.UserControls
{
    public delegate void ClearVMErrors();
    /// <summary>
    /// Interaction logic for EditItem.xaml
    /// </summary>
    public partial class EditItem : UserControl
    {
        ClearVMErrors Clear;

        public EditItem()
        {
            InitializeComponent();
        }

        public EditItem(object context, ClearVMErrors clearErrors) : this()
        {
            DataContext = context;
            Clear = clearErrors;
        }

        private void FieldValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(Clear != null)
                Clear();
        }
    }
}
