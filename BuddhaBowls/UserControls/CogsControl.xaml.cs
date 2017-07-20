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
    /// Interaction logic for CogsControl.xaml
    /// </summary>
    public partial class CogsControl : UserControl
    {
        public CogsControl()
        {
            InitializeComponent();
        }

        public CogsControl(CogsVM context) : this()
        {
            DataContext = context;
        }

        //private void recOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    e.Handled = true;
        //    PurchaseOrder order = ((CatPO)((DataGrid)sender).SelectedItem).GetPO();
        //    ((ReportsTabVM)DataContext).RecOrderDouleClicked(order);
        //}

        //private void Expander_Clicked(object sender, RoutedEventArgs e)
        //{
        //    for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
        //        if (vis is DataGridRow)
        //        {
        //            var row = (DataGridRow)vis;
        //            row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        //            break;
        //        }
        //}
    }
}
