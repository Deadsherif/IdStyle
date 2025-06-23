using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.MVVM.Model
{
    public class RoomData
    {
        public string RoomName { get; set; }  // e.g., STAIR01
        public string WallType { get; set; }  // e.g., W3
        public string FloorType { get; set; } // e.g., F3
        public List<string> CeilingTypes { get; set; } // e.g., C2 or C1/C4
        public List<double> CeilingHeights { get; set; } // e.g., 3200 or [2600, 2650] in mm
    }
}
