using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using BimSpeedUtils;
using MoreLinq;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.Model
{
   public class CutRebarModel
   {
      public double Diameter { get; set; }
      public string RebarNumber { get; set; }
      public bool IsTop { get; set; } = true;
      public Rebar Rebar { get; set; }
      public List<Curve> Curves { get; set; }
      public Curve MainCurve { get; set; }

      public XYZ Mid { get; set; }

      public double Length { get; set; }

      public string MidPointString
      {
         get
         {
            var mid = MainCurve.Midpoint();
            var x = mid.X.Round2Number();
            var y = mid.Y.Round2Number();
            var z = mid.Z.Round2Number();

            return $"{x}-{y}-{z}";
         }
      }


      public List<BPlane> AllRebarPlanes { get; set; } = new();

      public CutRebarModel(Rebar rebar)
      {
         Curves = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0).ToList();

         MainCurve = Curves.Maxima(x => x.Length).FirstOrDefault();

         Length = Curves.Sum(x => x.Length);
         Rebar = rebar;

         RebarNumber = Rebar.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER);

         Mid = MainCurve.Midpoint();

         Diameter = rebar.BarDiameter();
      }


      public CutRebarModel(Rebar rebar, Transform tf)
      {
         Curves = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0).ToList();

         Curves = Curves.Select(x => x.CreateTransformed(tf)).ToList();

         MainCurve = Curves.Maxima(x => x.Length).FirstOrDefault();

         Length = Curves.Sum(x => x.Length);
         Rebar = rebar;

         RebarNumber = Rebar.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER);

         Mid = MainCurve.Midpoint();

         Diameter = rebar.BarDiameter();
      }

      public CutRebarModel()
      {

      }

      public CutRebarModel Clone()
      {
         return new CutRebarModel()
         {
            Rebar = Rebar,
            Curves = Curves.Select(x => x.Clone()).ToList(),
            MainCurve = MainCurve.Clone(),
            Length = Length,
            IsTop = IsTop,
            Diameter = Diameter,
            Mid = Mid.EditZ(Mid.Z),
            AllRebarPlanes = AllRebarPlanes,
         };
      }


      public CutRebarModel Clone(List<Curve> curves, List<Curve> curveCoKhoangHo)
      {
         return new CutRebarModel()
         {
            Rebar = Rebar,
            MainCurve = curves.Maxima(x => x.Length).FirstOrDefault(),
            Length = curves.Sum(x => x.Length),
            Curves = curveCoKhoangHo.Select(x => x.Clone()).ToList(),
            AllRebarPlanes = AllRebarPlanes,
            IsTop = IsTop,
            Diameter = Diameter,
            Mid = Mid.EditZ(Mid.Z),
         };
      }
   }
}
