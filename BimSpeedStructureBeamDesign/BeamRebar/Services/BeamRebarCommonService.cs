using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedUtils;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public class BeamRebarCommonService
   {
      public static List<NumberOfRebarByWidth> GetNumberOfRebarByWidthsDefault()
      {
         var list = new List<NumberOfRebarByWidth>
            {
                new(149, 1, Define.OneBar),
                new( 219, 150, Define.TwoBars),
                new( 299, 220, Define.ThreeBars),
                new( 349, 300, Define.FourBars1),
                new( 399, 350, Define.FourBars2),
                new( 499, 400, Define.FiveBars1),
                new( 599, 500, Define.FiveBars2),
                new( 749, 600, Define.SixBars),
                new( 849, 750, Define.SeventBars),
                new( 20000, 850, Define.NineBars)
            };

         return list;
      }

      public static XYZ EditBeamDirection(XYZ vector)
      {
         if (vector.X < 0)
         {
            vector = -vector;
         }
         if (vector.DotProduct(XYZ.BasisX).IsEqual(0, 0.001))
         {
            if (vector.Y < 0)
            {
               vector = -vector;
            }
         }
         return vector;
      }

      public static bool CheckBeamsValidToPutRebars(List<FamilyInstance> beams, out string errorMessage)
      {
         errorMessage = "";
         //Check Same Level + Is Concrete Beam+ Is Straight+
         var levelIds = new List<int>();
         var lines = new List<Line>();
         foreach (var beam in beams)
         {
            var level = GetBeamLevel(beam);
            if (level == null)
            {
               errorMessage = Define.BeamsAreNotSameLevel;
               return false;
            }
            if (beam.StructuralMaterialType != StructuralMaterialType.Concrete)
            {
               errorMessage = Define.BeamsAreNotConcrete;
               return false;
            }

            var lc = beam.Location as LocationCurve;
            //if (lc == null)
            //{
            //   errorMessage = Define.BeamsAreNotStraight;
            //   return false;
            //}

            var line = lc.Curve as Line;
            //if (line == null)
            //{
            //   errorMessage = Define.BeamsAreNotStraight;
            //   return false;
            //}
            lines.Add(line);
            var startLevelOffset =
                beam.GetParameterValueByNameAsDouble(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
            var endLevelOffset =
                beam.GetParameterValueByNameAsDouble(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION);
            //if (startLevelOffset.IsEqual(endLevelOffset, 0.0001) == false)
            //{
            //   errorMessage = Define.BeamsAreNotHorizontal;
            //   return false;
            //}
            levelIds.Add(level.Id.GetElementIdValue());
            levelIds = levelIds.Distinct().ToList();
         }
         if (levelIds.Count > 1)
         {
            errorMessage = Define.BeamsAreNotSameLevel;
            return false;
         }

         //Check Beams line up and continuos

         if (lines.Count > 1)
         {
            var line0 = lines[0];
            var direct0 = line0.Direction;
            //Check Line paralell
            if (lines.Any(x => x.Direction.IsParallel(direct0) == false))
            {
               errorMessage = Define.BeamsAreNotParalell;
               return false;
            }
            //Check lệch cao độ Z

            //Check Line nối tiếp nhau
            lines = lines.OrderBy(x => x.Midpoint().DotProduct(direct0)).ToList();
         }

         return true;
      }

      public static Level GetBeamLevel(FamilyInstance fi)
      {
         var levelParam = fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
         var level = levelParam.AsElementId().ToElement() as Level;
         return level;
      }

      public static void ArrangeRebar()
      {
         var vmb = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel;
         var vmt = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel;

         if (!vmb.AllBars.Concat(new List<BottomAdditionalBar> { vmb.SelectedBar }).Where(x => x != null && x.Layer.Layer == 1).ToList().Any() && BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet == false)
         {
            foreach (var rebarQuantityByWidth in BeamRebarRevitData.Instance.BeamRebarViewModel.RebarQuantityManager.RebarQuantityByWidths)
            {
               rebarQuantityByWidth.AddBot1 = 0;
            }
         }

         if (!vmt.AllBars.Concat(new List<TopAdditionalBar> { vmt.SelectedBar }).Where(x => x != null && x.Layer.Layer == 1).ToList().Any() && BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet == false)
         {
            foreach (var rebarQuantityByWidth in BeamRebarRevitData.Instance.BeamRebarViewModel.RebarQuantityManager.RebarQuantityByWidths)
            {
               rebarQuantityByWidth.AddTop1 = 0;
            }
         }

         var barListBot = vmb.AllBars.Concat(new List<BottomAdditionalBar> { vmb.SelectedBar }).Where(x => x != null)
             .ToList();
         var barListTop = vmt.AllBars.Concat(new List<TopAdditionalBar> { vmt.SelectedBar }).Where(x => x != null)
             .ToList();

         foreach (var mainBar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars)
         {
            var span = mainBar.Start.GetSpanModelByIndex();
            var setting = span.GetRebarQuantityByWidth();
            if (mainBar.Layer == 1)
            {
               var list1 = RebarLocationLayer1(setting.TotalBot1, setting.MainBot1);
 

               foreach (var bar in barListBot)
               {
                  if (bar.Layer.IsTop == mainBar.IsTop)
                  {
                     if (bar.Layer.Layer == 1)
                     {
                        bar.RebarPointsInSection = ToRebarPoints(list1);
                        SetPositionForAdditionalBars(bar.RebarPointsInSection.Where(x => x.Checked).ToList(),
                           bar.selectedNumberOfRebar);
                        bar.RebarPointsInSection = new List<RebarPoint>(bar.RebarPointsInSection);
                        bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                        bar.RaiseNumber();
                     }
                     else if (bar.Layer.Layer == 2)
                     {
                        var list2 = RebarLocationLayer2(bar.selectedNumberOfRebar, list1);
                     
                        bar.RebarPointsInSection = ToRebarPoints(list2, "m");
                        bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                        bar.RaiseNumber();
                     }
                     else if (bar.Layer.Layer == 3)
                     {
                        var list3 = RebarLocationLayer2(bar.selectedNumberOfRebar, list1);
                        bar.RebarPointsInSection = ToRebarPoints(list3, "m");
                        bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                        bar.RaiseNumber();
                     }
                  }
               }

               vmb.AllBars = vmb.AllBars.Where(x => x.selectedNumberOfRebar > 0)
                   .ToList();
               mainBar.RebarPointsInSection = ToRebarPoints(list1, "m");
               mainBar.RaiseNumber();
            }
            else
            {
               var list = RebarLocationLayer1(mainBar.SelectedNumberOfRebar, mainBar.SelectedNumberOfRebar);
               mainBar.RebarPointsInSection = ToRebarPoints(list, "m");
               mainBar.selectedNumberOfRebar = mainBar.RebarPointsInSection.Count(x => x.Checked);
               mainBar.RaiseNumber();
            }
         }

         foreach (var mainBar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars)
         {
            var span = mainBar.Start.GetSpanModelByIndex();
            var setting = span.GetRebarQuantityByWidth();
            if (mainBar.Layer == 1)
            {
               var list1 = RebarLocationLayer1(setting.TotalTop1, setting.MainTop1);
               
             
               foreach (var bar in barListTop)
               {
                  if (bar == null)
                  {
                     continue;
                  }

                  if (bar.Layer.Layer == 1)
                  {
                     bar.RebarPointsInSection = ToRebarPoints(list1);
                     SetPositionForAdditionalBars(bar.RebarPointsInSection.Where(x => x.Checked).ToList(),
                        bar.selectedNumberOfRebar);
                     bar.RebarPointsInSection = new List<RebarPoint>(bar.RebarPointsInSection);
                     bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                     bar.RaiseNumber();
                  }
                  else if (bar.Layer.Layer == 2)
                  {
                     var list2 = RebarLocationLayer2(bar.selectedNumberOfRebar, list1);
                     bar.RebarPointsInSection = ToRebarPoints(list2, "m");

                     bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                     bar.RaiseNumber();
                  }
                  else if (bar.Layer.Layer == 3)
                  {
                     var list3 = RebarLocationLayer2(bar.selectedNumberOfRebar, list1);
                     bar.RebarPointsInSection = ToRebarPoints(list3, "m");
                     bar.selectedNumberOfRebar = bar.RebarPointsInSection.Count(x => x.Checked);
                     bar.RaiseNumber();
                  }
               }

               vmt.AllBars = vmt.AllBars.Where(x => x.selectedNumberOfRebar > 0)
                   .ToList();
               mainBar.RebarPointsInSection = ToRebarPoints(list1, "m");
               mainBar.RaiseNumber();
            }
            else
            {
               var list = RebarLocationLayer1(mainBar.SelectedNumberOfRebar, mainBar.SelectedNumberOfRebar);
               mainBar.RebarPointsInSection = ToRebarPoints(list, "m");
               mainBar.selectedNumberOfRebar = mainBar.RebarPointsInSection.Count(x => x.Checked);
               mainBar.RaiseNumber();
            }
         }

         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
      }

      private static List<RebarPoint> ToRebarPoints(List<string> list, string symbol = "a", int rebarCount = 0)
      {
         var result = new List<RebarPoint>();
         for (var index = 0; index < list.Count; index++)
         {
            var s = list[index];
            var rp = new RebarPoint(index, s == symbol);
            result.Add(rp);
         }

         return result;
      }

      private static void SetPositionForAdditionalBars(List<RebarPoint> rebarPoints, int rebarCount = 0)
      {
         rebarPoints.ForEach(x => x.Checked = false);

         var midIndex = rebarPoints.Count / 2;

         if (rebarPoints.Count == 1)
         {
            rebarPoints.ForEach(x => x.Checked = true);
            return;
         }
         if (rebarCount % 2 > 0)
         {
            rebarPoints[midIndex].Checked = true;
            rebarCount--;
         }

         while (rebarCount > 0)
         {
            var item = rebarCount % 2 == 0 ? rebarPoints.FirstOrDefault(x => x.Checked == false) : rebarPoints.LastOrDefault(x => x.Checked == false);
            if (item != null)
            {
               item.Checked = true;
            }

            rebarCount--;
         }

      }

      public static int Return_Total_Quantity_from_Array(List<string>[] lists, bool soThepLe)
      {
         int xValue = 0;
         int arrayNum = lists.Length - 1;
         for (int i = 1; i <= arrayNum - 1; i++)
         {
            xValue += (lists[i].Count + 1) * 2;
         }

         if (soThepLe)
         {
            xValue += (lists[arrayNum].Count + 1) * 2;
            xValue += 1;
         }
         else
            xValue += lists[arrayNum].Count + 2;

         return xValue;
      }

      public static List<string> RebarLocationLayer1(int total, int mainNumber)
      {
         string main = "m"; // "o"
         string add = "a"; // "x"
         bool soThepLe = mainNumber % 2 != 0;
         int arrayNum = (int)Math.Floor(mainNumber / 2.0);
         List<string>[] lists = new List<string>[arrayNum + 1];

         for (int i = 0; i <= arrayNum; i++)
         {
            lists[i] = new List<string>();
         }
         bool keyFound = false;
         var count = 0;
         while (keyFound == false)
         {
            count++;
            if (count > 25)
            {
               break;
            }
            for (int i = 1; i <= arrayNum; i++)
            {
               lists[i].Add(add);
               if (Return_Total_Quantity_from_Array(lists, soThepLe) > total)
               {
                  lists[i].RemoveAt(0);
                  keyFound = true;
               }
            }
         }
         // END
         var list = new List<string>();
         for (int i = 1; i <= arrayNum; i++)
         {
            list.Add(main);
            list.AddRange(lists[i]);
         }
         // Trong truong hop chua du Add_Bar
         if (Return_Total_Quantity_from_Array(lists, soThepLe) < total)
            list.Add(add);

         if (soThepLe)
         {
            list.Add(main);
            for (int i = arrayNum; i >= 1; i += -1)
            {
               list.AddRange(lists[i]);
               list.Add(main);
            }
         }
         else
         {
            list.Add(main);
            for (int i = arrayNum - 1; i >= 1; i += -1)
            {
               list.AddRange(lists[i]);
               list.Add(main);
            }
         }

         return list;
      }

      public static List<string> RebarLocationLayer2(int barNum, List<string> layer1)
      {
         string Symbol_Main = "m";
         string Symbol_Add = "a";
         var xSpace = (layer1.Count - 1) / (double)(barNum - 1);
         // END
         var xArray1 = new List<string>();
         for (int i = 1; i <= barNum - 1; i++)
         {
            int id1 = (int)Math.Round((i - 1) * xSpace) + 1;
            int id2 = (int)Math.Round(i * xSpace) + 1;
            xArray1.Add(Symbol_Main);
            for (int j = id1 + 1; j <= id2 - 1; j++)
               xArray1.Add(Symbol_Add);
         }
         xArray1.Add(Symbol_Main);
         return xArray1;
      }

      public static List<Line> TrimLinesByDBSolids(Line line, List<Solid> solids)
      {
          // truong hop dam nghieng
          if (solids.Count == 0)
          {
              return new List<Line> { line };
          }
          var solid = solids[0];
          if (solids.Count > 1)
          {
              var z = line.SP();
              solid = SolidUtils.Clone(solid);
              for (int i = 0; i < solids.Count; i++)
              {
                  var s = solids[i];
                  try
                  {
                      var solidZ = SolidUtils.CreateTransformed(s,null);
                      BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid, solidZ, BooleanOperationsType.Union);
                  }
                  catch
                  {
                      //Ignore
                      var b = 1;
                  }
              }
          }
          var curveIntersection = solid.IntersectWithCurve(line,
              new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside });
          var list = curveIntersection.Where(x => x is Line).Cast<Line>().ToList();

          return list;
        }

      public static List<Line> TrimLinesBySolids(Line line, List<Solid> solids)
      {
         if (solids.Count == 0)
         {
            return new List<Line> { line };
         }
         var solid = solids[0];
         if (solids.Count > 1)
         {
            var z = line.SP().Z;
            solid = SolidUtils.Clone(solid);
            for (int i = 0; i < solids.Count; i++)
            {
               var s = solids[i];
               try
               {
                  var translation = XYZ.BasisZ * (z - s.ComputeCentroid().Z);
                  var solidZ = SolidUtils.CreateTransformed(s, Transform.CreateTranslation(translation));
                  BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid, solidZ, BooleanOperationsType.Union);
               }
               catch
               {
                  //Ignore
                  var b = 1;
               }
            }
         }
         var curveIntersection = solid.IntersectWithCurve(line,
             new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside });
         var list = curveIntersection.Where(x => x is Line).Cast<Line>().ToList();

         return list;
      }

      public static Line EditLineByDirection(Line line, XYZ vector)
      {
         var sp = line.SP();
         var ep = line.EP();
         var direct = ep - sp;
         if (direct.DotProduct(vector) < -0.0001)
         {
            return Line.CreateBound(ep, sp);
         }
         return line;
      }

      public static List<Line> EditLinesByDirectionAndOrdering(List<Line> lines, XYZ vector)
      {
         return lines.Select(x => EditLineByDirection(x, vector)).OrderBy(x => x.Midpoint().DotProduct(vector))
             .ToList();
      }

      public static bool IsPointNearSolid(Solid solid, XYZ p, XYZ direct, out Line lineInsideSupport)
      {
         lineInsideSupport = null;
         var center = solid.ComputeCentroid();
         p = p.EditZ(center.Z);
         var sp = p.Add(direct * 50.MmToFoot());
         var ep = p.Add(-direct * 50.MmToFoot());
         var line = Line.CreateBound(sp, ep);
         var solidCurveIntersection = solid.IntersectWithCurve(line,
             new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside });
         if (solidCurveIntersection.Any())
         {
            var sp1 = p.Add(direct * 5000.MmToFoot());
            var ep1 = p.Add(-direct * 5000.MmToFoot());
            var line1 = Line.CreateBound(sp1, ep1);
            var solidCurveIntersection1 = solid.IntersectWithCurve(line1,
                new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside });
            if (solidCurveIntersection1.Any())
            {
               lineInsideSupport = solidCurveIntersection1.GetCurveSegment(0) as Line;
               if (lineInsideSupport != null)
               {
                  lineInsideSupport = Line.CreateBound(lineInsideSupport.SP().EditZ(p.Z),
                      lineInsideSupport.EP().EditZ(p.Z));
                  lineInsideSupport = EditLineByDirection(lineInsideSupport, direct);

                  return true;
               }
            }
         }
         return false;
      }

      public static List<FamilyInstance> GetBeamAsSupports(List<FamilyInstance> mainBeams)
      {
         var supports = new List<FamilyInstance>();
         var beams = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(FamilyInstance))
             .OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>()
             .Where(x => x.StructuralMaterialType == StructuralMaterialType.Concrete).Where(y =>
                 mainBeams.Select(x => x.Id.GetElementIdValue()).Contains(y.Id.GetElementIdValue()) == false).ToList();

         var beamGeometries = mainBeams.Where(CheckBeamStraightAndHorizontal).Select(x => new BeamGeometry(x)).ToList();
         var originalSolids = beamGeometries.Select(x => x.GetOriginalSolidTransformed(300.MmToFoot())).ToList();
         foreach (var familyInstance in beams)
         {
            if (CheckBeamStraightAndHorizontal(familyInstance) == false)
            {
               continue;
            }
            var beamSupportGeometry = new BeamGeometry(familyInstance);

            var line = beamSupportGeometry.BeamLine;
            var sp = line.SP().EditZ(beamSupportGeometry.TopElevation - 10.MmToFoot()).Add(-line.Direction * 50.MmToFoot());
            var ep = line.EP().EditZ(beamSupportGeometry.TopElevation - 10.MmToFoot()).Add(line.Direction * 50.MmToFoot());
            var line1 = Line.CreateBound(sp, ep);
            //CreateModelLine(line1);
            var insideLines = GetInsideLinesIntersectSolids(line1, originalSolids);
            if (insideLines.Count > 0)
            {
               supports.Add(familyInstance);
            }
         }
         return supports;
      }

      public static ModelCurve CreateModelLine(Curve line)
      {
         try
         {
            if (line.Direction().IsParallel(XYZ.BasisZ))
            {
               var plane = Plane.CreateByThreePoints(line.SP(), line.EP(), line.SP().Add(XYZ.BasisX));
               var sk = SketchPlane.Create(AC.Document, plane);
               AC.Document.Create.NewDetailCurve(AC.ActiveView, line);
               return AC.Document.Create.NewModelCurve(line, sk);
            }
            else
            {
               var plane = Plane.CreateByThreePoints(line.SP(), line.EP(), line.SP().Add(XYZ.BasisZ));
               var sk = SketchPlane.Create(AC.Document, plane);
               AC.Document.Create.NewDetailCurve(AC.ActiveView, line);
               return AC.Document.Create.NewModelCurve(line, sk);
            }
         }
         catch (Exception)
         {

            return null;
         }
      }

      public static bool CheckRebarCanGoThrough2Spans(SpanModel spanModel1, SpanModel spanModel2, bool isBottomBar = true)
      {
         //Check 2 span same width
         if (spanModel1.Width.IsEqual(spanModel2.Width, 5.MmToFoot()) == false)
         {
            return false;
         }
         //Check cùng trục đối xứng
         var line = spanModel1.TopLine;
         var plane1 = BPlane.CreateByThreePoints(line.SP(), line.EP(), line.SP().Add(XYZ.BasisZ));
         var distance = plane1.SignedDistanceTo(spanModel2.TopLine.SP());
         if (distance > 5.MmToFoot())
         {
            return false;
         }
         //Check giat cap
         if (isBottomBar)
         {
            var d = Math.Abs(spanModel1.BotRight.Z - spanModel2.BotLeft.Z);
            if (d > BeamRebarRevitData.Instance.BeamRebarSettingJson.KhoangGiatCapDuocNhanThep + 5.MmToFoot())
            {
               return false;
            }
         }
         if (isBottomBar == false)
         {
            var d = Math.Abs(spanModel1.TopRight.Z - spanModel2.TopLeft.Z);
            if (d > BeamRebarRevitData.Instance.BeamRebarSettingJson.KhoangGiatCapDuocNhanThep + 5.MmToFoot())
            {
               return false;
            }
         }
         return true;
      }

      public static List<Line> GetInsideLinesIntersectSolids(Line line, List<Solid> solids)
      {
         var lines = new List<Line>();
         var option = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside };
         foreach (var solid in solids)
         {
            var intersection = solid.IntersectWithCurve(line, option);
            intersection.Cast<Line>().ToList().ForEach(x => lines.Add(x));
         }
         return lines;
      }

      public static void CreateDirectShape(Solid solid, bool needTransa = false)
      {
         if (needTransa)
         {
            using (var tx = new Transaction(AC.Document, "a"))
            {
               tx.Start();
               var ds = DirectShape.CreateElement(AC.Document, new ElementId(BuiltInCategory.OST_GenericModel));
               ds.SetShape(new List<GeometryObject> { solid });
               tx.Commit();
            }
         }
         else
         {
            var ds = DirectShape.CreateElement(AC.Document, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.SetShape(new List<GeometryObject> { solid });
         }
      }

      public static bool CheckBeamStraightAndHorizontal(FamilyInstance fi)
      {
         if (fi.Location is LocationCurve lc)
         {
            var line = lc.Curve as Line;
            if (line != null)
            {
               if (line.Direction.IsPerpendicular(XYZ.BasisZ))
               {
                  return true;
               }
            }
         }

         return false;
      }

      public static bool IsHorizontalBeam(FamilyInstance fi)
      {
         if (fi.Location is LocationCurve lc)
         {
            var line = lc.Curve as Line;
            if (line != null)
            {
               return false;
            }
         }
         return true;
      }

      public static void CreateLineIndependentTag(IndependentTag tag, ViewSection viewSection)
      {
         var plane = viewSection.ToBPlane();
         var bb = tag.get_BoundingBox(viewSection);
         var min = bb.Min.ProjectOnto(plane);
         var max = bb.Max.ProjectOnto(plane);
         AC.Document.Create.NewDetailCurve(viewSection, min.CreateLineByPointAndDirection(XYZ.BasisZ));
         AC.Document.Create.NewDetailCurve(viewSection, max.CreateLineByPointAndDirection(2 * XYZ.BasisZ));
      }

      /// <summary>
      /// Must be set detail level coarse for rebar first
      /// </summary>
      /// <param name="rebar"> Must be set detail level coarse for rebar first</param>
      /// <param name="viewSection"></param>
      /// <returns></returns>
      public static List<ReferenceAndPoint> GetStirrupReferences(Rebar rebar, ViewSection viewSection)
      {
         var raps = new List<ReferenceAndPoint>();
         var lines = rebar.Lines(viewSection);
         foreach (var line in lines)
         {
            if (line.Reference != null && line.Direction.IsParallel(XYZ.BasisZ))
            {
               var rf = line.Reference;
               var sp = line.SP();
               var rap = new ReferenceAndPoint { Normal = viewSection.RightDirection, Point = sp, Reference = rf };
               raps.Add(rap);
            }
         }
         var ordered = raps.OrderBy(x => x.Point.DotProduct(viewSection.RightDirection)).ToList();
         if (ordered.Count > 2)
         {
            var first = ordered.First();
            var last = ordered.Last();
            return new List<ReferenceAndPoint> { first, last };
         }
         return raps;
      }
   }
}