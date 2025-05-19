using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public static class RebarShopService
   {
      public static BeamShopSettingJson BeamShopSetting = new();
      public static List<ElementId> Ids = new();
      public static double RebarLengthStandard = 11700.MmToFoot();
      public static double RebarMinLength = 1950.MmToFoot();
      public static List<double> CommonSegmentLengths = new() { 11700.MmToFoot(), 7800.MmToFoot(), 5850.MmToFoot(), 3900.MmToFoot(), 2925.MmToFoot(), 2340.MmToFoot(), 9360.MmToFoot(), 8775.MmToFoot() };

      public static List<RebarShopModel> CutRebarShop(List<RebarShopModel> rebarToCuts, List<CutZone> allCutZones)
      {
         var rebarInStorages = new List<RebarInStorageModel>()
            {
                new(3000,20,3000.MmToFoot()),
                new(4000,20,4000.MmToFoot()),
                new(3000,20,5000.MmToFoot()),
                new(3000,20,6000.MmToFoot()),
            };

         var remainings = new List<RebarShopModel>();

         var list = new List<RebarShopModel>();
         var i = 0;
         while (rebarToCuts.Count > 0)
         {
            var rebarToCut = rebarToCuts[0];
            var useCase1 = false; //Cắt thép số chẵn
            var useCase2 = false; //Cắt thép phối với thép cũ
            var useCase3 = false; //Cắt thép bất kì theo CutZones
            rebarToCut.IsCut = true;

            var sourceLength = CalculateLengthOfRebarByCurves(rebarToCut.CurvesPair.OuterCurves);
            var cutZones = allCutZones.Where(x => IsCutZoneOfRebar(rebarToCut, x)).ToList();
            if (sourceLength.IsEqual(RebarLengthStandard, 10.MmToFoot()) || sourceLength < RebarLengthStandard || cutZones.Count == 0)
            {
               goto Here;
            }

            RebarShopModel rb1;
            RebarShopModel rb2;

            //Case1
            if (TryCutRebarOverLengthByCommonNumber(rebarToCut, cutZones, out rb1, out rb2))
            {
               useCase1 = true;
               if (rb2.Length > RebarLengthStandard)
               {
                  rebarToCuts.Add(rb2);
               }
               else
               {
                  list.Add(rb2);
                  rebarInStorages.Add(rb2.ToRebarInStorageModel());
               }

               list.Add(rb1);
               goto Here;
            }

            //Case 2
            foreach (var rebarInStorageModel in rebarInStorages)
            {
               if (rebarInStorageModel.Quantity < rebarToCut.Quantity)
               {
                  continue;
               }

               var requiredLength = RebarLengthStandard - rebarInStorageModel.Length;
               if (TryCutRebarOverLengthByCustomRequiredLength(rebarToCut, requiredLength, cutZones, out rb1, out rb2))
               {
                  useCase2 = true;
                  if (rb2.Length > RebarLengthStandard)
                  {
                     rebarToCuts.Add(rb2);
                  }
                  else
                  {
                     list.Add(rb2);
                  }

                  list.Add(rb1);
                  rebarInStorageModel.Quantity -= rebarToCut.Quantity;
                  goto Here;
               }
            }

            foreach (var rebarInStorageModel1 in rebarInStorages)
            {
               if (rebarInStorageModel1.Quantity < rebarToCut.Quantity)
               {
                  continue;
               }

               foreach (var rebarInStorageModel2 in rebarInStorages)
               {
                  if (rebarInStorageModel2.Quantity < rebarToCut.Quantity)
                  {
                     continue;
                  }
               }
            }

            //Case 3
            if (TryCutRebarOverLengthByCutZones(rebarToCut, cutZones, out rb1, out rb2))
            {
               useCase3 = true;
               if (rb2.Length > RebarLengthStandard)
               {
                  rebarToCuts.Add(rb2);
               }
               else
               {
                  list.Add(rb2);
               }

               list.Add(rb1);
            }

         Here:
            if (!useCase1 && !useCase3 && !useCase2)
            {
               rebarToCut.IsCut = false;
               list.Add(rebarToCut);
            }

            rebarToCuts.Remove(rebarToCut);
            i++;
            if (i > 500)
            {
               break;
            }
         }

         foreach (var rebarShopModel in list)
         {
            rebarShopModel?.CreateRebarShop();

            if (rebarShopModel != null && rebarShopModel.OriginalRebar.IsValidObject && rebarShopModel.IsCut)
            {
               AC.Document.Delete(rebarShopModel.OriginalRebar.Id);
            }
         }

         remainings.Clear();

         return list;
      }

      public static List<XYZ> GetPointsOfCurves(this List<Curve> curves)
      {
         var list = new List<XYZ>();
         foreach (var curve in curves)
         {
            list.Add(curve.SP());
            list.Add(curve.EP());
         }

         return list;
      }

      public static List<CutZone> GetCutZonesBySpan(BeamModel beamModel)
      {
         var zones = new List<CutZone>();
         var spanModels = beamModel.SpanModels;
         var z = beamModel.ZTop;
         if (spanModels.Count > 1)
         {
            for (int i = 0; i < spanModels.Count - 1; i++)
            {
               var currentSpanModel = spanModels[i];
               var nextSpanModel = spanModels[i + 1];

               var length = currentSpanModel.Length;
               var direct = currentSpanModel.Direction;
               if (currentSpanModel.LeftSupportModel != null && currentSpanModel.RightSupportModel != null)
               {
                  var tp1 = currentSpanModel.TopLeft.Add(length * BeamShopSetting.TopShopSpanFactor * direct).EditZ(z);
                  var tp2 = currentSpanModel.TopRight.Add(length * BeamShopSetting.TopShopSpanFactor * -direct).EditZ(z);
                  var topZone = new CutZone(tp1, tp2, 0, 0);
                  zones.Add(topZone);

                  if (i == 0)
                  {
                     var leftPoint = ((currentSpanModel.LeftSupportModel.TopLeft + currentSpanModel.LeftSupportModel.TopRight) / 2).EditZ(z); ;
                     var rightPoint =
                         currentSpanModel.TopLeft.Add(direct * length * BeamShopSetting.BotShopSpanFactor);
                     var leftZone = new CutZone(leftPoint, rightPoint, 0, 0);
                     zones.Add(leftZone);
                  }

                  {
                     var leftPoint = currentSpanModel.TopRight.Add(length * BeamShopSetting.BotShopSpanFactor * -direct).EditZ(z);
                     var rightPoint = nextSpanModel.TopLeft.Add(length * BeamShopSetting.BotShopSpanFactor * direct).EditZ(z);
                     var rightZone = new CutZone(leftPoint, rightPoint, 0, 0);
                     zones.Add(rightZone);
                  }
               }
            }

            var lastSpan = spanModels.Last();
            if (lastSpan.RightSupportModel != null)
            {
               var length = lastSpan.Length;
               var direct = lastSpan.Direction;
               {
                  var tp1 = lastSpan.TopLeft.Add(length * BeamShopSetting.TopShopSpanFactor * direct).EditZ(z);
                  var tp2 = lastSpan.TopRight.Add(length * BeamShopSetting.TopShopSpanFactor * -direct).EditZ(z);
                  var topZone = new CutZone(tp1, tp2, 0, 0);
                  zones.Add(topZone);
               }

               {
                  var rightPoint = ((lastSpan.RightSupportModel.TopLeft + lastSpan.RightSupportModel.TopRight) / 2).EditZ(z);
                  var leftPoint =
                      lastSpan.TopLeft.Add(direct * length * -BeamShopSetting.BotShopSpanFactor);
                  var rightSupportZone = new CutZone(leftPoint, rightPoint, 0, 0);
                  zones.Add(rightSupportZone);
               }
            }
         }
         else
         {
            var span = spanModels.First();
            if (span.LeftSupportModel != null && span.RightSupportModel != null)
            {
               var length = span.Length;
               var direct = span.Direction;
               {
                  var tp1 = span.TopLeft.Add(length * BeamShopSetting.TopShopSpanFactor * direct).EditZ(z);
                  var tp2 = span.TopRight.Add(length * BeamShopSetting.TopShopSpanFactor * -direct).EditZ(z);
                  var topZone = new CutZone(tp1, tp2, 0, 0);
                  zones.Add(topZone);
               }

               {
                  var rightPoint = ((span.RightSupportModel.TopLeft + span.RightSupportModel.TopRight) / 2).EditZ(z);
                  var leftPoint =
                      span.TopLeft.Add(direct * length * -BeamShopSetting.BotShopSpanFactor);
                  var rightSupportZone = new CutZone(leftPoint, rightPoint, 0, 0);
                  zones.Add(rightSupportZone);
               }

               {
                  var leftPoint = ((span.LeftSupportModel.TopLeft + span.LeftSupportModel.TopRight) / 2).EditZ(z);
                  var rightPoint =
                      span.TopLeft.Add(direct * length * BeamShopSetting.BotShopSpanFactor);
                  var leftSupportZone = new CutZone(leftPoint, rightPoint, 0, 0);
                  zones.Add(leftSupportZone);
               }
            }
         }

         return zones;
      }

      public static double CalculateLengthOfRebarByCurves(this List<Curve> curves, int roundingMm = 10)
      {
         var d = curves.Sum(x => x.Length);
         if (roundingMm > 0)
         {
            return d.RoundMilimet(roundingMm);
         }

         return d;
      }

      public static void CutCurveByRequiredLength(this Curve curve, double requiredLength, double lapping, out Curve first, out Curve last)
      {
         var sp = curve.SP();
         var ep = curve.EP();
         var direct = (ep - sp).Normalize();
         var p1 = sp.Add(direct * requiredLength);
         first = sp.CreateLine(p1);
         var p2 = p1.Add(-direct * lapping);
         last = p2.CreateLine(ep);
      }

      public static void SplitCurvesByRequiredLength(this List<Curve> curves, double lapping, double requiredLength, out List<Curve> list1, out List<Curve> list2)
      {
         curves = curves.Select(x => x.Clone()).ToList();
         list1 = new List<Curve>();
         list2 = new List<Curve>();

         var isCutted = false;

         for (int i = 0; i < curves.Count; i++)
         {
            var length = CalculateLengthOfRebarByCurves(curves.Take(i + 1).ToList());
            if (length < requiredLength)
            {
               list1.Add(curves[i].Clone());
            }

            if (isCutted)
            {
               list2.Add(curves[i].Clone());
            }
            else
            {
               if (length > requiredLength)
               {
                  CutCurveByRequiredLength(curves[i], requiredLength, lapping, out var c1, out var c2);
                  list1.Add(c1);
                  list2.Add(c2);
                  isCutted = true;
               }
            }

            requiredLength -= curves[i].Length;
         }
      }

      public static void SplitCurvesByPointOnCurves(this List<Curve> curves, XYZ p, double lapping, out List<Curve> list1, out List<Curve> list2)
      {
         curves = curves.Select(x => x.Clone()).ToList();
         list1 = new List<Curve>();
         list2 = new List<Curve>();

         var min = double.MaxValue;
         var index = 0;
         for (int i = 0; i < curves.Count; i++)
         {
            var d = curves[i].Distance(p);
            if (d < min)
            {
               min = d;
               index = i;
            }
         }

         var isCut = false;
         for (int i = 0; i < curves.Count; i++)
         {
            var c = curves[i].Clone();
            if (i == index)
            {
               isCut = true;
               var l1 = c.SP().CreateLine(p);
               var l2 = p.Add(l1.Direction * -lapping).CreateLine(c.EP());

               list1.Add(l1);
               list2.Add(l2);
            }
            else
            {
               if (isCut)
               {
                  list2.Add(c);
               }
               else
               {
                  list1.Add(c);
               }
            }
         }
      }

      public static List<Rebar> CreateRebarShop(this RebarShopModel rebarShopModel)
      {
         try
         {
            if (rebarShopModel.IsCut)
            {
               var normal = rebarShopModel.CurvesPair.CenterCurves.OrderByDescending(x => x.Length).First().Direction().CrossProduct(XYZ.BasisZ);
               if (rebarShopModel.CurvesPair.CenterCurves.Count > 1)
               {
                  normal = rebarShopModel.CurvesPair.CenterCurves[0].Direction().CrossProduct(rebarShopModel.CurvesPair.CenterCurves[1].Direction());
               }

               var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rebarShopModel.RebarBarType, null, null, rebarShopModel.Host,
                   normal, rebarShopModel.CurvesPair.CenterCurves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);

               //Set bar position
               if (rebarShopModel.PositionPoints.Count > 1)
               {
                  var arrayLength = rebarShopModel.PositionPoints.First()
                      .DistanceTo(rebarShopModel.PositionPoints.Last());

                  rebar.SetRebarLayoutAsFixedNumber(rebarShopModel.PositionPoints.Count, arrayLength, true, true, true);

                  var centerX = rebarShopModel.PositionPoints.Sum(x => x.X) / rebarShopModel.PositionPoints.Count;
                  var centerY = rebarShopModel.PositionPoints.Sum(x => x.Y) / rebarShopModel.PositionPoints.Count;
                  var centerZ = rebarShopModel.PositionPoints.Sum(x => x.Z) / rebarShopModel.PositionPoints.Count;

                  var center = new XYZ(centerX, centerY, centerZ);
                  var bPlane = BPlane.CreateByNormalAndOrigin(normal, center);
                  AC.Document.Regenerate();
                  BeamRebarServices.MoveRebarToCenterPlane(rebar, bPlane);
               }
               else
               {
                  var bPlane = BPlane.CreateByNormalAndOrigin(normal, rebarShopModel.PositionPoints.First());
                  AC.Document.Regenerate();
                  BeamRebarServices.MoveRebarToCenterPlane(rebar, bPlane);
               }

               rebarShopModel.NewRebar = rebar;
               // var list = SetRebarLayoutForRebar(mainRebar.RebarPointsInSection, rebar, span);

               rebar.SetRebarSolidUnobscured(AC.ActiveView, "");
            }
            else
            {
               rebarShopModel.NewRebar = rebarShopModel.OriginalRebar;
            }

            return null;
         }
         catch (Exception e)
         {
            MessageBox.Show(e.Message);
            return null;
         }
      }

      public static List<Curve> GetRebarCurvesInPlane(this Rebar rebar)
      {
         var list = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves,
                         0).ToList();

         list = list.ReOrderCurves();
         return list;
      }

      public static bool TryCutRebarOverLengthByCommonNumber(RebarShopModel rebarShopModel, List<CutZone> cutZones, out RebarShopModel result1, out RebarShopModel result2)
      {
         result1 = rebarShopModel.Clone();
         result2 = rebarShopModel.Clone();

         foreach (var commonLength in CommonSegmentLengths)
         {
            if (TryComputePointOfCurvesByLength(rebarShopModel.CurvesPair.OuterCurves, commonLength, out var p, out var direct))
            {
               if (cutZones.Any(x => x.IsPointInCutZoneValid(p, direct)))
               {
                  SplitCurvesByRequiredLength(rebarShopModel.CurvesPair.OuterCurves, 800.MmToFoot(), commonLength, out var list1, out var list2);

                  var length1 = list1.CurvesLength();
                  var length2 = list2.CurvesLength();

                  if (length1 > RebarMinLength && length2 > RebarMinLength)
                  {
                     result1.CurvesPair.OuterCurves = list1;
                     result1.CurvesPair.CenterCurves = list1.GetOffsetCurves(rebarShopModel.Diameter, false);

                     result2.CurvesPair.OuterCurves = list2;
                     result2.CurvesPair.CenterCurves = list2.GetOffsetCurves(rebarShopModel.Diameter, false);

                     return true;
                  }
               }
            }
         }

         return false;
      }

      public static bool TryCutRebarOverLengthByCustomRequiredLength(RebarShopModel rebarShopModel, double requiredLength, List<CutZone> cutZones, out RebarShopModel result1, out RebarShopModel result2)
      {
         result1 = rebarShopModel.Clone();
         result2 = rebarShopModel.Clone();

         var sourceLength = CalculateLengthOfRebarByCurves(rebarShopModel.CurvesPair.OuterCurves);

         if (sourceLength.IsEqual(RebarLengthStandard, 10.MmToFoot()) || sourceLength < RebarLengthStandard)
         {
            return false;
         }

         if (cutZones == null || cutZones.Count == 0)
         {
            return false;
         }

         if (TryComputePointOfCurvesByLength(rebarShopModel.CurvesPair.OuterCurves, requiredLength, out var p, out var direct))
         {
            if (cutZones.Any(x => x.IsPointInCutZoneValid(p, direct)))
            {
               SplitCurvesByRequiredLength(rebarShopModel.CurvesPair.OuterCurves, 800.MmToFoot(), requiredLength, out var list1, out var list2);

               result1.CurvesPair.OuterCurves = list1;
               result1.CurvesPair.CenterCurves = list1.GetOffsetCurves(rebarShopModel.Diameter, false);

               result2.CurvesPair.OuterCurves = list2;
               result2.CurvesPair.CenterCurves = list2.GetOffsetCurves(rebarShopModel.Diameter, false);

               return true;
            }
         }

         return false;
      }

      public static bool TryCutRebarOverLengthByCutZones(RebarShopModel rebarShopModel, List<CutZone> cutZones, out RebarShopModel result1, out RebarShopModel result2)
      {
         double maxLengthToCompare = 8000.0.MmToFoot();
         result1 = rebarShopModel.Clone();
         result2 = rebarShopModel.Clone();

         var sourceLength = CalculateLengthOfRebarByCurves(rebarShopModel.CurvesPair.OuterCurves);

         if (sourceLength.IsEqual(RebarLengthStandard, 10.MmToFoot()) || sourceLength < RebarLengthStandard)
         {
            return false;
         }

         if (cutZones == null || cutZones.Count == 0)
         {
            return false;
         }

         foreach (var cutZone in cutZones)
         {
            var points = ComputeIntersectionPointsOfRebarAndCutZone(rebarShopModel, cutZone);
            //AC.Document.Create.NewDetailCurve(AC.ActiveView, points[0].CreateLine(points[1]));
            if (points.Count == 2)
            {
               var p1 = points[0];
               var p2 = points[1];
               if (IsPointInCutZoneValid(cutZone, p1, cutZone.Direction))
               {
                  SplitCurvesByPointOnCurves(rebarShopModel.CurvesPair.CenterCurves, p1, 800.MmToFoot(), out var list1, out var list2);
                  var length1 = list1.Sum(x => x.Length);
                  var length2 = list2.Sum(x => x.Length);
                  if (length1 > RebarMinLength && length2 > RebarMinLength)
                  {
                     result1.CurvesPair.CenterCurves = list1;
                     result1.CurvesPair.OuterCurves = list1;
                     result2.CurvesPair.CenterCurves = list2;
                     result2.CurvesPair.OuterCurves = list2;
                     return true;
                  }
               }

               if (IsPointInCutZoneValid(cutZone, p2, cutZone.Direction))
               {
                  SplitCurvesByPointOnCurves(rebarShopModel.CurvesPair.CenterCurves, p2, 800.MmToFoot(), out var list1, out var list2);
                  var length1 = list1.Sum(x => x.Length);
                  var length2 = list2.Sum(x => x.Length);
                  if (length1 > RebarMinLength && length2 > RebarMinLength)
                  {
                     result1.CurvesPair.CenterCurves = list1;
                     result1.CurvesPair.OuterCurves = list1;
                     result2.CurvesPair.CenterCurves = list2;
                     result2.CurvesPair.OuterCurves = list2;
                     return true;
                  }
               }
            }
         }

         return false;
      }

      public static double CurvesLength(this List<Curve> curves)
      {
         return curves.Sum(x => x.Length);
      }

      public static List<XYZ> ComputeIntersectionPointsOfRebarAndCutZone(RebarShopModel model, CutZone cutZone)
      {
         var points = new List<XYZ>();
         if (cutZone.HostId == model.HostId || true)
         {
            var line1 = cutZone.Start.ProjectOnto(model.Plane).EditZ(cutZone.BotElevation)
                .CreateLineByPointAndDirection(XYZ.BasisZ * 3.MeterToFoot());

            var line2 = cutZone.End.ProjectOnto(model.Plane).EditZ(cutZone.BotElevation)
                .CreateLineByPointAndDirection(XYZ.BasisZ * 3.MeterToFoot());

            var rs1 = TryComputeListCurveIntersectWithLine(model.CurvesPair.CenterCurves, line1, model.Plane, out var p1);
            var rs2 = TryComputeListCurveIntersectWithLine(model.CurvesPair.CenterCurves, line2, model.Plane, out var p2);
            if (rs1 && rs2 && p1 != null && p2 != null)
            {
               points.Add(p1);
               points.Add(p2);
            }
         }

         return points;
      }

      private static bool TryComputeListCurveIntersectWithLine(this List<Curve> curves, Line line, BPlane plane, out XYZ p)
      {
         p = null;
         line = line.ProjectOntoPlane(plane);
         foreach (var curve in curves)
         {
            var point = curve.GetIntersectionPointByExtend(line, 10);
            if (point != null)
            {
               p = point;
               return true;
            }
         }

         return false;
      }

      private static bool IsPointInCutZoneValid(this CutZone cutZone, XYZ point, XYZ direct, double lap = 850)
      {
         lap = 800.MmToFoot();
         var d1 = cutZone.Start.DotProduct(direct);
         var d2 = cutZone.End.DotProduct(direct);
         var d3 = point.DotProduct(direct);
         var max = Math.Max(d1, d2);
         var min = Math.Min(d1, d2);

         if (d3 < max && d3 > min + lap)
         {
            return true;
         }

         return false;
      }


      /// <summary>
      /// check lapped rebar is not too many in a position
      /// </summary>
      /// <returns></returns>
      private static bool IsCanCutRebarInThisPosition()
      {

         return true;
      }

      private static bool TryComputePointOfCurvesByLength(List<Curve> curves, double requiredLength, out XYZ point, out XYZ direct, bool isInvert = false)
      {
         point = curves.Last().EP();
         direct = curves.First().Direction();

         var length = CalculateLengthOfRebarByCurves(curves);
         if (length < requiredLength)
         {
            return false;
         }

         var total = 0.0;
         var inputCurves = curves.Select(x => x.Clone()).ToList();

         if (isInvert)
         {
            inputCurves = curves.Select(x => x.CurveReverse()).Reverse().ToList();
         }

         foreach (var curve in inputCurves)
         {
            total += curve.Length;
            if (total > requiredLength)
            {
               point = curve.Evaluate(requiredLength - total + curve.Length, false);

               direct = (curve.EP() - curve.SP()).Normalize();
               return true;
            }
         }

         return true;
      }

      public static Curve CurveReverse(this Curve curve)
      {
         return Line.CreateBound(curve.EP(), curve.SP());
      }

      public static List<Curve> CurvesReverse(this List<Curve> curves)
      {
         return curves.Select(x => x.CurveReverse()).ToList();
      }

      private static double GetRebarElevation(this Rebar rebar)
      {
         var curves = rebar.GetRebarCurvesInPlane();
         return curves.OrderByDescending(x => x.ApproximateLength).First().Midpoint().Z;
      }

      private static bool IsCutZoneOfRebar(this RebarShopModel rebar, CutZone cutZone)
      {
         if (rebar.HostId == cutZone.HostId || true)
         {
            if (cutZone.BotElevation < rebar.Elevation && cutZone.TopElevation > rebar.Elevation)
            {
               return true;
            }
         }

         return false;
      }

      public static List<Curve> GetOffsetCurves(this List<Curve> curves, int offset, bool isOuter = true)
      {
         if (curves.Count > 1)
         {
            var cl = new CurveLoop();
            var p1 = curves[0].SP();
            var p2 = curves[0].EP();
            var p3 = curves[1].SP();
            var p4 = curves[1].EP();
            curves = curves.ReOrderCurves();
            var p5 = curves[0].SP();
            var p6 = curves[0].EP();
            var p7 = curves[1].SP();
            var p8 = curves[1].EP();
            curves.ForEach(x => cl.Append(x));
            var normal = cl.GetPlane().Normal.Normalize();
            var cl1 = CurveLoop.CreateViaOffset(cl, offset.MmToFoot() / 2, normal);
            if (isOuter)
            {
               if (cl1.GetExactLength() < cl.GetExactLength())
               {
                  cl1 = CurveLoop.CreateViaOffset(cl, offset.MmToFoot() / 2, -normal);
               }
            }
            else
            {
               if (cl1.GetExactLength() > cl.GetExactLength())
               {
                  cl1 = CurveLoop.CreateViaOffset(cl, offset.MmToFoot() / 2, -normal);
               }
            }

            return cl1.ToList();
         }

         return curves;
      }

      public static List<Curve> ReOrderCurves(this List<Curve> curves)
      {
         return curves;
         if (curves.Count == 1)
         {
            return curves;
         }
         var list = new List<Curve>();

         var tol = 1.MmToFoot();

         XYZ end = curves[0].EP();

         for (var index = 0; index < curves.Count; index++)
         {
            var curve1 = curves[index];

            var sp1 = curve1.SP();
            var ep1 = curve1.EP();

            if (index == 0)
            {
               var curve2 = curves[index + 1];
               var sp2 = curve2.SP();
               var ep2 = curve2.EP();
               if (ep1.IsAlmostEqualTo(sp2, tol) || ep1.IsAlmostEqualTo(ep2, tol))
               {
                  end = ep1;
                  list.Add(curve1.Clone());
               }
               else
               {
                  end = sp1;
                  list.Add(ep1.CreateLine(sp1));
               }
            }
            else
            {
               if (sp1.IsAlmostEqualTo(end))
               {
                  end = ep1;
                  list.Add(curve1.Clone());
               }
               else
               {
                  end = sp1;
                  list.Add(ep1.CreateLine(sp1));
               }
            }
         }

         return list;
      }
   }
}