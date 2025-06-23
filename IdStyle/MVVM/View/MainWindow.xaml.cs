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
    public partial class MainWindow : Window
    {
        public MainPageViewModel mainPageViewModel { get; set; }
        public static MainWindow instance { get; set; }
        public bool IsClosed { get; private set; }
        public MainWindow(MainPageViewModel viewModel)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolve);

            //LocalizeDictionary.Instance.Culture.ClearCachedData();
            //LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo("it"); 
            InitializeComponent();
            mainPageViewModel = viewModel;
            DataContext = mainPageViewModel;
            //Loaded += MainWindow_Loaded;
            // Navigate to the LoginPage on application startup
            NavigateToMainPage();

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
        // This method can be used for navigating from LoginPage to MainPage
        public void NavigateToMainPage()
        {
            MainFrame.Navigate(new MainPage(mainPageViewModel,this));
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
