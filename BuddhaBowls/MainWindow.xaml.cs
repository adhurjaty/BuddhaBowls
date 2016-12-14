using BuddhaBowls.Models;
using MahApps.Metro.Controls;
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

namespace BuddhaBowls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private const string TEMP_TAB_NAME = "tempTab";
        private int _lastTab = -1;

        public MainWindow(MainViewModel mvm)
        {
            DataContext = mvm;
            InitializeComponent();

            // put settings back in when it is clear what GUI will look like
            //Left = Properties.Settings.Default.WindowLocation.X;
            //Top = Properties.Settings.Default.WindowLocation.Y;
            //Height = Properties.Settings.Default.WindowSize.Height;
            //Width = Properties.Settings.Default.WindowSize.Width;

            //if ((WindowState)Properties.Settings.Default.windowState == WindowState.Minimized)
            //{
            //    WindowState = WindowState.Normal;
            //}
            //else
            //{
            //    WindowState = (WindowState)Properties.Settings.Default.windowState;
            //}
        }

        public void AddTempTab(string headerName, UserControl userControl)
        {
            TabControl tabs = Tabs;

            DeleteTempTab();
            TabItem newTab = new TabItem() { Header = headerName, Name = TEMP_TAB_NAME };

            newTab.Content = userControl;

            _lastTab = tabs.SelectedIndex;
            tabs.Items.Add(newTab);
            tabs.SelectedIndex = tabs.Items.Count - 1;
        }

        internal void DeleteTempTab()
        {
            TabControl tabs = Tabs;

            TabItem lastTab = (TabItem)tabs.Items[tabs.Items.Count - 1];
            if (lastTab.Name == TEMP_TAB_NAME)
            {
                tabs.Items.RemoveAt(tabs.Items.Count - 1);
            }

            if(_lastTab != -1)
                tabs.SelectedIndex = _lastTab;
        }

        //private void FilterItems_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox textBox = FilterItemBox;

        //    ((MainViewModel)DataContext).FilterInventoryItems(textBox.Text);
        //}

        //private void MasterList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        //{
        //    SetBlankToZero((TextBox)e.EditingElement);
        //    ((MainViewModel)DataContext).InventoryItemCountChanged();
        //}

        //private void SetBlankToZero(TextBox tb)
        //{
        //    if (string.IsNullOrWhiteSpace(tb.Text))
        //        tb.Text = "0";
        //}

        //private void FilterOrderItems_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox textBox = FilterOrderItemBox;

        //    ((MainViewModel)DataContext).FilterOrderItems(textBox.Text);
        //}
    }
}
