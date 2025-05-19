using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Utils
{
    public static class TagUtils2
    {
        public static IndependentTag CreateIndependentTag(ElementId tagId, ElementId viewId, Rebar rebar, bool addLeader, TagOrientation orientation, XYZ point)
        {
            IndependentTag tag = null;

            if (tagId == null || tagId == ElementId.InvalidElementId)
            {
                return null;
            }
#if R23 || R24 || R25
            var rf = rebar.GetSubelements().FirstOrDefault()?.GetReference();

#else
 var rf = new Reference(rebar);
#endif
#if Version2017
         tag = AC.Document.Create.NewTag(viewId.ToElement() as View, rf.ToElement(), addLeader, TagMode.TM_ADDBY_CATEGORY,
             orientation, point);
         if (tagId != null)
         {
            tag.ChangeTypeId(tagId);
         }
#elif Version2018 || Version2019
         tag = IndependentTag.Create(AC.Document, viewId, rf, addLeader, TagMode.TM_ADDBY_CATEGORY, orientation, point);
         tag.ChangeTypeId(tagId);
#else
            tag = IndependentTag.Create(AC.Document, tagId, viewId, rf, addLeader, orientation, point);
#endif
            return tag;
        }
    }
}
