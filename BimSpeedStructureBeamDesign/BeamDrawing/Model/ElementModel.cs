using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class ElementModel
   {
      public ElementId ElementId { get; set; }
      public Element Element { get; set; }
      public string Name { get; set; }

      public ElementModel()
      {
      }
   }
}