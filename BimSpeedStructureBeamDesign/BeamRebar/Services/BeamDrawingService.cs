using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
    public static class BeamDrawingService
    {
        #region Tạo View Nhỏ

        //Tạo View nhỏ
        private static List<List<CrossSectionModel>> GetPositionNeedCrossSections(BeamModel beamModel)
        {
            var list = new List<CrossSectionModel>();
            foreach (var spanModel in beamModel.SpanModels)
            {
                var cs1 = new CrossSectionModel(spanModel, 1);
                var cs2 = new CrossSectionModel(spanModel, 2);
                var cs3 = new CrossSectionModel(spanModel, 3);
                list.Add(cs1);
                list.Add(cs2);
                list.Add(cs3);
            }

            var distinct = list.Distinct().ToList();

            var lst = new List<List<CrossSectionModel>>();

            foreach (var crossSectionModel in distinct)
            {
                var sames = list.Where(x => x.Equals(crossSectionModel)).ToList();
                lst.Add(sames);
            }

            return lst;
        }

        public static List<ViewSection> DetailingCrossSection(BeamModel beamModel, ViewSection horizontalSection)
        {
            var rList = new List<ViewSection>();
            var sectionsList = GetPositionNeedCrossSections(beamModel);
            var j = 1;
            var mark = beamModel.SpanModels.Select(x => x.Mark).FirstOrDefault(x => string.IsNullOrEmpty(x) == false) ?? beamModel.SpanModels[0].Beam.Id.GetElementIdValue().ToString();
            
            foreach (var list in sectionsList)
            {
                var crossSectionModel = list.FirstOrDefault();
                if (crossSectionModel == null)
                {
                    return rList;
                }

                crossSectionModel.Detailing(mark, ref j);
                rList.Add(crossSectionModel.ViewSection);
                //referenced view
                for (int i = 1; i < list.Count; i++)
                {
                    var section = list[i];
                    var span = section.SpanModel;
                    XYZ head = section.Point.EditZ(span.TopElevation + 250.MmToFoot());
                    XYZ tail = section.Point.EditZ(span.BotElevation - 250.MmToFoot());
                    ViewSection.CreateReferenceSection(AC.Document, horizontalSection.Id, crossSectionModel.ViewSection.Id, head, tail);
                }
            }
            return rList;
        }

        #endregion Tạo View Nhỏ

        public static HorizontalSectionModel DetailingHorizontalSection(BeamModel beamModel)
        {
            var section = new HorizontalSectionModel(beamModel);
            section.Detailing();
            var mark = beamModel.SpanModels.Select(x => x.Mark).FirstOrDefault();
            var name = BeamRebarDefine.GetChiTietDam();

            if (!string.IsNullOrEmpty(name))
            {
                name = name + mark;
            }
            

            while (true)
            {
                try
                {
                    section.ViewSection.Name = name;
                    break;
                }
                catch
                {
                    name += ".";
                }
            }
            return section;
        }

        public static ViewSheet AddToSheet(Dictionary<string, ViewSheet> dicSheets, string sheetNumber, string sheetName, ViewSection horizontalView, List<ViewSection> crossSections, FamilySymbol titleBlock)
        {
            ViewSheet vs = null;
            if (string.IsNullOrEmpty(sheetNumber) == false && dicSheets.ContainsKey(sheetNumber))
            {
                vs = dicSheets[sheetNumber];
            }
            else
            {
                var titleBlockId = AC.Document.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_TitleBlocks));
                vs = ViewSheet.Create(AC.Document, titleBlockId);
                if (string.IsNullOrEmpty(sheetNumber) == false)
                {
                    vs.SheetNumber = sheetNumber;
                }
                if (string.IsNullOrEmpty(sheetName) == false)
                {
                    vs.Name = sheetName;
                }
            }
            AC.Document.Regenerate();
            var pointForDetail = XYZ.Zero;

            var detailViewport = Viewport.Create(AC.Document, vs.Id, horizontalView.Id, pointForDetail);
            try
            {
                detailViewport.SetParameterValueByName("Detail Number", detailViewport.Id.GetElementIdValue().ToString());
            }
            catch (Exception e)
            {
                AC.Log("detail number", e);
            }

            if (BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamDetailSetting.ViewportType != null)
            {
                detailViewport.ChangeTypeId(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamDetailSetting.ViewportType.Id);
            }

            var detailBb = detailViewport.get_BoundingBox(vs);
            var center = detailBb.CenterPoint();
            var titleBlockBb = titleBlock?.get_BoundingBox(vs);
            if (titleBlockBb != null)
            {
                var maxY = detailBb.Max.Y;
                var centerTop = center.ModifyVector(maxY, XYZEnum.Y);
                var ttCenter = titleBlockBb.CenterPoint().ModifyVector(titleBlockBb.Max.Y - 10.MmToFoot(), XYZEnum.Y);
                var ttCenterTop = new XYZ(ttCenter.X, titleBlockBb.Max.Y, 0);
                ElementTransformUtils.MoveElement(AC.Document, detailViewport.Id, ttCenterTop - centerTop);
                AC.Document.Regenerate();
                detailBb = detailViewport.get_BoundingBox(vs);
                center = detailBb.CenterPoint();
            }
            var minY = detailBb.Min.Y;
            var centerBotom = center.ModifyVector(minY, XYZEnum.Y).Add(XYZ.BasisY * -10.MmToFoot());
            var sectionViewports = AddViewsToSheet(vs, crossSections, XYZ.Zero, BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViewportType?.Id);
            var topCenterPoint = GetTopCenterPointOfViewports(sectionViewports);
            ElementTransformUtils.MoveElements(AC.Document, sectionViewports.Select(x => x.Id).ToList(), centerBotom - topCenterPoint);
            return vs;
        }

        private static List<Viewport> AddViewsToSheet(ViewSheet vs, List<ViewSection> views, XYZ p, ElementId viewportTypeId)
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

        private static XYZ GetTopCenterPointOfViewports(List<Viewport> viewports)
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