using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.Model;

public class FloorInfo
{
    public List<PlanarFace> PlanarFaces = new List<PlanarFace>();

    public FloorInfo(Floor floor)
    {
        var solids = floor.GetSolids();
        IList<Solid> solids1 = new List<Solid>();

        foreach (var solid in solids)
        {
            if (solid.Volume > 0)
            {

                foreach (var solidFace in solid.Faces)
                {
                    var planarF = solidFace as PlanarFace;
                    if (planarF == null) continue;
                    if (planarF.Reference == null) continue;
                    PlanarFaces.Add(planarF);
                }
            }
        }
    }
}