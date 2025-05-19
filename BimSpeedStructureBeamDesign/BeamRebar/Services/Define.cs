using System.Windows.Media;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public static class Define
   {
      public static SolidColorBrush RebarPreviewColor = Brushes.Tomato;
      public static SolidColorBrush RebarColor = Brushes.Black;
      public static int BarThickness { get; set; } = 2;
      public static string BeamsAreNotSameLevel = "Các dầm được lựa chọn không nằm trên cùng 1 level!";
      public static string BeamsAreNotConcrete = "Tất cả các dầm cần phải là dầm bê tông!";
      public static string BeamsAreNotStraight = "Tất cả các dầm cần phải là dầm thẳng, không được là dầm cong!";
      public static string BeamsAreNotHorizontal = "Tất cả các dầm cần phải là dầm thẳng, không được là chéo!";
      public static string BeamsAreNotParalell = "Tất cả các dầm cần phải là song song với nhau!";
      public static double BeamViewerMaxLength = 1200.0;
      public static double BeamViewerMaxHeight = 300.0;
      public static string OneBar = "1 Thanh";
      public static string TwoBars = "2 Thanh";
      public static string ThreeBars = "3 Thanh";
      public static string FourBars1 = "4 Thanh Loại 1";
      public static string FourBars2 = "4 Thanh Loại 2";
      public static string FiveBars1 = "5 Thanh Loại 1";
      public static string FiveBars2 = "5 Thanh Loại 2";
      public static string SixBars = "6 Thanh";
      public static string SeventBars = "7 Thanh";
      public static string NineBars = "9 Thanh";
      public static string DaiKep = "Đai Kép";
      public static string DaiLongKin = "Đai Lồng Kín";
      public static string DaiLongChuU = "Đai Lồng Chữ U";
      public static string DaiMoc = "Đai Móc";
      public static string DaiDon = "Đai Đơn";
      public static string BoTriDeu => "BEAMREBAR_DEFINE_BOTRIDEU".GetValueInResources(nameof(BimSpeedStructureBeamDesign));
      public static string BoTriDeu2Dau = "BEAMREBAR_DEFINE_BOTRI2DAU".GetValueInResources(nameof(BimSpeedStructureBeamDesign));
        
      #region Stirrup

      public static string PathDaiDon = @"/BeamTools;component/Resources/STI_DAI_DON.bmp";
      public static string PathDaiKep = @"/BeamTools;component/Resources/STI_DAI_KEP.bmp";
      public static string PathDaiLongChuU = @"/BeamTools;component/Resources/STI_DAI_LONG_CHU_U.bmp";
      public static string PathDaiLongKin = @"/BeamTools;component/Resources/STI_DAI_LONG_KIN.bmp";
      public static string PathDaiMoc = @"/BeamTools;component/Resources/STI_DAI_MOC.bmp";
      public static string PathStirrupSpacing1 = @"/BeamTools;component/Resources/STI_TYPE1.bmp";
      public static string PathStirrupSpacing2 = @"/BeamTools;component/Resources/STI_TYPE2.bmp";

      #endregion Stirrup

      #region Rebar Extensible storage

      public static Guid RebarSchemaGuid = new Guid("e55b2b27-69bc-4ada-9c1e-6d746d69037e");
      public static string StorageFieldRebarLayer = "RebarLayer";
      public static string StorageFieldRebarType = "RebarType";
      public static string MainBarTop = "MainBarTop";
      public static string MainBarBot = "MainBarBot";
      public static string AddtitionalTopBar = "AdditionalTopBar";
      public static string AddtitionalBotBar = "AdditionalBotBar";
      public static string DaiGiaCuongDamPhu = "Dai Gia Cuong Dam Phu";
      public static string ThepDaiAtMidSpan = "Thep Dai At Mid Span";
      public static string ThepDaiAtEndSpan = "Thep Dai At End Span";

      #endregion Rebar Extensible storage
   }
}