using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
   public class CutZoneSelectionFilter : ISelectionFilter
   {
      public bool AllowElement(Element element)
      {
         if (element.Category == null)
         {
            return false;
         }
         if (element is FilledRegion)
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