using Autodesk.Revit.DB;
using BimSpeedUtils;
using MoreLinq.Extensions;

namespace BimSpeedRebar.ColumnSectionGenerator.Model.BeamRebar.Model;

public class BeamGeometry
{
    private Solid _solid;
    private Solid _solid2;
    public FamilyInstance Beam { get; set; }

    /// <summary>
    /// Line at center top of beam
    /// </summary>
    public Line BeamLine { get; set; }

    public XYZ MidPoint { get; set; }
    public Transform Transform { get; set; } = Transform.Identity;

    public Solid Solid
    {
        get
        {
            try
            {
                var b = SolidUtils.Clone(_solid);
                return b;
            }
            catch (Exception)
            {
                var b = SolidUtils.Clone(_solid2);
                return b;
            }
        }
        set
        {
            _solid = value;
            if (value != null)
            {
                _solid2 = SolidUtils.Clone(value);
            }

        }
    }

    public Solid OriginalSolidTransformed { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double TopElevation { get; set; }
    public double BotElevation { get; set; }
    public string Mark { get; set; }
    public bool IsColumn { get; set; } = false;



    public BeamGeometry(FamilyInstance beam)
    {
        Beam = beam;
        GetData();
    }

    public BeamGeometry()
    {

    }


    private void GetData()
    {
        Transform = Beam.GetTransform();

        if (Beam.Location is LocationCurve lc)
        {
            var line = lc.Curve as Line;
            if (line != null)
            {
                var sp = line.SP().EditZ(TopElevation);
                var ep = line.EP().EditZ(TopElevation);
                var line1 = Line.CreateBound(sp, ep);
                var origin = Transform.Origin;
                var normal = Transform.OfVector(XYZ.BasisY);
                var plane = BPlane.CreateByNormalAndOrigin(normal, origin);
                BeamLine = line1.ProjectOntoPlane(plane);
            }
        }


        BeamLine = Line.CreateBound(BeamLine.SP().EditZ(TopElevation), BeamLine.EP().EditZ(TopElevation));
        MidPoint = BeamLine.Midpoint();
        OriginalSolidTransformed = SolidUtils.CreateTransformed(GetOriginalGeometry(), Transform);
        var s = Beam.GetSolids();
        Solid = Beam.GetSolids().FirstOrDefault(x => x.Volume > 0.01);
        Mark = Beam.GetParameterValueAsString(BuiltInParameter.DOOR_NUMBER);
        BeamLine = Line.CreateBound(BeamLine.SP().EditZ(TopElevation), BeamLine.EP().EditZ(TopElevation));
    }



    public Solid GetOriginalSolidTransformed(double extent = 0.16404199475)
    {
        return SolidUtils.CreateTransformed(GetOriginalGeometry(extent), Transform);
    }

    private Solid GetOriginalGeometry(double extent = 0.16404199475)
    {
        var geometryElement = Beam.GetOriginalGeometry(new Options());
        var solid1 = geometryElement.Flatten().FirstOrDefault(x => x is Solid) as Solid;
        var solid1Bb = solid1.GetBoundingBox();
        var min = solid1Bb.Min;
        var max = solid1Bb.Max;
        var bbTf = solid1Bb.Transform;
        TopElevation = Transform.OfPoint(bbTf.OfPoint(max)).Z;
        BotElevation = Transform.OfPoint(bbTf.OfPoint(min)).Z;
        Height = max.Z - min.Z;
        Width = max.Y - min.Y;
        var sp = Transform.Inverse.OfPoint(BeamLine.SP().Add(BeamLine.Direction * -extent));
        var direct = Transform.Inverse.OfVector(BeamLine.Direction);
        var p1 = min;
        var p2 = p1.Add(XYZ.BasisZ * Height);
        var p3 = p2.Add(XYZ.BasisY * Width);
        var p4 = p1.Add(XYZ.BasisY * Width);
        p1 = p1.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
        p2 = p2.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
        p3 = p3.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
        p4 = p4.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
        var l1 = Line.CreateBound(p1, p2);
        var l2 = Line.CreateBound(p2, p3);
        var l3 = Line.CreateBound(p3, p4);
        var l4 = Line.CreateBound(p4, p1);
        var cl = new CurveLoop();
        cl.Append(l1);
        cl.Append(l2);
        cl.Append(l3);
        cl.Append(l4);
        var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { cl }, direct,
            BeamLine.Length + 2 * extent);
        return solid;
    }
}