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
    /// Interaction logic for RecipeTabControl.xaml
    /// </summary>
    public partial class RecipeTabControl : UserControl
    {
        public RecipeTabControl(RecipeTabVM context)
        {
            InitializeComponent();
            DataContext = context;
        }

        private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = FilterItemBox;

            ((RecipeTabVM)DataContext).FilterItems(textBox.Text);
        }

        private void dataGrid1_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem != null)
            {
                ((DataGrid)sender).RowEditEnding -= dataGrid1_RowEditEnding;
                ((DataGrid)sender).CommitEdit();
                ((DataGrid)sender).RowEditEnding += dataGrid1_RowEditEnding;
                ((RecipeTabVM)DataContext).RowEdited(((DisplayRecipe)e.Row.Item));
            }
        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            DisplayRecipe item = (DisplayRecipe)((ComboBox)sender).DataContext;
            item.RecipeUnit = (string)((ComboBox)sender).SelectedItem;
            ((RecipeTabVM)DataContext).RowEdited(item);
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
    }
}
