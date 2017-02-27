using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class WizardVM : TempTabVM
    {
        protected int _currentStep = 0;

        #region Content Binders

        private UserControl _wizardStepControl;
        public UserControl WizardStepControl
        {
            get
            {
                return _wizardStepControl;
            }
            set
            {
                _wizardStepControl = value;
                NotifyPropertyChanged("WizardStepControl");
            }
        }

        private List<string> _categoryList;
        public List<string> CategoryList
        {
            get
            {
                return _categoryList;
            }
            set
            {
                _categoryList = value;
                NotifyPropertyChanged("CategoryList");
            }
        }

        private List<string> _countUnitsList;
        public List<string> CountUnitsList
        {
            get
            {
                return _countUnitsList;
            }
            set
            {
                _countUnitsList = value;
                NotifyPropertyChanged("CountUnitsList");
            }
        }

        private List<string> _recipeUnitsList;
        public List<string> RecipeUnitsList
        {
            get
            {
                return _recipeUnitsList;
            }
            set
            {
                _recipeUnitsList = value;
                NotifyPropertyChanged("RecipeUnitsList");
            }
        }

        private Visibility _nextVisibility = Visibility.Visible;
        public Visibility NextVisibility
        {
            get
            {
                return _nextVisibility;
            }
            set
            {
                _nextVisibility = value;
                _finishVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                NotifyPropertyChanged("FinishVisibility");
                NotifyPropertyChanged("NextVisibility");
            }
        }

        private Visibility _backVisibility = Visibility.Hidden;
        public Visibility BackVisibility
        {
            get
            {
                return _backVisibility;
            }
            set
            {
                _backVisibility = value;
                NotifyPropertyChanged("BackVisibility");
            }
        }

        private Visibility _finishVisibility = Visibility.Hidden;
        public Visibility FinishVisibility
        {
            get
            {
                return _finishVisibility;
            }
            set
            {
                _finishVisibility = value;
                _nextVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                NotifyPropertyChanged("NextVisibility");
                NotifyPropertyChanged("FinishVisibility");
            }
        }

        private List<string> _puchasedUnitsList;
        public List<string> PurchasedUnitsList
        {
            get
            {
                return _puchasedUnitsList;
            }
            set
            {
                _puchasedUnitsList = value;
                NotifyPropertyChanged("PurchasedUnitsList");
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                _errorMessage = value;
                NotifyPropertyChanged("ErrorMessage");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand CancelCommand { get; set; }
        public ICommand NextCommand { get; set; }
        public ICommand BackCommand { get; set; }
        public ICommand FinishCommand { get; set; }

        #endregion

        public WizardVM() : base()
        {
            _tabControl = new WizardControl(this);
            InitFirstPage();

            CancelCommand = new RelayCommand(Cancel);
            NextCommand = new RelayCommand(NextPage);
            BackCommand = new RelayCommand(GoBack);
            FinishCommand = new RelayCommand(FinishWizard);

            InitAutocompleteLists();
        }

        #region ICommand Helpers

        private void Cancel(object obj)
        {
            Close();
        }

        protected void NextPage(object obj)
        {
            if (ValidateInputs())
            {
                _currentStep++;
                ErrorMessage = "";
                SetWizardStep();
            }
            else
            {
                DisplayErrorMessage();
            }
        }

        protected void GoBack(object obj)
        {
            _currentStep--;
            FinishVisibility = Visibility.Hidden;
            SetWizardStep();
        }

        protected virtual void FinishWizard(object obj)
        {
            Close();
        }

        #endregion

        #region Initializers

        protected void InitFirstPage()
        {
            _currentStep = 0;
            SetWizardStep();
        }

        private void InitAutocompleteLists()
        {
            CategoryList = _models.ItemCategories.OrderBy(x => x).ToList();
            CountUnitsList = _models.GetCountUnits();
            RecipeUnitsList = _models.GetRecipeUnits();
            PurchasedUnitsList = _models.GetPurchasedUnits();
        }

        #endregion

        #region Update UI

        protected virtual void SetWizardStep()
        {

        }

        protected virtual void DisplayErrorMessage()
        {
            ErrorMessage = "There is a problem with the info provided. Make sure to fill in all of the values";
        }

        #endregion

        protected virtual bool ValidateInputs()
        {
            return true;
        }
    }
}
