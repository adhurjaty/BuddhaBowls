using Squirrel;
using System;
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
            //using (var mgr = new UpdateManager("C:\\Projects\\MyApp\\Releases"))
            //{
            //    mgr.UpdateApp().Wait();
            //}
        }

        protected override void OnStartup(StartupEventArgs e)
        {
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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _mv.SaveSettings();
        }

        private void MainClass_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message + "\n" + e.Exception.StackTrace, "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Forms.Application.Restart();
            Current.Shutdown();
        }
    }
}
