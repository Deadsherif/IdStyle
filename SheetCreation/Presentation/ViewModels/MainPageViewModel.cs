using Autodesk.Revit.UI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using SheetCreation.Presentation.Commands;
using HandyControl.Controls;



namespace SheetCreation.Presentation.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(Document doc)
        {
          List<ViewPlan> viewPlans = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().Cast<ViewPlan>().ToList() ;
          List<ViewFamilyType> viewFamilyTypes = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList();
          var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();
          TypeList = new ObservableCollection<ViewFamilyType>(viewFamilyTypes.Where(a => a.ViewFamily == ViewFamily.Elevation));
          ViewList = new ObservableCollection<ViewPlan>( viewPlans);
          SaveCommand = new RelayCommand(P => SaveSettings(P));
        }
        #region Attributes
        private bool _isChecked;
        private bool isBusy = false;
        private ViewFamilyType selectedType;
        #endregion
        #region Properties
        public ViewFamilyType SelectedType
        {
            get { return selectedType; }
            set { selectedType = value; OnPropertyChanged(nameof(selectedType)); }
        }


        private ObservableCollection<ViewPlan> viewList;
        public ObservableCollection<ViewPlan> ViewList
        {
            get { return viewList; }
            set { viewList = value; OnPropertyChanged(nameof(ViewList)); }
        }


        private ObservableCollection<ViewFamilyType> typeList;
        public ObservableCollection<ViewFamilyType> TypeList
        {
            get { return typeList; }
            set { typeList = value; OnPropertyChanged(nameof(TypeList)); }
        }
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;

                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        // Commands
        public ICommand SaveCommand { get; }
        #endregion
        #region Functions
        public void SaveSettings(object parameter)
        {

            Growl.Success("✅ Settings saved!");

        }
        public void SetStatus(bool isBusy)
        {
            this.isBusy = isBusy;
        }
        #endregion

    }



}
