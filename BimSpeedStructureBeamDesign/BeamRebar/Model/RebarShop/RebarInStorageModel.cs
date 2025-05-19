using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop
{
   public class RebarInStorageModel
   {
      public int Quantity { get; set; }
      public int Diameter { get; set; }
      public double Length { get; set; }

      public List<ElementId> Ids { get; set; }

      public RebarInStorageModel()
      {
      }

      public RebarInStorageModel(int quantity, int dia, double length)
      {
         Quantity = quantity;
         Diameter = dia;
         Length = length;
      }
   }
}