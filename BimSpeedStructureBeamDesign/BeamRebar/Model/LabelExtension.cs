using Autodesk.Revit.DB;
using Point = System.Windows.Point;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class LabelPointExtension
   {
      public Point Point { get; set; }
      public XYZ RevitPoint { get; set; }
      public int Type { get; set; }
      public int Index { get; set; }
      public string Location { get; set; } = "BOT";

   }
}
