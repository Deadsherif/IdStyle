using Autodesk.Revit.UI;
using IdStyle.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using IdStyle.MVVM.Model;
using Style = IdStyle.MVVM.Model.Style;


namespace IdStyle.MVVM.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        #region Attributes
        private ExternalEvent ev;
        private ExternalEvent jsonev;

        private bool _isChecked;

        private bool isBusy = false;

        private Style selectedStyle;
        #endregion
        #region Properties


        public Style SelectedStyle
        {
            get { return selectedStyle; }
            set { selectedStyle = value; OnPropertyChanged(nameof(SelectedStyle)); }
        }
        private ObservableCollection<Style> styleList;

        public ObservableCollection<Style> StyleList
        {
            get { return styleList; }
            set { styleList = value; OnPropertyChanged(nameof(StyleList)); }
        }

        public ExternalEvent Ev
        {
            get { return ev; }
            set { ev = value; OnPropertyChanged(nameof(Ev)); }
        }
        public ExternalEvent JsonEv
        {
            get { return jsonev; }
            set { jsonev = value; OnPropertyChanged(nameof(JsonEv)); }
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
        // All available Revit rooms
        public List<string> AllRooms { get; set; } = new List<string>();

        // All available Revit levels
        public List<string> AllLevels { get; set; } = new List<string>();

        // Room Cards Collection
        public ObservableCollection<RoomSelectionViewModel> RoomSelections { get; set; } = new ObservableCollection<RoomSelectionViewModel>();

        // Commands
        public ICommand AddRoomCommand { get; }
        public ICommand RemoveRoomCommand { get; }
        public ICommand EditLevelsCommand { get; }

        public MainPageViewModel()

        {
            IdStyleCommand = new RelayCommand(P => RunExporter(P));
            AddRoomCommand = new RelayCommand(_ => AddRoom());
            RemoveRoomCommand = new RelayCommand(param => RemoveRoom(param as RoomSelectionViewModel));
            EditLevelsCommand = new RelayCommand(param => TogglePopup(param as RoomSelectionViewModel));
            // Dummy data for testing (replace with Revit API calls)
            AllRooms = new List<string> { "Room 101", "Room 102", "Room 103" };
            AllLevels = new List<string> { "Level 1", "Level 2", "Level 3" };

            RoomSelections = new ObservableCollection<RoomSelectionViewModel>();
        }
        private void TogglePopup(RoomSelectionViewModel room)
        {
            room.IsPopupOpen = !room.IsPopupOpen;

            room.UpdateSelectedLevels();
        }
        private void AddRoom()
        {
            var room = new RoomSelectionViewModel(AllLevels);
            RoomSelections.Add(room);
        }

        private void RemoveRoom(RoomSelectionViewModel room)
        {
            if (room != null && RoomSelections.Contains(room))
                RoomSelections.Remove(room);
        }


        // Collection for Units
        public ObservableCollection<ExportUnit> Units { get; private set; }

        // Collection for Resolutions

        #endregion
        #region Functions
        public ICommand IdStyleCommand { get; }
        public ICommand NavigateCommand { get; }
 

        public void RunExporter(object parameter)
        {



            Ev.Raise();


        }

        public void SetStatus(bool isBusy)
        {
            this.isBusy = isBusy;
        }



        #endregion

    }



}
