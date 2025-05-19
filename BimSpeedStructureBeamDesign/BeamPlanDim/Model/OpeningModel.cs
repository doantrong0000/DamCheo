using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.Model;

public class OpeningModel
{
    public List<Line> ListLine = new List<Line>();
    
    public XYZ PointMax { get; set; }
    
    public XYZ PointMin { get; set; }
    
}