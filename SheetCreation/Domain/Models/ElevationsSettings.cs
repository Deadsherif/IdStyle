using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCreation.Domain.Models
{
    public class ElevationsSettings
    {
        public string SelectedType { get; set; }
        public string ViewOption { get; set; }
        public List<string> SelectedViews { get; set; }
    }
}
