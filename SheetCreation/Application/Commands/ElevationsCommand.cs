using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using RevitAutomationToolkit.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace SheetCreation.Application.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ElevationsCommand : IExternalCommand
    {
        private Dictionary<string, List<ViewSection>> _roomElevations = new();
        private string _pathFolder;
        private Document _doc;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            _doc = doc;
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pathFolder = Path.GetDirectoryName(assemblyLocation);
            _pathFolder = pathFolder;
            string excelPath = $"{pathFolder}\\E1 - Log -Suites.xlsx"; // Path to Excel

            if (!File.Exists(excelPath))
            {
                message = "Excel file not found.";
                return Result.Failed;
            }
            var modelFileRef = Path.GetFileNameWithoutExtension(doc.PathName);
            var sheetNames = LoadSheetNamesFromExcel(excelPath, modelFileRef);

            using (Transaction t = new Transaction(doc, "Create Room Elevations and Sheets"))
            {
                t.Start();

                CreateRoomElevations(doc);
                CreateSheets(doc, sheetNames);

                t.Commit();
            }

            return Result.Succeeded;
        }
        /// <summary>
        /// This function used to edit elevation crop view to be with the boundary of its room
        /// </summary>
        public void EditElevationBoundary(ViewSection elevation, Room room, XYZ elevationPoint)
        {
            var crop = elevation.CropBox;
            Transform t = crop.Transform;

            List<XYZ> projectedLocalPoints = new List<XYZ>();

            var roomBoundary = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            if (roomBoundary == null || roomBoundary.Count == 0) return;

            IList<BoundarySegment> segments = roomBoundary[0];
            if (segments.Count == 0) return;

            foreach (var segment in segments)
            {
                var curve = segment.GetCurve();
                if (!(curve is Line line)) continue;

                var cross = line.Direction.CrossProduct(elevation.ViewDirection);
                bool isParallel = cross.IsZeroLength();

                if (isParallel)
                {
                    var intersectionResult = line.Project(elevationPoint);
                    var projectedGlobalPoint = intersectionResult.XYZPoint;

                    // Check if truly perpendicular
                    var projectLine = Line.CreateBound(projectedGlobalPoint, elevationPoint);
                    double dot = Math.Round(line.Direction.DotProduct(projectLine.Direction), 3);
                    if (dot == 0)
                    {
                        // Convert to local space
                        XYZ localPoint = t.Inverse.OfPoint(projectedGlobalPoint);
                        projectedLocalPoints.Add(localPoint);
                        // Optional: draw in current view
                        //_doc.Create.NewDetailCurve(_doc.ActiveView, projectLine);
                    }
                }
            }

            if (projectedLocalPoints.Count < 2)
                return;
            // STEP 1 – Get Z of slab below & above (in world)
            var (zBelow, zAbove) = GetLinkedFloorZRange(_doc, room);
            if (zBelow == null || zAbove == null)
                return;

            // STEP 2 – Convert slab elevations (global Z) into local Y direction
            XYZ ptBelowWorld = new XYZ(elevationPoint.X, elevationPoint.Y, zBelow.Value);
            XYZ ptAboveWorld = new XYZ(elevationPoint.X, elevationPoint.Y, zAbove.Value);

            double localYBelow = t.Inverse.OfPoint(ptBelowWorld).Y;
            double localYAbove = t.Inverse.OfPoint(ptAboveWorld).Y;

            // STEP 3 – Apply Y values to crop box
            BoundingBoxXYZ newBox = new BoundingBoxXYZ();
            newBox.Transform = crop.Transform;

            newBox.Min = new XYZ(
                projectedLocalPoints.Min(p => p.X - 0.984252),
                Math.Min(localYBelow, localYAbove),
                crop.Min.Z);

            newBox.Max = new XYZ(
                projectedLocalPoints.Max(p => p.X + 0.984252),
                Math.Max(localYBelow, localYAbove),
                crop.Max.Z);

            elevation.CropBox = newBox;

        }
        private (double? zBelow, double? zAbove) GetLinkedFloorZRange(Document doc, Room room)
        {
            var roomCenter = (room.Location as LocationPoint)?.Point;
            if (roomCenter == null) return (null, null);

            double roomZ = roomCenter.Z;
            double? zBelow = null, zAbove = null;

            foreach (var linkInstance in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
            {
                var linkDoc = linkInstance.GetLinkDocument();
                if (linkDoc == null) continue;

                var transform = linkInstance.GetTransform();
                var floors = new FilteredElementCollector(linkDoc).OfClass(typeof(Floor)).Cast<Floor>();

                foreach (var floor in floors)
                {
                    var bbox = floor.get_BoundingBox(null);
                    if (bbox == null) continue;

                    double zTop = transform.OfPoint(bbox.Max).Z;
                    double zBot = transform.OfPoint(bbox.Min).Z;

                    if (zBot < roomZ && (zBelow == null || zBot > zBelow)) zBelow = zBot;
                    if (zTop > roomZ && (zAbove == null || zTop < zAbove)) zAbove = zTop;
                }
            }

            return (zBelow, zAbove);
        }
        ///           Logic
        ///          seqment
        ///  (p1)--------------(p2)
        ///             |
        ///             | -projectLine-
        ///             |
        ///           (p0)

       
        private void CreateRoomElevations(Document doc)
        {
            View view = doc.ActiveView;
            ViewFamilyType elevationType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Elevation);

            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var roomElement in rooms)
            {
                if (roomElement is Room room)
                {
                    var point = GeometryUtils.GetCentroid(room);
                    if (point == null) continue;

                    ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, elevationType.Id, point, 25);
                    List<ViewSection> views = new List<ViewSection>();

                    for (int i = 0; i < 4; i++)
                    {
                        ViewSection elevation = marker.CreateElevation(doc, doc.ActiveView.Id, i);
                        elevation.Scale = 25;
                        elevation.Name = $"{room.Name}_Elevation_{i + 1}";
                        EditElevationBoundary(elevation, room, point);
                        views.Add(elevation);
                    }
                    string roomName = room.LookupParameter("Name").AsValueString();
                    _roomElevations[roomName] = views;
                }
            }
        }

        private void CreateSheets(Document doc, List<(string number, string Name, string modelFileRef)> sheetNames)
        {
            Family family = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .FirstOrDefault(e => e.Name == "WSF_Title Block_A0") as Family;
            var viewPlan = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).WhereElementIsNotElementType()
                    .Cast<View>()
                   .FirstOrDefault(X => X.ViewType == ViewType.FloorPlan && !X.IsTemplate);

            FamilySymbol familySymbol = GetFamilySymbol(doc, family);
            if (family == null)
            {
                var familypath = $"{_pathFolder}\\WSF_Title Block_A0.rfa";
                doc.LoadFamily(familypath, out Family titleBlockfamily);
                 familySymbol = GetFamilySymbol(doc, titleBlockfamily);

            };
            int i = 0;
            foreach (var sheetName in sheetNames)
            {
              
                ViewSheet sheet = ViewSheet.Create(doc, familySymbol.Id);
                sheet.Name = sheetName.Name.Replace(':', '/');
                sheet.SheetNumber = sheetName.number;
                //if (!_roomElevations.ContainsKey(roomName)) continue;
                if (i < 5)
                {
                    var newFloorPlan = doc.GetElement(viewPlan.Duplicate(ViewDuplicateOption.Duplicate)) as View;
                    newFloorPlan.Name = sheetName.Name.Replace(':', '/');
                    CreateViewportsInPlansSheets(doc, sheet, newFloorPlan);
                }
                var roomKey = _roomElevations.Keys.Where(x => sheetName.Name.Contains(x)).FirstOrDefault();
                if (roomKey != null)
                    try
                    {
                        CreateViewportsInElevationsSheets(roomKey, doc, sheet);

                    }
                    catch (Exception)
                    {


                    }
                i++;

            }
        }
        public FamilySymbol GetFamilySymbol(Document doc , Family family)
        {
            var familySymbolId = family.GetFamilySymbolIds().FirstOrDefault();
            var familySymbol = doc.GetElement(familySymbolId) as FamilySymbol;
            familySymbol.Activate();

            return familySymbol;
        }
        private List<(string number, string Name, string modelFileRef)> LoadSheetNamesFromExcel(string filePath, string targetModelFileRef)
        {
            var names = new List<(string Number, string Name, string modelFileRef)>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[2];

                string modelfileRef = "";
                int totalRows = worksheet.Dimension.End.Row;
                for (int row = 3; row <= totalRows + 3; row++)
                {
                    var cellKValue = worksheet.Cells[row, 11].Text;
                    if (cellKValue != "")
                    { modelfileRef = cellKValue; }
                    var cellBValue = worksheet.Cells[row, 2].Text;
                    var cellEValue = worksheet.Cells[row, 5].Text;

                    if (cellEValue == "" || cellBValue == "")
                        continue;

                    var splittedName = cellBValue.Split('-');
                    var number = $"{splittedName.ElementAt(7)}-{splittedName.ElementAt(8)}";

                    var name = $"{cellEValue}".Trim();

                    names.Add((number, name, modelfileRef));

                }
            }

            return names.Where(s => s.modelFileRef == targetModelFileRef).ToList();
        }
        public void CreateViewportsInElevationsSheets(string roomKey, Document doc, ViewSheet sheet)
        {
            if (!_roomElevations.ContainsKey(roomKey)) return;

            var views = _roomElevations[roomKey];

            BoundingBoxUV outline = sheet.Outline;
            double sheetWidth = outline.Max.U - outline.Min.U;
            double sheetHeight = outline.Max.V - outline.Min.V;

            int columns = 2;
            int rows = (int)Math.Ceiling(views.Count / (double)columns);

            double marginX = 0.2;
            double marginY = 0.2;

            double cellWidth = (sheetWidth - 2 * marginX) / columns;
            double cellHeight = (sheetHeight - 2 * marginY) / rows;

            for (int i = 0; i < views.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Center point of the cell
                double x = outline.Min.U + marginX + (col + 0.3) * cellWidth;
                double y = outline.Min.V + marginY + (rows - row - .5) * cellHeight;

                XYZ location = new XYZ(x, y, 0);
                Viewport.Create(doc, sheet.Id, views[i].Id, location);
            }
        }
        public void CreateViewportsInPlansSheets(Document doc, ViewSheet sheet, View view)
        {
            BoundingBoxUV outline = sheet.Outline;
            var centre = (outline.Min + outline.Max) / 2;
            XYZ location = new XYZ(centre.U, centre.V, 0);
            Viewport.Create(doc, sheet.Id, view.Id, location);
        }



    }
}
