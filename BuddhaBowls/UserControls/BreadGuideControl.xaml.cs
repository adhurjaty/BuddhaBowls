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
    /// Interaction logic for BreadGuideControl.xaml
    /// </summary>
    public partial class BreadGuideControl : UserControl
    {
        public BreadGuideControl()
        {
            InitializeComponent();
        }

        public BreadGuideControl(BreadGuideVM context)
        {
            DataContext = context;
        }

        private void c_dataGridScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Add MouseWheel support for the datagrid scrollviewer.
            c_stackPanel.AddHandler(MouseWheelEvent, new RoutedEventHandler(DataGridMouseWheelHorizontal), true);
        }

        private void DataGridMouseWheelHorizontal(object sender, RoutedEventArgs e)
        {
            MouseWheelEventArgs eargs = (MouseWheelEventArgs)e;
            double x = (double)eargs.Delta;
            double y = c_dataGridScrollViewer.VerticalOffset;
            c_dataGridScrollViewer.ScrollToVerticalOffset(y - x);
        }

        private void c_dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RotateDataGridValues(ref c_dataGrid);
        }

        private void type_dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            RotateDataGridValues(ref grid);
        }
        //private void ItemsControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    for (int i = 0; i < bread_ItemsControl.Items.Count; i++)
        //    {
        //        var item = bread_ItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
        //        //RotateDataGridValues(ref grid);
        //    }
        //}

        private void RotateDataGridValues(ref DataGrid grid)
        {
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new RotateTransform(90));
            foreach (DataGridColumn dataGridColumn in grid.Columns)
            {
                if (dataGridColumn is DataGridTextColumn)
                {
                    DataGridTextColumn dataGridTextColumn = dataGridColumn as DataGridTextColumn;

                    Style style = new Style(dataGridTextColumn.ElementStyle.TargetType, dataGridTextColumn.ElementStyle.BasedOn);
                    style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 2, 0, 2)));
                    style.Setters.Add(new Setter(TextBlock.LayoutTransformProperty, transformGroup));
                    style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));

                    Style editingStyle = new Style(dataGridTextColumn.EditingElementStyle.TargetType, dataGridTextColumn.EditingElementStyle.BasedOn);
                    editingStyle.Setters.Add(new Setter(TextBox.MarginProperty, new Thickness(0, 2, 0, 2)));
                    editingStyle.Setters.Add(new Setter(TextBox.LayoutTransformProperty, transformGroup));
                    editingStyle.Setters.Add(new Setter(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));

                    dataGridTextColumn.ElementStyle = style;
                    dataGridTextColumn.EditingElementStyle = editingStyle;
                }
            }
            List<DataGridColumn> dataGridColumns = new List<DataGridColumn>();
            foreach (DataGridColumn dataGridColumn in grid.Columns)
            {
                dataGridColumns.Add(dataGridColumn);
            }
            grid.Columns.Clear();
            dataGridColumns.Reverse();
            foreach (DataGridColumn dataGridColumn in dataGridColumns)
            {
                grid.Columns.Add(dataGridColumn);
            }
        }
    }
}
