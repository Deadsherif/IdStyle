using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using SheetCreation.Presentation.ViewModels;
using SheetCreation.Presentation.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCreation.Application.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SetupCommand : IExternalCommand
    { 
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            var uidoc = uiApp.ActiveUIDocument;
            var doc = uidoc.Document;
            View view = uidoc.ActiveView;
            MainPageViewModel viewModel = new MainPageViewModel(doc);
            var setupWindow =  MainWindow.CreateInstance(viewModel);
            setupWindow.Show(); 
            return Result.Succeeded;
        }
    }
}
