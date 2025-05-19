using Autodesk.Revit.DB;
using BimSpeedUtils;
using BimSpeedUtils.Rebars.RebarShape2D;

namespace BimSpeedStructureBeamDesign.RebarShape2D.ViewModel
{
   public class RebarShapeSettingViewModel : ViewModelBase
   {
      private bool _isCreateTag = true;
      public List<FamilySymbol> Tags { get; set; }
      public FamilySymbol StirrupTag { get; set; }
      public FamilySymbol StandardTag { get; set; }

      public List<double> TextSizes { get; set; } = new List<double>()
        {
            1.8,2.0,2.2,2.5
        };

      public double TextSize { get; set; } = 2.0;

      public bool IsCreateTag
      {
         get => _isCreateTag;
         set
         {
            _isCreateTag = value;
            OnPropertyChanged();
         }
      }

      public RelayCommand OkCommand { get; set; }
      public RelayCommand CloseCommand { get; set; }

      public RebarShapeSettingViewModel()
      {
         Tags = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol))
             .OfCategory(BuiltInCategory.OST_RebarTags).Cast<FamilySymbol>().OrderBy(x => x.Name).ToList();
         StandardTag = Tags.FirstOrDefault();
         StirrupTag = Tags.FirstOrDefault();
         OkCommand = new RelayCommand(Ok);
         CloseCommand = new RelayCommand(Close);
         //Load Data

         var data = JsonUtils.GetSettingFromFile<RebarShape2DSettingJson>(
             AC.BimSpeedSettingPath + "\\RebarShape2DSetting.json");
         if (data != null)
         {
            if (TextSizes.Contains(data.TextSize))
            {
               TextSize = data.TextSize;
            }

            StirrupTag = Tags.FirstOrDefault(x => x.Name == data.StirrupTag);
            if (StirrupTag == null)
            {
               StirrupTag = Tags.FirstOrDefault();
            }
            StandardTag = Tags.FirstOrDefault(x => x.Name == data.StandardTag);
            if (StandardTag == null)
            {
               StandardTag = Tags.FirstOrDefault();
            }

            IsCreateTag = data.IsCreate;
         }
      }

      private void Ok(object obj)
      {
         if (obj is System.Windows.Window window)
         {
            window.Close();
            //Save Data
            var data = new RebarShape2DSettingJson()
            {
               IsCreate = IsCreateTag,
               TextSize = TextSize,
               StirrupTag = StirrupTag.Name,
               StandardTag = StandardTag.Name
            };
            JsonUtils.SaveSettingToFile(data, AC.BimSpeedSettingPath + "\\RebarShape2DSetting.json");
         }
      }

      private void Close(object obj)
      {
         if (obj is System.Windows.Window window)
         {
            window.Close();
         }
      }
   }
}