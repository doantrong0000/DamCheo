using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Models;
using BimSpeedUtils;
using BimSpeedUtils.Models;
using MoreLinq.Extensions;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
    public class CurvedBeamGeometry
    {
        public Solid Solid;
        public FamilyInstance Beam { get; set; }
        public Curve BeamCurved { get; set; }
        public XYZ MidCurved { get; set; }
        public Transform Transform { get; set; } = Transform.Identity;
        public double Width { get; set; }
        public double Height { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }
        public string Mark { get; set; }


        public CurvedBeamGeometry(FamilyInstance beam)
        {
            Beam = beam;
            GetData();
        }

        public CurvedBeamGeometry()
        {

        }

        private void GetData()
        {
            Transform = Beam.GetTransform();

            BeamCurved = Beam.GetCurve();

            var s = Beam.GetSolids();

            Solid = Beam.GetSolids().FirstOrDefault(x => x.Volume > 0.01);

            Mark = Beam.GetParameterValueAsString(BuiltInParameter.ALL_MODEL_MARK);

            var geometryElement = Beam.GetOriginalGeometry(new Options());
            var solid1 = geometryElement.Flatten().FirstOrDefault(x => x is Solid) as Solid;

            var solid1Bb = solid1.GetBoundingBox();
            var min = solid1Bb.Min;
            var max = solid1Bb.Max;
            var bbTf = solid1Bb.Transform;
            TopElevation = Transform.OfPoint(bbTf.OfPoint(max)).Z;
            BotElevation = Transform.OfPoint(bbTf.OfPoint(min)).Z;
            Height = max.Z - min.Z;
            Width = Beam.Symbol.LookupParameter("b").AsDouble();
        }
   
    }


}