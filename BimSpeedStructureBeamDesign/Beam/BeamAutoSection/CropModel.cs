using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Beam.BeamAutoSection
{
   public class CropModel
   {
      public double TopOffset { get; set; } = 100.MmToFoot();
      public double BotOffset { get; set; } = 100.MmToFoot();
      public double LeftOffset { get; set; } = 100.MmToFoot();
      public double RightOffset { get; set; } = 100.MmToFoot();
      public double FarClipOffset { get; set; } = 100.MmToFoot();
      public int HideScale { get; set; } = 100;
   }
}