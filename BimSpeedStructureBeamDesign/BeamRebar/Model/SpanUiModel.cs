using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SpanUiModel
   {
      public int Index { get; set; }
      public Point TopStartPoint { get; set; }
      public Point TopLastPoint { get; set; }
      public Point BotStartPoint { get; set; }
      public Point BotLastPoint { get; set; }
      public double Height { get; set; }
      public double Length { get; set; }
      public Line TopLine { get; set; } = new Line();
      public Line BotLine { get; set; } = new Line();
      public Line GridLeft { get; set; }
      public Line GridRight { get; set; }
      public Path Path { get; set; } = new Path { Visibility = Visibility.Hidden };
      public SupportUiModel LeftSupport { get; set; }
      public SupportUiModel RightSupport { get; set; }
      public List<SecondaryBeamUiModel> SecondaryBeamUiModels { get; set; } = new List<SecondaryBeamUiModel>();
      public double MidX { get; set; }

      public SpanUiModel(SpanModel spanModel, BeamModel beamModel, BeamUiModel beamUiModel, SupportUiModel left = null)
      {
         Index = spanModel.Index;
         var xScale = BeamRebarRevitData.XScale;
         var yScale = BeamRebarRevitData.YScale;
         var startXUi = beamUiModel.Origin.X;
         var startYUi = beamUiModel.Origin.Y;

         var length = spanModel.Length * xScale;
         var height = spanModel.Height * yScale;
         var startX = (spanModel.TopLeft - beamModel.Origin).DotProduct(beamModel.Direction) * xScale + startXUi;
         var endX = startX + length;
         var spanModelTopElevation = spanModel.TopElevation;
         var topYUi = startYUi + (beamModel.ZTop - spanModelTopElevation) * yScale;
         var botYUi = topYUi + height;

         //Set
         Height = height;
         Length = length;
         TopStartPoint = new Point(startX, topYUi);
         TopLastPoint = new Point(endX, topYUi);
         BotStartPoint = new Point(startX, botYUi);
         BotLastPoint = new Point(endX, botYUi);
         MidX = (startX + endX) / 2;
         //Support
         if (spanModel.LeftSupportModel != null)
         {
            if (left != null)
            {
               LeftSupport = left;
               LeftSupport.TopEndY = TopStartPoint.Y;
               LeftSupport.BotEndY = BotStartPoint.Y;
            }
            else
            {
               LeftSupport = new SupportUiModel(spanModel.LeftSupportModel, beamModel, beamUiModel)
               {
                  TopEndY = TopStartPoint.Y,
                  BotEndY = BotStartPoint.Y
               };
            }
         }
         if (spanModel.RightSupportModel != null)
         {
            RightSupport = new SupportUiModel(spanModel.RightSupportModel, beamModel, beamUiModel)
            {
               TopStartY = TopLastPoint.Y,
               BotStartY = BotLastPoint.Y
            };
         }

         //Secondary Beam
         foreach (var secondaryBeam in spanModel.SecondaryBeamModels)
         {
            var secondaryBeamUi = new SecondaryBeamUiModel(secondaryBeam, beamModel, beamUiModel);
            SecondaryBeamUiModels.Add(secondaryBeamUi);
         }
      }

      public void DrawLine()
      {
         TopLine = BeamRebarUiServices.CreateLine(TopStartPoint, TopLastPoint);
         BotLine = BeamRebarUiServices.CreateLine(BotStartPoint, BotLastPoint);

         var l1 = 0.0;
         var r1 = 0.0;
         if (LeftSupport != null)
         {
            l1 = LeftSupport.Width / 2;
         }

         if (RightSupport != null)
         {
            r1 = RightSupport.Width / 2;
         }

         var p1 = TopStartPoint;
         var p2 = BotLastPoint;
         var rectangleGeometry = new RectangleGeometry(new Rect(p1, p2));
         Path.Data = rectangleGeometry;
         Path.Fill = Brushes.IndianRed;
         Path.Stroke = Brushes.Green;
         Path.Opacity = 0.5;
         Path.StrokeThickness = 2;

         //DrawRectangleRegion Truc vi tri
         var p3Top = new Point(TopStartPoint.X - l1, BeamRebarRevitData.BreakLineTopY - 65);
         var p3Bot = new Point(TopStartPoint.X - l1, BeamRebarRevitData.BreakLineBotY + 40);
         var p3Center = new Point(TopStartPoint.X - l1, BeamRebarRevitData.BreakLineTopY - 75);
         GridLeft = BeamRebarUiServices.CreateLine(p3Top, p3Bot, Brushes.Blue);
         var p4Top = new Point(TopLastPoint.X + r1, BeamRebarRevitData.BreakLineTopY - 65);
         var p4Bot = new Point(TopLastPoint.X + r1, BeamRebarRevitData.BreakLineBotY + 40);
         var p4Center = new Point(TopLastPoint.X + r1, BeamRebarRevitData.BreakLineTopY - 75);
         GridRight = BeamRebarUiServices.CreateLine(p4Top, p4Bot, Brushes.Blue);

         var egLeft = new EllipseGeometry(p3Center, 10, 10);
         var pathLeft = new Path() { Data = egLeft, Stroke = Brushes.Blue };
         var tbLeft = new Label() { Content = Index.ToString(), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14, Foreground = Brushes.Blue };

         var egRight = new EllipseGeometry(p4Center, 10, 10);
         var pathRight = new Path() { Data = egRight, Stroke = Brushes.Blue };
         var tbRight = new Label() { Content = (Index + 1).ToString(), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14, Foreground = Brushes.Blue };

         tbLeft.SetValue(CenterOnPoint.CenterPointProperty, p3Center);

         tbRight.SetValue(CenterOnPoint.CenterPointProperty, p4Center);

         BeamRebarRevitData.Instance.Grid.Children.Add(tbLeft);
         BeamRebarRevitData.Instance.Grid.Children.Add(tbRight);

         BeamRebarRevitData.Instance.Grid.Children.Add(Path);

         BeamRebarRevitData.Instance.Grid.Children.Add(TopLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(BotLine);
         BeamRebarRevitData.Instance.Grid.Children.Add(GridLeft);
         BeamRebarRevitData.Instance.Grid.Children.Add(GridRight);
         BeamRebarRevitData.Instance.Grid.Children.Add(pathLeft);
         BeamRebarRevitData.Instance.Grid.Children.Add(pathRight);
      }
   }
}