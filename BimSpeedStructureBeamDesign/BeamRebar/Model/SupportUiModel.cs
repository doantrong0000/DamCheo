using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SupportUiModel
   {
      public int Index { get; set; }
      public double Width { get; set; }
      public Line TopLine { get; set; }
      public Line BotLine { get; set; }
      public Line LeftTopLine { get; set; }
      public Line LeftBotLine { get; set; }
      public Line RightTopLine { get; set; }
      public Line RightBotLine { get; set; }
      public double StartX { get; set; }
      public double EndX { get; set; }
      public double TopStartY { get; set; }
      public double TopEndY { get; set; }
      public double BotStartY { get; set; }
      public double BotEndY { get; set; }
      public double MidX { get; set; }
      public Path Path { get; set; } = new Path() { Visibility = Visibility.Hidden, Stroke = Brushes.Green, StrokeThickness = 2 };

      public SupportUiModel(SupportModel supportModel, BeamModel beamModel, BeamUiModel beamUiModel)
      {
         Index = supportModel.Index;

         Width = supportModel.Width * BeamRebarRevitData.XScale;
         StartX = beamUiModel.Origin.X + (supportModel.TopLeft - beamModel.Origin).DotProduct(beamModel.Direction) * BeamRebarRevitData.XScale;
         EndX = StartX + Width;
         MidX = StartX + Width * 0.5;
      }

      public void DrawLine()
      {
         TopLine = BeamRebarUiServices.CreateLine(new Point(StartX, BeamRebarRevitData.BreakLineTopY),
             new Point(EndX, BeamRebarRevitData.BreakLineTopY), Brushes.Red);

         BotLine = BeamRebarUiServices.CreateLine(new Point(StartX, BeamRebarRevitData.BreakLineBotY),
             new Point(EndX, BeamRebarRevitData.BreakLineBotY), Brushes.Red);

         LeftTopLine = BeamRebarUiServices.CreateLine(new Point(StartX, BeamRebarRevitData.BreakLineTopY), TopStartY.IsEqual(0, 1) ? new Point(StartX, BeamRebarRevitData.BreakLineBotY) : new Point(StartX, TopStartY));

         RightTopLine = BeamRebarUiServices.CreateLine(new Point(EndX, BeamRebarRevitData.BreakLineTopY), TopEndY.IsEqual(0, 1) ? new Point(EndX, BeamRebarRevitData.BreakLineBotY) : new Point(EndX, TopEndY));

         LeftBotLine = BeamRebarUiServices.CreateLine(new Point(StartX, BeamRebarRevitData.BreakLineBotY), BotStartY.IsEqual(0, 1) ? new Point(StartX, BeamRebarRevitData.BreakLineBotY) : new Point(StartX, BotStartY));

         RightBotLine = BeamRebarUiServices.CreateLine(new Point(EndX, BeamRebarRevitData.BreakLineBotY), BotEndY.IsEqual(0, 1) ? new Point(EndX, BeamRebarRevitData.BreakLineBotY) : new Point(EndX, BotEndY));

         var p1 = new Point(StartX - 5, BeamRebarRevitData.BreakLineTopY - 10);
         var p2 = new Point(EndX + 5, BeamRebarRevitData.BreakLineBotY + 10);
         var rec = new Rect(p1, p2);
         var recGeometry = new RectangleGeometry(rec);
         Path.Data = recGeometry;
         BeamRebarRevitData.Instance.Grid.Children.Add(TopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(BotLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(LeftTopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(RightTopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(LeftBotLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(RightBotLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(Path);
      }
   }
}