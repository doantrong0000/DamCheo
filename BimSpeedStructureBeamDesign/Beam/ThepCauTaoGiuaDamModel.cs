using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;
using Newtonsoft.Json;

namespace BimSpeedStructureBeamDesign.Beam
{
   public class ThepCauTaoGiuaDamModel
   {
      public double MinBeamHeight { get; set; } = 700.MmToFoot();

      [JsonIgnore]
      private RebarBarType _barDiameter { get; set; }

      [JsonIgnore]
      public RebarBarType BarDiameter
      {
         get => _barDiameter;
         set
         {
            _barDiameter = value;
            BarDiameterInt = _barDiameter.DiameterInMm();
         }
      }
      public double BarDiameterInt { get; set; } = 14;
      public double Distance { get; set; } = 400.MmToFoot();
      public double LengthGoInColumn { get; set; } = 100.MmToFoot();

      [JsonIgnore]
      private RebarBarType _barDiameterForBarGoInColumn;

      [JsonIgnore]
      public RebarBarType BarDiameterForBarGoInColumn
      {
         get => _barDiameterForBarGoInColumn;
         set
         {
            _barDiameterForBarGoInColumn = value;
            BarDiameterForBarGoInColumnInt = _barDiameterForBarGoInColumn.DiameterInMm();
         }
      }
      public double BarDiameterForBarGoInColumnInt { get; set; } = 8;
      public double DistanceForBarGoInColumn { get; set; } = 100.MmToFoot();
   }
}