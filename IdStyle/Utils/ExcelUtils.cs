using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using IdStyle.MVVM.Model;
using OfficeOpenXml;
using Style = IdStyle.MVVM.Model.Style;

namespace IdStyle.Utils
{
    public class ExcelUtils
    {
        internal static List<Style> GetStylesFromExcel()
        {
            var location =Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string excelPath = $@"{location}/Idstyle.xlsx";
          
            var stylesDict = new Dictionary<string, Style>();

            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var worksheet = package.Workbook.Worksheets[1];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                   
                    string styleName = worksheet.Cells[row, 1].Text.Trim();
                    if (styleName == string.Empty) break;
                    string roomName = worksheet.Cells[row, 2].Text.Trim();
                    string wallType = worksheet.Cells[row, 3].Text.Trim();
                    string floorType = worksheet.Cells[row, 4].Text.Trim();
                    string ceilingTypesRaw = worksheet.Cells[row, 5].Text.Trim();
                    string CeilingHeightsRaw = worksheet.Cells[row, 6].Text.Trim();

                    var ceilingTypes = ceilingTypesRaw.Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    var CeilingHeights = CeilingHeightsRaw
                        .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(level => double.TryParse(level.Trim(), out double val) ? val : 0)
                        .Where(val => val > 0)
                        .ToList();

                    if (!stylesDict.ContainsKey(styleName))
                    {
                        stylesDict[styleName] = new Style
                        {
                            StyleName = styleName
                        };
                    }

                    stylesDict[styleName].roomsData.Add(new RoomData
                    {
                        RoomName = roomName,
                        WallType = wallType,
                        FloorType = floorType,
                        CeilingTypes = ceilingTypes,
                        CeilingHeights = CeilingHeights
                    });
                }
            }

            return stylesDict.Values.ToList();

        }
    }
}
