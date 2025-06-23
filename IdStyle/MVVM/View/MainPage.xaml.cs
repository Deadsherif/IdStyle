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

using IdStyle.MVVM.ViewModel;

using System.Threading;
using System.Reflection;
using System.IO;
using Path = System.IO.Path;

namespace IdStyle.MVVM.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; set; }
        private MainWindow _mainWindow;


        public MainPage(MainPageViewModel viewModel,MainWindow mainWindow)
        {
            //LocalizeDictionary.Instance.Culture.ClearCachedData();
            //LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo("it"); 
            InitializeComponent();
            DataContext = viewModel;
            _mainWindow = mainWindow;
        }

       

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Close();
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            int position = args.Name.IndexOf(",");
            if (position > -1)
            {
                try
                {
                    string assemblyName = args.Name.Substring(0, position);
                    string assemblyFullPath = string.Empty;

                    //look in main folder
                    assemblyFullPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + assemblyName + ".dll";
                    if (File.Exists(assemblyFullPath))
                        return Assembly.LoadFrom(assemblyFullPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            return null;
        }


    }
}
