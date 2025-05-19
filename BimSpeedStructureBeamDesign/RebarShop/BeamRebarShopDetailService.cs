using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.RebarShop.Model;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.RebarShop
{
   public class BeamRebarShopDetailService
   {
      private BeamRebarShopModel rebarShopModel;

      public BeamRebarShopDetailService()
      {
      }

      private void Run()
      {
         var beams = AC.Selection.PickObjects(ObjectType.Element, new BimSpeedUtils.BeamSelectionFilter(), "Beams...")
             .Select(x => x.ToElement()).Cast<FamilyInstance>().ToList();


         //rebarShopModel = new BeamRebarShopModel(beams, AC.ActiveView);
      }

      private void DetailTopRebar()
      {
         var zTop = rebarShopModel.BeamShopGeometryModels.Max(x => x.ZTop) + 100.MmToFoot();
         var zMin = rebarShopModel.BeamShopGeometryModels.Max(x => x.ZBot);
      }

      //double ShopRebarsInLayer(List<Rebar> rebars, double z)
      //{
      //    var models = rebars.Select(x => new RebarShopDetailModel(x)).Where(x => x.Direct.IsPerpendicular(XYZ.BasisZ)).ToList();
      //    var alreadyProcesses = new List<Guid>();

      //    foreach (var rebarShopDetailModel in models)
      //    {
      //        alreadyProcesses.Add(rebarShopDetailModel.Id);
      //        var inPlaneRebars =
      //    }

      //    return z;
      //}

      //bool IsLapping(RebarShopDetailModel rb1, RebarShopDetailModel rb2)
      //{
      //}
   }
}