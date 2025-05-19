using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public class RebarQuantityManager
   {
      public List<RebarQuantityByWidth> RebarQuantityByWidths { get; set; } = new List<RebarQuantityByWidth>();

      public RebarQuantityManager()
      {
         GetData();
      }

      private void GetData()
      {
         foreach (var g in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels.GroupBy(x => x.Width.Round2Number()))
         {
            var first = g.First();
            var model = new RebarQuantityByWidth(g.Key)
            {
               SpanModels = g.ToList(),
            };
            RebarQuantityByWidths.Add(model);
         }
      }
   }

   public class RebarQuantityByWidth
   {
      private int mainTop1;
      private int addTop1;
      private int totalTop1;
      private int addBot1;
      private int addBot2;
      private int mainBot1;

      public double Width { get; set; }
      public List<SpanModel> SpanModels { get; set; }

      public int TotalTop1
      {
         get => totalTop1;
         set => totalTop1 = value;
      }

      public int AddTop1
      {
         get => addTop1;
         set
         {
            addTop1 = value;
            totalTop1 = mainTop1 + addTop1;
         }
      }

      public int MainTop1
      {
         get => mainTop1;
         set
         {
            mainTop1 = value;
            totalTop1 = mainTop1 + addTop1;
         }
      }

      public int AddTop2 { get; set; }
      public int AddTop3 { get; set; } = 2;
      public int TotalBot1 { get; set; }

      public int AddBot1
      {
         get => addBot1;
         set
         {
            addBot1 = value;
            TotalBot1 = addBot1 + mainBot1;
         }
      }

      public int MainBot1
      {
         get => mainBot1;
         set
         {
            mainBot1 = value;
            TotalBot1 = addBot1 + mainBot1;
         }
      }

      public int AddBot2
      {
         get => addBot2;
         set
         {
            addBot2 = value;
         }
      }

      public int AddBot3 { get; set; } = 2;
      public List<int> MaxBars { get; set; } = new List<int>();

      public RebarQuantityByWidth(double w)
      {
         Width = w;
         var setting = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting;

         MainTop1 = setting.MainTop1;
         AddTop2 = setting.AddTop2;
         AddBot2 = setting.AddBot2;
         MainBot1 = setting.MainBot1;
         AddTop1 = setting.HasTop1 ? setting.AddTop1 : 0;
         AddBot1 = setting.HasBot1 ? setting.AddBot1 : 0;
         AddTop2 = setting.HasTop2 ? setting.AddTop2 : 0;
         AddBot2 = setting.HasBot2 ? setting.AddBot2 : 0;
         TotalTop1 = MainTop1 + AddTop1;
         TotalBot1 = MainBot1 + AddBot1;
         var m = (int)(Width / 65.MmToFoot() + 1);
         for (int i = 1; i < m + 2; i++)
         {
            MaxBars.Add(i);
         }
      }
   }
}