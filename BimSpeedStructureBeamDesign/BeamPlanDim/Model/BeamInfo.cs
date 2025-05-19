using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.Model;

public class BeamInfo
{
    public List<PlanarFace> PlanarFaces = new List<PlanarFace>();

    public XYZ PointCenter { get; set; }
    
    public XYZ Direction { get; set; }
    public FamilyInstance Beam { get; set; }
    public BeamInfo(FamilyInstance beam)
    {
        this.Beam = beam;
        GetData();
        
    }

    private void GetData()
    {
        var solids = Beam.GetAllSolids();

        foreach (var solid in solids)
        {
            if (solid.Volume > 0)
                foreach (var solidFace in solid.Faces)
                {
                    var planarF = solidFace as PlanarFace;
                    if (planarF == null) continue;
                    if (planarF.Reference == null) continue;
                    PlanarFaces.Add(planarF);
                }
        }

        var locationCurve = Beam.Location as LocationCurve;
        var line = locationCurve.Curve as Line;

        PointCenter = line.Origin;

        var curve = Beam.Location as LocationCurve;
        var line1 = curve.Curve as Line;

        Direction = line1.Direction;
    }
}