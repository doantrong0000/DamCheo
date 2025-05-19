using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.RebarShape2D.Model
{
   public class RebarDetailModel
   {
      public RebarShape RebarShape { get; set; }
      public Rebar Rebar { get; set; }
      public List<Curve> Curves { get; set; }
      public XYZ Normal { get; set; }

      /// <summary>
      /// The First Point of MainBotBot curve
      /// </summary>
      public XYZ Origin { get; set; }

      /// <summary>
      /// The First Line
      /// </summary>
      public Curve MainXCurve { get; set; }

      public Transform Transform { get; set; }
      public Curve MaxCurve { get; set; }
      public Curve FamilyMaxCurve { get; set; }
      public Transform InvertTransform { get; set; }
      public List<Curve> FamilyCurves { get; set; }
      public List<Curve> FamilyOuterCurves { get; set; } = new List<Curve>();

      public RebarRoundingManager RebarRoundingManager { get; set; }
      public RebarBendData RebarBendData { get; set; }
      public List<int> HookTextIndexs { get; set; } = new List<int>();
      public List<string> SegmentParamNames { get; set; } = new List<string>();
      public List<SegmentLength> SegmentLengths { get; set; } = new List<SegmentLength>();
      public List<RebarData> RebarDatas { get; set; } = new List<RebarData>();
      public bool IsVariable { get; set; }
      public bool IsAVariable { get; set; }
      public bool IsBVariable { get; set; }
      public bool IsCVariable { get; set; }
      public bool IsDVariable { get; set; }
      public bool IsEVariable { get; set; }
      public bool Is2HooksSame { get; set; }

      public RebarDetailModel(Rebar rebar)
      {
         Rebar = rebar;
         if (rebar.IsVariableLength() == true)
         {
            IsVariable = true;
         }
         var normal = rebar.RebarNormal();
         Normal = normal;
         Curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0).ToList();

         if (IsVariable)
         {
            var num = rebar.NumberOfBarPositions / 2;
            Curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, num).ToList();
         }

         MainXCurve = Curves.OrderByDescending(x => x.Length).FirstOrDefault();
         MaxCurve = Curves.OrderByDescending(x => x.Length).FirstOrDefault();
         Transform = Transform.Identity;
         Origin = MainXCurve.GetEndPoint(0);
         Transform.Origin = Origin;
         Transform.BasisX = (MainXCurve.GetEndPoint(1) - MainXCurve.GetEndPoint(0)).Normalize();
         Transform.BasisZ = normal;
         Transform.BasisY = (Transform.BasisX).CrossProduct(normal);
         InvertTransform = Transform.Inverse;
         FamilyCurves = Curves.Select(x => x.CreateTransformed(InvertTransform)).ToList();
         FamilyMaxCurve = MaxCurve.CreateTransformed(InvertTransform);
         RebarShape = rebar.GetRebarShape();
         RebarBendData = rebar.GetBendData();
         var setting = new FilteredElementCollector(AC.Document).OfClass(typeof(ReinforcementSettings))
             .Cast<ReinforcementSettings>().First();
         RebarRoundingManager = setting.GetRebarRoundingManager();
         GetData();
      }

      private void GetData()
      {
         GetSegmentParamNames();
         if (IsVariable)
         {
            using (var st = new Transaction(AC.Document, "a"))
            {
               st.Start();
               //Create news
               var rebars = new List<Rebar>();
               RebarBarType rebarBarType = AC.Document.GetElement(Rebar.GetTypeId()) as RebarBarType;
               RebarHookType rebarHookType = AC.Document.GetElement(Rebar.GetHookTypeId(0)) as RebarHookType;
               RebarHookType rebarHookType2 = AC.Document.GetElement(Rebar.GetHookTypeId(1)) as RebarHookType;

               RebarStyle rebarStyle =
                   (RebarStyle)Rebar.GetParameterValueByNameAsInteger(BuiltInParameter.REBAR_ELEM_HOOK_STYLE);
               RebarHookOrientation hookOrient = Rebar.GetBendData().HookOrient0;
               RebarHookOrientation hookOrient2 = Rebar.GetBendData().HookOrient1;
               var normal = Rebar.RebarNormal();
               var host = Rebar.GetHostId().ToElement();
               for (int i = 0; i < Rebar.NumberOfBarPositions; i++)
               {
                  if (Rebar.DoesBarExistAtPosition(i))
                  {
                     var list2 = Rebar.GetCenterlineCurves(false, true, true, 0, i);
                     Transform transform = Rebar.GetRebarPositionTransform(i);
                     list2 = list2.Select(x => x.CreateTransformed(transform)).ToList();
                     Rebar rebar = Rebar.CreateFromCurves(AC.Document, rebarStyle, rebarBarType, rebarHookType, rebarHookType2, host, normal, list2, hookOrient, hookOrient2, true, true);
                     rebars.Add(rebar);
                     AC.Document.Regenerate();
                     var data = new RebarData(rebar, SegmentParamNames, this);
                     RebarDatas.Add(data);
                  }
               }
               st.RollBack();
            }

            //Cần tìm ra đoạn có chiều dài biến thiên
            var first = RebarDatas[0];
            foreach (var rebarData in RebarDatas)
            {
               if (IsAVariable == false && RebarDatas.Any(x => x.A > 0))
               {
                  IsAVariable = !first.A.IsEqual(rebarData.A, 1.MmToFoot());
               }

               if (IsBVariable == false && RebarDatas.Any(x => x.B > 0))
               {
                  IsBVariable = !first.B.IsEqual(rebarData.B, 1.MmToFoot());
               }

               if (IsCVariable && RebarDatas.Any(x => x.C > 0))
               {
                  IsCVariable = !first.C.IsEqual(rebarData.C, 1.MmToFoot());
               }

               if (IsDVariable && RebarDatas.Any(x => x.D > 0))
               {
                  IsDVariable = !first.D.IsEqual(rebarData.D, 1.MmToFoot());
               }

               if (IsEVariable && RebarDatas.Any(x => x.E > 0))
               {
                  IsEVariable = !first.E.IsEqual(rebarData.E, 1.MmToFoot());
               }
            }
         }

         GetSegmentLengths();
         Check2HookNear();
      }

      private void GetSegmentLengths()
      {
         var start = 0;
         var rebar = Rebar;
         var rrm = RebarRoundingManager;
         List<double> dimVals = new List<double>();
         var rbd = RebarBendData;
         if (rbd.HookAngle0 > 0)
         {
            start++;
            HookTextIndexs.Add(0);
            double hookLen = 0;
            var bip = BuiltInParameter.REBAR_SHAPE_START_HOOK_LENGTH;
            hookLen = rebar.get_Parameter(bip).AsDouble().FootToMm();
            dimVals.Add(hookLen);
         }
         SegmentParamNames.ForEach(x => dimVals.Add(
             Math.Round(Rebar.GetParameterValueByNameAsDouble(x).FootToMm())));
         if (rbd.HookAngle1 > 0)
         {
            HookTextIndexs.Add(dimVals.Count);
            double hookLen = 0;
            var bip = BuiltInParameter.REBAR_SHAPE_END_HOOK_LENGTH;
            hookLen = Rebar.GetParameterValueByNameAsDouble(bip).FootToMm();
            dimVals.Add(hookLen);
         }

         double roundingNum = rrm.ApplicableSegmentLengthRounding;
         if (roundingNum.IsEqual(0)) roundingNum = 1;
         for (int i = 0; i < dimVals.Count; i++)
         {
            if (rrm.ApplicableSegmentLengthRoundingMethod == RoundingMethod.Nearest)
            {
               dimVals[i] = Math.Round(dimVals[i] / roundingNum) * roundingNum;
            }
            else if (rrm.ApplicableSegmentLengthRoundingMethod == RoundingMethod.Up)
            {
               dimVals[i] = Math.Ceiling(dimVals[i] / roundingNum) * roundingNum;
            }
            else
            {
               dimVals[i] = Math.Floor(dimVals[i] / roundingNum) * roundingNum;
            }
         }

         for (int i = 0; i < dimVals.Count; i++)
         {
            if (dimVals[i].IsEqual(0))
            {
               dimVals[i] = 5000;
            }
         }

         for (var index = 0; index < dimVals.Count; index++)
         {
            var dimVal = dimVals[index];
            var segmentLength = new SegmentLength() { Length = dimVal.MmToFoot() };
            if (index == 0 && rbd.HookAngle0 > 0)
            {
               segmentLength.Hook = 0;
            }
            if (index == dimVals.Count - 1 && rbd.HookAngle1 > 0)
            {
               segmentLength.Hook = 1;
            }
            SegmentLengths.Add(segmentLength);
         }

         if (IsAVariable)
         {
            var index = start + 0;
            SegmentLengths[index].IsVariable = true;
            var min = RebarDatas.Min(x => x.SegmentLengths[index]);
            var max = RebarDatas.Max(x => x.SegmentLengths[index]);
            SegmentLengths[index].Min = min;
            SegmentLengths[index].Max = max;
         }
         if (IsBVariable)
         {
            var index = start + 1;
            SegmentLengths[index].IsVariable = true;
            var min = RebarDatas.Min(x => x.SegmentLengths[index]);
            var max = RebarDatas.Max(x => x.SegmentLengths[index]);
            SegmentLengths[index].Min = min;
            SegmentLengths[index].Max = max;
         }
         if (IsCVariable)
         {
            var index = start + 2;
            SegmentLengths[index].IsVariable = true;
            var min = RebarDatas.Min(x => x.SegmentLengths[index]);
            var max = RebarDatas.Max(x => x.SegmentLengths[index]);
            SegmentLengths[index].Min = min;
            SegmentLengths[index].Max = max;
         }
         if (IsDVariable)
         {
            var index = start + 3;
            SegmentLengths[index].IsVariable = true;
            var min = RebarDatas.Min(x => x.SegmentLengths[index]);
            var max = RebarDatas.Max(x => x.SegmentLengths[index]);
            SegmentLengths[index].Min = min;
            SegmentLengths[index].Max = max;
         }

         if (IsEVariable)
         {
            var index = start + 4;
            SegmentLengths[index].IsVariable = true;
            var min = RebarDatas.Min(x => x.SegmentLengths[index]);
            var max = RebarDatas.Max(x => x.SegmentLengths[index]);
            SegmentLengths[index].Min = min;
            SegmentLengths[index].Max = max;
         }
      }

      private void GetSegmentParamNames()
      {
         var def = RebarShape.GetRebarShapeDefinition();

         if (def is RebarShapeDefinitionBySegments)
         {
            if (RebarShape.GetRebarShapeDefinition() is RebarShapeDefinitionBySegments defBySegment)
            {
               for (int i = 0; i < defBySegment.NumberOfSegments; i++)
               {
                  var rss = defBySegment.GetSegment(i);
                  if (rss.GetConstraints() is List<RebarShapeConstraint> constraints)
                  {
                     foreach (var rsc in constraints)
                     {
                        if (!(rsc is RebarShapeConstraintSegmentLength))
                           continue;
                        ElementId paramId = rsc.GetParamId();
                        if ((paramId == ElementId.InvalidElementId))
                           continue;
                        foreach (Parameter p in RebarShape.Parameters)
                        {
                           if (p.Id.GetElementIdValue() == paramId.GetElementIdValue())
                           {
                              SegmentParamNames.Add(p.Definition.Name);
                              break;
                           }
                        }
                     }
                  }
               }
            }
         }
         if (def is RebarShapeDefinitionByArc)
         {
            if (RebarShape.GetRebarShapeDefinition() is RebarShapeDefinitionByArc defByArc)
            {
               var constraints = defByArc.GetConstraints();
               foreach (var constraint in constraints)
               {
                  ElementId paramId = constraint.GetParamId();
                  if ((paramId == ElementId.InvalidElementId))
                     continue;
                  foreach (Parameter p in RebarShape.Parameters)
                  {
                     if (p.Id.GetElementIdValue() == paramId.GetElementIdValue())
                     {
                        SegmentParamNames.Add(p.Definition.Name);
                        break;
                     }
                  }
               }
            }
         }
      }

      private void Check2HookNear()
      {
         RebarHookType rebarHookType = AC.Document.GetElement(Rebar.GetHookTypeId(0)) as RebarHookType;
         RebarHookType rebarHookType2 = AC.Document.GetElement(Rebar.GetHookTypeId(1)) as RebarHookType;
         if (rebarHookType != null && rebarHookType2 != null && rebarHookType.Id.GetElementIdValue() == rebarHookType2.Id.GetElementIdValue())
         {
            Is2HooksSame = true;
         }
         if (Is2HooksSame)
         {
            var curves = Rebar.GetCenterlineCurves(true, false, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            var hook1 = curves.First();
            var hook2 = curves.Last();
            if (hook1.Midpoint().DistanceTo(hook2.Midpoint()) < 100.MmToFoot())
            {
               Is2HooksSame = true;
            }
            else
            {
               Is2HooksSame = false;
            }
         }
      }
   }
}