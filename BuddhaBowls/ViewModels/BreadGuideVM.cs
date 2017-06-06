using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        #region Content Binders

        private ObservableCollection<BreadOrder> _breadOrderList;
        public ObservableCollection<BreadOrder> BreadOrderList
        {
            get
            {
                return _breadOrderList;
            }
            set
            {
                _breadOrderList = value;
                NotifyPropertyChanged("BreadOrderList");
                NotifyPropertyChanged("BreadDescriptors");
            }
        }

        private ObservableCollection<BreadDescType> _breadDescriptors;
        public ObservableCollection<BreadDescType> BreadDescriptors
        {
            get
            {
                if(_breadDescriptors == null && BreadOrderList != null)
                {
                    InitBreadDescriptors();
                }
                return _breadDescriptors;
            }
            set
            {
                _breadDescriptors = value;
                NotifyPropertyChanged("BreadDescriptors");
            }
        }

        #endregion

        #region ICommand and CanExecute

        #endregion

        public BreadGuideVM() : base()
        {
            BreadOrderList = new ObservableCollection<BreadOrder>(_models.BreadWeek.Where(x => x != null).ToList());
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        private void InitBreadDescriptors()
        {
            _breadDescriptors = new ObservableCollection<BreadDescType>();
            foreach (string breadType in _models.InventoryItems.Where(x => x.Category == "Bread").Select(x => x.Name))
            {
                List<BreadDescriptor> descList = BreadOrderList.Where(x => x.BreadDescDict != null).Select(x => x.BreadDescDict[breadType]).ToList();
                for (int i = 0; i < 8 - descList.Count; i++)
                {
                    descList.Add(new BreadDescriptor(BreadOrderList[i]));
                }
                _breadDescriptors.Add(new BreadDescType(breadType, descList));
            }
        }

        #endregion

        #region UI Updaters

        #endregion
    }

    public class BreadDescType : INotifyPropertyChanged
    {

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }

        private ObservableCollection<BreadDescriptor> _breadTypeList;
        public ObservableCollection<BreadDescriptor> BreadTypeList
        {
            get
            {
                return _breadTypeList;
            }
            set
            {
                _breadTypeList = value;
                NotifyPropertyChanged("BreadTypeList");
            }
        }

        public BreadDescType(string name, List<BreadDescriptor> breadTypeWeek)
        {
            Name = name;
            BreadTypeList = new ObservableCollection<BreadDescriptor>(breadTypeWeek);
        }
    }
}
