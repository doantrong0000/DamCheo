using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Filter
{
   public class BeamColumnSelectionFilter : ISelectionFilter
   {
      public bool AllowElement(Element element)
      {
         if (element.Category == null)
         {
            return false;
         }
         if (element.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralFraming || element.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralColumns)
         {
            return true;
         }
         return false;
      }

      public bool AllowReference(Reference refer, XYZ point)
      {
         return false;
      }
   }
}
