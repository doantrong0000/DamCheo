using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using BimSpeedStructureBeamDesign.BeamRebar.Services;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SecondaryBeamUiModel
   {
      public int Index { get; set; }
      public double Width { get; set; }
      public double Height { get; set; }
      public Line TopLine { get; set; }
      public Line BotLine { get; set; }
      public Line LeftTopLine { get; set; }
      public Line RightTopLine { get; set; }
      public double StartX { get; set; }
      public double EndX { get; set; }
      public double TopStartY { get; set; }
      public double TopEndY { get; set; }
      public double BotStartY { get; set; }
      public double BotEndY { get; set; }
      public double MidX { get; set; }
      public double TopY { get; set; }
      public double BotY { get; set; }

      public Path Path { get; set; } = new Path() { Visibility = Visibility.Hidden, Stroke = Brushes.Green, StrokeThickness = 2 };

      public SecondaryBeamUiModel(SecondaryBeamModel secondaryBeam, BeamModel beamModel, BeamUiModel beamUiModel)
      {
         Index = secondaryBeam.Index;
         if (secondaryBeam.BeamGeometry.IsColumn)
         {
            Width = secondaryBeam.Width * BeamRebarRevitData.XScale;
            StartX = beamUiModel.Origin.X + (secondaryBeam.TopLeft - beamModel.Origin).DotProduct(beamModel.Direction) * BeamRebarRevitData.XScale;
            EndX = StartX + Width;
            MidX = StartX + Width * 0.5;

            var xScale = BeamRebarRevitData.XScale;
            var yScale = BeamRebarRevitData.YScale;
            var startYUi = beamUiModel.Origin.Y;

            Height = secondaryBeam.Height * yScale;
            TopY = startYUi + (beamModel.ZTop - secondaryBeam.TopLeft.Z) * yScale;
            BotY = TopY + Height;
         }
         else
         {
            Width = secondaryBeam.Width * BeamRebarRevitData.XScale;
            StartX = beamUiModel.Origin.X + (secondaryBeam.TopLeft - beamModel.Origin).DotProduct(beamModel.Direction) * BeamRebarRevitData.XScale;
            EndX = StartX + Width;
            MidX = StartX + Width * 0.5;

            var xScale = BeamRebarRevitData.XScale;
            var yScale = BeamRebarRevitData.YScale;
            var startYUi = beamUiModel.Origin.Y;

            Height = secondaryBeam.Height * yScale;
            TopY = startYUi + (beamModel.ZTop - secondaryBeam.TopLeft.Z) * yScale;
            BotY = TopY + Height;
         }

      }

      public void DrawLine()
      {
         var db = new DoubleCollection() { 6, 6 };
         TopLine = BeamRebarUiServices.CreateLine(new Point(StartX, TopY),
             new Point(EndX, TopY), Brushes.Green);

         TopLine.StrokeDashArray = db;

         BotLine = BeamRebarUiServices.CreateLine(new Point(StartX, BotY),
               new Point(EndX, BotY), Brushes.Green);
         BotLine.StrokeDashArray = db;

         LeftTopLine = BeamRebarUiServices.CreateLine(new Point(StartX, TopY), new Point(StartX, BotY), Brushes.Green);
         LeftTopLine.StrokeDashArray = db;

         RightTopLine = BeamRebarUiServices.CreateLine(new Point(EndX, TopY), new Point(EndX, BotY), Brushes.Green);
         RightTopLine.StrokeDashArray = db;

         var p1 = new Point(StartX - 5, BeamRebarRevitData.BreakLineTopY - 10);
         var p2 = new Point(EndX + 5, BeamRebarRevitData.BreakLineBotY + 10);
         var rec = new Rect(p1, p2);
         var recGeometry = new RectangleGeometry(rec);
         Path.Data = recGeometry;
         BeamRebarRevitData.Instance.Grid.Children.Add(TopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(BotLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(LeftTopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(RightTopLine);

         BeamRebarRevitData.Instance.Grid.Children.Add(Path);
      }
   }
}