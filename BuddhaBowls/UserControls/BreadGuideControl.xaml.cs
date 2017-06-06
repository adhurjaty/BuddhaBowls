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
    /// Interaction logic for BreadGuideControl.xaml
    /// </summary>
    public partial class BreadGuideControl : UserControl
    {
        private string[] _typeHeaders = new string[] { "{0}-Par", "Buffer", "Beginning Inv", "Delivery", "Projected Order", "Walk In",
                                                       "Freezer Count", "Useage" };
        private string[] _breadDescProps = new string[] { "Par", "Buffer", "BeginInventory", "Delivery", "ProjectedOrder", "WalkIn",
                                                       "FreezerCount", "Useage" };
        private string[] _editableFields = new string[] { "BeginInventory", "Delivery", "FreezerCount" };

        private TextBlock _editingTextBlock;

        public BreadGuideControl()
        {
            InitializeComponent();
        }

        public BreadGuideControl(BreadGuideVM context)
        {
            DataContext = context;
        }

        public void SetBreadGrid(BreadOrder[] breadWeek, List<string> breadTypes)
        {
            SetDateGrid(breadWeek);
            SetOrderDetails(breadWeek);
            foreach (string breadType in breadTypes)
            {
                SetBreadType(breadType, breadWeek.Select(x => x.GetBreadDescriptor(breadType)).ToList());
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ((BreadGuideVM)DataContext).InitializeDataGrid(this);
        }

        private void SetDateGrid(BreadOrder[] breadWeek)
        {
            for (int i = 0; i < breadWeek.Length; i++)
            {
                TextBlock t = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center
                };

                Binding b = new Binding(string.Format("BreadOrderList[{0}].Date", i));
                b.Source = DataContext;
                b.StringFormat = "dd-MMM";
                BindingOperations.SetBinding(t, TextBlock.TextProperty, b);
                Grid.SetRow(t, 0);
                Grid.SetColumn(t, i + 1);
                date_grid.Children.Add(t);

                t = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center
                };

                b = new Binding(string.Format("BreadOrderList[{0}].Day", i));
                b.Source = DataContext;
                BindingOperations.SetBinding(t, TextBlock.TextProperty, b);
                Grid.SetRow(t, 1);
                Grid.SetColumn(t, i + 1);
                date_grid.Children.Add(t);
            }
        }

        private void SetOrderDetails(BreadOrder[] breadWeek)
        {
            for (int i = 0; i < breadWeek.Length; i++)
            {
                TextBlock t = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
                                                TextAlignment = TextAlignment.Center };
                Binding b = new Binding(string.Format("BreadOrderList[{0}].GrossSales", i));
                b.Source = DataContext;
                BindingOperations.SetBinding(t, TextBlock.TextProperty, b);
                Grid.SetRow(t, 0);
                Grid.SetColumn(t, i + 1);
                t.MouseLeftButtonUp += T_EditValue;
                bread_grid.Children.Add(t);

                t = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
                                      TextAlignment = TextAlignment.Center };
                b = new Binding(string.Format("BreadOrderList[{0}].SalesForecast", i));
                b.Source = DataContext;
                BindingOperations.SetBinding(t, TextBlock.TextProperty, b);
                Grid.SetRow(t, 1);
                Grid.SetColumn(t, i + 1);
                t.MouseLeftButtonUp += T_EditValue;
                bread_grid.Children.Add(t);
            }
        }

        private void SetBreadType(string name, List<BreadDescriptor> breadTypesWeek)
        {
            bread_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });
            int startRow = bread_grid.RowDefinitions.Count;

            Border spacer = new Border() { Background = Brushes.Gray };
            Grid.SetRow(spacer, startRow - 1);
            Grid.SetColumnSpan(spacer, 9);
            bread_grid.Children.Add(spacer);

            for (int i = 0; i < _typeHeaders.Length; i++)
            {
                string header = _typeHeaders[i];
                if (i == 0)
                    header = string.Format(header, name);
                bread_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(28) });
                Label headerLabel = new Label()
                {
                    Content = header,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(headerLabel, startRow + i);
                Grid.SetColumn(headerLabel, 0);
                bread_grid.Children.Add(headerLabel);

                for (int col = 0; col < breadTypesWeek.Count; col++)
                {
                    TextBlock t = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        TextAlignment = TextAlignment.Center
                    };

                    Binding binding = new Binding(string.Format("BreadOrderList[{0}].BreadDescDict[{1}].{2}", col, name, _breadDescProps[i]));
                    binding.Source = DataContext;
                    BindingOperations.SetBinding(t, TextBlock.TextProperty, binding);
                    Grid.SetRow(t, startRow + i);
                    Grid.SetColumn(t, col + 1);

                    if(_editableFields.Contains(_breadDescProps[i]))
                    {
                        t.MouseLeftButtonUp += T_EditValue;
                    }
                    bread_grid.Children.Add(t);
                }
            }
        }

        private void T_EditValue(object sender, MouseButtonEventArgs e)
        {
            TextBlock t = (TextBlock)sender;
            _editingTextBlock = t;

            Grid thisGrid = (Grid)t.Parent;
            thisGrid.Children.Remove(t);

            int row = Grid.GetRow(t);
            int col = Grid.GetColumn(t);
            Binding b = t.GetBindingExpression(TextBlock.TextProperty).ParentBinding;

            TextBox box = new TextBox()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Right
            };
            box.LostFocus += EditBox_LostFocus;
            box.KeyUp += EditBox_KeyUp;
            BindingOperations.SetBinding(box, TextBox.TextProperty, b);
            Grid.SetRow(box, row);
            Grid.SetColumn(box, col);

            thisGrid.Children.Add(box);
            box.Focus();
            box.SelectAll();
        }

        private void EditBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox box = (TextBox)sender;

            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                box.LostFocus -= EditBox_LostFocus;
                ExitEdit(box, (Grid)box.Parent);
            }
        }

        private void EditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ExitEdit((TextBox)sender, (Grid)((TextBox)sender).Parent);
        }

        private void ExitEdit(TextBox box, Grid grid)
        {
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            grid.Children.Remove(box);
            grid.Children.Add(_editingTextBlock);

            ((BreadGuideVM)DataContext).UpdateValue(Grid.GetColumn(box) - 1);
        }
    }
}
