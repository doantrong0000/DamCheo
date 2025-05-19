using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class GridDimensionModel
   {

      public Grid Grid { get; set; }
      public Reference Reference { get; set; }
      public ReferenceAndPoint ReferenceAndPoint { get; set; }
      public Direction Direction { get; set; } = Direction.Undefine;
      public Line Line { get; set; }
      public bool IsLinear { get; set; } = false;
      public XYZ Direct = XYZ.Zero;
      public Plane Plane { get; set; }
      private int centerSegment;
      private GridType gridType;
      public Autodesk.Revit.DB.View View { get; set; }

      public GridDimensionModel(Grid grid, Autodesk.Revit.DB.View view)
      {
         View = view;
         Grid = grid;
         gridType = grid.GetTypeId().ToElement() as GridType;
         centerSegment = gridType.GetParameterValueByNameAsInteger("Center Segment");
         if (centerSegment != 0)
         {
            gridType.SetParameterValueByName("Center Segment", 0);
         }
         AC.Document.Regenerate();
         GetInfo();
         ReferenceAndPoint = new ReferenceAndPoint()
         {
            Reference = Reference,
            Point = Line.SP()
         };
      }

      public void ResetGridType()
      {
         gridType.SetParameterValueByName("Center Segment", centerSegment);
      }

      private void GetInfo()
      {
         var lines = Grid.Lines(View);
         if (lines.Count == 0) return;
         IsLinear = true;
         Line = lines.OrderByDescending(x => x.ApproximateLength).FirstOrDefault();
         var dir = Line.Direction();

         if (dir.IsParallel(View.RightDirection))
         {
            Direction = Direction.Horizontal;
         }
         else if (dir.IsParallel(View.UpDirection))
         {
            Direction = Direction.Vertical;
         }

         if (Line != null)
         {
            Reference = new Reference(Grid);
            Direct = Line.Direction;
            if (Direct.IsPerpendicular(XYZ.BasisZ))
            {
               Plane = Plane.CreateByThreePoints(Line.SP(), Line.EP(), Line.SP().Add(XYZ.BasisZ));
            }
            if (Direct.IsPerpendicular(XYZ.BasisX))
            {
               Plane = Plane.CreateByThreePoints(Line.SP(), Line.EP(), Line.SP().Add(XYZ.BasisX));
            }
            if (Direct.IsPerpendicular(XYZ.BasisY))
            {
               Plane = Plane.CreateByThreePoints(Line.SP(), Line.EP(), Line.SP().Add(XYZ.BasisY));
            }
         }
      }
   }

   public enum Direction
   {
      Horizontal,
      Vertical,
      Undefine
   }
}