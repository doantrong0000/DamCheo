using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.RebarShop.Model
{
   public class BeamShopGeometryModel
   {
      public FamilyInstance Beam { get; set; }
      public double ZTop { get; set; }
      public double ZBot { get; set; }
      public double ZMid { get; set; }
      public string Mark { get; set; }

      public BeamShopGeometryModel(FamilyInstance beam)
      {
         Beam = beam;
         GetData();
      }

      private void GetData()
      {
         ZTop = Beam.get_Parameter(BuiltInParameter.STRUCTURAL_ELEVATION_AT_TOP).AsDouble();
         ZBot = Beam.get_Parameter(BuiltInParameter.STRUCTURAL_ELEVATION_AT_BOTTOM).AsDouble();
         ZMid = (ZTop + ZBot) / 2;
      }
   }
}