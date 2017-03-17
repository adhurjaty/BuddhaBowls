using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BuddhaBowls.UserControls
{
    public class MultiSelectDataGrid : DataGrid
    {
        public IList SelectedItemsList
        {
            get
            {
                return (IList)GetValue(SelectedItemsListProperty);
            }
            set
            {
                SetValue(SelectedItemsListProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectDataGrid), new PropertyMetadata(null));

        public MultiSelectDataGrid()
        {
            SelectionChanged += MultiSelectDataGrid_SelectionChanged;
        }

        private void MultiSelectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsList = SelectedItems;
        }
    }
}
