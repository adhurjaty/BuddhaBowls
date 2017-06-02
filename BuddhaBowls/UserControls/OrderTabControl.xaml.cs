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

        private void dataGrid2_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            PurchaseOrder order = (PurchaseOrder)e.Row.Item;
            order.ReceivedDate = (DateTime)e.EditingElement.GetValue(DatePicker.DisplayDateProperty);
            order.Update();

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
            factory.SetBinding(BindingGroupProperty, bind);
            DataTemplate cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = factory;
            ReceivedColumn.CellTemplate = cellTemplate;
        }
    }
}
