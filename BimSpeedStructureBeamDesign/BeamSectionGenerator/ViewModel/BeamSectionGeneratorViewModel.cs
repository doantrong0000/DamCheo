using BimSpeedStructureBeamDesign.Beam.BeamAutoSection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel
{
   public class BeamSectionGeneratorViewModel : ViewModelBase
   {
      public BeamDetailViewModel BeamDetailViewModel { get; set; }
      public BeamSectionViewModel BeamSectionViewModel { get; set; }

      public BeamSectionGeneratorViewModel()
      {
         var data = GetSetting();
         BeamSectionViewModel = new BeamSectionViewModel(data);
         BeamDetailViewModel = new BeamDetailViewModel(data) { BeamSectionViewModel = BeamSectionViewModel };
         BeamSectionViewModel.BeamDetailViewModel = BeamDetailViewModel;
      }

      public void Save()
      {
         var beamDetailJson = new BeamDetailJson()
         {

            IsAll = BeamDetailViewModel.IsAll,
            SectionOffsetFromSideFace = BeamDetailViewModel.SectionOffsetFromSideFace,
            IsX = BeamDetailViewModel.IsX,
            IsY = BeamDetailViewModel.IsY,
            IsInclined = BeamDetailViewModel.IsInclined,
            IsSelection = BeamDetailViewModel.IsSelection,
            CropModel = BeamDetailViewModel.CropModel,
            XViewFamilyType = BeamDetailViewModel.HorizontalBeamSectionTypeModel.ViewFamilyType.Name,
            YViewFamilyType = BeamDetailViewModel.VerticalBeamSectionTypeModel.ViewFamilyType.Name,
            InclinedViewFamilyType = BeamDetailViewModel.InclinedBeamSectionTypeModel.ViewFamilyType.Name,
            Length3Sections = BeamDetailViewModel.Length3Sections,
            Position1 = BeamDetailViewModel.Position1,
            Position2 = BeamDetailViewModel.Position2,
            Position3 = BeamDetailViewModel.Position3,
            RecordModels = BeamDetailViewModel.NamingViewModel.RecordModels

         };

         if (BeamDetailViewModel.HorizontalBeamSectionTypeModel.ViewTemplate.Element != null)
         {
            beamDetailJson.XViewTemplate =
                BeamDetailViewModel.HorizontalBeamSectionTypeModel.ViewTemplate.Element.Name;
         }

         if (BeamDetailViewModel.VerticalBeamSectionTypeModel.ViewTemplate.Element != null)
         {
            beamDetailJson.YViewTemplate =
                BeamDetailViewModel.VerticalBeamSectionTypeModel.ViewTemplate.Element.Name;
         }

         if (BeamDetailViewModel.InclinedBeamSectionTypeModel.ViewTemplate.Element != null)
         {
            beamDetailJson.InclinedViewTemplate =
                BeamDetailViewModel.InclinedBeamSectionTypeModel.ViewTemplate.Element.Name;
         }

         var beamSectionJson = new BeamSectionJson()
         {
            CropModel = BeamSectionViewModel.CropModel,
            ViewFamilyType = BeamSectionViewModel.SectionTypeModel.ViewFamilyType.Name,
            RecordModels = BeamSectionViewModel.NamingViewModel.RecordModels,
            Operation = BeamSectionViewModel.Operation
         };

         if (BeamSectionViewModel.SectionTypeModel.ViewTemplate.Element != null)
         {
            beamSectionJson.ViewTemplate =
                BeamSectionViewModel.SectionTypeModel.ViewTemplate.Element.Name;
         }

         var data = new BeamAutoSectionJson()
         {
            BeamSectionJson = beamSectionJson,
            BeamDetailJson = beamDetailJson,
         };

         JsonUtils.SaveSettingToFile(data, AC.BimSpeedSettingPath + "\\BeamSectionAutoJson.json");

      }

      public BeamAutoSectionJson GetSetting()
      {

         var data = JsonUtils.GetSettingFromFile<BeamAutoSectionJson>(AC.BimSpeedSettingPath + "\\BeamSectionAutoJson.json");
         return data;

      }
   }
}