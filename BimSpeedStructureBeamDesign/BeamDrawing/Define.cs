using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing
{
    public static class Define
    {
        public static double BeamDetailCropBoxOffsetLeft = 200.MmToFoot();
        public static double BeamDetailCropBoxOffsetRight = 200.MmToFoot();
        public static double BeamDetailCropBoxOffsetTop = 300.MmToFoot();
        public static double BeamDetailCropBoxOffsetBot = 300.MmToFoot();

        public static string FamilySymbolDauThep = "BS_DAUTHEP";
        public static string FamilySymbolBreakLine = "1-25";

        #region Tags

        /// <summary>
        /// Tag cho thép đai có khoảng cách
        /// </summary>
        public static string IndependentTagForStirrupTrai = "A1_T_RT_DK&KC_BOT";
        public static string IndependentTagForStirrupPhai = "BS.Tag_SL& DK (MCN)-P";
        public static string IndependentTagForStirrupTraiDetailView = "A1_T_RT_DK&KC_MID";
        public static string IndependentTagForStirrupPhaiDetailView = "A1_T_RT_DK&KC_MID";

        /// <summary>
        /// Dùng để tag thép standard đơn lẻ
        /// </summary>
        public static string IndependentTagForStandardBarTrai = "A1_T_RT_SL&DK_BOT";

        public static string IndependentTagForStandardBarPhai = "A1_P_RT_SL&DK_BOT";

        //Dùng để tag số lượng  thép nhóm standard
        public static string MultiTagForStandardBarTrai = "BS.Tag_SL& DK (MCN)-T";

        public static string MultiTagForStandardBarPhai = "BS.Tag_SL& DK (MCN)-P";

        #endregion Tags

        #region Views

        public static string ViewTemplateCrossSection = "BS-24-MCN-CT-Dầm PX";
        public static string ViewTemplateSection = "BS-25-MCD-CT-Dầm PY";
        public static string DimensionGap = "@BS-Dim A1";
        public static string DimensionFixed = "@BS-Dim A1";
        public static string ViewportTypeCrossSection = "@BS- Tên view MB, MĐ";
        public static string ViewportTypeHorizontalSection = "@BS- Tên view MB, MĐ";

        #endregion Views
    }
}