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
            CurveBeam = TrimCurveBySolids(CurvedBeamGeometry.BeamCurved, supports);
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
        public static Curve TrimCurveBySolids(Curve curve, List<Solid> solids)
        {
            if (solids == null || solids.Count == 0)
            {
                return curve;
            }

            // Gộp solids lại thành 1 solid duy nhất
            Solid unionSolid = solids[0];
            if (solids.Count > 1)
            {
                unionSolid = SolidUtils.Clone(unionSolid);
                for (int i = 1; i < solids.Count; i++)
                {
                    try
                    {
                        BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(
                            unionSolid, solids[i], BooleanOperationsType.Union);
                    }
                    catch
                    {
                        // Bỏ qua nếu không union được
                    }
                }
            }

            // Lấy các đoạn nằm ngoài solid
            SolidCurveIntersection intersection = unionSolid.IntersectWithCurve(
                curve,
                new SolidCurveIntersectionOptions
                {
                    ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside
                });

            Curve longestCurve = null;
            double maxLength = 0;

            if (intersection != null && intersection.SegmentCount > 0)
            {
                for (int i = 0; i < intersection.SegmentCount; i++)
                {
                    Curve segment = intersection.GetCurveSegment(i);
                    if (segment != null)
                    {
                        double len = segment.Length;
                        if (len > maxLength)
                        {
                            maxLength = len;
                            longestCurve = segment;
                        }
                    }
                }
            }

            // Nếu không có đoạn nào nằm ngoài solid, trả về null
            return longestCurve;
        }
    }
}
