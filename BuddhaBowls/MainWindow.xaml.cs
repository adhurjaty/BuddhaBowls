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
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel mvm)
        {
            DataContext = mvm;
            InitializeComponent();
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
    }
}
