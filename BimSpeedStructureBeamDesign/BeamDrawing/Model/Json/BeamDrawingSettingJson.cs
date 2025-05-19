using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model.Json
{
    public class BeamDrawingSettingJson
    {
        public string Name { get; set; }
        public BeamDetailSettingJson BeamDetailSettingJson { get; set; }
        public BeamSectionSettingJson BeamSectionSettingJson { get; set; }
        public BeamSheetSettingJson BeamSheetSettingJson { get; set; }

        public BeamDrawingSettingJson()
        {
        }

        public BeamDrawingSettingJson(BeamDrawingSetting setting)
        {
            Name = setting.Name;
            BeamDetailSettingJson = new BeamDetailSettingJson(setting.BeamDetailSetting);
            BeamSectionSettingJson = new BeamSectionSettingJson(setting.BeamSectionSetting);
            BeamSheetSettingJson = new BeamSheetSettingJson(setting.BeamSheetSetting);
        }

        public bool IsValid()
        {
            if (BeamDetailSettingJson != null && BeamSectionSettingJson != null && BeamSheetSettingJson != null)
            {
                return true;
            }
            return false;
        }

        public BeamDrawingSetting GetBeamDrawingSetting(BeamDrawingSettingViewModel viewModel)
        {
            var setting = new BeamDrawingSetting
            {
                Name = Name,
                BeamSectionSetting = BeamSectionSettingJson.GetBeamSectionSetting(viewModel),
                BeamDetailSetting = BeamDetailSettingJson.GetBeamDetailSetting(viewModel),
                BeamSheetSetting = BeamSheetSettingJson.GetBeamSheetSetting(viewModel)
            };

            //get view templates

            return setting;
        }
    }
}