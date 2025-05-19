using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model.Json
{
    public class BeamDetailSettingJson
    {
        public string TagRebarStandardTrai { get; set; }
        public string TagRebarStandardPhai { get; set; }
        public string TagThepDaiTrai { get; set; }
        public string TagThepDaiPhai { get; set; }
        public string DimensionType { get; set; }
        public string DimensionTypeGap { get; set; }
        public string DimensionTypeFixed { get; set; }
        public double KhoangCachGiua2Dim { get; set; }
        public double KhoangCachDimDenDam { get; set; }
        public double KhoangCachDimDenDamLeft { get; set; }
        public double KhoangCachTagDenDam { get; set; }
        public double KhoangCachTagElevationDenDam { get; set; }
        public double KhoangCach2Tag { get; set; }
        public string BreakLineSymbol { get; set; }
        public double KhoangCachBreakLineDenDam { get; set; }
        public string SpotDimensionType { get; set; }
        public string ViewTemplate { get; set; }
        public string ViewportType { get; set; }
        public string ViewFamilyType { get; set; }
        public int Scale { get; set; }
        public bool IsDimGoi { get; set; }
        public bool IsDrawBreakLine { get; set; } = true;
        public bool IsDrawTagRebar { get; set; } = true;
        public bool IsDrawDim { get; set; } = true;
        public bool IsDrawTagElevation { get; set; } = true;
        public bool IsDrawStick { get; set; } = true;

        public BeamDetailSettingJson()
        {
        }

        public BeamDetailSettingJson(BeamDetailSetting setting)
        {
            TagRebarStandardTrai = GetName(setting.TagRebarStandardTrai);
            TagRebarStandardPhai = GetName(setting.TagRebarStandardPhai);
            TagThepDaiTrai = GetName(setting.TagThepDaiTrai);
            TagThepDaiPhai = GetName(setting.TagThepDaiPhai);
            DimensionType = GetName(setting.DimensionTypeFixed);
            DimensionTypeFixed = GetName(setting.DimensionTypeFixed);
            DimensionTypeGap = GetName(setting.DimensionTypeGap);
            KhoangCachGiua2Dim = setting.KhoangCachGiua2Dim;
            KhoangCachDimDenDam = setting.KhoangCachDimDenDam;
            KhoangCachDimDenDamLeft = setting.KhoangCachDimDenDamLeft;
            KhoangCachTagDenDam = setting.KhoangCachTagDenDam;
            KhoangCachTagElevationDenDam = setting.KhoangCachTagElevationDenDam;
            BreakLineSymbol = GetName(setting.BreakLineSymbol);
            KhoangCachBreakLineDenDam = setting.KhoangCachBreakLineDenDam;
            SpotDimensionType = GetName(setting.SpotDimensionType);
            ViewTemplate = GetName(setting.ViewTemplate);
            ViewportType = GetName(setting.ViewportType);
            ViewFamilyType = GetName(setting.ViewFamilyType);
            Scale = setting.Scale;
            KhoangCach2Tag = setting.KhoangCach2Tags;
            IsDrawBreakLine = setting.IsDrawBreakLine;
            IsDrawTagElevation = setting.IsDrawTagElevation;
            IsDrawTagRebar = setting.IsDrawTagRebar;
            IsDrawDim = setting.IsDrawDim;
            IsDrawStick = setting.IsDrawStick;
        }

        public BeamDetailSetting GetBeamDetailSetting(BeamDrawingSettingViewModel viewModel)
        {
            var setting = new BeamDetailSetting
            {
                TagRebarStandardPhai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagRebarStandardPhai),
                TagRebarStandardTrai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagRebarStandardTrai),
                TagThepDaiTrai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagThepDaiTrai),
                TagThepDaiPhai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagThepDaiPhai),
                KhoangCachTagElevationDenDam = KhoangCachTagElevationDenDam,
                DimensionTypeFixed = viewModel.DimensionTypes.FirstOrDefault(x => x.Name == DimensionType),
                DimensionTypeGap = viewModel.DimensionTypes.FirstOrDefault(x => x.Name == DimensionTypeGap),
                KhoangCachGiua2Dim = KhoangCachGiua2Dim,
                KhoangCachDimDenDam = KhoangCachDimDenDam,
                KhoangCachDimDenDamLeft = KhoangCachDimDenDamLeft,
                KhoangCachTagDenDam = KhoangCachTagDenDam,               
                BreakLineSymbol = viewModel.BreakLineSymbols.FirstOrDefault(x => x.Name == BreakLineSymbol),
                KhoangCachBreakLineDenDam = KhoangCachBreakLineDenDam,
                SpotDimensionType = viewModel.SpotDimensionTypes.FirstOrDefault(x => x.Name.StartsWith(SpotDimensionType)),
                ViewTemplate = viewModel.ViewTemplates.FirstOrDefault(x => x.Name.StartsWith(ViewTemplate)),
                ViewportType = viewModel.ViewportTypes.FirstOrDefault(x => x.Name.StartsWith(ViewportType)),
                ViewFamilyType = viewModel.ViewFamilyTypes.FirstOrDefault(x => x.Name.StartsWith(ViewFamilyType)),
                Scale = Scale,
                KhoangCach2Tags = KhoangCach2Tag,
                IsDrawBreakLine = IsDrawBreakLine,
                IsDrawTagRebar = IsDrawTagRebar,
                IsDrawDim = IsDrawDim,
                IsDrawTagElevation = IsDrawTagElevation,
                IsDrawStick = IsDrawStick,
                //IsDimDamLamGoi = viewModel.BeamDrawingSetting.BeamDetailSetting.IsDimDamLamGoi

            };
            return setting;
        }

        private string GetName(Element ele)
        {
            if (ele != null)
            {
                return ele.Name;
            }
            return string.Empty;
        }
    }
}