using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IdStyle.MVVM.View;
using IdStyle.MVVM.ViewModel;
using IdStyle.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WallUtils = IdStyle.Utils.WallUtils;
using Style = IdStyle.MVVM.Model.Style;
using Autodesk.Revit.DB.Architecture;
namespace IdStyle.EVHandler
{
    internal class IdStyleExternalEventHandler : IExternalEventHandler
    {
        public MainPageViewModel MainViewModel { get; set; }

        public void Execute(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                var doc = uidoc.Document;
                var activeView = doc.ActiveView;
                var projectName = doc.Title;
                MainViewModel.SetStatus(true);
                Transaction tr = new Transaction(doc, "Create Component");
                tr.Start();

                var selectionReference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
                Room room = doc.GetElement(selectionReference) as Room;
                var boundary = RoomUtils.GetRoomBoundary(room);

                CreateModelFromStyle(doc, MainViewModel.SelectedStyle, boundary, room);

                tr.Commit();
            }
            catch (Exception ex) { TaskDialog.Show("Error", ex.Message); }
        }
        public static void CreateModelFromStyle(Document doc, Style style, List<Curve> boundary, Room room)
        {
            var roomData = style.roomsData.FirstOrDefault(X => X.RoomName == room.get_Parameter(BuiltInParameter.ROOM_NAME).AsValueString());
            WallUtils.CreateWalls(doc, boundary, roomData, room);
            FloorUtils.CreateFloorFromBoundary(doc, boundary, roomData.FloorType, room.LevelId);
            int i = 0;
            foreach (double height in roomData.CeilingHeights)
            {
                CeilingUtils.CreateCeilingFromBoundary(doc, boundary, roomData.CeilingTypes[i], room.LevelId, height);
                i++;
            }
        }
        public string GetName() => "Run Tool";
    }

}
