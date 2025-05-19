using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.Others;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using MoreLinq.Extensions;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class CrossSectionModel
   {
      public double Width { get; set; }
      public double Height { get; set; }
      public MainRebar MainTopRebar { get; set; }
      public MainRebar MainBotRebar { get; set; }
      public List<TopAdditionalBar> TopAdditionalBars { get; set; } = new List<TopAdditionalBar>();
      public List<BottomAdditionalBar> BottomAdditionalBars { get; set; } = new List<BottomAdditionalBar>();
      public int StirrupDiameter { get; set; }
      public double StirrupSpacing { get; set; }
      public string KieuThepDai { get; set; }
      public SpanModel SpanModel { get; set; }
      public int ViTri { get; set; }
      public ViewSection ViewSection { get; set; }
      private BeamSectionSetting setting;
      public XYZ Point { get; set; }

      private List<ElementGeometryModel> floorsGeometryModels = new();

      private PlanarFace leftFace;
      private PlanarFace rightFace;
      private PlanarFace topFace;
      private PlanarFace botFace;
      private Grid grid;
      private XYZ right;
      private XYZ rightPoint;
      private XYZ leftPoint;
      private BPlane viewPlane;

      public CrossSectionModel(SpanModel spanModel, int viTri)
      {
         SpanModel = spanModel;
         ViTri = viTri;
         GetData(spanModel, viTri);
      }

      private void GetData(SpanModel spanModel, int viTri)
      {
         setting = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting
             .BeamSectionSetting;
         //Get Mainbar
         Width = spanModel.Width;
         Height = spanModel.Height;
         MainTopRebar = GetMainBarForSpan(spanModel);
         MainBotRebar = GetMainBarForSpan(spanModel, false);
         TopAdditionalBars = GetTopAdditionalBars(spanModel, viTri);
         BottomAdditionalBars = GetBotAdditionalBars(spanModel, viTri);
         GetStirrupInfo(spanModel, viTri);
         right = spanModel.Normal;
         var line = SpanModel.TopLine;
         
         var settings = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting;
         Point = line.Evaluate(viTri switch
         {
             2 => settings.Position2,
             3 => settings.Position3,
             _ => settings.Position1
         }, normalized: true);
      }

      #region Compairer

      public override bool Equals(object obj)
      {
         var crossSection = obj as CrossSectionModel;
         if (obj == null || GetType() != obj.GetType() || crossSection == null)
         {
            return false;
         }
         if (crossSection.Height.Equals(Height)
             && crossSection.Width.Equals(Width)
             && IsMainBarSame(MainTopRebar, crossSection.MainTopRebar)
             && IsMainBarSame(MainBotRebar, crossSection.MainBotRebar)
             && IsAdditionalBotBarsSame(BottomAdditionalBars, crossSection.BottomAdditionalBars)
             && IsAdditionalTopBarsSame(TopAdditionalBars, crossSection.TopAdditionalBars)
             && StirrupDiameter == crossSection.StirrupDiameter
             && StirrupSpacing.IsEqual(crossSection.StirrupSpacing, 10.MmToFoot())
             && KieuThepDai == crossSection.KieuThepDai
         )
         {
            return true;
         }
         return false;
      }

      private bool IsMainBarSame(MainRebar mainRebar1, MainRebar mainRebar2)
      {
         if (mainRebar1.IsTop == mainRebar2.IsTop
             && mainRebar1.Rebars.First().IsSameRebarNumber(mainRebar2.Rebars.First()))
         {
            return true;
         }
         return false;
      }

      private bool IsAdditionalTopBarsSame(List<TopAdditionalBar> bars1, List<TopAdditionalBar> bars2)
      {
         var bar1Layer1 = bars1.FirstOrDefault(x => x.Layer.Layer == 1);
         var bar1Layer2 = bars1.FirstOrDefault(x => x.Layer.Layer == 2);
         var bar1Layer3 = bars1.FirstOrDefault(x => x.Layer.Layer == 3);
         var bar2Layer1 = bars2.FirstOrDefault(x => x.Layer.Layer == 1);
         var bar2Layer2 = bars2.FirstOrDefault(x => x.Layer.Layer == 2);
         var bar2Layer3 = bars2.FirstOrDefault(x => x.Layer.Layer == 3);
         if (IsTopAdditionalBarSame(bar1Layer1, bar2Layer1)
         && IsTopAdditionalBarSame(bar1Layer2, bar2Layer2)
         && IsTopAdditionalBarSame(bar1Layer3, bar2Layer3)
         )
         {
            return true;
         }
         return false;
      }

      private bool IsTopAdditionalBarSame(TopAdditionalBar bar1, TopAdditionalBar bar2)
      {
         if (bar1 == null && bar2 == null)
         {
            return true;
         }
         if (bar1 != null && bar2 != null)
         {
            if (
                bar1.Layer == bar2.Layer
                && bar1.BarDiameter == bar2.BarDiameter
                && bar1.selectedNumberOfRebar == bar2.selectedNumberOfRebar)
            {
               //Check Real Rebar
               if (bar1.Rebars != null && bar2.Rebars != null)
               {
                  if (bar1.Rebars.Count == bar2.Rebars.Count)
                  {
                     if (bar1.Rebars.FirstOrDefault().IsSameRebarNumber(bar2.Rebars.FirstOrDefault()))
                     {
                        return true;
                     }
                  }
               }
            }
         }
         return false;
      }

      private bool IsAdditionalBotBarsSame(List<BottomAdditionalBar> bars1, List<BottomAdditionalBar> bars2)
      {
         var bar1Layer1 = bars1.FirstOrDefault(x => x.Layer.Layer == 1);
         var bar1Layer2 = bars1.FirstOrDefault(x => x.Layer.Layer == 2);
         var bar1Layer3 = bars1.FirstOrDefault(x => x.Layer.Layer == 3);
         var bar2Layer1 = bars2.FirstOrDefault(x => x.Layer.Layer == 1);
         var bar2Layer2 = bars2.FirstOrDefault(x => x.Layer.Layer == 2);
         var bar2Layer3 = bars2.FirstOrDefault(x => x.Layer.Layer == 3);
         if (IsBotAdditionalBarSame(bar1Layer1, bar2Layer1)
             && IsBotAdditionalBarSame(bar1Layer2, bar2Layer2)
             && IsBotAdditionalBarSame(bar1Layer3, bar2Layer3)
         )
         {
            return true;
         }
         return false;
      }

      private bool IsBotAdditionalBarSame(BottomAdditionalBar bar1, BottomAdditionalBar bar2)
      {
         if (bar1 == null && bar2 == null)
         {
            return true;
         }
         if (bar1 != null && bar2 != null)
         {
            if (bar1.Layer == bar2.Layer
                && bar1.BarDiameter == bar2.BarDiameter
                && bar1.selectedNumberOfRebar == bar2.selectedNumberOfRebar)
            {
               if (bar1.Rebars != null && bar2.Rebars != null)
               {
                  if (bar1.Rebars.Count == bar2.Rebars.Count)
                  {
                     if (bar1.Rebars.First().IsSameRebarNumber(bar2.Rebars.First()))
                     {
                        return true;
                     }
                  }
               }
            }
         }
         return false;
      }

      public override int GetHashCode()
      {
         return 0;
      }

      #endregion Compairer

      #region GetData

      private MainRebar GetMainBarForSpan(SpanModel spanModel, bool isTop = true)
      {
         if (isTop)
         {
            var mainBars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars;
            return mainBars.FirstOrDefault(x => x.Start <= spanModel.Index && x.End >= spanModel.Index + 1);
         }
         else
         {
            var mainBars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars;
            return mainBars.FirstOrDefault(x => x.Start <= spanModel.Index && x.End >= spanModel.Index + 1);
         }
      }

      private List<TopAdditionalBar> GetTopAdditionalBars(SpanModel spanModel, int viTri)
      {
         var bars = new List<TopAdditionalBar>();
         var index = spanModel.Index;
         var allTopBars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars;
         if (viTri == 1)
         {
            foreach (var topAdditionalBar in allTopBars)
            {
               if (topAdditionalBar.End == index && topAdditionalBar.RebarEndType == 1)
               {
                  bars.Add(topAdditionalBar);
               }
               else if (topAdditionalBar.End > index && topAdditionalBar.Start <= index)
               {
                  bars.Add(topAdditionalBar);
               }
            }
         }
         else if (viTri == 2)
         {
            foreach (var topAdditionalBar in allTopBars)
            {
               if (topAdditionalBar.End > index && topAdditionalBar.Start <= index)
               {
                  bars.Add(topAdditionalBar);
               }
            }
         }
         else if (viTri == 3)
         {
            foreach (var topAdditionalBar in allTopBars)
            {
               if (topAdditionalBar.Start == index + 1 && topAdditionalBar.RebarStartType == 1)
               {
                  bars.Add(topAdditionalBar);
               }
               else if (topAdditionalBar.End > index && topAdditionalBar.Start <= index)
               {
                  bars.Add(topAdditionalBar);
               }
            }
         }

         return bars;
      }

      private List<BottomAdditionalBar> GetBotAdditionalBars(SpanModel spanModel, int viTri)
      {
         var bars = new List<BottomAdditionalBar>();
         var index = spanModel.Index;
         var allBotBars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars;
         if (viTri == 1)
         {
            foreach (var botBar in allBotBars)
            {
               if (botBar.Start == index && botBar.RebarEndType != 1)
               {
                  bars.Add(botBar);
               }
               else if (botBar.End > index && botBar.Start < index)
               {
                  bars.Add(botBar);
               }
            }
         }
         else if (viTri == 2)
         {
            foreach (var botBar in allBotBars)
            {
               if (botBar.End > index && botBar.Start <= index)
               {
                  bars.Add(botBar);
               }
            }
         }
         else if (viTri == 3)
         {
            foreach (var botBar in allBotBars)
            {
               if (botBar.End == index + 1 && botBar.RebarStartType != 1)
               {
                  bars.Add(botBar);
               }
               else if (botBar.End > index + 1 && botBar.Start <= index)
               {
                  bars.Add(botBar);
               }
            }
         }
         return bars;
      }

      private void GetStirrupInfo(SpanModel spanModel, int viTri)
      {
         var info = spanModel.StirrupForSpan;
         StirrupDiameter = (int)info.BarDiameter.BarDiameter().FootToMm();
         StirrupSpacing = info.SpacingAtEnd;
         if (viTri == 2 && info.KieuPhanBoThepDai != 1)
         {
            StirrupSpacing = info.SpacingAtMid;
         }
      }

      #endregion GetData

      public void Detailing(string mark, ref int j)
      {
         setting = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting
             .BeamSectionSetting;
         ViewSection = CreateViewSection();
         //Set Name
         var name = mark + "-" + j;
         while (true)
         {
            try
            {
               ViewSection.Name = name;
               break;
            }
            catch
            {
               name = name + ".";
            }
         }
         j++;

         ViewSection.Scale = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale;
         SetViewTemplateAndScale();
         AC.Document.Regenerate();
         CreateBreakLines();
         CreateDims();
         CreateTags();
         HideOtherRebarAndBeam();
      }

      private void HideOtherRebarAndBeam()
      {
         var rebars = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Rebar)).Cast<Rebar>()
             .ToList();
         var idsHost = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamModel.SpanModels.Select(x => x.Beam.Id.GetElementIdValue()).ToList();
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
               AC.Log("Cross Section Model.cs +Lỗi hide rebar");
            }
         }

         //Hide Others Beam
         var beams = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming);
         foreach (var beam in beams)
         {
            if (idsHost.Contains(beam.Id.GetElementIdValue()) == false)
            {
               ViewSection.HideElements(new List<ElementId>() { beam.Id });
            }
         }
      }

      private ViewSection CreateViewSection()
      {
         ViewSection viewSection = null;
         var origin = Point;
         var up = XYZ.BasisZ;
         var tf = Transform.Identity;
         tf.Origin = origin;
         tf.BasisX = right;
         tf.BasisY = up;
         tf.BasisZ = right.CrossProduct(up);

         var box = new BoundingBoxXYZ
         {
            Transform = tf,
            Min = new XYZ(-Width / 2 - 100.MmToFoot(), -Height - 250.MmToFoot(), 0),
            Max = new XYZ(Width / 2 + 100.MmToFoot(), 250.MmToFoot(), 200.MmToFoot())
         };

         viewSection = setting.ViewFamilyType.ViewFamily == ViewFamily.Section ? ViewSection.CreateSection(AC.Document, setting.ViewFamilyType.Id, box) : ViewSection.CreateDetail(AC.Document, setting.ViewFamilyType.Id, box);
         right = viewSection.RightDirection;
         var planarFaces = SpanModel.BeamGeometry.Beam.Faces().Where(x => x != null).ToList();

         leftFace = right.FirstFace(planarFaces);
         rightFace = (-right).FirstFace(planarFaces);
         botFace = XYZ.BasisZ.FirstFace(planarFaces);
         topFace = (-XYZ.BasisZ).FirstFace(planarFaces);

         rightPoint = Point.Add(right * Width / 2);

         leftPoint = Point.Add(right * -Width / 2);

         viewPlane = viewSection.ToBPlane();

         var grids = new FilteredElementCollector(AC.Document, viewSection.Id).OfClass(typeof(Grid)).Cast<Grid>()
             .ToList();

         grid = grids.FirstOrDefault();

         var ids = new FilteredElementCollector(AC.Document, viewSection.Id).OfCategory(BuiltInCategory.OST_Viewers).ToElementIds();

         if (ids.Count > 0)
         {
            viewSection.HideElements(ids);
         }

         AC.Document.Regenerate();

         var floors = new FilteredElementCollector(AC.Document, viewSection.Id).OfClass(typeof(Floor))
             .ToList();
         floorsGeometryModels = floors.Select(x => new ElementGeometryModel(x)).ToList();
         ViewSection = viewSection;
         //Hide Rebars
         var rebars = new FilteredElementCollector(AC.Document, viewSection.Id).OfClass(typeof(Rebar)).Cast<Rebar>();
         var idsToHide = new List<ElementId>();
         var beamIds = BeamRebarRevitData.Instance.BeamModel.SpanModels.Select(x => x.Beam.Id.GetElementIdValue()).ToList();
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
         return viewSection;
      }

      private void SetViewTemplateAndScale()
      {
         if (setting.ViewTemplate != null)
         {
            ViewSection.ViewTemplateId = setting.ViewTemplate.Id;
         }
      }

      private void CreateBreakLines()
      {

         var rp = rightPoint.Add(setting.KhoangCachBreakLineDenDam * right);
         var lp = leftPoint.Add(setting.KhoangCachBreakLineDenDam * -right);
         var rightLine = Line.CreateBound(rp.Add(XYZ.BasisZ * 20), rp.Add(XYZ.BasisZ * -20));
         var leftLine = Line.CreateBound(lp.Add(XYZ.BasisZ * 20), lp.Add(XYZ.BasisZ * -20));
         var floorSolids = floorsGeometryModels.Select(x => x.Solids).Flatten().Cast<Solid>().ToList();
         var insideLines = rightLine.GetInsideLinesIntersectSolids(floorSolids);
         foreach (var insideLine in insideLines)
         {
            var extendLine = ExtendLine(insideLine, 20, XYZ.BasisZ);
            AC.Document.Create.NewFamilyInstance(extendLine, setting.BreakLineSymbol, ViewSection);
         }
         var insideLinesLeft = leftLine.GetInsideLinesIntersectSolids(floorSolids);
         foreach (var insideLine in insideLinesLeft)
         {
            var extendLine = ExtendLine(insideLine, 20, -XYZ.BasisZ);
            AC.Document.Create.NewFamilyInstance(extendLine, setting.BreakLineSymbol, ViewSection);
         }
      }

      private void CreateDims()
      {
         //Right Dim
         var raps = new List<ReferenceAndPoint>();
         if (botFace != null)
         {
            var rap = new ReferenceAndPoint { Reference = botFace.Reference, Point = botFace.FaceCenter(), Normal = botFace.FaceNormal };
            raps.Add(rap);
         }

         if (topFace != null)
         {
            var rap = new ReferenceAndPoint { Reference = topFace.Reference, Point = topFace.FaceCenter(), Normal = topFace.FaceNormal };
            raps.Add(rap);
         }

         foreach (var elementGeometryModel in floorsGeometryModels)
         {
            foreach (var referenceAndPoint in elementGeometryModel.ReferenceAndPoints)
            {
               if (referenceAndPoint.Normal.IsParallel(XYZ.BasisZ))
               {
                  raps.Add(referenceAndPoint);
               }
            }
         }

         if (raps.Count >= 2)
         {
            raps = raps.OrderBy(x => x.Point.Z).ToList();
            var rap1 = raps.First();
            var rap2 = raps.Last();
            var ra = new ReferenceArray();
            ra.Append(rap1.Reference);
            ra.Append(rap2.Reference);
            var line = leftPoint.Add(-right * setting.KhoangCachSideDimDenDam).CreateLineByPointAndDirection(XYZ.BasisZ);
            AC.Document.Create.NewDimension(ViewSection, line, ra, setting.DimensionTypeFixed);
         }
         //Bot Dim
         if (leftFace != null && rightFace != null)
         {
            var ra = new ReferenceArray();
            ra.Append(leftFace.Reference);
            ra.Append(rightFace.Reference);
            var line = SpanModel.BotLeft.Add(-XYZ.BasisZ * setting.KhoangCachBotDimDenDam).CreateLineByPointAndDirection(right);
            AC.Document.Create.NewDimension(ViewSection, line, ra, setting.DimensionTypeFixed);
         }
      }

      private Line ExtendLine(Line line, double num, XYZ vector)
      {
         var direct = line.Direction;
         var p1 = line.SP();
         var p2 = line.EP();
         p1 = p1.Add(direct * (-num).MmToFoot());
         p2 = p2.Add(direct * num.MmToFoot());
         var l = Line.CreateBound(p1, p2);
         if (vector.IsOppositeDirectionTo(direct))
         {
            l = Line.CreateBound(p2, p1);
         }
         return l;
      }

      private void CreateTags()
      {
         TagMainBar(MainTopRebar);
         TagMainBar(MainBotRebar);
         TagAdditionalTopBars();
         TagAdditionalBotBars();
         TagStirrup();
         TagThepGiaCuongBung();
      }

      private void TagMainBar(MainRebar bar)
      {
         var p = leftPoint.EditZ(SpanModel.TopElevation + 60.MmToFoot()).Add(-right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
         if (bar.IsTop == false)
         {
            p = leftPoint.EditZ(SpanModel.BotElevation - 60.MmToFoot()).Add(-right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
         }
         if (bar.Rebars.Count == 1)
         {
            if (bar.Rebars.First().NumberOfBarPositions == 1)
            {
               CreateIndependentTag(bar.Rebars.First(), p, false);
            }
            else
            {
               var ids = bar.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(false, ids, p);
            }
         }
         else
         {
            var ids = bar.Rebars.Select(x => x.Id).ToList();
            CreateMultiReferenceAnnotation(false, ids, p);
         }
      }

      private void TagAdditionalTopBars()
      {
         var barLayer1 = TopAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 1);
         var barLayer2 = TopAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 2);
         var barLayer3 = TopAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 3);
         if (barLayer1 != null)
         {
            var p = rightPoint.EditZ(leftPoint.Z + 90.MmToFoot() + 50.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            if (barLayer1.Rebars.Count == 1)
            {
               if (barLayer1.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer1.Rebars.First(), p, true, false, true);
               }
               else
               {
                  var ids = barLayer1.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer1.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }

         if (barLayer2 != null)
         {
            var p = rightPoint.EditZ(barLayer2.Z - 20.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            //if (barLayer1 == null)
            //{
            //    p = rightPoint.EditZ(leftPoint.Z + 90.MmToFoot() + 50.MmToFoot()).AddBot(right * (50.MmToFoot() + 2.MmToFoot() * scale));
            //}
            if (barLayer2.Rebars.Count == 1)
            {
               if (barLayer2.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer2.Rebars.First(), p, false, false, true);
               }
               else
               {
                  var ids = barLayer2.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer2.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }

         if (barLayer3 != null)
         {
            var p = rightPoint.EditZ(barLayer3.Z - 30.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            if (barLayer3.Rebars.Count == 1)
            {
               if (barLayer3.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer3.Rebars.First(), p, false, false, true);
               }
               else
               {
                  var ids = barLayer3.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer3.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }
      }

      private void TagAdditionalBotBars()
      {
         var barLayer1 = BottomAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 1);
         var barLayer2 = BottomAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 2);
         var barLayer3 = BottomAdditionalBars.FirstOrDefault(x => x.Layer.Layer == 3);
         if (barLayer1 != null)
         {
            var p = rightPoint.EditZ(SpanModel.BotElevation - 90.MmToFoot() - 50.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            if (barLayer1.Rebars.Count == 1)
            {
               if (barLayer1.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer1.Rebars.First(), p, true, false, true);
               }
               else
               {
                  var ids = barLayer1.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer1.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }

         if (barLayer2 != null)
         {
            var p = rightPoint.EditZ(barLayer2.Z + 20.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            //if (barLayer1 == null)
            //{
            //    p = rightPoint.EditZ(SpanModel.BotElevation - 90.MmToFoot() - 50.MmToFoot()).AddBot(right * (50.MmToFoot() + 2.MmToFoot() * scale));
            //}
            if (barLayer2.Rebars.Count == 1)
            {
               if (barLayer2.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer2.Rebars.First(), p, false, false, true);
               }
               else
               {
                  var ids = barLayer2.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer2.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }

         if (barLayer3 != null)
         {
            var p = rightPoint.EditZ(barLayer3.Z + 30.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
            if (barLayer3.Rebars.Count == 1)
            {
               if (barLayer3.Rebars.First().NumberOfBarPositions == 1)
               {
                  CreateIndependentTag(barLayer3.Rebars.First(), p, false, false, true);
               }
               else
               {
                  var ids = barLayer3.Rebars.Select(x => x.Id).ToList();
                  CreateMultiReferenceAnnotation(true, ids, p);
               }
            }
            else
            {
               var ids = barLayer3.Rebars.Select(x => x.Id).ToList();
               CreateMultiReferenceAnnotation(true, ids, p);
            }
         }
      }

      private void TagStirrup()
      {
         var list1 = SpanModel.StirrupForSpan.MainStirrupEnd1;
         var list2 = SpanModel.StirrupForSpan.SecondaryStirrupEnd1;
         if (ViTri == 2)
         {
            list1 = SpanModel.StirrupForSpan.MainStirrupMid;
            list2 = SpanModel.StirrupForSpan.SecondaryStirrupMid;
         }
         else if (ViTri == 3)
         {
            list1 = SpanModel.StirrupForSpan.MainStirrupEnd2;
            list2 = SpanModel.StirrupForSpan.SecondaryStirrupEnd2;
         }
         //Tag MainBotBot stirrup
         var n = 0.4;
         if (SpanModel.DaiGiaCuongs.Count == 1)
         {
            n = 0.3;
         }
         else if (SpanModel.DaiGiaCuongs.Count > 1)
         {
            n = 0.2;
         }
         if (list1.Count > 0)
         {
            var first = list1.First();
            var p = leftPoint.EditZ(SpanModel.BotElevation + SpanModel.Height * n);

            CreateIndependentTag(first, p, false, true);
         }
         //Tag Secondary stirrup
         if (list2.Count > 0)
         {
            var first = list2.Last();
            var p = rightPoint.EditZ(SpanModel.BotElevation + SpanModel.Height * n);
            CreateIndependentTag(first, p, true, true);
         }
      }

      private void TagThepGiaCuongBung()
      {
         if (SpanModel.ThepGiaCuong.Count > 0)
         {
            foreach (var rebar in SpanModel.ThepGiaCuong)
            {
               if (rebar.NumberOfBarPositions > 1)
               {
                  var z = CommonService.GetMaxLineOfRebar(rebar).Origin.Z;
                  var p = rightPoint.EditZ(z + 50.MmToFoot()).Add(right * (50.MmToFoot() + 2.MmToFoot() * BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.Scale));
                  CreateMultiReferenceAnnotation(true, new List<ElementId>() { rebar.Id }, p);
               }
            }
         }

         if (SpanModel.DaiGiaCuongs.Count > 0)
         {
            foreach (var rebar in SpanModel.DaiGiaCuongs)
            {
               var z = CommonService.GetMaxLineOfRebar(rebar).Origin.Z;
               var p = rightPoint.EditZ(z - 50.MmToFoot());
               CreateIndependentTag(rebar, p, true, true, true);
            }
         }
      }

      private IndependentTag CreateIndependentTag(Rebar rebar, XYZ p, bool isRight, bool isStirrup = false, bool isNeedElbow = false)
      {
         ElementId tagId = null;
         if (isRight)
         {
         }
         if (isRight == false)
         {
            tagId = isStirrup ? setting.TagThepDaiTrai.Id : setting.IndependentTagRebarStandardLeft.Id;
         }
         else
         {
            tagId = isStirrup ? setting.TagThepDaiPhai.Id : setting.IndependentTagRebarStandardRight.Id;
         }

         var independentTag = TagUtils2.CreateIndependentTag(tagId, ViewSection.Id, rebar, false, TagOrientation.Horizontal, p);
         MoveTag(independentTag, isRight);
         independentTag.HasLeader = true;
         if (isNeedElbow)
         {
            var centerPoint = CommonService.GetMaxLineOfRebar(rebar).Midpoint().ProjectOnto(viewPlane);
            var leaderElbow = new XYZ(centerPoint.X, centerPoint.Y, p.Z);
            independentTag.SetLeaderElbow(leaderElbow);
         }
         return independentTag;
      }

      private void MoveTag(IndependentTag independentTag, bool isRight = true)
      {
         independentTag.HasLeader = false;
         AC.Document.Regenerate();
         if (isRight)
         {
            ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, ViewSection.RightDirection * -5);
         }
         else
         {
            ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, ViewSection.RightDirection * 5);
         }
         var bb = independentTag.get_BoundingBox(ViewSection);
         if (bb != null)
         {
            var min = bb.Min;
            var max = bb.Max;
            if (isRight)
            {
               var first = right.FirstPointByDirection(new List<XYZ> { min, max });
               var d = (first - rightPoint).DotProduct(right) - 50.MmToFoot(); ;
               ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, right * -d);
            }
            else
            {
               var first = (-right).FirstPointByDirection(new List<XYZ> { min, max });
               var d = (first - leftPoint).DotProduct(right) + 50.MmToFoot();
               ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, -right * d);
            }
         }
         independentTag.HasLeader = true;
         //BeamRebarCommonService.CreateLineIndependentTag(independentTag, ViewSection);
      }

      private MultiReferenceAnnotation CreateMultiReferenceAnnotation(bool isRight, List<ElementId> ids, XYZ p)
      {
         if (ids == null || ids.Count == 0)
         {
            return null;
         }
         var type = setting.TagThepNhomPhai;
         var direction = right;
         if (isRight == false)
         {
            direction = -right;
            type = setting.TagThepNhomTrai;
         }
         var option = new MultiReferenceAnnotationOptions(type);
         option.SetElementsToDimension(ids);
         option.DimensionPlaneNormal = ViewSection.ViewDirection;
         option.DimensionLineDirection = direction;
         option.DimensionLineOrigin = p;
         option.TagHeadPosition = p;
         if (MultiReferenceAnnotation.AreReferencesValidForLinearDimension(AC.Document, ViewSection.Id, option))
         {
            var multiTag = MultiReferenceAnnotation.Create(AC.Document, ViewSection.Id, option);
            if (multiTag.TagId.ToElement() is IndependentTag indepentTag)
            {
               MoveTag(indepentTag, isRight);
            }
            return multiTag;
         }
         return null;
      }
   }
}