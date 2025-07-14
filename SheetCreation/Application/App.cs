using Autodesk.Revit.UI;
using SheetCreation.Application.Tab;

using System;
using System.IO;

namespace SheetCreation
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            var builder = new RibbonBuilder(application, "Interior");

            builder.AddPanel("Elevations")
                   .AddPushButton("Generate", "Generate", "SheetCreation.Application.Commands.ElevationsCommand", Properties.Resources.elevator);
            builder.AddPanel("Elevations")
                   .AddPushButton("Setup", "Setup", "SheetCreation.Application.Commands.SetupCommand", Properties.Resources.setup);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        private System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];
            var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), name + ".dll");

            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        }
    }
}
