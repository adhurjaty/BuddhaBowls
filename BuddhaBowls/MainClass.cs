using BuddhaBowls.Helpers;
using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace BuddhaBowls
{
    public partial class MainClass : Application
    {
        private MainViewModel _mv;
        private MainWindow _mainWindow;

        //[STAThread]
        //static void Main()
        //{
        //    MainClass mainClass = new MainClass();
        //    mainClass.Run();
        //}

        public MainClass()
        {
            string procName = Process.GetCurrentProcess().ProcessName;

            // get the list of all processes by the "procName"       
            //Process[] processes = Process.GetProcessesByName(procName);

            //if (processes.Length > 1)
            //{
            //    MessageBox.Show(procName + " already running");
            //    Current.Shutdown();
            //}
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //temporary: make sure that the Logs folder exists, create if not
            Logger.Info("Application starting");
            base.OnStartup(e);
            DispatcherUnhandledException += MainClass_DispatcherUnhandledException;

            Show();
        }

        private void Show()
        {
            _mv = new MainViewModel();
            _mainWindow = new MainWindow(_mv);
            _mv.InitializeWindow(_mainWindow);
            _mainWindow.Show();

            // set tab switching event handler after the tabs have been set up
            _mainWindow.Tabs.SelectionChanged += _mainWindow.Tabs_SelectionChanged;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application closing");
            base.OnExit(e);

            _mv.SaveSettings();
        }

        private void MainClass_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception.Message + "\n" + e.Exception.StackTrace);
            MessageBox.Show(e.Exception.Message + "\n" + e.Exception.StackTrace, "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Forms.Application.Restart();
            Current.Shutdown();
        }
    }
}
