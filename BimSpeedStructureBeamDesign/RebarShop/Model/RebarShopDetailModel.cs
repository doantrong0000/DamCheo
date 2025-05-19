using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.RebarShop.Model
{
   public class RebarShopDetailModel
   {
      public Guid Id { get; set; }
      public List<Curve> Curves { get; set; }
      public Curve MainCurve { get; set; }
      public double MainZ { get; set; }
      public XYZ MainStart { get; set; }
      public XYZ MainEnd { get; set; }
      public BPlane BPlane { get; set; }
      public XYZ Direct { get; set; }

      public RebarShopDetailModel(Rebar rebar)
      {
         Id = Guid.NewGuid();
         Curves = rebar.GetRebarCurvesInPlane();
         MainCurve = Curves.OrderByDescending(x => x.Length).First();
         MainZ = MainCurve.SP().Z;
         MainStart = MainCurve.SP();
         MainEnd = MainCurve.EP();
         if (Direct.IsParallel(XYZ.BasisZ) == false)
         {
            BPlane = BPlane.CreateByThreePoints(MainStart, MainEnd, MainStart.Add(XYZ.BasisZ));
         }
      }
   }
}