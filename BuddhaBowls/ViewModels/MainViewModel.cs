using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class MainViewModel : INotifyPropertyChanged
    {
        MainWindow _window;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Data Bindings
        private string _dataFileFolder;
        public string DataFileFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_dataFileFolder))
                    DataFileFolder = Properties.Settings.Default.DBLocation;
                return _dataFileFolder;
            }
            set
            {
                _dataFileFolder = value;
                NotifyPropertyChanged("DataFileFolder");
            }
        }
        #endregion

        #region ICommand Bindings
        public ICommand BrowseButtonCommand { get; set; }
        public ICommand ReportCommand { get; set; }

        public bool ReportCanExecute
        {
            get
            {
                return Directory.Exists(DataFileFolder);
            }
        }
        #endregion

        public MainViewModel()
        {
            BrowseButtonCommand = new RelayCommand(BrowseHelper);
            ReportCommand = new RelayCommand(ReportHelper, x => ReportCanExecute);
        }

        private void ReportHelper(object obj)
        {
            throw new NotImplementedException();
        }

        private void BrowseHelper(object obj)
        {
            throw new NotImplementedException();
        }

        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        public void SaveSettings()
        {
            // TODO: add settings to save

            Properties.Settings.Default.Save();
        }
    }
}
