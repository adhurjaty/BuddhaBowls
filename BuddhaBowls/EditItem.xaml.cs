using System.Windows.Controls;

namespace BuddhaBowls
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

        private void FieldValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((MainViewModel)DataContext).ClearErrors();
        }
    }
}
