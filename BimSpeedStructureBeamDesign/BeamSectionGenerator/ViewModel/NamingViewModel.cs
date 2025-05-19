using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using BimSpeedUtils.JsonData;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel
{
   public class NamingViewModel : ViewModelBase
   {
      private string preview;
      private string text;

      public string Text
      {
         get => text;
         set
         {
            text = value;
            OnPropertyChanged();
         }
      }

      public string Preview
      {
         get => preview;
         set
         {
            preview = value;
            OnPropertyChanged();
         }
      }

      public List<string> Parameters { get; set; }
      public string Parameter { get; set; }
      public List<RecordModel> RecordModels { get; set; } = new List<RecordModel>();
      public RelayCommand AddTextCommand { get; set; }
      public RelayCommand AddParameterCommand { get; set; }
      public RelayCommand UndoCommand { get; set; }

      public NamingViewModel(List<string> parameters)
      {
         Parameters = parameters;
         AddTextCommand = new RelayCommand(x => AddText());
         AddParameterCommand = new RelayCommand(x => AddParam());
         UndoCommand = new RelayCommand(x => Undo());
         RecordModels.Add(new RecordModel(BeamRebarDefine.GetChiTietDam(), false));
         RecordModels.Add(new RecordModel("Mark", true));

         GetPreview();
      }

      private void Undo()
      {
         if (RecordModels.Count > 0)
         {
            RecordModels.RemoveAt(RecordModels.Count - 1);
         }
         GetPreview();
      }

      private void AddText()
      {
         if (string.IsNullOrEmpty(Text))
         {
         }
         else
         {
            RecordModels.Add(new RecordModel(Text, false));
            Text = "";
            GetPreview();
         }
      }

      private void AddParam()
      {
         if (string.IsNullOrEmpty(Parameter))
         {
         }
         else
         {
            RecordModels.Add(new RecordModel(Parameter, true));
            GetPreview();
         }
      }

      public void GetPreview()
      {
         preview = "";
         foreach (var recordModel in RecordModels)
         {
            preview += recordModel.PreviewText;
         }
         OnPropertyChanged(nameof(Preview));
      }
   }
}