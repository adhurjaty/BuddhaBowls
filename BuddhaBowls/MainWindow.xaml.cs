using BuddhaBowls.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
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
        //private const string TEMP_TAB_NAME = "tempTab";
        private int _lastTabIdx = -1;
        //private Stack<TabItem> _tabStack;

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

        //public void AddTempTab(string headerName, UserControl userControl)
        //{
        //    TabControl tabs = Tabs;
        //    TabItem newTab = new TabItem() { Header = headerName, Content = userControl };

        //    _lastTabIdx = tabs.SelectedIndex;
        //    userControl.Tag = headerName;

        //    if(TempTabVM.TabStack == null)
        //    {
        //        TempTabVM.TabStack = new List<UserControl>();
        //    }

        //    TempTabVM.TabStack.Insert(0, userControl);
        //    if(TempTabVM.TabStack.Count == 0)
        //    {
        //        tabs.Items.Add(newTab);
        //    }
        //    else
        //    {
        //        tabs.Items[tabs.Items.Count - 1] = newTab;
        //    }

        //    tabs.SelectedIndex = tabs.Items.Count - 1;

        //    //TabItem lastTab = (TabItem)tabs.Items[tabs.Items.Count - 1];
        //    //if (lastTab.Name == TEMP_TAB_NAME)
        //    //{
        //    //    if(_tabStack == null)
        //    //    {
        //    //        _tabStack = new Stack<TabItem>();
        //    //    }

        //    //    DeleteTempTab();
        //    //    _tabStack.Push(lastTab);
        //    //}
        //}

        //public void DeleteTempTab()
        //{
        //    TabControl tabs = Tabs;

        //    TabItem lastTab = (TabItem)tabs.Items[tabs.Items.Count - 1];
        //    if (lastTab.Name == TEMP_TAB_NAME)
        //    {
        //        tabs.Items.RemoveAt(tabs.Items.Count - 1);
        //    }

        //    if(_tabStack != null && _tabStack.Count > 0)
        //    {
        //        tabs.Items.Add(_tabStack.Pop());
        //        tabs.SelectedIndex = tabs.Items.Count - 1;
        //    }
        //    else if(_lastTabIdx != -1)
        //        tabs.SelectedIndex = _lastTabIdx;
        //}

        public void AppendTempTab(UserControl tabControl)
        {
            _lastTabIdx = Tabs.SelectedIndex;
            TabItem newTab = new TabItem() { Header = (string)tabControl.Tag, Content = tabControl };
            Tabs.Items.Add(newTab);
            Tabs.SelectedIndex = Tabs.Items.Count - 1;
        }

        public void RemoveTempTab()
        {
            Tabs.Items.RemoveAt(Tabs.Items.Count - 1);
            Tabs.SelectedIndex = _lastTabIdx;
        }

        public void ReplaceTempTab(UserControl tabControl)
        {
            _lastTabIdx = Tabs.SelectedIndex;
            TabItem newTab = new TabItem() { Header = (string)tabControl.Tag, Content = tabControl };
            Tabs.Items[Tabs.Items.Count - 1] = newTab;
            Tabs.SelectedIndex = Tabs.Items.Count - 1;
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
