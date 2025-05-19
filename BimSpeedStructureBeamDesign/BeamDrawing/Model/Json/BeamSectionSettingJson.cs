using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model.Json
{
    public class BeamSectionSettingJson
    {
        public string TagThepNhom { get; set; }
        public string TagThepNhomTrai { get; set; }
        public string TagThepDaiTrai { get; set; }
        public string TagThepDaiPhai { get; set; }
        public string IndependentTagRebarStandardRight { get; set; }
        public string IndependentTagRebarStandardLeft { get; set; }
        public string DimensionType { get; set; }
        public double KhoangCachGiua2Dim { get; set; }
        public double KhoangCachSideDimDenDam { get; set; }
        public double KhoangCachTagDenDam { get; set; }
        public double KhoangCachTagElevationDenDam { get; set; }
        public double KhoangCachBotDimDenDam { get; set; }
        public string BreakLineSymbol { get; set; }
        public double KhoangCachBreakLineDenDam { get; set; }
        public string SpotDimensionType { get; set; }
        public string ViewTemplate { get; set; }
        public string ViewportType { get; set; }
        public string ViewFamilyType { get; set; }
        public string DimensionTypeGap { get; set; }
        public string DimensionTypeFixed { get; set; }
        public string DetailSectionName { get; set; }
        public int Scale { get; set; }
        public bool IsLongSection { get; set; }
        public bool IsCrossSection { get; set; }
        public bool IsDrawBreakLine { get; set; } = true;
        public bool IsDrawTagRebar { get; set; } = true;
        public bool IsDrawDim { get; set; } = true;
        public bool IsDrawTagElevation{ get; set; } = true;
        public bool IsDrawStick { get; set; } = true;
        public BeamSectionSettingJson()
        {
        }

        public BeamSectionSettingJson(BeamSectionSetting setting)
        {
            TagThepNhom = GetName(setting.TagThepNhomPhai);
            TagThepNhomTrai = GetName(setting.TagThepNhomTrai);
            TagThepDaiTrai = GetName(setting.TagThepDaiTrai);
            TagThepDaiPhai = GetName(setting.TagThepDaiPhai);
            IndependentTagRebarStandardRight = GetName(setting.IndependentTagRebarStandardRight);
            IndependentTagRebarStandardLeft = GetName(setting.IndependentTagRebarStandardLeft);
            DimensionType = GetName(setting.DimensionType);
            DimensionTypeFixed = GetName(setting.DimensionTypeFixed);
            DimensionTypeGap = GetName(setting.DimensionTypeGap);
            KhoangCachGiua2Dim = setting.KhoangCachGiua2Dim;
            KhoangCachSideDimDenDam = setting.KhoangCachSideDimDenDam;
            KhoangCachTagDenDam = setting.KhoangCachTagDenDam;
            KhoangCachTagElevationDenDam = setting.KhoangCachTagElevationDenDam;
            KhoangCachBotDimDenDam = setting.KhoangCachBotDimDenDam;
            BreakLineSymbol = GetName(setting.BreakLineSymbol);
            KhoangCachBreakLineDenDam = setting.KhoangCachBreakLineDenDam;
            SpotDimensionType = GetName(setting.SpotDimensionType);
            ViewTemplate = GetName(setting.ViewTemplate);
            ViewportType = GetName(setting.ViewportType);
            ViewFamilyType = GetName(setting.ViewFamilyType);
            DetailSectionName = setting.DetailSectionName;
            Scale = setting.Scale;
            IsLongSection = setting.IsLongSection;
            IsCrossSection = setting.IsCrossSection;
            IsDrawBreakLine = setting.IsDrawBreakLine;
            IsDrawTagRebar = setting.IsDrawTagRebar;
            IsDrawDim = setting.IsDrawDim;
            IsDrawTagElevation = setting.IsDrawTagElevation;
            IsDrawStick = setting.IsDrawStick;
        }

        public BeamSectionSetting GetBeamSectionSetting(BeamDrawingSettingViewModel viewModel)
        {
            var setting = new BeamSectionSetting
            {
                TagThepNhomPhai = viewModel.MultiReferenceAnnotationTypes.FirstOrDefault(x => x.Name == TagThepNhom),
                TagThepNhomTrai = viewModel.MultiReferenceAnnotationTypes.FirstOrDefault(x => x.Name == TagThepNhomTrai),
                TagThepDaiPhai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagThepDaiPhai),
                TagThepDaiTrai = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == TagThepDaiTrai),
                IndependentTagRebarStandardLeft = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == IndependentTagRebarStandardLeft),
                IndependentTagRebarStandardRight = viewModel.IndependentTagSymbols.FirstOrDefault(x => x.Name == IndependentTagRebarStandardRight),
                DimensionType = viewModel.DimensionTypes.FirstOrDefault(x => x.Name == DimensionType),
                DimensionTypeFixed = viewModel.DimensionTypes.FirstOrDefault(x => x.Name == DimensionTypeFixed),
                DimensionTypeGap = viewModel.DimensionTypes.FirstOrDefault(x => x.Name == DimensionTypeGap),
                KhoangCachGiua2Dim = KhoangCachGiua2Dim,
                KhoangCachSideDimDenDam = KhoangCachSideDimDenDam,
                KhoangCachTagDenDam = KhoangCachTagDenDam,
                KhoangCachTagElevationDenDam = KhoangCachTagElevationDenDam,
                KhoangCachBotDimDenDam = KhoangCachBotDimDenDam,
                BreakLineSymbol = viewModel.BreakLineSymbols.FirstOrDefault(x => x.Name == BreakLineSymbol),
                KhoangCachBreakLineDenDam = KhoangCachBreakLineDenDam,
                SpotDimensionType = viewModel.SpotDimensionTypes.FirstOrDefault(x => x.Name.StartsWith(SpotDimensionType)),
                ViewTemplate = viewModel.ViewTemplates.FirstOrDefault(x => x.Name.StartsWith(ViewTemplate)),
                ViewportType = viewModel.ViewportTypes.FirstOrDefault(x => x.Name.StartsWith(ViewportType)),
                ViewFamilyType = viewModel.ViewFamilyTypes.FirstOrDefault(x => x.Name.StartsWith(ViewFamilyType)),
                DetailSectionName = DetailSectionName,
                Scale = Scale,
                IsLongSection = IsLongSection,
                IsCrossSection = IsCrossSection,
                IsDrawBreakLine = IsDrawBreakLine,
                IsDrawTagRebar = IsDrawTagRebar,
                IsDrawDim = IsDrawDim,
                IsDrawTagElevation = IsDrawTagElevation,
                IsDrawStick = IsDrawStick
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