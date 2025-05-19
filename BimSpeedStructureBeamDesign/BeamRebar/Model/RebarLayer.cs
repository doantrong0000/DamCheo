namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class RebarLayer
   {
      public int Layer { get; set; }
      public bool IsTop { get; set; }
      public string Name { get; set; }

      public RebarLayer(int layer, bool isTop)
      {
         Layer = layer;
         IsTop = isTop;
         Name = "T";
         if (IsTop == false)
         {
            Name = "B";
         }

         Name += " " + layer;
      }

      public override bool Equals(object obj)
      {
         if (obj is RebarLayer layer)
         {
            if (layer.Layer == Layer && layer.IsTop == IsTop)
            {
               return true;
            }
         }
         return false;
      }

      public override int GetHashCode()
      {
         return 0;
      }
   }
}