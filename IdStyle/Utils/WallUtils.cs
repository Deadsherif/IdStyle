using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using IdStyle.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.Utils
{
    public class WallUtils
    {
        public static void CreateWalls(Document doc, List<Curve> boundary, RoomData roomData, Room room)
        {
            ElementId levelId = room.LevelId;
            WallType wallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name.Contains(roomData.WallType));

            if (wallType == null)
            {
                TaskDialog.Show("Error", $"Wall type {roomData.WallType} not found.");
                return;
            }

            double wallHeight = UnitUtils.ConvertToInternalUnits(roomData.CeilingHeights.Max() + 100, UnitTypeId.Millimeters);

            foreach (Curve curve in boundary)
            {
                // Create wall along boundary curve
                Wall wall = Wall.Create(doc, curve, wallType.Id, levelId, wallHeight, 0.0, false, false);
                wall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set(0); // Location line: Wall Centerline

                // Get direction of curve and compute normal vector
                XYZ curveDirection = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
                XYZ up = XYZ.BasisZ;
                XYZ normal = curveDirection.CrossProduct(up).Normalize();

                // Move wall by half the wall thickness towards the interior (to align EXTERIOR face with curve)
                double offset = wallType.Width / 2.0;
                ElementTransformUtils.MoveElement(doc, wall.Id, normal * (-offset));
                wall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set(2); // Location line: Wall Centerline

            }
        }

    }
}
