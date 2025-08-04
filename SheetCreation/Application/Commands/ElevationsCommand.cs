using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using RevitAutomationToolkit.Utils;
using SheetCreation.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using Floor = Autodesk.Revit.DB.Floor;

namespace SheetCreation.Application.Commands
{
   [Transaction(TransactionMode.Manual)]
   public class ElevationsCommand : IExternalCommand
   {
      private Dictionary<string, List<ViewSection>> _roomElevations = new();
      private string _pathFolder;
      //private Document _doc;
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         UIApplication uiApp = commandData.Application;
         Document doc = uiApp.ActiveUIDocument.Document;
         //_doc = doc;
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

         var tg = new TransactionGroup(doc, "Run Addin Of Sheet Creation");
         tg.Start();

         CreateRoomElevations(doc);
         CreateSheets(doc, sheetNames);
         ApplyTemplates(doc);

         tg.Assimilate();


         return Result.Succeeded;
      }
      /// <summary>
      /// This function used to edit elevation crop view to be with the boundary of its room
      /// </summary>
      public void EditElevationBoundary(Document doc, ViewSection elevation, Room room, XYZ elevationPoint)
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
         var (zBelow, zAbove) = GetLinkedFloorZRange(doc, room);
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
            if (linkDoc == null || !Path.GetFileNameWithoutExtension(linkDoc.PathName).Contains("ST")) continue;

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
             .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Elevation && vft.Name == Properties.Settings.Default.ViewType);
         Transaction tr = new Transaction(doc, "Creat Elevation For Rooms");
         tr.Start();
         var rooms = new List<Room>();
         if (Properties.Settings.Default.UseCurrentView)
            rooms = GetRooms(doc, RoomQueryMode.CurrentView);
         else if (Properties.Settings.Default.UseAllViews)
            rooms = GetRooms(doc, RoomQueryMode.AllViews);
         else
            rooms = GetRooms(doc, RoomQueryMode.SelectedViews);
         foreach (var roomElement in rooms)
         {
            if (roomElement is Room room)
            {
               var point = GeometryUtils.GetCentroid(room);
               if (point == null) continue;

               ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, elevationType.Id, point, 25);
               List<ViewSection> views = new List<ViewSection>();
               string roomName = room.LookupParameter("Name").AsString();
               string roomNumber = Properties.Settings.Default.IsRoomNumberAdded ? room.LookupParameter("Number").AsString() : "";

               for (int i = 00; i < 04; i++)
               {
                  ViewSection elevation = marker.CreateElevation(doc, view.Id, i);
                  elevation.Scale = 25;
                  elevation.Name = $"{roomName}{roomNumber} Elevation 0{i + 1}";
                  EditElevationBoundary(doc, elevation, room, point);
                  views.Add(elevation);

               }
               if (roomName.Contains("TOILET") || roomName.Contains("SHOWER"))
               {

                  roomName = "TOILETAndSHOWER";
                  if (_roomElevations.FirstOrDefault(X => X.Key == roomName).Key == null)
                     _roomElevations[roomName] = views;
                  else
                     _roomElevations[roomName].AddRange(views);
               }
               else
                  _roomElevations[roomName] = views;

            }
         }
         tr.Commit();
      }
      public List<Room> GetRooms(Document doc, RoomQueryMode mode, List<ElementId> selectedViewsIds = null)
      {
         switch (mode)
         {
            case RoomQueryMode.CurrentView:
               return GetRoomsInView(doc, doc.ActiveView.Id);

            case RoomQueryMode.AllViews:
               return GetAllRooms(doc);

            case RoomQueryMode.SelectedViews:
               if (selectedViewsIds == null)
                  throw new ArgumentException("SelectedViewId cannot be null in SelectedView mode.");
               return GetRoomsInViews(doc, selectedViewsIds);

            default:
               throw new ArgumentOutOfRangeException(nameof(mode), "Invalid room query mode.");
         }
      }

      private List<Room> GetRoomsInView(Document doc, ElementId viewId)
      {
         return new FilteredElementCollector(doc, viewId)
             .OfCategory(BuiltInCategory.OST_Rooms)
             .WhereElementIsNotElementType()
             .Cast<Room>()
             .ToList();
      }
      private List<Room> GetRoomsInViews(Document doc, List<ElementId> viewsIds)
      {
         List<Room> rooms = new List<Room>();
         foreach (var viewId in viewsIds) { var roomsInView = GetRoomsInView(doc, viewId); rooms.AddRange(roomsInView); }
         return rooms;
      }

      private List<Room> GetAllRooms(Document doc)
      {
         return new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_Rooms)
             .WhereElementIsNotElementType()
             .Cast<Room>()
             .ToList();
      }
      public static Element FindElementByName(Document doc, Type targetType, string targetName)
      {
         return new FilteredElementCollector(doc)
           .OfClass(targetType)
           .FirstOrDefault<Element>(
             e => e.Name.Equals(targetName));
      }

      public static FamilySymbol GetFamilySymbole(Document doc, string FamilyName, string symbolName)
      {
         /// <summary>
         /// Return complete family file path
         /// </summary>
         var addinFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
         // Retrieve the family if it is already present:

         string FamilyPath = $"C:/Users/DELL/AppData/Roaming/Autodesk/Revit/Addins/2022/SheetCreation/{FamilyName}.rfa";

         Family family = FindElementByName(doc,
           typeof(Family), FamilyName) as Family;

         if (null == family)
         {
            // It is not present, so check for 
            // the file to load it from:

            if (!File.Exists(FamilyPath))
            {
               TaskDialog.Show("Error", string.Format(
                 "Please ensure that  "
                 + "family file '{0}' exists in '{1}'.",
                 FamilyName, FamilyPath));
               return null;

            }

            // Load family from file:

            doc.LoadFamily(FamilyPath, out family);

         }

         // Determine the family symbol

         FamilySymbol symbol = null;

         foreach (var symboleId in family.GetFamilySymbolIds())
         {
            var _symbol = doc.GetElement(symboleId) as FamilySymbol;
            if (_symbol.Name == symbolName)
            { symbol = _symbol; break; }

         }
         return symbol;
      }
      private void CreateSheets(Document doc, List<(string number, string Name, string modelFileRef)> sheetNames)
      {
         Family family = new FilteredElementCollector(doc)
             .OfClass(typeof(Family))
             .FirstOrDefault(e => e.Name == "WSF") as Family;
         var viewPlan = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).WhereElementIsNotElementType()
                 .Cast<View>()
                .FirstOrDefault(X => X.ViewType == ViewType.FloorPlan && !X.IsTemplate);


         Transaction tr = new Transaction(doc, "Load Family");
         tr.Start();
         FamilySymbol familySymbol = null;
         familySymbol = GetFamilySymbole(doc, "WSF_Title Block_A0", "A0");
         tr.Commit();

         Transaction tr1 = new Transaction(doc, "Create ");
         tr1.Start();
         int i = 0;
         foreach (var sheetName in sheetNames)
         {
            var name = sheetName.Name.Replace(':', '/');
            ViewSheet sheet = ViewSheet.Create(doc, familySymbol.Id);
            sheet.Name = name;
            sheet.SheetNumber = sheetName.number;
            //if (!_roomElevations.ContainsKey(roomName)) continue;
            if (i < 5 && Properties.Settings.Default.IsFloorPlanCreationChecked)
            {
               var floorPlan = new FilteredElementCollector(doc)
                   .OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().Cast<View>().FirstOrDefault(vp => vp.Name == name);
               if (floorPlan == null)
               {
                  floorPlan = doc.GetElement(viewPlan.Duplicate(ViewDuplicateOption.Duplicate)) as View;
                  var viewName = name.Split('-').LastOrDefault() ?? "";
                  floorPlan.Name = viewName;
                  floorPlan.Scale = viewPlan.Scale;
                  var scopeBoxId = viewPlan.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP).AsElementId();
                  if (scopeBoxId != null)
                     floorPlan.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP).Set(scopeBoxId);
                  floorPlan.ViewTemplateId = viewPlan.ViewTemplateId;
               }
               CreateViewportsInPlansSheets(doc, sheet, floorPlan);
            }
            var roomKey = _roomElevations.Keys.Where(x => sheetName.Name.Contains(x)).FirstOrDefault();
            if (sheetName.Name.Contains("TOILET"))
               roomKey = "TOILETAndSHOWER";

            if (roomKey != null)
               try
               {
                  CreateViewportsInElevationsSheets(roomKey, doc, sheet);
               }
               catch (Exception)
               { }
            i++;

         }
         tr1.Commit();
      }
      public void ApplyTemplates(Document doc)
      {
         List<string> _keywords = new List<string>
                                                 {
                                                     "FLOOR FINISHES",
                                                     "FURNITURE",
                                                     "GENERAL ARRANGEMENT",
                                                     "REFLECTED CEILING",
                                                     "WALL FINISHES"
                                                 };
         // Get all view templates
         var templateMap = new FilteredElementCollector(doc)
             .OfClass(typeof(View))
             .Cast<View>()
             .Where(v => v.IsTemplate)
             .ToDictionary(v => v.Name.Replace(" ", "").ToUpper(), v => v);

         // Get all regular (non-template) views
         var viewPlans = new FilteredElementCollector(doc)
             .OfClass(typeof(ViewPlan))
             .Cast<View>()
             .Where(v => !v.IsTemplate)
             .ToList();
            var elevations = new FilteredElementCollector(doc)
          .OfClass(typeof(View))
          .Cast<View>().Where(x=>x.ViewType==ViewType.Elevation)
          .Where(v => !v.IsTemplate)
          .ToList();
         using (Transaction tx = new Transaction(doc, "Assign View Templates by Name"))
         {
            tx.Start();

            foreach (var view in elevations)
            {
               string viewName = view.Name.ToUpper();
               if (view.ViewType == ViewType.Elevation)
               {

                  string templateName = $"UBJ_ELEVATIONS_1/25".ToUpper();
                  var template = templateMap.FirstOrDefault(k => k.Key.Contains(templateName)).Value;
                  if (template != null)
                  {
                     view.ViewTemplateId = template.Id;
                  }

                  continue; // Stop checking other keywords once matched

               }
            }
            foreach (var view in viewPlans)
            {
               string viewName = view.Name.ToUpper();
               foreach (var keyword in _keywords)
               {
                  if (viewName.Contains(keyword.ToUpper()))
                  {
                     string templateName = $"UBJ_{keyword}_1/25".ToUpper().Replace(" ", "");

                     var template = templateMap.FirstOrDefault(k => k.Key.Contains(templateName)).Value;
                     if (template != null)
                     {
                        view.ViewTemplateId = template.Id;
                     }

                     break; // Stop checking other keywords once matched
                  }
               }
            }

            tx.Commit();
         }
      }
      public FamilySymbol GetFamilySymbol(Document doc, Family family)
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
               var cellDValue = worksheet.Cells[row, 4].Text;
               var cellEValue = worksheet.Cells[row, 5].Text;

               if (cellDValue == "" || cellEValue == "" || cellBValue == "")
                  continue;

               var splittedName = cellBValue.Split('-');
               var number = $"{splittedName.ElementAt(7)}-{splittedName.ElementAt(8)}";

               var name = $"{cellDValue}{cellEValue}".Trim();

               names.Add((number, name, modelfileRef));

            }
         }

         return names.Where(s => targetModelFileRef.Contains(s.modelFileRef)).ToList();
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
