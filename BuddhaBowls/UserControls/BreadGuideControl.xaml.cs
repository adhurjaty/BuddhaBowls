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
        private string[] _typeHeaders = new string[] { "Kitchen Rack", "Freezer Count", "Delivery", "Backup", "{0}-Par", "Buffer",
                                                       "Projected Order", "Freezer Adj", "Useage" };
        private string[] _breadDescProps = new string[] { "BeginInventory", "FreezerCount", "Delivery", "Backup", "Par", "Buffer",
                                                          "ProjectedOrder", "WalkIn", "Useage" };
        private string[] _editableFields = new string[] { "BeginInventory", "Delivery", "Backup", "FreezerCount" };

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
            ClearDateGrid();
            ClearBreadGrid();

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
                TextBlock t = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center,
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
            bread_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(28) });
            int startRow = bread_grid.RowDefinitions.Count;

            Label typeLabel = new Label()
            {
                Content = name,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            Grid.SetRow(typeLabel, startRow - 1);
            Grid.SetColumnSpan(typeLabel, 9);
            bread_grid.Children.Add(typeLabel);

            for (int i = 0; i < _typeHeaders.Length; i++)
            {
                string header = _typeHeaders[i];
                if (_typeHeaders[i].Contains("{0}"))
                    header = string.Format(header, name);

                bool projected = _typeHeaders[i].Contains("Projected");
                bool editable = _editableFields.Contains(_breadDescProps[i]);
                bool freezerAdj = _breadDescProps[i] == "WalkIn";

                bread_grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(28) });
                Label headerLabel = new Label()
                {
                    Content = header,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                if (projected)
                    headerLabel.FontWeight = FontWeights.Bold;
                Grid.SetRow(headerLabel, startRow + i);
                Grid.SetColumn(headerLabel, 0);
                bread_grid.Children.Add(headerLabel);

                for (int col = 0; col < breadTypesWeek.Count; col++)
                {
                    TextBlock t = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };

                    if (projected)
                        t.FontWeight = FontWeights.Bold;

                    Binding binding = new Binding(string.Format("BreadOrderList[{0}].BreadDescDict[{1}].{2}", col, name, _breadDescProps[i]));
                    binding.Source = DataContext;
                    BindingOperations.SetBinding(t, TextBlock.TextProperty, binding);

                    if(freezerAdj)
                    {
                        Border colorBord = new Border()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                        Grid.SetRow(colorBord, startRow + i);
                        Grid.SetColumn(colorBord, col + 1);
                        Binding colorBind = new Binding(string.Format("BreadOrderList[{0}].BreadDescDict[{1}].WalkInColor", col, name));
                        colorBind.Source = DataContext;
                        BindingOperations.SetBinding(colorBord, Border.BackgroundProperty, colorBind);
                        bread_grid.Children.Add(colorBord);
                    }

                    Grid.SetRow(t, startRow + i);
                    Grid.SetColumn(t, col + 1);

                    if(editable)
                    {
                        Border bord = new Border()
                        {
                            Background = Brushes.LightGray,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        Grid.SetRow(bord, startRow + i);
                        Grid.SetColumn(bord, col + 1);
                        bread_grid.Children.Add(bord);
                        t.MouseLeftButtonUp += T_EditValue;
                    }
                    bread_grid.Children.Add(t);
                }
            }
        }

        private void ClearDateGrid()
        {
            for (int i = date_grid.Children.Count - 1; i >= 0; i--)
            {
                if (Grid.GetColumn(date_grid.Children[i]) > 0)
                    date_grid.Children.RemoveAt(i);
            }
        }

        private void ClearBreadGrid()
        {
            int startIdx = bread_grid.RowDefinitions.IndexOf(bread_grid.RowDefinitions.First(x => x.Name == "bottom_fixed"));
            for (int i = bread_grid.Children.Count - 1; i >= 0; i--)
            {
                if (Grid.GetColumn(bread_grid.Children[i]) > 0 || Grid.GetRow(bread_grid.Children[i]) > startIdx)
                    bread_grid.Children.RemoveAt(i);
            }

            if (startIdx + 1 < bread_grid.RowDefinitions.Count)
                bread_grid.RowDefinitions.RemoveRange(startIdx + 1, bread_grid.RowDefinitions.Count - (startIdx + 1));
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            int outInt = 0;
            if (int.TryParse(box.Text, out outInt))
            {
                box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                for (int i = 0; i < 8; i++)
                {
                    ((BreadGuideVM)DataContext).UpdateValue(i);
                }
            }
            else if(box.Text.Length > 0)
            {
                box.Text = box.Text.Substring(0, box.Text.Length - 1);
            }
        }
    }
}
