using System;
using System.Windows;

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

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
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

    }
}
