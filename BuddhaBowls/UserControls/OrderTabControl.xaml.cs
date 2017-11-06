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
    /// Interaction logic for OrderTabControl.xaml
    /// </summary>
    public partial class OrderTabControl : UserControl
    {
        public OrderTabControl()
        {
            InitializeComponent();
        }

        public OrderTabControl(OrderTabVM viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void Expander_Clicked(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
            {
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
            }
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (dataGrid2.SelectedItem != null && e.Row != null || (dataGrid1.SelectedItem != null && e.Column.Header.ToString() == "Order Date"))
            {
                ((DataGrid)sender).CellEditEnding -= dataGrid_CellEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).CellEditEnding += dataGrid_CellEditEnding;
                ((OrderTabVM)DataContext).RecOrderChanged((PurchaseOrder)e.Row.Item);
            }
        }
    }
}
