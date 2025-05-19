using BimSpeedUtils.JsonData;

namespace BimSpeedStructureBeamDesign.Beam.BeamAutoSection
{
   public class BeamSectionJson
   {
      public CropModel CropModel { get; set; }
      public int Operation { get; set; }
      public string ViewFamilyType { get; set; }
      public string ViewTemplate { get; set; }
      public List<RecordModel> RecordModels { get; set; } = new List<RecordModel>();

      public BeamSectionJson()
      {
      }
   }
}