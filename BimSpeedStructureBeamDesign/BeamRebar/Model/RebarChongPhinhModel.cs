using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class RebarChongPhinhModel
   {
      public int Index { get; set; }
      public XYZ RevitPointLeft { get; set; }
      public XYZ RevitPointRight { get; set; }
      public System.Windows.Point WindowPointLeft { get; set; }
      public System.Windows.Point WindowPointRight { get; set; }

      public List<Path> Paths { get; set; } = new List<Path>();
      public List<Path> StirrupPaths { get; set; } = new List<Path>();

      public void RemovePath(Canvas canvas)
      {
         if (canvas == null)
         {
            return;
         }
         Paths.ForEach(x => canvas.Children.Remove(x));
         StirrupPaths.ForEach(x => canvas.Children.Remove(x));
      }
   }
}
