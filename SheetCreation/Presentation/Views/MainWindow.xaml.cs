using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SheetCreation.Presentation.ViewModels;

using System.Threading;
using System.Reflection;
using System.IO;
using Path = System.IO.Path;

namespace SheetCreation.Presentation.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainPageViewModel mainPageViewModel { get; set; }
        public static MainWindow instance { get; set; }
        public bool IsClosed { get; private set; }
        public MainWindow(MainPageViewModel viewModel)
        {
            InitializeComponent();
            mainPageViewModel = viewModel;
            DataContext = mainPageViewModel;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            IsClosed = true;
        }
        public static MainWindow CreateInstance(MainPageViewModel viewModel)
        {
            if (instance == null || instance.IsClosed)
                instance = new MainWindow(viewModel);
            else
                instance.Activate();

            return instance;
        }
        protected override void OnClosed(EventArgs e) => IsClosed = true;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
