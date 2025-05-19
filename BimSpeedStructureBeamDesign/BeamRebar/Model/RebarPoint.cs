using System.Windows.Controls;
using System.Windows.Shapes;
using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class RebarPoint
   {
      public int Index { get; set; }
      public XYZ Point { get; set; }
      public System.Windows.Point WindowPoint { get; set; }
      public Label LabelDiameter { get; set; }
      public List<Path> Paths { get; set; } = new List<Path>();
      public bool Checked { get; set; }

      public RebarPoint()
      {
      }
      public RebarPoint(int index, bool c = false)
      {
         Index = index;
         Checked = c;
      }
   }
}