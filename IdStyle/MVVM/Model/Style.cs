using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdStyle.MVVM.Model
{
    public class Style
    {
        public string StyleName { get; set; } // e.g., BLP
        public List<RoomData> roomsData { get; set; } = new List<RoomData>();
    }
}
