using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop
{
   public class CutZone
   {
      public XYZ Start { get; set; }
      public XYZ End { get; set; }
      public double BotElevation { get; set; }
      public double TopElevation { get; set; }
      public XYZ Direction { get; set; }
      public ElementId HostId { get; set; }
      public string HostMarkName { get; set; }
      public bool IsTop { get; set; }

      public CutZone(XYZ start, XYZ end, double topElevation, double botElevation, string hostMarkName = null, ElementId id = null)
      {
         TopElevation = topElevation;
         BotElevation = botElevation;
         Start = start;
         End = end;
      }
   }
}