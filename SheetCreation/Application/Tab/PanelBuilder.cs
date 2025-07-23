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

        public PanelBuilder AddPushButton(string name, string text, string commandNamespace, string iconPath32, string iconPath16)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var data = new PushButtonData(name, text, assemblyPath, commandNamespace);
            var button = _panel.AddItem(data) as PushButton;


               var image32 = Image.FromFile(iconPath32);// Load image from path
               var image16 = Image.FromFile(iconPath16);// Load image from path

               var imageSource32 = ConvertToImageSource(image32); // Convert to BitmapSource
               var imageSource16 = ConvertToImageSource(image16); // Convert to BitmapSource

         button.LargeImage = imageSource32;
               button.Image = imageSource16;
            
         

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
