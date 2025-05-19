using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using Line = System.Windows.Shapes.Line;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Visibility = System.Windows.Visibility;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public static class BeamRebarUiServices
   {

      public static double DimSize = 10;

      /// <summary>
      /// Hàm chuyển đổi tọa độ revit sang tọa độ MainBotBot view beam
      /// </summary>
      /// <param name="p"></param>
      /// <param name="windowOrigin">Điểm này tương đương điểm bot left của span đầu tiên</param>
      /// <param name="s"></param>
      /// <returns></returns>
      public static Point ConvertToMainViewPoint(this XYZ p)
      {
         var beamModel = BeamRebarRevitData.Instance.BeamModel;
         var direction = beamModel.Direction;
         var upVector = XYZ.BasisZ;
         var origin = beamModel.Origin.EditZ(BeamRebarRevitData.Instance.BeamModel.SpanModels[0].TopElevation);
         var windowOrigin = BeamRebarRevitData.Instance.OriginUiMainView;
         var scale = BeamRebarRevitData.Scale;
         var dx = (p - origin).DotProduct(direction.Normalize());
         var dy = (p - origin).DotProduct(upVector.Normalize());
         dx = dx * scale;
         dy = dy * scale;
         return new Point(windowOrigin.X + dx, windowOrigin.Y - dy);
      }

      public static Path ConvertCurvesToPath(this List<Curve> curves, bool isPreview = true)
      {
         var path = new Path { StrokeThickness = Define.BarThickness, Stroke = isPreview ? Define.RebarPreviewColor : Define.RebarColor };
         var geometryGroup = new GeometryGroup();
         foreach (var curve in curves)
         {
            var sp = curve.SP().ConvertToMainViewPoint();
            var ep = curve.EP().ConvertToMainViewPoint();
            var lineGeometry = new LineGeometry(sp, ep);
            geometryGroup.Children.Add(lineGeometry);
         }
         path.Data = geometryGroup;
         return path;
      }

      public static Path DrawHorizontalDimension(List<double> xs, double yGap, double yDimensionLine, out List<Label> textBlocks, List<string> nums2 = null, bool isDimOverall = true, SolidColorBrush color = null)
      {
         if (color == null)
         {
            color = BeamRebarRevitData.DimensionColor;
         }

         xs.Sort();
         textBlocks = new List<Label>();
         Path path = new Path();
         var gg = new GeometryGroup();

         if (xs.Count > 1)
         {
            xs.Sort();
            var xStart = xs.First();
            var xLast = xs.Last();
            var p1 = new Point(xStart, yDimensionLine);
            var p2 = new Point(xLast, yDimensionLine);
            var l1 = new LineGeometry(p1, p2);
            gg.Children.Add(l1);
            foreach (var x in xs)
            {
               //Vertical
               {
                  var s1 = new Point(x, yDimensionLine + 5);
                  if (yDimensionLine < yGap)
                  {
                     s1 = new Point(x, yDimensionLine - 5);
                  }
                  var s2 = new Point(x, yGap);
                  var l = new LineGeometry(s1, s2);
                  gg.Children.Add(l);
               }
               //Inline
               {
                  var s1 = new Point(x, yDimensionLine + 5);
                  var s2 = new Point(x, yDimensionLine - 5);
                  var sMarkLine = new LineGeometry(s1, s2)
                  {
                     Transform = new RotateTransform(45, s1.X, s1.Y / 2 + s2.Y / 2)
                  };
                  gg.Children.Add(sMarkLine);
               }
            }
         }

         var y = yDimensionLine - 15;

         if (xs.Count > 2 && isDimOverall)
         {
            xs.Sort();
            var xStart = xs.First();
            var xLast = xs.Last();
            var p1 = new Point(xStart, y);
            var p2 = new Point(xLast, y);
            var l1 = new LineGeometry(p1, p2);
            gg.Children.Add(l1);
            for (var index = 0; index < xs.Count; index++)
            {
               if (index == 0 || index == xs.Count - 1)
               {
                  var x = xs[index];
                  //Vertical
                  {
                     var s1 = new Point(x, y + 5);
                     if (y < yGap)
                     {
                        s1 = new Point(x, y - 5);
                     }

                     var s2 = new Point(x, yGap);
                     var l = new LineGeometry(s1, s2);
                     gg.Children.Add(l);
                  }
                  //Inline
                  {
                     var s1 = new Point(x, y + 5);
                     var s2 = new Point(x, y - 5);
                     var sMarkLine = new LineGeometry(s1, s2)
                     {
                        Transform = new RotateTransform(45, s1.X, s1.Y / 2 + s2.Y / 2)
                     };
                     gg.Children.Add(sMarkLine);
                  }
               }
            }
         }

         path.Data = gg;
         path.StrokeThickness = 1;
         path.Stroke = color;

         for (int i = 0; i < xs.Count - 1; i++)
         {
            var current = xs[i];
            var next = xs[i + 1];
            var mid = (current + next) / 2;

            var mm2 = "";
            if (nums2 != null)
            {
               try
               {
                  mm2 = nums2[i];
               }
               catch
               {
                  //
               }
            }
            {
               var mm = Math.Round((Math.Abs(current - next) / BeamRebarRevitData.Scale).FootToMm()).ToString(CultureInfo.InvariantCulture);
               var tbMid = new Label() { Content = mm.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color };
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, yDimensionLine - 8));
               textBlocks.Add(tbMid);
            }

            //Duong dim phia duoi cho thep dai spacing
            if (nums2 != null)
            {
               var tbMid = new Label() { Content = mm2.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color };
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, yDimensionLine + 8));
               textBlocks.Add(tbMid);
            }
         }

         if (xs.Count > 2 && isDimOverall)
         {
            var current = xs.First();
            var next = xs.LastOrDefault();
            var mid = (current + next) / 2;

            var mm = Math.Round((Math.Abs(current - next) / BeamRebarRevitData.Scale).FootToMm()).ToString(CultureInfo.InvariantCulture);
            var tbMid = new Label() { Content = mm.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color };
            if (yDimensionLine < yGap)
            {
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, y - 8));
            }
            else
            {
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, y + 8));
            }
            textBlocks.Add(tbMid);
         }

         return path;
      }

      public static Path DrawVerticalDimension(List<double> ys, double xGap, double xDimensionLine, out List<Label> textBlocks, List<string> nums2 = null, bool isDimOverall = true, SolidColorBrush color = null, int textLeft = -1)
      {
         if (color == null)
         {
            color = BeamRebarRevitData.DimensionColor;
         }

         ys.Sort();
         textBlocks = new List<Label>();
         Path path = new Path();
         var gg = new GeometryGroup();
         if (ys.Count > 1)
         {
            ys.Sort();
            var yStart = ys.First();
            var yLast = ys.Last();
            var p1 = new Point(xDimensionLine, yStart);
            var p2 = new Point(xDimensionLine, yLast);
            var l1 = new LineGeometry(p1, p2);
            gg.Children.Add(l1);
            foreach (var yIndex in ys)
            {
               //Horizontal
               {
                  var s1 = new Point(xDimensionLine + 5, yIndex);
                  if (xDimensionLine < xGap)
                  {
                     s1 = new Point(xDimensionLine - 5, yIndex);
                  }
                  var s2 = new Point(xGap, yIndex);
                  var l = new LineGeometry(s1, s2);
                  gg.Children.Add(l);
               }
               //Inline
               {
                  var s1 = new Point(xDimensionLine + 5, yIndex);
                  var s2 = new Point(xDimensionLine - 5, yIndex);
                  var sMarkLine = new LineGeometry(s1, s2)
                  {
                     Transform = new RotateTransform(45, s1.X / 2 + s2.X / 2, s1.Y)
                  };
                  gg.Children.Add(sMarkLine);
               }
            }
         }

         var y = xDimensionLine - 250.MmToFoot() * BeamRebarRevitData.Scale;
         //if (ys.Count > 2 && isDimOverall)
         //{
         //    ys.Sort();
         //    var xStart = ys.First();
         //    var xLast = ys.Last();
         //    var p1 = new Point(xStart, y);
         //    var p2 = new Point(xLast, y);
         //    var l1 = new LineGeometry(p1, p2);
         //    gg.Children.Add(l1);
         //    for (var index = 0; index < ys.Count; index++)
         //    {
         //        if (index == 0 || index == ys.Count - 1)
         //        {
         //            var x = ys[index];
         //            //Vertical
         //            {
         //                var s1 = new Point(x, y + 5);
         //                if (y < xGap)
         //                {
         //                    s1 = new Point(x, y - 5);
         //                }

         //                var s2 = new Point(x, xGap);
         //                var l = new LineGeometry(s1, s2);
         //                gg.Children.Add(l);
         //            }
         //            //Inline
         //            {
         //                var s1 = new Point(x, y + 5);
         //                var s2 = new Point(x, y - 5);
         //                var sMarkLine = new LineGeometry(s1, s2)
         //                {
         //                    Transform = new RotateTransform(45, s1.X, s1.Y / 2 + s2.Y / 2)
         //                };
         //                gg.Children.Add(sMarkLine);
         //            }
         //        }

         //    }
         //}

         path.Data = gg;
         path.StrokeThickness = 1;
         path.Stroke = color;

         for (int i = 0; i < ys.Count - 1; i++)
         {
            var current = ys[i];
            var next = ys[i + 1];
            var mid = (current + next) / 2;

            var mm2 = "";
            if (nums2 != null)
            {
               try
               {
                  mm2 = nums2[i];
               }
               catch
               {
                  //
               }
            }

            {
               var tf = new RotateTransform(-90, xDimensionLine - 8, mid);
               var mm = Math.Round((Math.Abs(current - next) / BeamRebarRevitData.Scale).FootToMm()).ToString(CultureInfo.InvariantCulture);
               var tbMid = new Label() { Content = mm.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color, LayoutTransform = tf };
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(xDimensionLine - 8, mid));
               textBlocks.Add(tbMid);
            }

            //Duong dim phia duoi cho thep dai spacing
            if (nums2 != null)
            {
               var tbMid = new Label() { Content = mm2.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color };
               tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, xDimensionLine + 8));
               textBlocks.Add(tbMid);
            }
         }

         //if (ys.Count > 2 && isDimOverall)
         //{
         //    var current = ys.First();
         //    var next = ys.LastOrDefault();
         //    var mid = (current + next) / 2;

         //    var mm = Math.Round((Math.Abs(current - next) / RevitData.Scale).FootToMm()).ToString(CultureInfo.InvariantCulture);
         //    var tbMid = new Label() { Content = mm.ToString(CultureInfo.InvariantCulture), FontSize = DimSize, Foreground = color };
         //    if (xDimensionLine < xGap)
         //    {
         //        tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, y - 8));
         //    }
         //    else
         //    {
         //        tbMid.SetValue(CenterOnPoint.CenterPointProperty, new Point(mid, y + 8));
         //    }
         //    textBlocks.Add(tbMid);
         //}

         return path;
      }

      public static Path CreateEllipseRebar(double x, double y, double r, bool isFill, Canvas canvas = null, Brush b = null)
      {
         var eg = new EllipseGeometry(new Point(x, y), r, r);
         var path = new Path { Data = eg, Stroke = Brushes.Green, StrokeThickness = 1 };
         if (isFill)
         {
            if (b != null)
            {
               path.Fill = b;
            }
            else
            {
               path.Fill = Brushes.Red;
            }
         }

         if (canvas != null)
         {
            canvas.Children.Add(path);
         }
         return path;
      }

      public static Rectangle CreateRectangle(Canvas canvas, double width, double height, SolidColorBrush color, int thickness, int radius = 0, double canvasTop = 0.0, double canvasLeft = 0.0)
      {
         Rectangle rec = new Rectangle { Height = height, Width = width, StrokeThickness = thickness, Stroke = color };
         // AddBot Rectangle to the Grid.
         if (radius > 0)
         {
            rec.RadiusX = radius;
            rec.RadiusY = radius;
         }

         canvas.Children.Add(rec);
         Canvas.SetTop(rec, (canvas.Height - height) / 2);
         Canvas.SetLeft(rec, (canvas.Width - width) / 2);
         if (canvasTop > 0)
         {
            Canvas.SetTop(rec, canvasTop);
         }
         if (canvasLeft > 0)
         {
            Canvas.SetLeft(rec, canvasLeft);
         }
         return rec;
      }

      public static void GetScale(double maxLength, double maxHeight)
      {
         BeamRebarRevitData.XScale = maxLength / Define.BeamViewerMaxLength;
         BeamRebarRevitData.YScale = maxHeight / Define.BeamViewerMaxHeight;
      }

      //public static double MmToPixel(this double mm)
      //{
      //    return mm * 37.795275590551178;
      //}

      //public static double FootToPixel(this double foot)
      //{
      //    return foot.FootToMm() * 37.795275590551178;
      //}

      //public static double MetToPixel(this double met)
      //{
      //    return met * 3779.5275591;
      //}

      public static Line CreateLine(Point p1, Point p2, Brush brush = null, bool isAddToGrid = false)
      {
         var x1 = p1.X;
         var x2 = p2.X;
         var y1 = p1.Y;
         var y2 = p2.Y;
         var line = new Line
         {
            X1 = x1,
            X2 = x2,
            Y1 = y1,
            Y2 = y2,
            Stroke = new SolidColorBrush(Colors.Black)
         };
         if (brush != null)
         {
            line.Stroke = brush;
         }

         if (isAddToGrid)
         {
            BeamRebarRevitData.Instance.Grid.Children.Add(line);
         }
         return line;
      }

      public static Line CreateLineDash(Point p1, Point p2)
      {
         var line = CreateLine(p1, p2, Brushes.Purple);

         line.StrokeThickness = 1;
         line.StrokeDashArray = new DoubleCollection { 2, 2 };
         return line;
      }

      public static int SelectSupportByX(double y)
      {
         var i = -1;
         if (BeamRebarRevitData.Instance.BeamUiModel == null)
         {
            return i;
         }
         var index = BeamRebarRevitData.Instance.BeamUiModel.SupportUiModels.OrderBy(x => Math.Abs(x.MidX - y))
             .FirstOrDefault()?.Index;
         if (index != null)
         {
            i = (int)index;
         }
         SelectSpanByIndex(i);
         return i;
      }

      public static int SelectSpanByX(double y, bool isHighlight = true)
      {
         var i = -1;
         if (BeamRebarRevitData.Instance.BeamUiModel == null)
         {
            return i;
         }
         var index = BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels
             .FirstOrDefault(x => x.TopStartPoint.X <= y && x.TopLastPoint.X >= y)?.Index;
         if (index != null)
         {
            i = (int)index;
         }
         else
         {
            if (BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.First().TopStartPoint.X > y)
            {
               i = -1;
            }
            else
            {
               i = BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.Count;
            }
         }
         if (isHighlight)
         {
            SelectSpanByIndex(i);
         }
         return i;
      }

      public static void DrawDimension()
      {
         var botUiY = BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels[0].BotLeft.ConvertToMainViewPoint().Y;
         {
            var xs = new List<double>();

            foreach (var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels)
            {
               var left = spanModel.TopLeft.ConvertToMainViewPoint();
               var right = spanModel.TopRight.ConvertToMainViewPoint();
               if (spanModel.Index == 0 && spanModel.LeftSupportModel != null)
               {
                  xs.Add(spanModel.LeftSupportModel.TopLeft.ConvertToMainViewPoint().X);
               }
               xs.Add(left.X);
               xs.Add(right.X);
               if (spanModel.Index == BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels.Count - 1 && spanModel.RightSupportModel != null)
               {
                  xs.Add(spanModel.RightSupportModel.TopRight.ConvertToMainViewPoint().X);
               }
            }

            var yEnd = botUiY + 20;
            var yLDimLine = botUiY + 40;

            var dim = BeamRebarUiServices.DrawHorizontalDimension(xs, yEnd, yLDimLine, out var labels, isDimOverall: false, color: Brushes.Black);
            BeamRebarRevitData.Instance.Grid.Children.Add(dim);
            labels.ForEach(x => BeamRebarRevitData.Instance.Grid.Children.Add(x));
         }

         {
            var xs2 = new List<double>();
            foreach (var beamModelSupportModel in BeamRebarRevitData.Instance.BeamModel.SupportModels)
            {
               var mid = beamModelSupportModel.Line.Midpoint().ConvertToMainViewPoint();
               xs2.Add(mid.X);
            }
            if (xs2.Count > 1)
            {
               var yEnd = botUiY + 40;
               var yLDimLine = botUiY + 60;
               var dim = BeamRebarUiServices.DrawHorizontalDimension(xs2, yEnd, yLDimLine, out var labels, isDimOverall: false, color: Brushes.Black);
               BeamRebarRevitData.Instance.Grid.Children.Add(dim);
               labels.ForEach(x => BeamRebarRevitData.Instance.Grid.Children.Add(x));
            }
         }
      }

      public static Point EditY(this Point p, double y)
      {
         return new Point(p.X, y);
      }

      public static Point EditX(this Point p, double x)
      {
         return new Point(x, p.Y);
      }

      public static void SelectSpanByIndex(int index)
      {

         if (BeamRebarRevitData.Instance.BeamUiModel != null)
         {
            foreach (var span in BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels)
            {
               span.Path.Visibility = span.Index == index && BeamRebarRevitData.Instance.BeamRebarViewModel.CurrentPageViewModel is StirrupTabViewModel ? Visibility.Visible : Visibility.Hidden;
            }
         }
      }

      public static void SelectSupportByIndex(int index)
      {
         if (BeamRebarRevitData.Instance.BeamUiModel != null)
         {
            foreach (var sp in BeamRebarRevitData.Instance.BeamUiModel.SupportUiModels)
            {
               sp.Path.Visibility = sp.Index == index ? Visibility.Visible : Visibility.Hidden;
            }
         }
      }

      /// <summary>
      /// for bottom bars
      /// </summary>
      /// <param name="mainRebar"></param>
      /// <param name="beamUiModel"></param>
      /// <param name="isPreview"></param>
      /// <param name="isBot"></param>
      /// <returns></returns>
      public static Path DrawMainRebar(MainRebar mainRebar, BeamUiModel beamUiModel, bool isPreview = true, bool isBot = true)
      {
         Path path = new Path { StrokeThickness = Define.BarThickness, Stroke = isPreview ? Define.RebarPreviewColor : Define.RebarColor };
         var points = new List<Point>();
         for (int i = mainRebar.Start; i < mainRebar.End; i++)
         {
            var span = beamUiModel.SpanUiModels[i];
            var left = span.BotStartPoint;
            var dx = 0.0;
            if (span.LeftSupport != null)
            {
               dx = -span.LeftSupport.Width / 4;
            }
            else
            {
               dx = 50.0.MmToFoot() * BeamRebarRevitData.XScale;
            }
            left = new Point(left.X + dx, left.Y - BeamRebarRevitData.YScale * 40.0.MmToFoot() * mainRebar.Layer);
            var right = span.BotLastPoint;
            var dy = 0.0;
            if (span.RightSupport != null)
            {
               dy = span.RightSupport.Width / 4;
            }
            else
            {
               dy = -50.0.MmToFoot() * BeamRebarRevitData.XScale;
            }
            right = new Point(right.X + dy, right.Y - BeamRebarRevitData.YScale * 40.0.MmToFoot() * mainRebar.Layer);
            points.Add(left);
            points.Add(right);
         }
         var lineGeometries = GetLineGeometriesFromPoints(points);
         var geomtryGroup = new GeometryGroup();
         lineGeometries.ForEach(x => geomtryGroup.Children.Add(x));
         path.Data = geomtryGroup;
         return path;
      }

      /// <summary>
      /// For top bars
      /// </summary>
      /// <param name="mainRebar"></param>
      /// <param name="beamUiModel"></param>
      /// <param name="isPreview"></param>
      /// <returns></returns>
      public static Path DrawMainRebar2(MainRebar mainRebar, BeamUiModel beamUiModel, bool isPreview = true)
      {
         Path path = new Path { StrokeThickness = Define.BarThickness, Stroke = isPreview ? Define.RebarPreviewColor : Define.RebarColor };
         var points = new List<Point>();
         for (int i = mainRebar.Start; i < mainRebar.End; i++)
         {
            var span = beamUiModel.SpanUiModels[i];
            var left = span.TopStartPoint;
            var dx = 0.0;
            if (span.LeftSupport != null)
            {
               dx = -span.LeftSupport.Width / 4;
            }
            else
            {
               dx = 50.0.MmToFoot() * BeamRebarRevitData.XScale;
            }
            left = new Point(left.X + dx, left.Y + BeamRebarRevitData.YScale * 40.0.MmToFoot() * mainRebar.Layer);
            var right = span.TopLastPoint;
            var dy = 0.0;
            if (span.RightSupport != null)
            {
               dy = span.RightSupport.Width / 4;
            }
            else
            {
               dy = -50.0.MmToFoot() * BeamRebarRevitData.XScale;
            }
            right = new Point(right.X + dy, right.Y + BeamRebarRevitData.YScale * 40.0.MmToFoot() * mainRebar.Layer);
            points.Add(left);
            points.Add(right);
         }
         var lineGeometries = GetLineGeometriesFromPoints(points);
         var geomtryGroup = new GeometryGroup();
         lineGeometries.ForEach(x => geomtryGroup.Children.Add(x));
         path.Data = geomtryGroup;
         return path;
      }

      /// <summary>
      /// For additional bar at bottom
      /// </summary>
      /// <param name="bar"></param>
      /// <param name="isPreview"></param>
      /// <returns></returns>
      public static Path DrawBottomAdditionalRebar(BottomAdditionalBar bar, bool isPreview = true)
      {
         Path path = new Path { StrokeThickness = Define.BarThickness, Stroke = isPreview ? Define.RebarPreviewColor : Define.RebarColor };
         var start = bar.Start;
         var startType = bar.RebarStartType;
         var endType = bar.RebarEndType;
         var points = new List<Point>();

         for (int i = bar.Start; i < bar.End; i++)
         {
            var span = i.GetSpanUiModelByIndex();

            var y = span.BotLastPoint.Y - BeamRebarRevitData.YScale * 30.0.MmToFoot();
            if (bar.Layer.Layer == 2)
            {
               y = y - BeamRebarRevitData.YScale * 50.0.MmToFoot();
            }
            else if (bar.Layer.Layer == 3)
            {
               y = y - 2 * BeamRebarRevitData.YScale * 50.0.MmToFoot();
            }
            var leftPoint = new Point(span.BotStartPoint.X, y);
            var rightPoint = new Point(span.BotLastPoint.X, y);
            //Hiệu chỉnh điểm đầu tiên
            var p1 = leftPoint;
            var p2 = rightPoint;
            if (i == bar.Start)
            {
               if (startType == 1)
               {
                  p1 = new Point(leftPoint.X + span.Length * 0.25, y);
               }
               else if (startType == 2)
               {
                  if (span.LeftSupport != null)
                  {
                     p1 = new Point(leftPoint.X - span.LeftSupport.Width, y);
                  }
                  else
                  {
                     p1 = new Point(leftPoint.X, y);
                  }
               }
               else if (startType == 3)
               {
                  p1 = new Point(leftPoint.X, y);
               }
            }
            if (i == bar.End - 1)
            {
               if (endType == 1)
               {
                  p2 = new Point(rightPoint.X - span.Length * 0.25, y);
               }
               else if (endType == 2)  //Dua ra ngoai support
               {
                  if (span.RightSupport != null)
                  {
                     p2 = new Point(rightPoint.X + span.RightSupport.Width, y);
                  }
                  else
                  {
                     p2 = new Point(rightPoint.X, y);
                  }
               }
               else if (endType == 3)
               {
                  p2 = new Point(rightPoint.X, y);
               }
            }
            points.Add(p1);
            points.Add(p2);
         }

         var lineGeometries = GetLineGeometriesFromPoints(points);
         var geometryGroup = new GeometryGroup();
         lineGeometries.ForEach(x => geometryGroup.Children.Add(x));
         path.Data = geometryGroup;
         return path;
      }

      /// <summary>
      /// For additional bar at top
      /// </summary>
      /// <param name="bar"></param>
      /// <param name="isPreview"></param>
      /// <returns></returns>
      public static Path DrawTopAdditionalRebar(TopAdditionalBar bar, out double x1, out double x2, bool isPreview = true)
      {
         x1 = double.MinValue;
         x2 = double.MinValue;
         Path path = new Path { StrokeThickness = Define.BarThickness, Stroke = isPreview ? Define.RebarPreviewColor : Define.RebarColor };
         var points = new List<Point>();
         var start = bar.Start;
         var end = bar.End;
         var startType = bar.RebarStartType;
         var endType = bar.RebarEndType;
         var span1 = start.GetSpanUiModelByIndex();
         var span0 = BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.FirstOrDefault(x => x.Index == start - 1);
         if (span1 == null)
         {
            return path;
         }
         if (start == end)
         {
            if (start == 0)
            {
               var y = GetYByLayer(span1.TopStartPoint.Y, bar.Layer.Layer, 1);
               var p1 = new Point(span1.TopStartPoint.X, y);
               if (span1.LeftSupport != null)
               {
                  p1 = new Point(p1.X - span1.LeftSupport.Width / 2, y);
               }
               var p2 = new Point(span1.TopStartPoint.X + span1.Length / 3, y);
               points.Add(p1);
               points.Add(p2);
            }
            else if (start == BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.Count)
            {
               var y = GetYByLayer(span1.TopStartPoint.Y, bar.Layer.Layer, 1);
               var p1 = new Point(span1.TopLastPoint.X, y);
               if (span1.RightSupport != null)
               {
                  p1 = new Point(p1.X + span1.RightSupport.Width / 2, y);
               }
               var p2 = new Point(span1.TopLastPoint.X - span1.Length / 3, y);
               points.Add(p1);
               points.Add(p2);
            }
            else
            {
               if (startType == 1 && endType == 1)
               {
                  var sp = span1.LeftSupport;
                  var yLeft = GetYByLayer(sp.TopStartY, bar.Layer.Layer, 1);
                  var p1 = new Point(span0.TopLastPoint.X - span0.Length / 3, yLeft);
                  var p2 = new Point(sp.LeftTopLine.X1, yLeft);
                  var yRight = GetYByLayer(span1.TopStartPoint.Y, bar.Layer.Layer, 1);
                  var p3 = new Point(span1.TopStartPoint.X, yRight);
                  var p4 = new Point(span1.TopStartPoint.X + span1.Length / 3, yRight);
                  points = new List<Point>() { p1, p2, p3, p4 };
               }
               else if (startType == 1 && endType == 2)
               {
                  var sp = span1.LeftSupport;
                  var yLeft = GetYByLayer(sp.TopStartY, bar.Layer.Layer, 1);
                  var p1 = new Point(span0.TopLastPoint.X - span0.Length / 3, yLeft);
                  var p2 = new Point(sp.LeftTopLine.X1 + sp.Width / 2, yLeft);
                  points = new List<Point>() { p1, p2 };
               }
               else if (startType == 2 && endType == 1)
               {
                  var sp = span1.LeftSupport;
                  var yRight = GetYByLayer(span1.TopStartPoint.Y, bar.Layer.Layer, 1);
                  var p3 = new Point(span1.TopStartPoint.X - sp.Width / 2, yRight);
                  var p4 = new Point(span1.TopStartPoint.X + span1.Length / 3, yRight);
                  points = new List<Point>() { p3, p4 };
               }
            }
         }
         else
         {
            for (int i = start; i <= end; i++)
            {
               var currentSpan = i.GetSpanUiModelByIndex();
               var previousSpan = BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.FirstOrDefault(x => x.Index == i - 1);
               //Điểm cuối
               if (i == end)
               {
                  if (endType == 1)
                  {
                     var sp = currentSpan.LeftSupport;
                     var yLeft = GetYByLayer(currentSpan.TopStartPoint.Y, bar.Layer.Layer, 1);
                     var p1 = new Point(sp.RightTopLine.X1, yLeft);
                     var p2 = new Point(currentSpan.TopStartPoint.X + currentSpan.Length / 3, yLeft);
                     points.Add(p1);
                     points.Add(p2);
                  }
                  else
                  {
                     if (i < BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
                     {
                        var sp = currentSpan.LeftSupport;
                        var yRight = GetYByLayer(currentSpan.TopLastPoint.Y, bar.Layer.Layer, 1);
                        var p3 = new Point(currentSpan.TopStartPoint.X, yRight);
                        if (sp != null)
                        {
                           p3 = new Point(p3.X - sp.Width / 2, yRight);
                        }
                        points.Add(p3);
                     }
                     else
                     {
                        var yRight = GetYByLayer(currentSpan.TopLastPoint.Y, bar.Layer.Layer, 1);
                        var p3 = new Point(currentSpan.TopStartPoint.X, yRight);
                        var p4 = new Point(currentSpan.TopLastPoint.X, yRight);
                        points.Add(p3);
                        points.Add(p4);
                     }
                  }
               }
               else
               {
                  if (i != start)
                  {
                     var yLeft = GetYByLayer(currentSpan.TopStartPoint.Y, bar.Layer.Layer, 1);
                     var p1 = new Point(currentSpan.TopStartPoint.X, yLeft);
                     var p2 = new Point(currentSpan.TopLastPoint.X, yLeft);
                     points.Add(p1);
                     points.Add(p2);
                  }
                  else
                  {
                     if (startType == 1)
                     {
                        var sp = currentSpan.LeftSupport;
                        var yLeft = GetYByLayer(sp.TopStartY, bar.Layer.Layer, 1);
                        var p1 = new Point(previousSpan.TopLastPoint.X - previousSpan.Length / 3, yLeft);
                        var p2 = new Point(sp.LeftTopLine.X1, yLeft);
                        points.Add(p1);
                        points.Add(p2);
                     }
                     else
                     {
                        var sp = currentSpan.LeftSupport;
                        var yRight = GetYByLayer(currentSpan.TopStartPoint.Y, bar.Layer.Layer, 1);
                        var p3 = new Point(currentSpan.TopStartPoint.X, yRight);
                        if (sp != null)
                        {
                           p3 = new Point(p3.X - sp.Width, yRight);
                        }
                        points.Add(p3);

                        var y = GetYByLayer(currentSpan.TopStartPoint.Y, bar.Layer.Layer, 1);
                        var pEnd = new Point(currentSpan.TopLastPoint.X, y);
                        points.Add(pEnd);
                     }
                  }
               }
            }
         }
         var lineGeometries = GetLineGeometriesFromPoints(points);
         if (points.Count > 1)
         {
            x1 = points.FirstOrDefault().X;
            x2 = points.LastOrDefault().X;
         }
         var geomtryGroup = new GeometryGroup();
         lineGeometries.ForEach(x => geomtryGroup.Children.Add(x));
         path.Data = geomtryGroup;
         return path;
      }

      private static List<LineGeometry> GetLineGeometriesFromPoints(List<Point> points)
      {
         var lines = new List<LineGeometry>();
         for (int i = 0; i < points.Count - 1; i++)
         {
            var currentPoint = points[i];
            var nextPoint = points[i + 1];
            var line = new LineGeometry(currentPoint, nextPoint);
            lines.Add(line);
         }

         return lines;
      }

      /// <summary>
      /// Trả về cao độ y ứng với từng layer
      /// </summary>
      /// <param name="caoDo"></param>
      /// <param name="layer"></param>
      /// <param name="multify">1 để tính cho top , -1 cho bot</param>
      /// <returns></returns>
      public static double GetYByLayer(double caoDo, int layer, int multify = 1)
      {
        var distence2layer = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.RebarCover.MmToFoot();
        var cover= BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.RebarDistance2Layers.MmToFoot();
        var y = caoDo + BeamRebarRevitData.YScale * cover * multify;
         if (layer == 2)
         {
            y = y + BeamRebarRevitData.YScale * distence2layer * multify;
         }
         else if (layer == 3)
         {
            y = y + 2 * BeamRebarRevitData.YScale * distence2layer * multify;
         }
         return y;
      }

      public static void HideSelectSpan()
      {
         if (BeamRebarRevitData.Instance.BeamUiModel != null)
         {
            BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.ForEach(x => x.Path.Visibility = Visibility.Hidden);
         }
      }
   }
}