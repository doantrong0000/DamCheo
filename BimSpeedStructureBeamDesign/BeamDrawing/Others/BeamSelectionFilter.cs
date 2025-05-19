using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
   public class BeamSelectionFilter : ISelectionFilter
   {
      public bool AllowElement(Element elem)
      {
         if (elem.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralFraming)
         {
            return true;
         }

         return false;
      }

      public bool AllowReference(Reference reference, XYZ position)
      {
         return false;
      }
   }
}