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

         List<ViewPlan> viewPlans = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().Cast<ViewPlan>().ToList();
         List<ViewFamilyType> viewFamilyTypes = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList();
         var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();
         TypeList = new ObservableCollection<ViewFamilyType>(viewFamilyTypes.Where(a => a.ViewFamily == ViewFamily.Elevation));
         ViewList = new ObservableCollection<ViewPlan>(viewPlans);
         // Load settings
         SelectedType = TypeList.FirstOrDefault(x => x.Name == Properties.Settings.Default.ViewType);
         IsFloorPlanCreationChecked = Properties.Settings.Default.IsFloorPlanCreationChecked;
         IsRoomNumberAdded = Properties.Settings.Default.IsRoomNumberAdded;
         UseCurrentView = Properties.Settings.Default.UseCurrentView;
         UseAllViews = Properties.Settings.Default.UseAllViews;
         SelectFromList = Properties.Settings.Default.SelectFromList;
         SaveCommand = new RelayCommand(P => SaveSettings(P));
      }
      #region Attributes

      private bool useCurrentView;
      public bool UseCurrentView
      {
         get { return useCurrentView; }
         set { useCurrentView = value; OnPropertyChanged(nameof(UseCurrentView)); }
      }
      private bool useAllViewsmyVar;

      public bool UseAllViews
      {
         get { return useAllViewsmyVar; }
         set { useAllViewsmyVar = value; OnPropertyChanged(nameof(UseAllViews)); }
      }

      private bool selectFromList;

      public bool SelectFromList
      {
         get { return selectFromList; }
         set { selectFromList = value; OnPropertyChanged(nameof(SelectFromList)); }
      }


      private bool _isChecked;

      private ViewFamilyType selectedType;
      #endregion
      #region Properties
      public ViewFamilyType SelectedType
      {
         get { return selectedType; }
         set { selectedType = value; OnPropertyChanged(nameof(selectedType)); }
      }


      private bool isFloorPlanCreationChecked = true;

      public bool IsFloorPlanCreationChecked
      {
         get { return isFloorPlanCreationChecked; }
         set { isFloorPlanCreationChecked = value; OnPropertyChanged(nameof(IsFloorPlanCreationChecked)); }
      }
      private bool isRoomNumberAdded = true;

      public bool IsRoomNumberAdded
      {
         get { return isRoomNumberAdded; }
         set { isRoomNumberAdded = value; OnPropertyChanged(nameof(IsRoomNumberAdded)); }
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

         Properties.Settings.Default.ViewType = SelectedType?.Name;
         Properties.Settings.Default.IsFloorPlanCreationChecked = IsFloorPlanCreationChecked;
         Properties.Settings.Default.IsRoomNumberAdded = IsRoomNumberAdded;
         Properties.Settings.Default.UseCurrentView = UseCurrentView;
         Properties.Settings.Default.UseAllViews = UseAllViews;
         Properties.Settings.Default.SelectFromList = SelectFromList;

         Properties.Settings.Default.Save();



      }

      #endregion

   }



}
