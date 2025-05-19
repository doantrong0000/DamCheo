using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.Model;

public class OutLineInfo
{
    public XYZ PointMax { get; set; }

    public XYZ PointMin { get; set; }
    
    public XYZ PointA { get; set; }
    
    public XYZ PointB { get; set; }
    
    public XYZ PointD { get; set; }
    
    public XYZ PointCenter { get; set; }
    
    public double distanceAB { get; set; }
    
    public double distanceAD { get; set; }

    public OutLineInfo(XYZ pointMax, XYZ pointMin)
    {
        var pointMaxX = Math.Max(pointMax.X, pointMin.X);
        var pointMinX = Math.Min(pointMax.X, pointMin.X);

        var pointMaxY = Math.Max(pointMax.Y, pointMin.Y);
        var pointMinY = Math.Min(pointMax.Y, pointMin.Y);

        var pointMaxZ = Math.Max(pointMax.Z, pointMin.Z);
        var pointMinZ = Math.Min(pointMin.Z, pointMax.Z);

        PointMax = new XYZ(pointMaxX, pointMaxY, pointMaxZ + 4000.MmToFoot());
        PointMin = new XYZ(pointMinX, pointMinY, pointMinZ - 4000.MmToFoot());
        PointA = new XYZ(pointMinX, pointMaxY, 0);
        PointB = new XYZ(pointMaxX, pointMaxY, 0);
        PointD = new XYZ(pointMinX, pointMinY, 0);
        PointCenter = new XYZ((PointB.X + PointD.X) / 2, (PointB.Y + PointB.Y) / 2, pointMax.Z);

        distanceAB = PointA.DistanceTo(PointB);
        distanceAD = PointA.DistanceTo(PointD);
    }
   
}