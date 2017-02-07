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
    /// Interaction logic for InventoryTabControl.xaml
    /// </summary>
    public partial class InventoryTabControl : UserControl
    {
        public InventoryTabControl()
        {
            InitializeComponent();
        }

        private void dataGrid2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //IEnumerable<Inventory> invs = ((DataGrid)sender).ItemsSource.Cast<Inventory>();
            //if (dataGrid2.SelectedItems.Count > 2)
            //{
            List<Inventory> invs = new List<Inventory>();
            //((DataGridRow)dataGrid2.ItemContainerGenerator.ContainerFromIndex(0)).IsSelected = false;
            for (int i = 0; i < dataGrid2.SelectedItems.Count; i++)
            {
                invs.Add((Inventory)dataGrid2.SelectedItems[i]);
                //((DataGridRow)dataGrid2.ItemContainerGenerator.ContainerFromItem).IsSelected = false;
            }
            //}
            ((InventoryTabVM)DataContext).SelectedMultiInventories = invs.ToList();

        }
    }
}
