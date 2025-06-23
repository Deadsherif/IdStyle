using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IdStyle.MVVM.ViewModel
{
    public class RoomSelectionViewModel : ViewModelBase
    {
        private string _roomName;
        private bool _isPopupOpen;

        public string RoomName
        {
            get => _roomName;
            set { _roomName = value; OnPropertyChanged(nameof(RoomName)); }
        }

        public ObservableCollection<string> SelectedLevels { get; set; } = new ObservableCollection<string>();

        public Dictionary<string, bool> LevelSelectionMap { get; set; } = new Dictionary<string, bool>();

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set { _isPopupOpen = value; OnPropertyChanged(nameof(IsPopupOpen)); }
        }

        //public ICommand EditLevelsCommand { get; }

        public RoomSelectionViewModel(List<string> allLevels)
        {
            foreach (var lvl in allLevels)
            {
                LevelSelectionMap[lvl] = false;
            }

            //EditLevelsCommand = new RelayCommand(_ => TogglePopup());
            RemoveLevelCommand = new RelayCommand(param => RemoveLevel(param as string));
        }

    

        public void UpdateSelectedLevels()
        {
            SelectedLevels.Clear();
            foreach (var pair in LevelSelectionMap.Where(kvp => kvp.Value))
            {
                SelectedLevels.Add(pair.Key);
            }
        }
        public ICommand RemoveLevelCommand { get; }



        private void RemoveLevel(string level)
        {
            if (LevelSelectionMap.ContainsKey(level))
            {
                LevelSelectionMap[level] = false;
                UpdateSelectedLevels();
                OnPropertyChanged(nameof(LevelSelectionMap));
            }
        }
    }
}
