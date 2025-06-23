using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.Utils
{
    public class RoomUtils
    {
        public static List<Curve> GetRoomBoundary(Room room)
        {
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(options);

            if (boundaries == null || boundaries.Count == 0)
                return null;

            List<Curve> boundaryCurves = new List<Curve>();

            foreach (var segment in boundaries[0])
            {
                boundaryCurves.Add(segment.GetCurve());
            }
            return boundaryCurves;
        }
    }
}
