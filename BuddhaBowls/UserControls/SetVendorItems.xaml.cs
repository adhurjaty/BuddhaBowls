using MahApps.Metro.Controls;
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
    /// Interaction logic for SetVendorItems.xaml
    /// </summary>
    public partial class SetVendorItems : UserControl
    {
        public SetVendorItems(SetVendorItemsVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        //public bool ShowModal(string message)
        //{
        //    MetroWindow window = (MetroWindow)Application.Current.MainWindow;
        //    window.ShowMessageAsync("What's up?", message, style: MessageDialogStyle.)
        //}

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((SetVendorItemsVM)DataContext).FilterVendorItems(textBox.Text);
        }
    }
}
