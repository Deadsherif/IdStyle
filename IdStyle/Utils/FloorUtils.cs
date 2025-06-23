using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.Utils
{
    public class FloorUtils
    {

        public static void CreateFloorFromBoundary(Document doc, List<Curve> boundary,string floorTypeName, ElementId levelId)
        {
            // Create curve loop
            CurveLoop loop = new CurveLoop();
            foreach (Curve curve in boundary)
            {
                loop.Append(curve);
            }

            // Get a floor type
            FloorType floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .FirstOrDefault(x=>x.Name.Contains(floorTypeName));

            if (floorType == null)
            {
                TaskDialog.Show("Error", "No floor type found.");
                return;
            }

            // Create floor using new API
            Floor.Create(doc, new List<CurveLoop> { loop }, floorType.Id, levelId);
        }

    }
}
