using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class BeamSectionSetting
    {
        public MultiReferenceAnnotationType TagThepNhomPhai { get; set; }
        public MultiReferenceAnnotationType TagThepNhomTrai { get; set; }
        public FamilySymbol TagThepDaiPhai { get; set; }
        public bool IsLongSection { get; set; } = true;
        public bool IsCrossSection { get; set; } = true;
        public FamilySymbol TagThepDaiTrai { get; set; }
        public FamilySymbol IndependentTagRebarStandardRight { get; set; }
        public FamilySymbol IndependentTagRebarStandardLeft { get; set; }
        public DimensionType DimensionType { get; set; }
        public double KhoangCachGiua2Dim { get; set; } = 10.MmToFoot();
        public double KhoangCachSideDimDenDam { get; set; } = 400.MmToFoot();
        public double KhoangCachBotDimDenDam { get; set; } = 260.MmToFoot();
        public double KhoangCachTagDenDam { get; set; } = 120.MmToFoot();
        public double KhoangCachBreakLine { get; set; } = 50.MmToFoot();
        public double KhoangCachTagElevationDenDam { get; set; }
        public FamilySymbol BreakLineSymbol { get; set; }
        public double KhoangCachBreakLineDenDam { get; set; } = 60.MmToFoot();
        public bool IsCreateSpot { get; set; } = true;
        public SpotDimensionType SpotDimensionType { get; set; }
        public Autodesk.Revit.DB.View ViewTemplate { get; set; }
        public ElementType ViewportType { get; set; }
        public ViewFamilyType ViewFamilyType { get; set; }
        public double ViTri1 { get; set; } = 0.05;
        public double ViTri2 { get; set; } = 0.5;
        public double ViTri3 { get; set; } = 0.95;
        public DimensionType DimensionTypeGap { get; set; }
        public DimensionType DimensionTypeFixed { get; set; }
        public string DetailSectionName { get; set; } 
        public bool IsDrawBreakLine { get; set; } = true;
        public bool IsDrawTagRebar { get; set; } = true;
        public bool IsDrawDim { get; set; } = true;
        public bool IsDrawTagElevation { get; set; } = true;
        public bool IsDrawStick { get; set; } = true;
        public int Scale { get; set; } = 25;

        public BeamSectionSetting()
        {
        }

        public BeamSectionSetting(BeamSectionSetting setting)
        {
            TagThepNhomPhai = setting.TagThepNhomPhai;
            TagThepNhomTrai = setting.TagThepNhomTrai;
            TagThepDaiPhai = setting.TagThepDaiPhai;
            TagThepDaiTrai = setting.TagThepDaiTrai;
            DimensionType = setting.DimensionType;
            DimensionTypeFixed = setting.DimensionTypeFixed;
            DimensionTypeGap = setting.DimensionTypeGap;
            IndependentTagRebarStandardLeft = setting.IndependentTagRebarStandardLeft;
            IndependentTagRebarStandardRight = setting.IndependentTagRebarStandardRight;
            DetailSectionName = setting.DetailSectionName;
            IsLongSection = setting.IsLongSection;
            IsCrossSection = setting.IsCrossSection;

            KhoangCachGiua2Dim = setting.KhoangCachGiua2Dim;
            KhoangCachSideDimDenDam = setting.KhoangCachSideDimDenDam;
            KhoangCachTagDenDam = setting.KhoangCachTagDenDam;
            KhoangCachTagElevationDenDam = setting.KhoangCachTagElevationDenDam;
            KhoangCachBotDimDenDam = setting.KhoangCachBotDimDenDam;
            BreakLineSymbol = setting.BreakLineSymbol;
            KhoangCachBreakLineDenDam = setting.KhoangCachBreakLineDenDam;
            SpotDimensionType = setting.SpotDimensionType;
            ViewTemplate = setting.ViewTemplate;
            ViewportType = setting.ViewportType;
            ViewFamilyType = setting.ViewFamilyType;
            Scale = setting.Scale;

            IsDrawBreakLine = setting.IsDrawBreakLine;
            IsDrawTagRebar = setting.IsDrawTagRebar;
            IsDrawTagElevation = setting.IsDrawTagElevation;
            IsDrawDim = setting.IsDrawDim;
            IsDrawStick = setting.IsDrawStick;
        }

        public BeamSectionSetting GetBeamSectionSettingByScale(int scale)
        {
            Scale = scale;
            if (scale == 25)
            {
                KhoangCachBreakLineDenDam = 60.MmToFoot();
                KhoangCachGiua2Dim = 7.MmToFoot();
                KhoangCachSideDimDenDam = 500.MmToFoot();
                KhoangCachBotDimDenDam = 260.MmToFoot();
            }
            else if (scale == 30)
            {
                KhoangCachBreakLineDenDam = 60.MmToFoot();
                KhoangCachGiua2Dim = 7.MmToFoot();
                KhoangCachSideDimDenDam = 500.MmToFoot();
                KhoangCachBotDimDenDam = 260.MmToFoot();
            }
            return this;
        }
    }
}