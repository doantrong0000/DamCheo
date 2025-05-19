using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Beam
{
   public class ConKeThepModel
   {
      public bool NeedConKeThep { get; set; } = true;
      public bool IsConKeBangCotThep { get; set; }

      public DiameterAndSpacingModel ConKeThepInfo { get; set; } = new()
      {
         Diameter = null,
         Spacing = 2000.MmToFoot()
      };

      public DiameterAndSpacingModel ConKeDaiMocInfo { get; set; } = new()
      {
         Diameter = null,
         Spacing = 2000.MmToFoot()
      };

      public ConKeThepModel()
      {
      }

      private void Initial()
      {
         //Load Data FromSetting
      }
   }
}