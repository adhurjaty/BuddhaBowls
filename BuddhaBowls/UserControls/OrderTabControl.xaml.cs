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

        private void dataGrid2_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (dataGrid2.SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= dataGrid2_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).RowEditEnding += dataGrid2_RowEditEnding;
                ((OrderTabVM)DataContext).UpdateRecDate((PurchaseOrder)e.Row.Item);
            }

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(TextBlock));
            Binding bind = new Binding("ReceivedDate");
            bind.Mode = BindingMode.OneWay;
            factory.SetBinding(TextBlock.TextProperty, bind);
            DataTemplate cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = factory;
            ReceivedColumn.CellTemplate = cellTemplate;
        }

        private void dataGrid2_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(DatePicker));
            Binding bind = new Binding("ReceivedDate");
            bind.Mode = BindingMode.TwoWay;
            factory.SetBinding(DatePicker.SelectedDateProperty, bind);
            factory.Name = "recDatePicker";
            DataTemplate cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = factory;
            ReceivedColumn.CellTemplate = cellTemplate;
        }

        private void Expander_Clicked(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        private void dataGrid2_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int debug = 1;
            //ScrollViewer scv = ((DataGrid)sender).;
            //scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            //e.Handled = true;
        }
    }
}
