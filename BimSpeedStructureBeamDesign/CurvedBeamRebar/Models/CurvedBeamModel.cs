using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BimSpeedRebar.ColumnSectionGenerator.Model.BeamRebar.Model;
using BimSpeedRebar.CurvedBeamRebar.Models;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.Models
{
    public class CurvedBeamModel
    {
        public XYZ Direction { get; set; }
        public XYZ Origin { get; set; }
        public XYZ Last { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }
        public CurvedBeamGeometry CurvedBeamGeometry { get; set; }
        public Curve CurveBeam { get; set; }
        public List<Solid> Solids { get; set; } = new List<Solid>();

        public CurvedBeamModel(FamilyInstance beam)
        {
            GetData(beam);
        }

        private void GetData(FamilyInstance beam)
        {
            CurvedBeamGeometry = new CurvedBeamGeometry(beam);
            TopElevation = CurvedBeamGeometry.TopElevation;
            BotElevation = CurvedBeamGeometry.BotElevation;
            var columns = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            var beams = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            beams = FilterLineBeam(beams);
            var columnSupports = columns.Select(x => new ElementGeometry(x)).ToList();
            var beamSupports = beams.Select(x => new ElementGeometry(x)).ToList(); ;
        
            var supports = columnSupports.Concat(beamSupports)
                .Select(x => x.Solid).Where(x => x != null && x.Volume > 0.001).ToList();
            Solids = supports;
        }
        
        public static List<FamilyInstance> FilterLineBeam(List<FamilyInstance> beams)
        {
            List<FamilyInstance> nonStraightBeams = new List<FamilyInstance>();
            foreach (FamilyInstance beam in beams)
            {
                // Get the location curve of the beam
                Location location = beam.Location;
                if (location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    // Check if the curve is not a Line (i.e., non-straight)
                    if (curve is Line)
                    {
                        nonStraightBeams.Add(beam);
                    }
                }
            }

            return nonStraightBeams;
        }

    }
}
