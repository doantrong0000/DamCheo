using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class BeamDetailSetting:ViewModelBase
   {
      public FamilySymbol TagRebarStandardTrai { get; set; }
      public FamilySymbol TagRebarStandardPhai { get; set; }
      public FamilySymbol TagThepDaiTrai { get; set; }
      public FamilySymbol TagThepDaiPhai { get; set; }
      public FamilySymbol DauMocThep { get; set; }
      public string DetailViewName { get; set; } = BeamRebarDefine.GetChiTietDam();

      /// <summary>
      /// Khoảng cách giữa 2 đường dim , nhân thêm scale để ra khoảng cách thật
      /// </summary>
      public double KhoangCachGiua2Dim { get; set; } = 10.MmToFoot();

      /// <summary>
      /// Khoảng cách từ đường dim gần nhất tới dầm, nếu có 3 lớp thép thì cần cộng thêm
      /// </summary>
      public double KhoangCachDimDenDam { get; set; } = 1000.MmToFoot();
      public double KhoangCachDimDenDamLeft { get; set; } = 500.MmToFoot();
      public double KhoangCachTagDenDam { get; set; } = 100.MmToFoot();
      public double KhoangCach2Tags { get; set; } = 4.MmToFoot();
      public double KhoangCachBreakLineDenDam { get; set; } = 200.MmToFoot();
      public double KhoangCachTagElevationDenDam { get; set; } =300.MmToFoot();
        public FamilySymbol BreakLineSymbol { get; set; }
      public SpotDimensionType SpotDimensionType { get; set; }
      public Autodesk.Revit.DB.View ViewTemplate { get; set; }
      public bool IsCreateSpot { get; set; } = true;
      public ElementType ViewportType { get; set; }
      public ViewFamilyType ViewFamilyType { get; set; }
      public DimensionType DimensionTypeGap { get; set; }
      public DimensionType DimensionTypeFixed { get; set; }
      public ElementId ViewPortTypeId { get; set; }
      public int Scale { get; set; }
      public bool IsDrawBreakLine { get; set; } = true;
      public bool IsDrawTagRebar { get; set; } = true;
      public bool IsDrawDim { get; set; } = true;
      public bool IsDrawTagElevation { get; set; } = true;
      public bool IsDrawStick { get; set; } = true;
  

        public BeamDetailSetting()
      {
      }

      public BeamDetailSetting(BeamDetailSetting setting)
      {
         TagThepDaiTrai = setting.TagThepDaiTrai;
         TagRebarStandardPhai = setting.TagRebarStandardPhai;
         TagRebarStandardTrai = setting.TagRebarStandardTrai;
         KhoangCachGiua2Dim = setting.KhoangCachGiua2Dim;
         KhoangCachDimDenDam = setting.KhoangCachDimDenDam;
         KhoangCachDimDenDamLeft = setting.KhoangCachDimDenDamLeft;
         KhoangCachTagDenDam = setting.KhoangCachTagDenDam;
         KhoangCachTagElevationDenDam = setting.KhoangCachTagElevationDenDam;
         BreakLineSymbol = setting.BreakLineSymbol;
         KhoangCachBreakLineDenDam = setting.KhoangCachBreakLineDenDam;
         SpotDimensionType = setting.SpotDimensionType;
         ViewTemplate = setting.ViewTemplate;
         ViewportType = setting.ViewportType;
         ViewFamilyType = setting.ViewFamilyType;
         DimensionTypeGap = setting.DimensionTypeGap;
         Scale = setting.Scale;
         DimensionTypeFixed = setting.DimensionTypeFixed;
         DauMocThep = setting.DauMocThep;
            IsDrawStick = setting.IsDrawStick;
        }

      public BeamDetailSetting GetBeamDetailSettingByScale(int scale)
      {
         Scale = scale;
         if (scale == 25)
         {
            KhoangCachGiua2Dim = 8.MmToFoot();
            KhoangCachDimDenDam = 450.MmToFoot();
            KhoangCachDimDenDamLeft = 200.MmToFoot();
            KhoangCachTagDenDam = 120.MmToFoot();
            KhoangCachBreakLineDenDam = 90.MmToFoot();
         }
         else if (scale == 30)
         {
            KhoangCachGiua2Dim = 8.MmToFoot();
            KhoangCachDimDenDam = 540.MmToFoot();
            KhoangCachDimDenDamLeft = 240.MmToFoot();
            KhoangCachTagDenDam = 150.MmToFoot();
            KhoangCachBreakLineDenDam = 110.MmToFoot();
         }
         else if (scale == 35 || scale == 40)
         {
            KhoangCachGiua2Dim = 8.MmToFoot();
            KhoangCachDimDenDam = 680.MmToFoot();
            KhoangCachDimDenDamLeft = 240.MmToFoot();
            KhoangCachTagDenDam = 200.MmToFoot();
            KhoangCachBreakLineDenDam = 150.MmToFoot();
         }
         else if (scale > 40 && scale < 70)
         {
            KhoangCachGiua2Dim = 8.MmToFoot();
            KhoangCachDimDenDam = 800.MmToFoot();
            KhoangCachDimDenDamLeft = 300.MmToFoot();
            KhoangCachTagDenDam = 250.MmToFoot();
            KhoangCachBreakLineDenDam = 230.MmToFoot();
         }
         else if (scale == 70)
         {
            KhoangCachGiua2Dim = 8.MmToFoot();
            KhoangCachDimDenDam = 1000.MmToFoot();
            KhoangCachDimDenDamLeft = 400.MmToFoot();
            KhoangCachTagDenDam = 300.MmToFoot();
            KhoangCachBreakLineDenDam = 260.MmToFoot();
         }
         return this;
      }
   }
}