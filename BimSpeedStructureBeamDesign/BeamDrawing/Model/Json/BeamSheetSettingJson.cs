using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model.Json
{
   public class BeamSheetSettingJson
   {
      public string SheetName { get; set; }
      public string SheetNumber { get; set; }
      public string TitleBlock { get; set; }

      public BeamSheetSettingJson()
      {
      }

      public BeamSheetSettingJson(BeamSheetSetting setting)
      {
         SheetNumber = setting.SheetNumber;
         SheetName = setting.SheetName;
         if (setting.TitleBlock == null)
         {
            TitleBlock = "";
         }
      }

      public BeamSheetSetting GetBeamSheetSetting(BeamDrawingSettingViewModel viewModel)
      {
         var setting = new BeamSheetSetting
         {
            SheetNumber = SheetNumber,
            SheetName = SheetName,
            TitleBlock = viewModel.TitleBlocks.FirstOrDefault(x => x.Name == TitleBlock)
         };
         return setting;
      }
   }
}