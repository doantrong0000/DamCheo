using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
    public class BeamDrawingService
    {
        public BeamDrawingSetting Setting { get; set; }
        public ViewSection DetailView { get; set; }
        public List<ViewSection> ViewSections { get; set; } = new List<ViewSection>();
        public ViewSheet ViewSheet { get; set; }
        public View3D View3D { get; set; }

        public BeamDrawingService(BeamDrawingSetting setting)
        {
            Setting = setting;
        }

        public void Run()
        {
            ViewSheet = CreateViewSheet();
        }

        private ViewSheet CreateViewSheet()
        {
            ViewSheet vs = null;
            var sheetNumber = Setting.BeamSheetSetting.SheetNumber;
            var sheetName = Setting.BeamSheetSetting.SheetName;
            var titleBlockSymbol = Setting.BeamSheetSetting.TitleBlock;
            var id = ElementId.InvalidElementId;
            if (titleBlockSymbol != null)
            {
                id = titleBlockSymbol.Id;
            }
            vs = SharedData.Instance.ViewSheets.FirstOrDefault(x => x.SheetNumber == sheetNumber);
            if (vs == null)
            {

                vs = ViewSheet.Create(AC.Document, id);
                if (string.IsNullOrWhiteSpace(sheetName) == false)
                {
                    vs.Name = sheetName;
                }

                try
                {
                    vs.SheetNumber = sheetNumber;
                }
                catch
                {
                    //
                }

            }



            AC.Document.Regenerate();

            var pointForDetail = XYZ.Zero;

            var titleBlock = new FilteredElementCollector(AC.Document, vs.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();

            var titleBlockBb = titleBlock?.get_BoundingBox(vs);

            var centerBottom = XYZ.Zero;


            if (View3D != null)
            {

                var view3dPoint = new XYZ(titleBlockBb.Max.X / 2, titleBlockBb.Max.Y - 200.MmToFoot(), 0);
                var viewport3D = Viewport.Create(AC.Document, vs.Id, View3D.Id, view3dPoint);

                AC.Document.Regenerate();
                var box3D = viewport3D.GetBoxOutline();
            }

            if (Setting.BeamSectionSetting.IsLongSection && DetailView != null)
            {

                var detailViewport = Viewport.Create(AC.Document, vs.Id, DetailView.Id, pointForDetail);

                try
                {
                    detailViewport.SetParameterValueByName(BuiltInParameter.VIEWPORT_DETAIL_NUMBER, detailViewport.Id.GetElementIdValue().ToString());
                }
                catch (Exception e)
                {
                    AC.Log("detail number", e);
                }

                var detailBb = detailViewport.get_BoundingBox(vs);

                var center = detailBb.CenterPoint();

                if (titleBlockBb != null)
                {
                    var maxY = detailBb.Max.Y;

                    var centerTop = center.ModifyVector(maxY, XYZEnum.Y);
                    var ttCenter = titleBlockBb.CenterPoint().ModifyVector(titleBlockBb.Max.Y - 10.MmToFoot(), XYZEnum.Y);
                    var ttCenterTop = new XYZ(ttCenter.X, titleBlockBb.Max.Y + 100.MmToFoot(), 0);

                    ElementTransformUtils.MoveElement(AC.Document, detailViewport.Id, ttCenterTop - centerTop);

                    AC.Document.Regenerate();

                    detailBb = detailViewport.get_BoundingBox(vs);

                    center = detailBb.CenterPoint();
                }
                var minY = detailBb.Min.Y;

                centerBottom = center.ModifyVector(minY, XYZEnum.Y).Add(XYZ.BasisY * -10.MmToFoot());
            }


            if (DetailView != null)
            {
                ViewSections = ViewSections.OrderBy(x => x.Origin.DotProduct(DetailView.RightDirection)).ToList();
            }


            try
            {
                if (Setting.BeamSectionSetting.IsCrossSection)
                {

                    var sectionViewports = AddViewsToSheet(vs, ViewSections, XYZ.Zero, Setting.BeamSectionSetting.ViewportType?.Id);

                    var topCenterPoint = GetTopCenterPointOfViewports(sectionViewports);

                    if (!centerBottom.IsEqual(XYZ.Zero))
                        ElementTransformUtils.MoveElements(AC.Document, sectionViewports.Select(x => x.Id).ToList(), centerBottom - topCenterPoint);
                }
            }
            catch
            {
                //
            }
            return vs;
        }

        private List<Viewport> AddViewsToSheet(ViewSheet vs, List<ViewSection> views, XYZ p, ElementId viewportTypeId)
        {

            var vps = new List<Viewport>();

            XYZ leftPoint = p;

            var isFirst = true;

            foreach (var view in views)
            {
                var vp = Viewport.Create(AC.Document, vs.Id, view.Id, p);

                if (viewportTypeId != null)
                {
                    vp.ChangeTypeId(viewportTypeId);
                }

                vps.Add(vp);

                AC.Document.Regenerate();

                if (isFirst)
                {
                    var boxOutline = vp.GetBoxOutline();
                    var max = boxOutline.MaximumPoint;
                    leftPoint = new XYZ(max.X, leftPoint.Y, 0);
                    isFirst = false;
                }
                else
                {
                    var boxOutline = vp.GetBoxOutline();
                    var min = boxOutline.MinimumPoint;
                    var max = boxOutline.MaximumPoint;
                    ElementTransformUtils.MoveElement(AC.Document, vp.Id, XYZ.BasisX * (leftPoint.X - min.X));
                    AC.Document.Regenerate();
                    leftPoint = leftPoint.Add(XYZ.BasisX * (max.X - min.X));
                }
            }

            return vps;
        }

        private XYZ GetTopCenterPointOfViewports(List<Viewport> viewports)
        {
            var xMax = double.MinValue;
            var xMin = double.MaxValue;
            var yMax = double.MinValue;
            var yMin = double.MaxValue;
            foreach (var viewport in viewports)
            {
                var outline = viewport.GetBoxOutline();
                var max = outline.MaximumPoint;
                var min = outline.MinimumPoint;
                if (xMax < max.X)
                {
                    xMax = max.X;
                }
                if (yMax < max.Y)
                {
                    yMax = max.Y;
                }
                if (xMin > min.X)
                {
                    xMin = min.X;
                }
                if (yMin > min.Y)
                {
                    yMin = min.Y;
                }
            }
            return new XYZ((xMax + xMin) / 2, yMax, 0);
        }
    }
}