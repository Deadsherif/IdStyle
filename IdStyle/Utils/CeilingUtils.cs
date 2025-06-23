using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.Utils
{
    public class CeilingUtils
    {
        public static void CreateCeilingFromBoundary(Document doc, List<Curve> boundary,string ceilingTypeName, ElementId levelId, double height)
        {
            var internalHeight = UnitUtils.ConvertToInternalUnits(height, UnitTypeId.Millimeters);
            // Elevate boundary to ceiling height

            CurveLoop loop = new CurveLoop();

            foreach (Curve curve in boundary)
            {
                loop.Append(curve);
            }

            // Get a ceiling type
            CeilingType ceilingType = new FilteredElementCollector(doc)
                .OfClass(typeof(CeilingType))
                .Cast<CeilingType>()
                .FirstOrDefault(x=>x.Name.Contains(ceilingTypeName));

            if (ceilingType == null)
            {
                TaskDialog.Show("Error", "No ceiling type found.");
                return;
            }

            // Create ceiling using new API
          var ceiling   = Ceiling.Create(doc, new List<CurveLoop> { loop }, ceilingType.Id, levelId);
           var heightPar =  ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
            if (heightPar != null)
                heightPar.Set(internalHeight);
        }
    }
}
