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
    /// Interaction logic for ViewOrderTabControl.xaml
    /// </summary>
    public partial class ViewOrderTabControl : UserControl
    {
        public ViewOrderTabControl()
        {
            InitializeComponent();
        }

        public ViewOrderTabControl(ViewOrderVM context) : this()
        {
            DataContext = context;
        }

        private void OpenOrder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //((ViewOrderVM)DataContext).ClearSelectedItems();
            ((OrderBreakdownVM)((UserControl)sender).DataContext).ClearSelectedItems();
        }

        private void ReceivedOrder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //((ViewOrderVM)DataContext).ClearSelectedItems();
            ((OrderBreakdownVM)((UserControl)sender).DataContext).ClearSelectedItems();
        }
    }
}
