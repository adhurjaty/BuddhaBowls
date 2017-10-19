using BuddhaBowls.Helpers;
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

        public void AppendTempTab(UserControl tabControl)
        {
            Logger.Info("Adding temp tab: " + tabControl.DataContext.GetType().Name);
            _lastTabIdx = Tabs.SelectedIndex;
            TabItem newTab = new TabItem() { Header = (string)tabControl.Tag, Content = tabControl };
            Tabs.Items.Add(newTab);
            Tabs.SelectedIndex = Tabs.Items.Count - 1;
        }

        public void RemoveTempTab()
        {
            Logger.Info("Removing temp tab");
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

        public void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem t = (TabItem)((TabControl)sender).SelectedItem;
            if(t != null)
                Logger.Info("Changed to tab: " + t.Header);
            e.Handled = true;
        }
    }
}
