using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
   public class RebarShopSelectFilter : ISelectionFilter
   {
      public bool AllowElement(Element element)
      {
         if (element.Category == null)
         {
            return false;
         }
         if (element is Rebar rebar)
         {
            return rebar.IsStandardRebar();
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