using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class BeamSheetSetting : ViewModelBase
   {
      private string sheetNumber;
      private string sheetName;
      public ViewSheet ViewSheet { get; set; }
      public bool IsEnable { get; set; }

      public string SheetNumber
      {
         get => sheetNumber;
         set
         {
            var vs = SharedData.Instance.ViewSheets.FirstOrDefault(x => x.SheetNumber == value);
            if (vs != null)
            {
               SheetName = vs.Name;
               IsEnable = false;
            }
            else
            {
               IsEnable = true;
            }
            sheetNumber = value;
            OnPropertyChanged(nameof(SheetNumber));
            OnPropertyChanged(nameof(SheetName));
            OnPropertyChanged(nameof(IsEnable));
            OnPropertyChanged(nameof(TitleBlock));
         }
      }

      public string SheetName
      {
         get => sheetName;
         set => sheetName = value;
      }

      public FamilySymbol TitleBlock { get; set; }

      public BeamSheetSetting()
      {

      }

      public BeamSheetSetting(BeamSheetSetting setting)
      {

         ViewSheet = setting.ViewSheet;
         IsEnable = setting.IsEnable;
         SheetName = setting.SheetName;
         sheetNumber = setting.SheetNumber;
         TitleBlock = setting.TitleBlock;

      }

   }

}