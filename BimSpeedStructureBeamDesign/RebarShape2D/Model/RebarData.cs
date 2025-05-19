using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;
using RebarDetailModel = BimSpeedStructureBeamDesign.RebarShape2D.Model.RebarDetailModel;

namespace BimSpeedStructureBeamDesign.RebarShape2D.Model
{
   public class RebarData
   {
      public double A { get; set; }
      public double B { get; set; }
      public double C { get; set; }
      public double D { get; set; }
      public double E { get; set; }
      public List<double> SegmentLengths { get; set; } = new List<double>();
      public List<string> SegmentParamNames { get; set; }

      public RebarData(Rebar rebar, List<string> paramNames, RebarDetailModel model)
      {
         SegmentParamNames = paramNames;
         var curves = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
         if (curves.Count >= 1)
         {
            A = curves[0].ApproximateLength;
         }
         if (curves.Count >= 2)
         {
            B = curves[1].ApproximateLength;
         }
         if (curves.Count >= 3)
         {
            C = curves[2].ApproximateLength;
         }
         if (curves.Count >= 4)
         {
            D = curves[3].ApproximateLength;
         }
         if (curves.Count >= 5)
         {
            E = curves[4].ApproximateLength;
         }

         GetSegmentLengths(rebar, model);
      }

      private void GetSegmentLengths(Rebar rebar, RebarDetailModel model)
      {
         var rrm = model.RebarRoundingManager;
         List<double> dimVals = new List<double>();
         var rbd = model.RebarBendData;
         if (rbd.HookAngle0 > 0)
         {
            dimVals.Add(0);
         }
         SegmentParamNames.ForEach(x => dimVals.Add(
             Math.Round(rebar.GetParameterValueByNameAsDouble(x).FootToMm())));
         if (rbd.HookAngle1 > 0)
         {
            dimVals.Add(0);
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

         SegmentLengths = dimVals.Select(x => x.MmToFoot()).ToList();
      }
   }
}