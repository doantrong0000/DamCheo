using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class EdgeInfo
    {
        public Edge Edge { get; set; }

        public string ElementId { get; set; }

        public Transform Transform { get; set; }
    }
}
