using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop
{
   public class RebarShopModel
   {
      public bool IsCut { get; set; } = true;
      public double Elevation { get; set; }
      public RebarBarType RebarBarType { get; set; }
      public RebarCurvePairModel CurvesPair { get; set; }

      public double Length
      {
         get
         {
            if (CurvesPair != null)
            {
               return CurvesPair.OuterCurves.Sum(x => x.Length);
            }

            return 0;
         }
      }

      public int Diameter { get; set; }
      public int Quantity { get; set; }
      public int OriginalQuantity { get; set; }
      public ElementId HostId { get; set; }
      public Element Host { get; set; }
      public string HostMarkName { get; set; }
      public Rebar OriginalRebar { get; set; }
      public string IdOriginalRebar { get; set; }
      public List<BPlane> BPlanes { get; set; }
      public BPlane Plane { get; set; }
      public Rebar NewRebar { get; set; }
      public List<XYZ> PositionPoints { get; set; } = new List<XYZ>();

      public RebarShopModel()
      {
      }

      public RebarShopModel(Rebar rebar)
      {
         OriginalRebar = rebar;
         IdOriginalRebar = rebar.UniqueId;
         HostId = rebar.GetHostId();
         Host = HostId.ToElement();
         RebarBarType = rebar.GetTypeId().ToElement() as RebarBarType;
         CurvesPair = new RebarCurvePairModel();
         if (RebarBarType != null)
         {
            Diameter = (int)RebarBarType.BarDiameter().FootToMm();
            CurvesPair.CenterCurves = rebar.GetRebarCurvesInPlane();
            var ps1 = CurvesPair.CenterCurves.GetPointsOfCurves();

            CurvesPair.OuterCurves = GetOuter(CurvesPair.CenterCurves);
            var ps2 = CurvesPair.OuterCurves.GetPointsOfCurves();
            OriginalQuantity = rebar.Quantity;
            Elevation = CurvesPair.CenterCurves.OrderByDescending(x => x.Length).First().Midpoint().Z;

            var normal = rebar.RebarNormal();

            Plane = BPlane.CreateByNormalAndOrigin(normal, CurvesPair.CenterCurves[0].SP());
         }

         PositionPoints = GetListPointPositions(OriginalRebar);
      }

      public RebarInStorageModel ToRebarInStorageModel()
      {
         return new RebarInStorageModel(Quantity, Diameter, Length);
      }

      public void Split(out RebarShopModel rebar1, out RebarShopModel rebar2)
      {
         rebar1 = Clone();
         rebar2 = Clone();
         var n1 = OriginalQuantity / 2;
         var n2 = OriginalQuantity - n1;
         rebar1.Quantity = n1;
         rebar2.Quantity = n2;

         rebar1.PositionPoints.Clear();
         rebar2.PositionPoints.Clear();

         for (int i = 0; i < PositionPoints.Count; i++)
         {
            var current = PositionPoints[i];
            if (i % 2 == 0)
            {
               rebar1.PositionPoints.Add(current);
            }
            else
            {
               rebar2.PositionPoints.Add(current);
            }
         }
      }

      private List<XYZ> GetListPointPositions(Rebar rebar)
      {
         var curves = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
         var list = new List<XYZ>();
         var sp = curves.First().SP();
         var num = rebar.Quantity;

         for (int i = 0; i < num; i++)
         {
            var tf = rebar.GetRebarPositionTransform(i);
            var p = tf.OfPoint(sp);
            list.Add(p);
         }

         return list;
      }

      public RebarShopModel Clone()
      {
         var pair = new RebarCurvePairModel
         {
            OuterCurves = CurvesPair.OuterCurves.Select(x => x.Clone()).ToList(),
            CenterCurves = CurvesPair.CenterCurves.Select(x => x.Clone()).ToList()
         };

         return new RebarShopModel()
         {
            RebarBarType = RebarBarType,
            Host = Host,
            CurvesPair = pair,
            Diameter = Diameter,
            Elevation = Elevation,
            Quantity = Quantity,
            OriginalQuantity = OriginalQuantity,
            HostId = HostId,
            HostMarkName = HostMarkName,
            OriginalRebar = OriginalRebar,
            BPlanes = BPlanes,
            Plane = Plane,
            IdOriginalRebar = IdOriginalRebar,
            PositionPoints = new List<XYZ>(PositionPoints)
         };
      }

      private List<Curve> GetOuter(List<Curve> curves)
      {
         if (curves.Count > 1)
         {
            var cl = new CurveLoop();
            curves.ForEach(x => cl.Append(x));
            var normal = cl.GetPlane().Normal.Normalize();
            var cl1 = CurveLoop.CreateViaOffset(cl, Diameter.MmToFoot() / 2, normal);
            if (cl1.GetExactLength() < cl.GetExactLength())
            {
               cl1 = CurveLoop.CreateViaOffset(cl, Diameter.MmToFoot() / 2, -normal);
            }

            return cl1.ToList();
         }

         return curves;
      }

      public double GetLapLength()
      {
         return Diameter.MmToFoot() * 40;
      }
   }
}