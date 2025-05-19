using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class RebarCurvePairModel
   {
      public List<Curve> CenterCurves { get; set; }
      public List<Curve> OuterCurves { get; set; }
      public Curve MaxCurve => CenterCurves.OrderByDescending(x => x.ApproximateLength).First();
   }
}