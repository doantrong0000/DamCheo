using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SupportModel
   {
      public int Index { get; set; }
      public Element Element { get; set; }
      public double Width { get; set; }
      public XYZ TopLeft { get; set; }
      public XYZ TopRight { get; set; }
      public XYZ BotLeft { get; set; }
      public XYZ BotRight { get; set; }
      public Line Line { get; set; }
      public ElementGeometry ElementGeometry { get; set; }
      public string Name { get; set; }

      public SupportModel(ElementGeometry elementGeometry, Line line)
      {
         Line = line;
         ElementGeometry = elementGeometry;
         TopLeft = line.SP();
         TopRight = line.EP();
         Width = line.Length;
         Element = ElementGeometry.Element;
         Name = Element.Category.Name + "-" + Element.Name;
      }
   }
}