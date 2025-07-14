using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SheetCreation.Application.Tab
{
    public class PanelBuilder
    {
        private readonly RibbonPanel _panel;

        public PanelBuilder(UIControlledApplication app, string tabName, string panelName)
        {
            _panel = GetOrCreatePanel(app, tabName, panelName);
        }

        private RibbonPanel GetOrCreatePanel(UIControlledApplication app, string tabName, string panelName)
        {
            foreach (var panel in app.GetRibbonPanels(tabName))
                if (panel.Name == panelName) return panel;

            return app.CreateRibbonPanel(tabName, panelName);
        }

        public PanelBuilder AddPushButton(string name, string text, string commandNamespace, Image icon)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var data = new PushButtonData(name, text, assemblyPath, commandNamespace);
            var button = _panel.AddItem(data) as PushButton;

            if (button != null)
            {
                var imageSource = ConvertToImageSource(icon);
                button.Image = imageSource;
                button.LargeImage = imageSource;
            }

            return this;
        }
        public static BitmapSource ConvertToImageSource(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                return bitmap;
            }
        }
    }
}
