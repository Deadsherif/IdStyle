using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCreation.Application.Tab
{
    public class RibbonBuilder
    {
        private readonly UIControlledApplication _app;
        private readonly string _tabName;
        private readonly List<PanelBuilder> _panels = new List<PanelBuilder>();

        public RibbonBuilder(UIControlledApplication app, string tabName)
        {
            _app = app;
            _tabName = tabName;

            try { _app.CreateRibbonTab(_tabName); } catch { } // Create or skip
        }

        public PanelBuilder AddPanel(string panelName)
        {
            var panel = new PanelBuilder(_app, _tabName, panelName);
            _panels.Add(panel);
            return panel;
        }
    }
}

