using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.Model
{
   public class SectionTypeModel
   {
      public ViewFamilyType ViewFamilyType { get; set; }
      public ElementModel ViewTemplate { get; set; }

      public SectionTypeModel()
      {
      }
   }
}