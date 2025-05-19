using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class BeamDrawingSetting : ViewModelBase
    {
        private string name;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        public bool IsCreate3DView { get; set; } = false;

        public BeamDetailSetting BeamDetailSetting { get; set; } = new BeamDetailSetting();
        public BeamSheetSetting BeamSheetSetting { get; set; } = new BeamSheetSetting();
        public BeamSectionSetting BeamSectionSetting { get; set; } = new BeamSectionSetting();

        public BeamDrawingSetting()
        {

        }

        public string DetailViewName { get; set; }
        public BeamDrawingSetting(BeamDrawingSetting setting, string name)
        {
            this.Name = name;
            BeamDetailSetting = new BeamDetailSetting(setting.BeamDetailSetting);
            BeamSheetSetting = new BeamSheetSetting(setting.BeamSheetSetting);
            BeamSectionSetting = new BeamSectionSetting(setting.BeamSectionSetting);
        }

        public bool CheckSettingValid()
        {
            #region Details

            if (BeamDetailSetting.TagRebarStandardTrai == null
                || BeamDetailSetting.TagRebarStandardPhai == null
                || BeamDetailSetting.TagThepDaiTrai == null
                || BeamDetailSetting.DimensionTypeFixed == null || BeamDetailSetting.BreakLineSymbol == null ||
                BeamDetailSetting.SpotDimensionType == null ||
                BeamDetailSetting.ViewFamilyType == null)
            {
                "BEAMDRAWINGSETTING_MESSAGE".NotificationError(this);
                return false;
            }

            #endregion Details

            return true;
        }
    }
}