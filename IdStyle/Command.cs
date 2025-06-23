using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IdStyle.EVHandler;
using IdStyle.MVVM.Model;
using IdStyle.MVVM.View;
using IdStyle.MVVM.ViewModel;
using IdStyle.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle
{
    [Transaction(TransactionMode.Manual)]
    internal class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //export stl 
            IdStyleExternalEventHandler IdStyleExternalEventHandler = new IdStyleExternalEventHandler();
            var ev = ExternalEvent.Create(IdStyleExternalEventHandler);

            List<Style> styles = ExcelUtils.GetStylesFromExcel();

            MainPageViewModel mainPageViewModel = new MainPageViewModel();
            mainPageViewModel.StyleList = new ObservableCollection<Style>(styles);
            var ui = MainWindow.CreateInstance(mainPageViewModel);

            IdStyleExternalEventHandler.MainViewModel = mainPageViewModel;

            mainPageViewModel.Ev = ev;


            ui.Show();
            return Result.Succeeded;
        }
    }
}
