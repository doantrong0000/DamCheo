using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.ViewModels
{
    public class RebarData
    {
        public List<RebarBarType> BarDiameters { get; set; }
        public List<RebarBarType> RebarBarTypes { get; set; } = new();

        private static RebarData _instance;
        public static RebarData Instance;
        public RebarData()
        {
            Instance = this;
            GetData();
        }

        private void GetData()
        {
            RebarBarTypes = GetRebarBarTypes();
            BarDiameters = RebarBarTypes;
            RebarUtils.BarDiameters = BarDiameters;
        }
        public static List<RebarBarType> GetRebarBarTypes(Document doc = null)
        {
            if (doc == null)
            {
                doc = AC.Document;
            }

            var diameters = new FilteredElementCollector(AC.Document).OfClass(typeof(RebarBarType)).Cast<RebarBarType>()
                .OrderBy(x => x.BarDiameter()).ToList();

            return diameters;
        }
    
    }
}
