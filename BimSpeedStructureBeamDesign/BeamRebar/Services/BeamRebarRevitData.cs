using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using Point = System.Windows.Point;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public class BeamRebarRevitData
   {
      public BeamShopSettingJson BeamShopSetting { get; set; } = new();
      public static SolidColorBrush DimensionColor;
      public static double YScale { get; set; }
      public static double XScale { get; set; }
      public static double Scale { get; set; }
      public static double BreakLineTopY { get; set; } = 120;
      public static double BreakLineBotY { get; set; } = 230;
      public QuickBeamRebarSettingViewModel RebarSettingViewModel { get; set; }

      public Point OriginUiMainView { get; set; }
      public Canvas Grid { set; get; }

      public BeamModel BeamModel { get; set; }
      public BeamUiModel BeamUiModel { get; set; }
      public List<RebarBarType> RebarBarTypes { get; set; } = new();

      public List<RebarBarType> BarDiameters { get; set; }

      private static BeamRebarRevitData _instance;
      public static BeamRebarRevitData Instance;
      public bool IsBack { get; set; } = false;
      public BeamRebarViewModel BeamRebarViewModel { get; set; }
      public BeamRebarView2 BeamRebarView2 { get; set; }


      public RebarShape StirrupShapeChuNhatKin { get; set; }
      public RebarShape StirrupUShape { get; set; }
      public RebarShape StirrupDaiMoc135x135 { get; set; }
      public RebarShape StirrupDaiMoc180x180 { get; set; }
      public RebarShape StirrupDaiMoc135x90 { get; set; }


      public double BeamRebarCover { get; set; }

      public List<string> KieuThepDais { get; set; } = new() { Define.DaiDon, Define.DaiKep, Define.DaiLongChuU, Define.DaiLongKin, Define.DaiMoc };
      public List<string> KieuPhanBoThepDais { get; set; } = new() { Define.BoTriDeu, Define.BoTriDeu2Dau };
      public List<string> TypeRebarByWidths { get; set; } = new();

      public List<NumberOfRebarByWidth> NumberOfRebarByWidths { get; set; } = new();
      public BeamRebarSettingJson BeamRebarSettingJson { get; set; }
      private string path = AC.BimSpeedSettingPath + "\\BeamRebarSetting.json";
      private ProgressBarWithStatusView progressBarView;

      public BeamRebarSettingViewModel BeamRebarSettingViewModel { get; set; }
      public QuickBeamRebarSettingViewModel QuickBeamRebarSettingViewModel { get; set; }

      public BeamRebarRevitData()
      {
         Instance = this;
         DimensionColor = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#0f94f7"));
         GetData();
      }

      private void GetData()
      {
         RebarBarTypes = RebarUtils.GetRebarBarTypes();
         BarDiameters = RebarBarTypes;
         RebarUtils.BarDiameters = BarDiameters;

         BeamRebarSettingViewModel = new BeamRebarSettingViewModel() { BeamDrawingSettingViewModel = new BeamDrawingSettingViewModel() };
         BeamRebarCover = BeamRebarSettingViewModel.Setting.RebarCover;

         var conKeThep = new ConKeThepModel
         {
            ConKeThepInfo = new DiameterAndSpacingModel() { Diameter = 25.GetRebarBarTypeByNumber(findBestMatchIfNull: true), Spacing = 2000.MmToFoot() },
            ConKeDaiMocInfo = new DiameterAndSpacingModel() { Diameter = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true), Spacing = 2000.MmToFoot() },
            IsConKeBangCotThep = true
         };


         BeamRebarSettingJson = JsonUtils.GetSettingFromFile<BeamRebarSettingJson>(path) ?? new BeamRebarSettingJson()
         {
            AnchorRebarSettingForBeam = new AnchorRebarSetting() { Bot = 30, Top = 30 },
            AnchorRebarSettingForFoundation = new AnchorRebarSetting() { Bot = 30, Top = 30 },
            AnchorRebarSettingForColumn = new AnchorRebarSetting() { Bot = 30, Top = 30 },
            AnchorRebarSettingForWall = new AnchorRebarSetting() { Bot = 30, Top = 30 },
            ConKeThep = conKeThep,
            DuongKinhThepCauTaoLopTren = 14.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
            ChieuDaiDoanNoiThepCauTaoLopTren = 400.MmToFoot(),
            ThepCauTaoGiuaDamModel = new ThepCauTaoGiuaDamModel() { BarDiameterForBarGoInColumn = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true), LengthGoInColumn = 100.MmToFoot(), BarDiameter = 14.GetRebarBarTypeByNumber(findBestMatchIfNull: true) },
            DuongKinhCotThepDaiBoXung = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
            KhoangCachCotThepDaiBoXung = 400.MmToFoot(),
            ThepNhip = 3,
            ThepGoiLop1 = 3,
            ThepGoiLop2 = 4,
            ThepGoiLop3 = 5,
            ThepNhipWithD = 0,
            ThepGoiLop1WithD = 0,
            ThepGoiLop2WithD = 0,
            ThepGoiLop3WithD = 0,
            KhoangCachDaiGiaCuong = 50.MmToFoot(),
            KhoangGiatCapDuocNhanThep = 50.MmToFoot()
         };

         if (BeamRebarSettingJson.ConKeThep.ConKeThepInfo.Diameter == null)
         {
            BeamRebarSettingJson.ConKeThep.ConKeThepInfo.Diameter =
               BeamRebarSettingJson.ConKeThep.ConKeThepInfo.DiameterInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }

         if (BeamRebarSettingJson.ConKeThep.ConKeDaiMocInfo.Diameter == null)
         {
            BeamRebarSettingJson.ConKeThep.ConKeDaiMocInfo.Diameter =
               BeamRebarSettingJson.ConKeThep.ConKeDaiMocInfo.DiameterInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }


         var shapes = new FilteredElementCollector(AC.Document).OfClass(typeof(RebarShape)).Cast<RebarShape>().ToList();


         StirrupShapeChuNhatKin = shapes.Where(x => x.RebarStyle == RebarStyle.StirrupTie)
             .FirstOrDefault(x => x.Name == "M_T1");

         StirrupUShape = shapes.Where(x => x.RebarStyle == RebarStyle.StirrupTie)
             .FirstOrDefault(x => x.Name == "M_S3");

         StirrupDaiMoc180x180 = shapes
             .FirstOrDefault(x => x.Name == "M_01");

         StirrupDaiMoc135x135 = shapes
            .FirstOrDefault(x => x.Name == "M_C_135_135");

         if (StirrupDaiMoc135x135 == null)
         {
            StirrupDaiMoc135x135 = StirrupDaiMoc180x180;
         }

         StirrupDaiMoc135x90 = shapes
            .FirstOrDefault(x => x.Name == "M_T9");

         NumberOfRebarByWidths = BeamRebarSettingJson.NumberOfRebarByWidths.ToList();
         TypeRebarByWidths.Add(Define.OneBar);
         TypeRebarByWidths.Add(Define.TwoBars);
         TypeRebarByWidths.Add(Define.ThreeBars);
         TypeRebarByWidths.Add(Define.FourBars1);
         TypeRebarByWidths.Add(Define.FourBars2);
         TypeRebarByWidths.Add(Define.FiveBars1);
         TypeRebarByWidths.Add(Define.FiveBars2);
         TypeRebarByWidths.Add(Define.SixBars);
         TypeRebarByWidths.Add(Define.SeventBars);
         TypeRebarByWidths.Add(Define.NineBars);
      }

   }
}