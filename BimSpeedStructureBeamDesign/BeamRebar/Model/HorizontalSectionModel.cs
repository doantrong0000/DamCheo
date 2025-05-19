using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.Others;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using MoreLinq.Extensions;
using ElementGeometryModel = BimSpeedStructureBeamDesign.BeamDrawing.Model.ElementGeometryModel;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class HorizontalSectionModel
   {
      public BeamModel BeamModel { get; set; }
      public List<MainRebar> MainTopRebars { get; set; } = new List<MainRebar>();
      public List<MainRebar> MainBotRebars { get; set; } = new List<MainRebar>();
      public List<TopAdditionalBar> TopAdditionalBars { get; set; } = new List<TopAdditionalBar>();
      public List<BottomAdditionalBar> BottomAdditionalBars { get; set; } = new List<BottomAdditionalBar>();
      public List<SpanModel> SpanModels { get; set; } = new List<SpanModel>();
      private XYZ right;
      public ViewSection ViewSection { get; set; }
      public BeamDetailSetting Setting { get; set; }

      private BPlane plane = null;
      private List<ElementGeometryModel> floorElements = new List<ElementGeometryModel>();
      private List<ElementGeometryModel> supportElements = new List<ElementGeometryModel>();

      private List<ElementGeometryModel> gridGeometryModels = new List<ElementGeometryModel>();
      private double beamTopElevation;
      private double beamBotElevation;
      private List<ElementGeometryModel> topSupports = new List<ElementGeometryModel>();
      private List<ElementGeometryModel> botSupports = new List<ElementGeometryModel>();
      private XYZ leftPoint;
      private XYZ rightPoint;
      private Line beamLine;

      public HorizontalSectionModel(BeamModel beamModel)
      {
         BeamModel = beamModel;
         GetData();
      }

      private void GetData()
      {
         Setting = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamDetailSetting;
         SpanModels = BeamModel.SpanModels;
         MainTopRebars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars;
         MainBotRebars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars;
         TopAdditionalBars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars;
         BottomAdditionalBars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars;
      }

      private void GetGridInfo()
      {
         var grids = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Grid)).Cast<Grid>().OrderBy(x => x.Curve.SP().DotProduct(ViewSection.RightDirection))
             .ToList();
         foreach (var grid in grids)
         {
            if (!grid.IsCurved)
            {
               var model = new ElementGeometryModel(grid);
               model.ReferenceAndPoints.ForEach(x => x.Normal = ViewSection.RightDirection);
               gridGeometryModels.Add(model);
            }
         }
      }

      private void GetSupporstInView()
      {
         if (ViewSection != null)
         {
            //Get Beams and Walls in view
            var beams = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns).ToElements();
            var walls = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Wall))
                .OfCategory(BuiltInCategory.OST_Walls).ToElements();
            var wallsAndColumns = beams.Union(walls).ToList();
            foreach (var ele in wallsAndColumns)
            {
               var egm = new ElementGeometryModel(ele);
               supportElements.Add(egm);
            }
            var floors = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Floor)).ToElements();
            foreach (var ele in floors)
            {
               var egm = new ElementGeometryModel(ele);
               floorElements.Add(egm);
            }
         }
      }

      public void Detailing()
      {
         if (Setting.DauMocThep?.IsActive == false)
         {
            Setting.DauMocThep.Activate();
         }
         if (Setting.BreakLineSymbol is { IsActive: false })
         {
            Setting.BreakLineSymbol.Activate();
         }

         CreateViewSection();

         CommonService.SetRebarDetailLevel(ViewSection);
         AC.Document.Regenerate();
         CreateBreakLines();
         SetGridsExtend();
         CreateDimensions();
         CreateTags();
         CreateMocThepForAdditionalBars();
         if (Setting.ViewTemplate != null)
         {
            ViewSection.ViewTemplateId = Setting.ViewTemplate?.Id;
         }
         HideNotNeededElements();
      }

      private void HideNotNeededElements()
      {
         //Hide Rebar
         var rebars = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Rebar)).Cast<Rebar>()
             .ToList();
         var idsHost = SpanModels.Select(x => x.Beam.Id.GetElementIdValue()).ToList();
         foreach (var rebar in rebars)
         {
            try
            {
               if (idsHost.Contains(rebar.GetHostId().GetElementIdValue()) == false)
               {
                  //Hide
                  ViewSection.HideElements(new List<ElementId>() { rebar.Id });
               }
            }
            catch
            {
               //
               AC.Log("HorizontalSectionModel.cs +Lỗi hide sections");
            }
         }
      }

      private void CreateViewSection()
      {
         ViewSection vs = null;
         beamTopElevation = SpanModels.Max(x => x.TopElevation);
         beamBotElevation = SpanModels.Max(x => x.BotElevation);
         var b = SpanModels.Max(x => x.Width);
         var h = beamTopElevation - beamBotElevation;
         var span0 = SpanModels.First();
         leftPoint = span0.BotLeft.EditZ(beamBotElevation);
         if (span0.LeftSupportModel != null)
         {
            leftPoint = leftPoint.Add(-span0.Direction * (span0.LeftSupportModel.Width + 50.MmToFoot()));
         }
         var span1 = SpanModels.Last();
         rightPoint = span1.BotRight.EditZ(beamBotElevation);
         if (span0.RightSupportModel != null)
         {
            rightPoint = rightPoint.Add(span0.Direction * (span0.RightSupportModel.Width + 50.MmToFoot()));
         }

         beamLine = leftPoint.CreateLine(rightPoint);
         var p0 = leftPoint;
         var p1 = rightPoint;

         XYZ a1 = p0;
         XYZ a2 = p1;

         var length = (p0 - p1).GetLength();

         var min = new XYZ(-0.5 * length - 200.MmToFoot(), -300.MmToFoot(), -b / 2 - 100.MmToFoot());
         var max = new XYZ(0.5 * length + 200.MmToFoot(), h + 300.MmToFoot(), b / 2 + 100.MmToFoot());

         var mid = (p0 + p1) / 2;
         var beamDirection = (a2 - a1).Normalize();
         var up = XYZ.BasisZ;
         var viewDir = -beamDirection.CrossProduct(up);

         var tf = Transform.Identity;
         tf.Origin = mid;
         tf.BasisX = -beamDirection;
         tf.BasisY = up;
         tf.BasisZ = viewDir;

         var sectionBox = new BoundingBoxXYZ
         {
            Transform = tf,
            Min = min,
            Max = max
         };

         if (Setting.ViewFamilyType.ViewFamily == ViewFamily.Detail)
         {
            vs = ViewSection.CreateDetail(AC.Document, Setting.ViewFamilyType.Id, sectionBox);
         }

         if (Setting.ViewFamilyType.ViewFamily == ViewFamily.Section)
         {
            vs = ViewSection.CreateSection(AC.Document, Setting.ViewFamilyType.Id, sectionBox);
         }

         if (Setting.ViewTemplate != null)
         {
            vs.ViewTemplateId = Setting.ViewTemplate.Id;
         }
         AC.Document.Regenerate();
         vs.Scale = Setting.Scale;
         right = BeamModel.Direction;
         ViewSection = vs;
         plane = vs.ToBPlane();
         GetGridInfo();
         GetSupporstInView();
         HideSections();
      }

      private void HideSections()
      {
         var ids = new FilteredElementCollector(AC.Document, ViewSection.Id).OfCategory(BuiltInCategory.OST_Viewers).ToElementIds();
         if (ids.Count > 0)
         {
            ViewSection.HideElements(ids);
         }

         //Hide Rebars
         var rebars = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Rebar)).Cast<Rebar>();
         var idsToHide = new List<ElementId>();
         var beamIds = SpanModels.Select(x => x.Beam.Id.GetElementIdValue()).ToList();
         foreach (var rebar in rebars)
         {
            var hostId = rebar.GetHostId();
            if (hostId == null)
            {
               ids.Add(rebar.Id);
            }
            else
            {
               if (beamIds.Contains(hostId.GetElementIdValue()) == false)
               {
                  ids.Add(rebar.Id);
               }
            }
         }

         if (idsToHide.Count > 0)
         {
            ViewSection.HideElements(idsToHide);
         }

         //Hide Others
      }

      private void CreateBreakLines()
      {
         if (Setting.BreakLineSymbol == null)
         {
            return;
         }
         var extendedLine = beamLine.ExtendLineBothEnd(1);
         var p1 = extendedLine.SP()
             .EditZ(beamBotElevation - Setting.KhoangCachBreakLineDenDam);
         var p2 = extendedLine.EP()
             .EditZ(beamBotElevation - Setting.KhoangCachBreakLineDenDam);
         var botLine = Line.CreateBound(p1, p2);

         var p3 = extendedLine.SP()
             .EditZ(beamTopElevation + Setting.KhoangCachBreakLineDenDam);
         var p4 = extendedLine.EP()
             .EditZ(beamTopElevation + Setting.KhoangCachBreakLineDenDam);
         var topLine = Line.CreateBound(p3, p4);

         foreach (var egm in supportElements)
         {
            if (true)
            {
               var lines = GetLineIntersectWithElement(egm, botLine);
               foreach (var line in lines)
               {
                  var l = line.ExtendLineBothEnd(30.MmToFoot()).ProjectOn(plane) as Line;
                  var fi = AC.Document.Create.NewFamilyInstance(l, Setting.BreakLineSymbol, ViewSection);
                  AC.Document.Regenerate();
                  var bb = fi.get_BoundingBox(ViewSection);
                  if (bb != null)
                  {
                     botSupports.Add(egm);
                     if (Math.Abs(bb.Max.Z - l.SP().Z) > Math.Abs(bb.Min.Z - l.SP().Z))
                     {
                        ((LocationCurve)fi.Location).Curve = l.CreateReversed();
                     }
                  }
               }
            }
            //Top BreakLines
            if (true)
            {
               var lines = GetLineIntersectWithElement(egm, topLine);
               foreach (var line in lines)
               {
                  var l = line.ExtendLineBothEnd(30.MmToFoot()).ProjectOn(plane) as Line;
                  var fi = AC.Document.Create.NewFamilyInstance(l, Setting.BreakLineSymbol, ViewSection);
                  AC.Document.Regenerate();
                  var bb = fi.get_BoundingBox(ViewSection);
                  if (bb != null)
                  {
                     topSupports.Add(egm);
                     if (Math.Abs(bb.Max.Z - l.SP().Z) < Math.Abs(bb.Min.Z - l.SP().Z))
                     {
                        ((LocationCurve)fi.Location).Curve = line.CreateReversed();
                     }
                  }
               }
            }
         }
      }

      private void CreateDimensions()
      {
         try
         {
      
            var gridRa = new ReferenceArray();
            gridGeometryModels.ForEach(x => gridRa.Append(x.ReferenceAndPoints.FirstOrDefault()?.Reference));

            #region Top Dims

            //Grids Dimension:
            var sp = beamLine.SP();
            var p3 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam -
                                         Setting.KhoangCachGiua2Dim * ViewSection.Scale);

            CommonService.CreateDimension(gridRa, p3, ViewSection.RightDirection, Setting.DimensionTypeFixed, ViewSection);

            //Dim Grid And supports
            var raps4 = gridGeometryModels.SelectMany(x => x.ReferenceAndPoints);

            var botSupportRaps = botSupports.Select(x => x.ReferenceAndPoints).Flatten().Cast<ReferenceAndPoint>().Where(x => x != null)
                .Where(x => x.Normal.IsParallel(right)).ToList();

            // SetPresentationForStirrup();
            AC.Document.Regenerate();
            var stirrupsRa = GetReferenceAndPointsOfStirrups2().Where(x => x != null).ToList();
            var consoleRaps = GetReferenceAtConsole().Where(x => x != null).ToList();

            var raps5 = raps4.Union(botSupportRaps).Union(consoleRaps).Where(x => x.Normal.IsParallel(right)).DistinctBy2(x => x.Point.DotProduct(right).Round2Number());

            var rf4 = new ReferenceArray();
            foreach (var referenceAndPoint in raps5)
            {
               //  if (referenceAndPoint.PlanarFace != null)
               {
                  rf4.Append(referenceAndPoint.Reference);
               }
            }

            stirrupsRa.ForEach(x => rf4.Append(x));
            var p4 = sp.EditZ(beamTopElevation + Setting.KhoangCachDimDenDam);

            if (rf4.Size > 1)
            {
               CommonService.CreateDimension(rf4, p4, ViewSection.RightDirection, Setting.DimensionTypeFixed, ViewSection);
            }

            //Dim thep gia cuong nhip
            var list = new List<ReferenceAndPoint>();
            foreach (var bar in BottomAdditionalBars)
            {
               var raps = GetBarEndReferences(bar.Rebars.FirstOrDefault());
               list.AddRange(raps);
            }

            list = list.DistinctBy2(x => x.Point.DotProduct(right).Round2Number()).ToList();

            #endregion Top Dims

            //Bot Dim
            var ra1 = new ReferenceArray();
            var raps1 = new List<ReferenceAndPoint>(gridGeometryModels.Select(x => x.ReferenceAndPoints).Flatten().Cast<ReferenceAndPoint>());

            raps1 = raps1.Concat(list).DistinctBy2(x => x.Point.DotProduct(right).Round2Number()).ToList();
            raps1.ForEach(x => ra1.Append(x.Reference));
            var p1 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam);
            CommonService.CreateDimension(ra1, p1, ViewSection.RightDirection, Setting.DimensionTypeFixed, ViewSection);


            #region Right Dim

            var ra6 = new ReferenceArray();
            var lastSpan = SpanModels.Last();
            var egm = new ElementGeometryModel(lastSpan.Beam);
            var filter = new ElementIntersectsSolidFilter(lastSpan.BeamGeometry.OriginalSolidTransformed);
            var floorElementsIntersect = new List<ElementGeometryModel>();
            foreach (var floorElement in floorElements)
            {
               if (filter.PassesFilter(floorElement.Element))
               {
                  floorElementsIntersect.Add(floorElement);
               }
            }

            var raps6 = floorElementsIntersect.Select(x => x.ReferenceAndPoints).Flatten().Cast<ReferenceAndPoint>()
                .Concat(egm.ReferenceAndPoints).Where(x => x.Normal.IsParallel(XYZ.BasisZ)).ToList();
            var botRf6 = raps6.FirstOrDefault(x => x.Point.Z.IsEqual(lastSpan.BotElevation));
            var topRf6 = raps6.FirstOrDefault(x => x.Point.Z.IsEqual(lastSpan.TopElevation));
            if (botRf6 != null && topRf6 != null)
            {
               ra6.Append(botRf6.Reference);
               ra6.Append(topRf6.Reference);
               var p6 = rightPoint.Add(right * 150.MmToFoot());
               CommonService.CreateDimension(ra6, p6, XYZ.BasisZ, Setting.DimensionTypeFixed, ViewSection);
            }

            #endregion Right Dim
         }
         catch (Exception e)
         {
            AC.Log("Lỗi Tạo Dim Detail" + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);
         }
      }

      private List<ReferenceAndPoint> GetReferenceAtConsole()
      {
         var list = new List<ReferenceAndPoint>();
         var first = SpanModels.FirstOrDefault();
         if (first != null)
         {
            var fegm = new ElementGeometryModel(first.Beam);
            var f = fegm.ReferenceAndPoints.OrderByDescending(x => x.Point.DotProduct(ViewSection.RightDirection))
                  .FirstOrDefault();
            list.Add(f);
         }
         var second = SpanModels.LastOrDefault();
         if (second != null)
         {
            var segm = new ElementGeometryModel(second.Beam);
            var f = segm.ReferenceAndPoints.OrderByDescending(x => x.Point.DotProduct(-ViewSection.RightDirection))
                .FirstOrDefault();
            list.Add(f);
         }
         return list;
      }

      #region Tags

      private void CreateTags()
      {
         TagStirrups();
         TagStandardBars();
      }

      private void TagStandardBars()
      {
         foreach (var spanModel in SpanModels)
         {
            TagStandardBarsAtPosition(2, spanModel);
            if (spanModel.Length >= 2000.MmToFoot())
            {
               TagStandardBarsAtPosition(1, spanModel);

               TagStandardBarsAtPosition(3, spanModel);
            }
         }
      }

      private void TagStandardBarsAtPosition(int position, SpanModel spanModel)
      {
         var mainBarTop = MainTopRebars.FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var mainBarBot = MainBotRebars.FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var topAdditionalBarLayer1 = TopAdditionalBars.Where(x => x.Layer.Layer == 1).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var topAdditionalBarLayer2 = TopAdditionalBars.Where(x => x.Layer.Layer == 2).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var topAdditionalBarLayer3 = TopAdditionalBars.Where(x => x.Layer.Layer == 3).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var bottomAdditionalBarLayer1 = BottomAdditionalBars.Where(x => x.Layer.Layer == 1).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var bottomAdditionalBarLayer2 = BottomAdditionalBars.Where(x => x.Layer.Layer == 2).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));
         var bottomAdditionalBarLayer3 = BottomAdditionalBars.Where(x => x.Layer.Layer == 3).FirstOrDefault(x => IsHasRebarAtPosition(position, spanModel, x.Curves));

         var p = spanModel.BotLine.Evaluate(0.15, true);
         if (position == 2)
         {
            p = spanModel.BotLine.Evaluate(0.5, true);
         }
         else if (position == 3)
         {
            p = spanModel.BotLine.Evaluate(0.9, true);
         }
         TagStandardBars(mainBarBot?.Rebars.FirstOrDefault(), bottomAdditionalBarLayer1?.Rebars.FirstOrDefault(), bottomAdditionalBarLayer2?.Rebars.FirstOrDefault(), bottomAdditionalBarLayer3?.Rebars.FirstOrDefault(), position, p, false);
         p = p.EditZ(spanModel.TopElevation);
         TagStandardBars(mainBarTop?.Rebars.FirstOrDefault(), topAdditionalBarLayer1?.Rebars.FirstOrDefault(), topAdditionalBarLayer2?.Rebars.FirstOrDefault(), topAdditionalBarLayer3?.Rebars.FirstOrDefault(), position, p, true);
      }

      private void TagStandardBars(Rebar mainbar, Rebar additionalBar1, Rebar additionalBar2, Rebar additionalBar3, int viTri, XYZ p, bool isTop = true)
      {
         var i = 1;
         if (isTop == false)
         {
            i = -1;
         }
         var z = p.Z + Setting.KhoangCachTagDenDam * i;
         if (additionalBar3 != null)
         {
            //Tag
            if (isTop && viTri != 2 || isTop == false && viTri == 2)
            {
               var p3 = p.Add(right * -25.MmToFoot()).EditZ(z);
               CreateIndependentTag(additionalBar3, p3, false);
               z = z + (Setting.Scale * 4.MmToFoot() + 10.MmToFoot()) * i;
            }
         }
         if (additionalBar2 != null)
         {
            //Tag
            if (isTop && viTri != 2 || isTop == false && viTri == 2)
            {
               var p2 = p.Add(right * -25.MmToFoot()).EditZ(z);
               CreateIndependentTag(additionalBar2, p2, false);
               //z = z + (scale * 4.MmToFoot() + 10.MmToFoot()) * i;
            }
         }
         if (additionalBar1 != null)
         {
            //Tag
            if (isTop && viTri != 2 || isTop == false && viTri == 2)
            {
               var p1 = p.Add(right * 25.MmToFoot()).EditZ(z);
               CreateIndependentTag(additionalBar1, p1, true);
            }
         }

         if (mainbar != null)
         {
            //Tags mainbar ở giữa trên top hoặc ở left phía bot
            if (isTop && viTri == 2 || isTop == false && viTri == 1)
            {
               var p1 = p.Add(right * -25.MmToFoot()).EditZ(z);
               if (viTri == 2 || additionalBar1 != null)
               {
                  CreateIndependentTag(mainbar, p1, false);
               }
               else
               {
                  CreateIndependentTag(mainbar, p1, true);
               }
            }
         }
      }

      private bool IsHasRebarAtPosition(int viTri, SpanModel spanModel, List<Curve> curves)
      {
         var line = spanModel.TopLine;
         var positionPoint = line.Evaluate(0.1, true);
         if (viTri == 2)
         {
            positionPoint = line.Evaluate(0.5, true);
         }
         else if (viTri == 3)
         {
            positionPoint = line.Evaluate(0.9, true);
         }

         if (curves.Count > 0)
         {
            var first = curves.First();
            var last = curves.Last();
            var sp = first.SP();
            var ep = last.EP();
            var d = positionPoint.DotProduct(right);
            var s = sp.DotProduct(right);
            var e = ep.DotProduct(right);
            var max = Math.Max(s, e);
            var min = Math.Min(s, e);
            if (d.IsBetweenEqual(min, max, 0.1))
            {
               return true;
            }
         }

         return false;
      }

      private void TagStirrups()
      {
         var rebars = new List<Rebar>();
         foreach (var spanModel in SpanModels)
         {
            var info = spanModel.StirrupForSpan;
            rebars.AddRange(info.MainStirrupEnd1);
            rebars.AddRange(info.MainStirrupEnd2);
            rebars.AddRange(info.MainStirrupMid);
         }

         foreach (var rebar in rebars)
         {
            var rf = new Reference(rebar);
            var center = rebar.GetElementCenter();
            var number = rebar.NumberOfBarPositions;
            var spacing = rebar.MaxSpacing.FootToMm();

            var z = beamTopElevation +
                    Setting.KhoangCachDimDenDam;
            var p = center.EditZ(z);
            if (number < 7 && spacing < 60)
            {
               p = center;
               TagUtils2.CreateIndependentTag(Setting.TagThepDaiTrai.Id, ViewSection.Id, rebar, false,
                   TagOrientation.Horizontal, p);
               continue;
            }
            var tag = TagUtils2.CreateIndependentTag(Setting.TagThepDaiTrai.Id, ViewSection.Id, rebar, false,
                TagOrientation.Horizontal, p);
            AC.Document.Regenerate();
            var bb = tag.get_BoundingBox(ViewSection);
            if (bb != null)
            {
               var mid = bb.CenterPoint();
               var t = (p - mid).DotProduct(ViewSection.RightDirection);
               ElementTransformUtils.MoveElement(AC.Document, tag.Id, t * ViewSection.RightDirection);
               var tt = bb.Max.Z - z;
               ElementTransformUtils.MoveElement(AC.Document, tag.Id, (-tt - ViewSection.Scale.MmToFoot()) * ViewSection.UpDirection);
            }
         }
      }

      private void CreateIndependentTag(Rebar rebar, XYZ p, bool isRight)
      {
         if (rebar == null)
         {
            return;
         }
         var tagId = isRight ? Setting.TagRebarStandardPhai?.Id : Setting.TagRebarStandardTrai?.Id;
         if (tagId == null)
         {

         }

         var rf = new Reference(rebar);
         var tag = TagUtils2.CreateIndependentTag(tagId, ViewSection.Id, rebar, false, TagOrientation.Horizontal, p);
         AC.Document.Regenerate();
         var bb = tag.get_BoundingBox(ViewSection);
         if (bb != null)
         {
            tag.HasLeader = true;
            AC.Document.Regenerate();
            var length = Math.Abs((bb.Max - bb.Min).DotProduct(right));
            var pLeft = bb.Max.ProjectOnto(plane).EditZ(p.Z);
            var pRight = bb.Min.ProjectOnto(plane).EditZ(p.Z);
            var d1 = pLeft.DotProduct(right);
            var d2 = pRight.DotProduct(right);
            if (d1 > d2)
            {
               var p3 = pLeft;
               pLeft = pRight;
               pRight = p3;
            }

            if (isRight)
            {
               //tag.LeaderElbow = p.Add(-right * length);
               tag.SetLeaderElbow(pLeft.Add(-right * 0));
            }
            else
            {
               //tag.LeaderElbow = p.Add(right * length);
               tag.SetLeaderElbow(pRight.Add(right * 0));
            }
            var translation = p - tag.LeaderElbow();
            ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation);

            //  ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation.Normalize() * 50 * ViewSection.Scale.MmToFoot());
         }
      }

      #endregion Tags

      private void SetGridsExtend()
      {
         foreach (var elementGeometryModel in gridGeometryModels)
         {
        
            try
            {
               if (elementGeometryModel.Element is Grid grid)
               {
                  var c = grid.GetCurvesInView(DatumExtentType.ViewSpecific, ViewSection).OrderByDescending(x => x.Length).FirstOrDefault();
                  if (c != null)
                  {
                     var sp = c.SP();
                     var ep = c.EP();
                     sp = sp.EditZ(beamBotElevation - (Setting.KhoangCachGiua2Dim + 1.MmToFoot()) * ViewSection.Scale -
                                                       Setting.KhoangCachDimDenDam);
                     ep = ep.EditZ(beamTopElevation + Setting.KhoangCachGiua2Dim * ViewSection.Scale +
                                   Setting.KhoangCachDimDenDam - ViewSection.Scale.MmToFoot());
                     var line = Line.CreateBound(sp, ep);
                     AC.Document.Create.NewDetailCurve(ViewSection, line.ProjectOn(plane));
                     if (grid.IsCurveValidInView(DatumExtentType.ViewSpecific, ViewSection, line))
                     {
                        grid.SetCurveInView(DatumExtentType.ViewSpecific, ViewSection, line);
                     }
                     //Set Bubble at bottom
                     grid.HideBubbleInView(DatumEnds.End0, ViewSection);
                     grid.HideBubbleInView(DatumEnds.End1, ViewSection);
                     AC.Document.Regenerate();
                     var gridBb = grid.get_BoundingBox(ViewSection);
                     if (gridBb != null)
                     {
                        var zBot = gridBb.Min.Z;
                        grid.ShowBubbleInView(DatumEnds.End0, ViewSection);
                        AC.Document.Regenerate();
                        gridBb = grid.get_BoundingBox(ViewSection);
                        if (zBot > gridBb.Min.Z + 2.MmToFoot())
                        {
                        }
                        else
                        {
                           grid.HideBubbleInView(DatumEnds.End0, ViewSection);
                           grid.ShowBubbleInView(DatumEnds.End1, ViewSection);
                        }
                     }
                  }
               }
            }
            catch (Exception e)
            {
               AC.Log(" :Grid Extend :" + e.Message);
            }
         }
      }

      private void CreateMocThepForAdditionalBars()
      {
         foreach (var bar in TopAdditionalBars)
         {
            //sort curve
            if (bar.Curves == null || bar.Curves.Count == 0)
            {
               continue;
            }
            var first = bar.Curves.First();
            var last = bar.Curves.Last();


            if (first is Line line)
            {
               if (line.Direction.IsHorizontal())
               {
                  DauMocThepByPoint(10.MmToFoot(), line.SP(), true, true);
               }
            }

            if (last is Line lLine)
            {
               if (lLine.Direction.IsHorizontal())
               {
                  DauMocThepByPoint(10.MmToFoot(), lLine.EP(), false, true);
               }
            }
         }

         //For Bot Additional Bar
         foreach (var bar in BottomAdditionalBars)
         {
            var first = bar.Curves.First();
            var last = bar.Curves.Last();
            if (first is Line line)
            {
               if (line.Direction.IsHorizontal())
               {
                  DauMocThepByPoint(10.MmToFoot(), line.SP(), true, false);
               }
            }
            if (last is Line lLine)
            {
               if (lLine.Direction.IsHorizontal())
               {
                  DauMocThepByPoint(10.MmToFoot(), lLine.EP(), false, false);
               }
            }
         }
      }

      private void DauMocThepByPoint(double diameter, XYZ p, bool isLeft, bool isTop)
      {

         if (Setting.DauMocThep == null)
         {
            Setting.DauMocThep = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents).Cast<FamilySymbol>().FirstOrDefault(x => x.Name.Contains("DAUTHEP"));
         
         }

         if (Setting.DauMocThep == null)
         {
            return;
            ;
         }

         if (Setting.DauMocThep.IsActive == false)
         {
            Setting.DauMocThep.Activate();
         }
         var fi = AC.Document.Create.NewFamilyInstance(p, Setting.DauMocThep, ViewSection);
         fi.SetParameterValueByName("DK", diameter);
         if (isTop)
         {
            if (isLeft)
            {
               fi.flipHand();
               ElementTransformUtils.MoveElement(AC.Document, fi.Id, ViewSection.RightDirection * -1 * 7.7.MmToFoot());
            }
            else
            {
               ElementTransformUtils.MoveElement(AC.Document, fi.Id, ViewSection.RightDirection * 1 * 7.7.MmToFoot());
            }
         }
         else
         {
            fi.flipFacing();
            if (isLeft)
            {
               fi.flipHand();
               ElementTransformUtils.MoveElement(AC.Document, fi.Id, ViewSection.RightDirection * -1 * 7.7.MmToFoot());
            }
            else
            {
               ElementTransformUtils.MoveElement(AC.Document, fi.Id, ViewSection.RightDirection * 1 * 7.7.MmToFoot());
            }
         }
      }

      private void SetPresentationForStirrup()
      {
         foreach (var spanModel in SpanModels)
         {
            var info = spanModel.StirrupForSpan;
            if (info.KieuPhanBoThepDai != 1)
            {
               foreach (var rebar in info.MainStirrupMid)
               {
                  rebar.SetPresentationMode(ViewSection, RebarPresentationMode.FirstLast);
               }

               foreach (var rebar in info.MainStirrupEnd1)
               {
                  rebar.SetPresentationMode(ViewSection, RebarPresentationMode.FirstLast);
                  rebar.SetBarHiddenStatus(ViewSection, rebar.NumberOfBarPositions - 1, true);
               }
               foreach (var rebar in info.MainStirrupEnd2)
               {
                  rebar.SetPresentationMode(ViewSection, RebarPresentationMode.FirstLast);
                  rebar.SetBarHiddenStatus(ViewSection, 0, true);
               }
            }
            else
            {
               foreach (var rebar in info.MainStirrupEnd1)
               {
                  rebar.SetPresentationMode(ViewSection, RebarPresentationMode.FirstLast);
               }
            }
         }
      }

      private List<Reference> GetReferenceAndPointsOfStirrups2()
      {
         var raps = new List<Reference>();
         foreach (var spanModel in SpanModels)
         {
            var info = spanModel.StirrupForSpan;
            if (info.KieuPhanBoThepDai != 1)
            {
               foreach (var rebar in info.MainStirrupMid)
               {
                  var lines = rebar.Lines(ViewSection).Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null).OrderBy(x => x.Origin.DotProduct(ViewSection.RightDirection)).ToList();
                  var first = lines.FirstOrDefault();
                  var second = lines.LastOrDefault();
                  if (first != null)
                  {
                     raps.Add(first.Reference);
                  }
                  if (second != null)
                  {
                     raps.Add(second.Reference);
                  }
               }
            }
         }
         return raps;
      }

      private List<ReferenceAndPoint> GetBarEndReferences(Rebar rebar, bool isLoaiBoReferenceTrongSp = false)
      {
         var raps = new List<ReferenceAndPoint>();
         if (rebar == null)
         {
            return raps;
         }
         var list = rebar.Lines(ViewSection).Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null).OrderBy(x => x.SP().DotProduct(right)).ToList();
         var lines = new List<Line>();
         //Loại bỏ đường trong support
         if (isLoaiBoReferenceTrongSp)
         {
            foreach (var line in list)
            {
               var mmm = line.SP().DotProduct(right);
               var flag = true;
               foreach (var sp in BeamModel.SupportModels)
               {
                  var m = sp.TopLeft.DotProduct(right);
                  var mm = sp.TopRight.DotProduct(right);
                  var max = Math.Max(m, mm);
                  var min = Math.Min(m, mm);
                  if (mmm.IsBetweenEqual(min, max, 0.1))
                  {
                     flag = false;
                     break;
                  }
               }

               if (flag)
               {
                  lines.Add(line);
               }
            }
         }
         else
         {
            lines.AddRange(list);
         }
         if (lines.Count > 0)
         {
            var line = lines.First();
            var rap = new ReferenceAndPoint()
            {
               Reference = line.Reference,
               Point = line.SP()
            };
            raps.Add(rap);
         }
         if (lines.Count > 1)
         {
            var line = lines.Last();
            var rap = new ReferenceAndPoint()
            {
               Reference = line.Reference,
               Point = line.SP()
            };
            raps.Add(rap);
         }
         return raps;
      }

      private List<Line> GetLineIntersectWithElement(ElementGeometryModel ele, Line beamLine)
      {
         var lines = new List<Line>();
         var solids = ele.Solids;
         var option = new SolidCurveIntersectionOptions
         {
            ResultType = SolidCurveIntersectionMode.CurveSegmentsInside
         };
         foreach (var solid in solids)
         {
            var intersection = solid.IntersectWithCurve(beamLine, option);
            foreach (var c in intersection)
            {
               if (c is Line line)
               {
                  if (line.Length > 0.1)
                  {
                     line = EditLineByDirection(ViewSection.RightDirection, line);
                     lines.Add(line);
                  }
               }
            }
         }
         return lines;
      }

      private Line EditLineByDirection(XYZ vector, Line line)
      {
         var direct = line.Direction;
         if (direct.DotProduct(vector) < 0)
         {
            return line.CreateReversed() as Line;
         }
         return line;
      }
   }
}