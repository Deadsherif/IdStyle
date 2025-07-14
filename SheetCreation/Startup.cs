using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCreation
{
    internal class Startup
    {
        private readonly UIApplication _uiApp;
        public Startup(UIApplication uiApp)
        {
              _uiApp = uiApp;
        }
        public void Run()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_uiApp.Application);
            services.AddSingleton(_uiApp.ActiveUIDocument.Document);
        }



    }
}