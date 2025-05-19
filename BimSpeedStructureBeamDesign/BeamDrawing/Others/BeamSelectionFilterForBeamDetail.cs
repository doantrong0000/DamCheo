using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
   public class BeamSelectionFilterForBeamDetail : ISelectionFilter
   {
      public bool AllowElement(Element elem)
      {
         if (elem.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralFraming)
         {
            if (elem is FamilyInstance fi)
            {
               var right = AC.ActiveView.RightDirection;
               if (fi.Location is LocationCurve lc)
               {
                  var c = lc.Curve;
                  if (c is Line && c.Direction().IsParallel(right))
                  {
                     return true;
                  }
               }
            }
         }
         return false;
      }

      public bool AllowReference(Reference reference, XYZ position)
      {
         return false;
      }
   }
}