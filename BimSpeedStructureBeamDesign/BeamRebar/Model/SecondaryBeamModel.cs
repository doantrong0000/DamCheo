using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SecondaryBeamModel
   {
      public int Index { get; set; }
      public Element Element { get; set; }
      public double Width { get; set; }
      public double Height { get; set; }
      public XYZ TopLeft { get; set; }
      public XYZ TopRight { get; set; }
      public XYZ BotLeft { get; set; }
      public XYZ BotRight { get; set; }
      public Line Line { get; set; }
      public BeamGeometry BeamGeometry { get; set; }
      public string Name { get; set; }

      public SecondaryBeamModel(BeamGeometry beamGeometry, Line line)
      {
         BeamGeometry = beamGeometry;
         TopLeft = line.SP().EditZ(BeamGeometry.TopElevation);
         TopRight = line.EP().EditZ(BeamGeometry.TopElevation);
         Line = TopLeft.CreateLine(TopRight);
         BotLeft = TopLeft.EditZ(beamGeometry.BotElevation);
         BotRight = TopRight.EditZ(beamGeometry.BotElevation);
         Width = line.Length;
         Element = BeamGeometry.Beam;
         Name = "Secondary Beam" + "-" + Element.Name;
         Height = beamGeometry.Height;
      }
   }
}