using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop;
using BimSpeedStructureBeamDesign.BeamRebar.Services;

namespace BimSpeedStructureBeamDesign.BeamRebar.Controller
{
   public class RebarShopController
   {
      /// <summary>
      /// Các cây thép cần shop , đa phần là các cấy thép lớn hơn 11.7m
      /// </summary>
      public List<RebarShopModel> RebarToCuts { get; set; }

      /// <summary>
      /// Là các cây thép trong dầm có chiều dài nhỏ hơn 11.7m
      /// </summary>

      public List<RebarShopModel> RebarCanBeUseToCombineWithsCanModify { get; set; }

      public List<RebarShopModel> RebarCanUseToCombineWithButCanNotModifys { get; set; }
      public List<CutZone> CutZones { get; set; }

      public RebarShopController()
      {
      }

      private void InitialForCurrentBeam()
      {
         CutZones = RebarShopService.GetCutZonesBySpan(BeamRebarRevitData.Instance.BeamModel);
         var list = new List<RebarShopModel>();
         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars)
         {
            //var rebarModel = new RebarShopModel(bar);
            //list.Add(rebarModel);
         }

         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars)
         {
            //var rebarModel = new RebarShopModel(bar);
            //list.Add(rebarModel);
         }

         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars)
         {
            //var rebarModel = new RebarShopModel(bar);
            //list.Add(rebarModel);
         }

         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars)
         {
            //var rebarModel = new RebarShopModel(bar);
            //list.Add(rebarModel);
         }

         foreach (var rebarShopModel in list)
         {
            if (rebarShopModel.Length > BeamRebarRevitData.Instance.BeamShopSetting.MaxLengthOfOneRebar)
            {
               RebarToCuts.Add(rebarShopModel);
            }
            else
            {
               RebarCanUseToCombineWithButCanNotModifys.Add(rebarShopModel);
            }
         }
      }

      public void CutRebarShop()
      {
      }
   }
}