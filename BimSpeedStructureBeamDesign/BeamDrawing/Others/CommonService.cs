using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
    public static class CommonService
    {

        public static void LoadFamilyByName()
        {

        }

        public static Dimension CreateDimension(ReferenceArray ra, XYZ p, XYZ vector, DimensionType dimensionType, Autodesk.Revit.DB.View view)
        {
            var line = Line.CreateBound(p, p.Add(vector));
            try
            {
                using var s = new SubTransaction(AC.Document);
                s.Start();
                var d = AC.Document.Create.NewDimension(view, line, ra, dimensionType);
                s.Commit();
                return d;
            }
            catch
            {
                return null;
            }
        }

        public static void SetRebarDetailLevel(ViewSection viewSection)
        {
            viewSection.ViewTemplateId = ElementId.InvalidElementId;
            var rebarCategoryId = new ElementId(BuiltInCategory.OST_Rebar);
            var setting = new OverrideGraphicSettings();
            setting.SetDetailLevel(ViewDetailLevel.Coarse);
            viewSection.SetCategoryOverrides(rebarCategoryId, setting);
        }

        public static List<Rebar> GetStiruppsInView(FamilyInstance beam)
        {
            var list = new List<Rebar>();
            var rebars = RebarHostData.GetRebarHostData(beam).GetRebarsInHost();
            foreach (var rebar in rebars)
            {
                if (IsStirupp(rebar))
                {
                    list.Add(rebar);
                }
            }
            return list;
        }

        public static bool IsStirupp(Rebar rebar)
        {
            var i = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_STYLE).AsInteger();
            if (i == 1)
            {
                return true;
            }

            return false;
        }

        public static List<Rebar> GetStandardBarsInView(FamilyInstance beam)
        {
            var list = new List<Rebar>();
            var rebars = RebarHostData.GetRebarHostData(beam).GetRebarsInHost();
            foreach (var rebar in rebars)
            {
                if (IsStirupp(rebar) == false)
                {
                    list.Add(rebar);
                }
            }
            return list;
        }

        public static RebarLocationInBeam GetRebarStandardLocationInBeam(Rebar rebar, double beamTopElevation, double beamBotElevation)
        {
            var lc = RebarLocationInBeam.Undefine;
            var height = beamTopElevation - beamBotElevation;
            var c = rebar.GetCenterlineCurves(true, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0)
                .OrderBy(x => x.ApproximateLength).LastOrDefault();
            if (c != null)
            {
                var point = c.Midpoint();
                var z = point.Z;
                if (z > beamBotElevation && z < beamBotElevation + height * 0.4)
                {
                    lc = RebarLocationInBeam.Bot;
                }
                else if (z < beamTopElevation && z > beamTopElevation - height * 0.4)
                {
                    lc = RebarLocationInBeam.Top;
                }
                else if (z <= beamTopElevation - height * 0.4 && z >= beamBotElevation + height * 0.4)
                {
                    lc = RebarLocationInBeam.Mid;
                }
            }
            return lc;
        }

        public static List<Rebar> GetRebarsNotIncludeInListOtherRebars(this List<Rebar> origin, List<Rebar> others)
        {
            var ids = others.Where(x => x.IsValidObject).Select(x => x.Id.GetElementIdValue()).ToList();
            return origin.Where(x => ids.Contains(x.Id.GetElementIdValue()) == false).ToList();
        }

        public static List<Rebar> GetRebarsIncludeInLis2List(this List<Rebar> origin, List<Rebar> others)
        {
            var ids = others.Where(x => x.IsValidObject).Select(x => x.Id.GetElementIdValue()).ToList();
            return origin.Where(x => ids.Contains(x.Id.GetElementIdValue())).ToList();
        }

        public static Line GetMaxLineOfRebar(Rebar rebar)
        {
            return rebar.GetCenterlineCurves(true, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0).Where(x => x is Line).Cast<Line>()
                .OrderBy(x => x.ApproximateLength).LastOrDefault();
        }

        public static void HideViewersNotUse(Autodesk.Revit.DB.View hostView, List<Autodesk.Revit.DB.View> neededViews)
        {
            try
            {
                var neededViewNames = neededViews.Where(x => x.IsValidObject).Select(x => x.Name).ToList();
                var viewers =
                   new FilteredElementCollector(AC.Document, hostView.Id).OfCategory(BuiltInCategory.OST_Viewers);
                var ids = new List<ElementId>();
                foreach (var viewer in viewers)
                {
                    var name = viewer.Name;
                    if (neededViewNames.Contains(name) == false)
                    {
                        ids.Add(viewer.Id);
                    }
                }
                hostView.HideElements(ids);
            }
            catch (Exception)
            {

            }

        }

        public static bool IsSheetNumberExist(List<ViewSheet> viewSheets, string sheetNumber, out ViewSheet vs)
        {
            vs = viewSheets.FirstOrDefault(x => x.SheetNumber == sheetNumber);
            if (vs == null)
            {
                return false;
            }
            return true;
        }

        public static int GetId(Element ele)
        {
            if (ele != null)
            {
                return ele.Id.GetElementIdValue();
            }

            return -1;
        }
    }
}