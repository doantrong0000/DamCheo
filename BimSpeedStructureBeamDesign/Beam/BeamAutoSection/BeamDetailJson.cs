using BimSpeedUtils.JsonData;

namespace BimSpeedStructureBeamDesign.Beam.BeamAutoSection
{
   public class BeamDetailJson
   {
      public CropModel CropModel { get; set; }
      public double SectionOffsetFromSideFace { get; set; }
      public double Length3Sections { get; set; }
      public double Position1 { get; set; }
      public double Position2 { get; set; }
      public double Position3 { get; set; }
      public bool AutoGenerateBeamCrossSection { get; set; }
      public bool IsAll { get; set; }
      public bool IsX { get; set; }
      public bool IsY { get; set; }
      public bool IsInclined { get; set; }
      public bool IsSelection { get; set; }
      public string XViewFamilyType { get; set; }
      public string YViewFamilyType { get; set; }
      public string InclinedViewFamilyType { get; set; }
      public string XViewTemplate { get; set; }
      public string YViewTemplate { get; set; }
      public string InclinedViewTemplate { get; set; }
      public List<RecordModel> RecordModels { get; set; } = new List<RecordModel>();

      public BeamDetailJson()
      {
      }
   }
}