using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using MoreLinq;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Visibility = System.Windows.Visibility;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class SectionUiModel : ViewModelBase
   {
      public Rectangle MainStirrupPath { get; set; }

      public SpanModel SpanModel { get; set; }

      public Canvas Canvas { get; set; }

      /// <summary>
      /// 1,2,3
      /// </summary>
      public int ViTri { get; set; }

      public string SectionName
      {
         get => sectionName;
         set
         {
            sectionName = value;
            OnPropertyChanged();
         }
      }

      private double scale;
      private double canvasWidth;
      private double canvasHeight;
      private Point originUi;
      private Point topLeftUi;
      private string sectionName;

      private Dictionary<string, LabelPointExtension> dicLabelPointExtensions =
         new Dictionary<string, LabelPointExtension>();

      public SectionUiModel(int vitri, SpanModel span)
      {
         SpanModel = span;
         ViTri = vitri;
      }

      public void Draw()
      {
         if (BeamRebarRevitData.Instance.BeamRebarView2 == null)
         {
            return;
         }

         if (ViTri == 1)
         {
            Canvas = BeamRebarRevitData.Instance.BeamRebarView2.Canvas1;
         }

         if (ViTri == 2)
         {
            Canvas = BeamRebarRevitData.Instance.BeamRebarView2.Canvas2;
         }

         if (ViTri == 3)
         {
            Canvas = BeamRebarRevitData.Instance.BeamRebarView2.Canvas3;
         }

         canvasWidth = Canvas.Width;
         canvasHeight = Canvas.Height;
         if (SpanModel.Width < SpanModel.Height)
         {
            scale = (canvasHeight - 10) / SpanModel.Height;
            originUi = new Point(canvasWidth / 2, -canvasHeight / 2);
            topLeftUi = new Point(originUi.X - scale * SpanModel.Width / 2, canvasHeight / 2 - SpanModel.Height * scale * 0.5);
         }
         else
         {
            scale = (canvasWidth - 10) / SpanModel.Width;
            originUi = new Point(canvasWidth / 2, -canvasHeight / 2);
            topLeftUi = new Point(originUi.X - scale * SpanModel.Width / 2, canvasHeight / 2 - SpanModel.Height * scale * 0.5);
         }

         ClearAll();
         DrawCrossSection();
         DrawMainStirrup();
         DrawMainBar();
         DrawDaiMoc();
         DrawDaiLongKin();
         DrawDaiChongPhinh();

         if (BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars.Contains(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.SelectedBar))
         {
            DrawMidBar(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars);
         }
         else
         {
            DrawMidBar(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars);
            DrawMidBar(new List<BottomAdditionalBar> { BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.SelectedBar });
         }

         if (BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars.Contains(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.SelectedBar))
         {
            DrawAdditionalBarAtSupport(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars);
         }
         else
         {
            DrawAdditionalBarAtSupport(BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars);
            DrawAdditionalBarAtSupport(new List<TopAdditionalBar> { BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.SelectedBar });
         }

         DrawTextIndexBottom();
         DrawTextIndexLeftSide();
      }

      void DrawDaiChongPhinh()
      {
         var chongPhinhVm = BeamRebarRevitData.Instance.BeamRebarViewModel.ThepChongPhinhViewModel;

         chongPhinhVm.ListRebarChongPhinhModels.ForEach(x => x.RemovePath(Canvas));
         chongPhinhVm.ListRebarChongPhinhModels.Clear();

         var cover = BeamRebarRevitData.Instance.BeamRebarCover;

         if (chongPhinhVm.SoLuongLopThepChongPhinh > 0)
         {
            var spacing = SpanModel.Height / (chongPhinhVm.SoLuongLopThepChongPhinh + 1);

            var xVector = SpanModel.XVecForStirrupBox.Normalize();

            var corner = SpanModel.TopLeftCorner;

            var spacing2 = SpanModel.Width - 2 * (cover + 20.MmToFoot() / 2 + 10.MmToFoot());

            for (int i = 0; i < chongPhinhVm.SoLuongLopThepChongPhinh; i++)
            {
               var revitPointLeft = corner.Add(xVector.Normalize() * (cover + 20.MmToFoot() / 2 + 10.MmToFoot()))
                  .Add(-XYZ.BasisZ * spacing * (i + 1));

               var windowPointLeft = SpanModel.ConvertToWindowPointForSection(revitPointLeft, topLeftUi, scale);

               var revitPointRight = revitPointLeft.Add(xVector * spacing2);

               var windowPointRight = SpanModel.ConvertToWindowPointForSection(revitPointRight, topLeftUi, scale);


               var rebarModel = new RebarChongPhinhModel()
               {
                  RevitPointLeft = revitPointLeft,
                  WindowPointLeft = windowPointLeft,
                  RevitPointRight = revitPointRight,
                  WindowPointRight = windowPointRight,
                  Index = i,
               };


               var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPointLeft.X, windowPointLeft.Y, 10.MmToFoot() * scale, true);

               var rebarPathRight = BeamRebarUiServices.CreateEllipseRebar(windowPointRight.X, windowPointRight.Y, 10.MmToFoot() * scale, true);

               var stirruPath = DrawPathDaiMocHorizontal(windowPointLeft.Y);

               rebarModel.Paths = new List<Path>() { rebarPath, rebarPathRight };
               rebarModel.StirrupPaths = new List<Path>() { stirruPath };

               Canvas?.Children?.Add(rebarPath);
               Canvas?.Children?.Add(rebarPathRight);
               Canvas?.Children?.Add(stirruPath);

               chongPhinhVm.ListRebarChongPhinhModels.Add(rebarModel);
            }
         }
      }

      private void DrawCrossSection()
      {
         var w = SpanModel.Width * scale;
         var h = SpanModel.Height * scale;
         MainStirrupPath = BeamRebarUiServices.CreateRectangle(Canvas, w, h, Brushes.Black, 1);
      }

      private void DrawMainStirrup()
      {
         var w = (SpanModel.Width - 50.MmToFoot()) * scale;
         var h = (SpanModel.Height - 50.MmToFoot()) * scale;
         MainStirrupPath = BeamRebarUiServices.CreateRectangle(Canvas, w, h, Brushes.Red, 3, 6);

         ToggleMainStirrup(BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.DrawMainStirrup);
      }

      public void DrawDaiMoc()
      {
         //Get mainBot bar
         var vector = SpanModel.XVecForStirrupBox.Normalize();

         var setting = SpanModel.GetRebarQuantityByWidth();
         //Bot
         foreach (var bar in SpanModel.StirrupForSpan.StirrupModels)
         {
            if (bar.IsDaiMoc && bar.IsHorizontal == false)
            {
               var maxBar = setting.TotalBot1;
               var spacing = Math.Abs((SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1));
               if (maxBar == 1)
               {
                  spacing = 0;
               }
               //Draw Rebar
               var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               if (maxBar == 1)
               {
                  p = p.Add(vector * SpanModel.Width / 2);
               }
               else
               {
                  p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               }

               var pp = p.Add(vector * (bar.StartIndex - 1) * spacing);

               var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
               var path = DrawPathDaiMocVertical(windowPoint.X);
               bar.Paths.Add(path);
               bar.Start = pp;
               Canvas.Children.Add(path);
            }

            if (bar.IsDaiMoc && bar.IsHorizontal == true)
            {
               var lp = dicLabelPointExtensions[bar.Location + bar.StartIndex];

               var path = DrawPathDaiMocHorizontal(lp.Point.Y);
               bar.Paths.Add(path);
               bar.Start = lp.RevitPoint;
               Canvas.Children.Add(path);
            }

         }
      }

      private Path DrawPathDaiMocVertical(double x)
      {
         var yTop = SpanModel.ConvertToWindowPointForSection(SpanModel.TopLeft.Add(-XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover)), topLeftUi, scale).Y;
         var yBot = SpanModel.ConvertToWindowPointForSection(SpanModel.BotLeft.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover)), topLeftUi, scale).Y;
         var convertUnit = scale;
         Path path = new Path
         {
            Stroke = Brushes.Purple,
            StrokeThickness = 2
         };
         var gg = new GeometryGroup();
         var x1 = x - 15.MmToFoot() * convertUnit;
         var y1 = yBot - 15.MmToFoot() * convertUnit;
         var p1 = new Point(x1, y1);
         var y2 = yTop + 15.MmToFoot() * convertUnit;
         var p2 = new Point(x1, y2);
         var line = new LineGeometry(p1, p2);
         //Bot Arc
         {
            var x1End = x + 15.MmToFoot() * convertUnit;
            var pBot = new Point(x1End, y1);
            var pBot1 = new Point(x1End, y1 - 15.MmToFoot() * convertUnit);
            var lineBot = new LineGeometry(pBot, pBot1);

            var p1End = new Point(x1End, y1);
            var pathGeometry = new PathGeometry();
            var pathFigBot = new PathFigure { StartPoint = p1 };
            var arcSegmentBot = new ArcSegment(p1End, new Size(1, 1), 90, true, SweepDirection.Counterclockwise, true);
            pathFigBot.Segments.Add(arcSegmentBot);
            pathGeometry.Figures.Add(pathFigBot);
            gg.Children.Add(line);
            gg.Children.Add(pathGeometry);
            gg.Children.Add(lineBot);
         }

         {
            var x1End = x + 15.MmToFoot() * convertUnit;
            var pTop = new Point(x1End, y2);
            var pTop1 = new Point(x1End, y2 + 15.MmToFoot() * convertUnit);
            var lineTop = new LineGeometry(pTop, pTop1);

            var p1End = new Point(x1End, y2);
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = p2 };
            var segmentTop = new ArcSegment(p1End, new Size(1, 1), 90, true, SweepDirection.Clockwise, true);
            pathFigure.Segments.Add(segmentTop);
            pathGeometry.Figures.Add(pathFigure);
            gg.Children.Add(pathGeometry);
            gg.Children.Add(lineTop);
         }
         path.Data = gg;
         return path;
      }

      private Path DrawPathDaiMocHorizontal(double y)
      {
         var xLeft = SpanModel.ConvertToWindowPointForSection(SpanModel.TopLeftCorner, topLeftUi, scale).X;

         var convertUnit = scale;
         Path path = new Path
         {
            Stroke = Brushes.Purple,
            StrokeThickness = 2
         };

         var gg = new GeometryGroup();

         var x1 = xLeft + 40.MmToFoot() * convertUnit;
         var y1 = y - 15.MmToFoot() * convertUnit;
         var p1 = new Point(x1, y1);

         var x2 = x1 + (SpanModel.Width - 2 * 40.MmToFoot()) * convertUnit;
         var p2 = new Point(x2, y1);
         var line = new LineGeometry(p1, p2);
         gg.Children.Add(line);


         {
            var x1End = x1;

            var p1End = new Point(x1End, y1 + 30.MmToFoot() * convertUnit);
            var pathGeometry = new PathGeometry();

            var pathFigBot = new PathFigure { StartPoint = p1 };

            var arcSegmentBot = new ArcSegment(p1End, new Size(1, 1), -90, true, SweepDirection.Counterclockwise, true);
            pathFigBot.Segments.Add(arcSegmentBot);

            pathGeometry.Figures.Add(pathFigBot);
            gg.Children.Add(pathGeometry);

         }

         {

            var p = new Point(x2, y1 + 30.MmToFoot() * convertUnit);
            var pathGeometry = new PathGeometry();

            var pathFigBot = new PathFigure { StartPoint = p2 };

            var arcSegmentBot = new ArcSegment(p, new Size(1, 1), 90, true, SweepDirection.Clockwise, true);
            pathFigBot.Segments.Add(arcSegmentBot);

            pathGeometry.Figures.Add(pathFigBot);
            gg.Children.Add(pathGeometry);

         }

         path.Data = gg;
         return path;
      }

      private void DrawDaiLongKin()
      {
         //Get mainBot bar
         var vector = SpanModel.XVecForStirrupBox.Normalize();
         var setting = SpanModel.GetRebarQuantityByWidth();
         //Bot
         foreach (var bar in SpanModel.StirrupForSpan.StirrupModels)
         {
            if (bar.IsDaiMoc == false)
            {
               var maxBar = setting.TotalBot1;
               var spacing = Math.Abs((SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1));
               //Draw Rebar
               var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               var pp1 = p.Add(vector * (bar.StartIndex - 1) * spacing);
               var pp2 = p.Add(vector * (bar.EndIndex - 1) * spacing);
               var windowPoint1 = SpanModel.ConvertToWindowPointForSection(pp1, topLeftUi, scale);
               var windowPoint2 = SpanModel.ConvertToWindowPointForSection(pp2, topLeftUi, scale);
               var path = DrawPathDaiLongKinBy2Points(windowPoint1.X, windowPoint2.X);
               bar.Paths.Add(path);
               bar.Start = pp1;
               bar.End = pp2;
               Canvas.Children.Add(path);
            }
         }
      }

      private Path DrawPathDaiLongKinBy2Points(double x1, double x2)
      {
         var yTop = SpanModel.ConvertToWindowPointForSection(SpanModel.TopLeft.Add(-XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover)), topLeftUi, scale).Y;
         var yBot = SpanModel.ConvertToWindowPointForSection(SpanModel.BotLeft.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover)), topLeftUi, scale).Y;

         var xMin = Math.Min(x1, x2);
         var xMax = Math.Max(x1, x2);
         var p1 = new Point(xMin - (20.MmToFoot() * scale), yTop);
         var p2 = new Point(xMax + (20.MmToFoot() * scale), yBot);
         var rect = new Rect(p1, p2);
         var rectangleGeometry = new RectangleGeometry(rect, 5, 5);
         var path = new Path { Data = rectangleGeometry, StrokeThickness = 2, Stroke = Brushes.Purple };
         return path;
      }

      private void DrawMainBar()
      {
         //Get mainBot bar
         var vector = SpanModel.XVecForStirrupBox.Normalize();
         var setting = SpanModel.GetRebarQuantityByWidth();
         //Bot
         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars)
         {
            if (bar.Layer == 1)
            {
               var maxBar = setting.TotalBot1;
               var spacing = Math.Abs((SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1));

               //Draw Rebar
               var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));

               if (maxBar == 1)
               {
                  spacing = 0;
                  p = p.Add(vector * (SpanModel.Width / 2));
               }
               else
               {
                  p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               }

               foreach (var rp in bar.RebarPointsInSection)
               {
                  var pp = p.Add(vector * rp.Index * spacing);
                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
                  rp.Point = pp;
                  if (bar.Start <= SpanModel.Index && bar.End >= SpanModel.Index)
                  {
                     var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y, 10.MmToFoot() * scale, true);
                     if (rp.Checked)
                     {
                        Canvas.Children.Add(rebarPath);
                     }
                  }
               }
            }
            else
            {
               var maxBar = bar.selectedNumberOfRebar;
               var spacing = Math.Abs((SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1));

               //Draw Rebar
               var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));

               if (maxBar == 1)
               {
                  spacing = 0;
                  p = p.Add(vector * (SpanModel.Width / 2));
               }
               else
               {
                  p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               }

               var z = BeamRebarServices.GetZAtLayer(SpanModel, false, bar.Layer, 20.GetRebarBarTypeByNumber());

               p = p.EditZ(z);

               foreach (var rp in bar.RebarPointsInSection)
               {
                  var pp = p.Add(vector * rp.Index * spacing);
                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
                  rp.Point = pp;
                  if (bar.Start <= SpanModel.Index && bar.End >= SpanModel.Index)
                  {
                     var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y, 10.MmToFoot() * scale, true);
                     if (rp.Checked)
                     {
                        Canvas.Children.Add(rebarPath);
                     }
                  }
               }
            }
         }

         foreach (var bar in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars)
         {
            if (bar.Layer == 1)
            {
               var maxBar = setting.TotalTop1;
               var spacing = (SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1);
               if (maxBar == 1)
               {
                  spacing = 0;
               }
               //Draw Rebar
               var p = SpanModel.TopLeftCorner.Add(-XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));

               if (maxBar == 1)
               {
                  spacing = 0;
                  p = p.Add(vector * (SpanModel.Width / 2));
               }
               else
               {
                  p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               }

               foreach (var rp in bar.RebarPointsInSection)
               {
                  var pp = p.Add(SpanModel.XVecForStirrupBox.Normalize() * rp.Index * spacing);
                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);

                  rp.Point = pp;
                  if (bar.Start <= SpanModel.Index && bar.End >= SpanModel.Index)
                  {
                     var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y, 10.MmToFoot() * scale, true);
                     rp.WindowPoint = windowPoint;
                     if (rp.Checked)
                     {
                        Canvas.Children.Add(rebarPath);
                     }
                  }
               }
            }
            else
            {
               var maxBar = bar.selectedNumberOfRebar;

               var spacing = (SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1);
               if (maxBar == 1)
               {
                  spacing = 0;
               }
               //Draw Rebar
               var p = SpanModel.TopLeftCorner.Add(-XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));

               if (maxBar == 1)
               {
                  spacing = 0;
                  p = p.Add(vector * (SpanModel.Width / 2));
               }
               else
               {
                  p = p.Add(vector * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
               }

               var z = BeamRebarServices.GetZAtLayer(SpanModel, true, bar.Layer, 20.GetRebarBarTypeByNumber());
               p = p.EditZ(z);

               foreach (var rp in bar.RebarPointsInSection)
               {
                  var pp = p.Add(SpanModel.XVecForStirrupBox.Normalize() * rp.Index * spacing);
                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);

                  rp.Point = pp;
                  if (bar.Start <= SpanModel.Index && bar.End >= SpanModel.Index)
                  {
                     var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y, 10.MmToFoot() * scale, true);
                     rp.WindowPoint = windowPoint;
                     if (rp.Checked)
                     {
                        Canvas.Children.Add(rebarPath);
                     }
                  }
               }
            }
         }
      }

      public void DrawMidBar(List<BottomAdditionalBar> bars)
      {
         //Bot
         var setting = SpanModel.GetRebarQuantityByWidth();
         foreach (var bar in bars)
         {
            if (bar == null)
            {
               continue;
            }
            var total = setting.TotalBot1;
            if (bar.Layer.Layer != 1)
            {
               total = bar.RebarPointsInSection.Count;
            }

            var spacing = (SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (total - 1);
            //Check cây thép nào đi qua vị trí section

            //Draw Rebar
            var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
            p = p.Add(SpanModel.XVecForStirrupBox.Normalize() * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
            var z = BeamRebarServices.GetZAtLayer(SpanModel, bar.Layer.IsTop, bar.Layer.Layer, 20.GetRebarBarTypeByNumber());
            p = p.EditZ(z);

            foreach (var rp in bar.RebarPointsInSection)
            {
               if (rp.Checked)
               {
                  var pp = p.Add(SpanModel.XVecForStirrupBox.Normalize() * rp.Index * spacing);
                  Point windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
                  var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y,
                      10.MmToFoot() * scale, true, null, Brushes.DarkRed);
                  rp.Point = pp;
                  rp.WindowPoint = windowPoint;
                  if (IsBarInSection(bar.Curves))
                  {
                     rp.Paths.Add(rebarPath);
                     Canvas.Children.Add(rebarPath);
                  }
               }
            }
         }
      }

      public void DrawAdditionalBarAtSupport(List<TopAdditionalBar> bars)
      {
         var setting = SpanModel.GetRebarQuantityByWidth();
         foreach (var bar in bars)
         {
            if (bar == null)
            {
               continue;
            }

            var maxBar = setting.TotalTop1;
            if (bar.Layer.Layer != 1)
            {
               maxBar = bar.RebarPointsInSection.Count;
            }

            var spacing = (SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1);


            //Check cây thép nào đi qua vị trí section
            var p = SpanModel.BotLeftCorner.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
            p = p.Add(SpanModel.XVecForStirrupBox.Normalize() * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
            var z = BeamRebarServices.GetZAtLayer(SpanModel, bar.Layer.IsTop, bar.Layer.Layer, 20.GetRebarBarTypeByNumber());
            p = p.EditZ(z);

            foreach (var rp in bar.RebarPointsInSection)
            {
               if (rp.Checked)
               {
                  var pp = p.Add(SpanModel.XVecForStirrupBox.Normalize() * rp.Index * spacing);
                  Point windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
                  var rebarPath = BeamRebarUiServices.CreateEllipseRebar(windowPoint.X, windowPoint.Y, 10.MmToFoot() * scale, true, null, Brushes.DarkRed);
                  rp.Point = pp;
                  rp.WindowPoint = windowPoint;

                  if (IsBarInSection(bar.Curves))
                  {
                     rp.Paths.Add(rebarPath);
                     Canvas.Children.Add(rebarPath);
                  }
               }
            }
         }
      }

      private bool IsBarInSection(List<Curve> curves)
      {
         if (curves.Any() == false)
         {
            return false;
         }
         var points = curves.Select(x => x.SP()).ToList();
         points.Add(curves.Last().EP());
         var p = SpanModel.TopLine.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri1, true);
         if (ViTri == 2)
         {
            p = SpanModel.TopLine.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri2, true);
         }
         else if (ViTri == 3)
         {
            p = SpanModel.TopLine.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri3, true);
         }

         var ds = points.Select(x => x.DotProduct(SpanModel.Direction)).ToList();
         var min = ds.Min();
         var max = ds.Max();
         var d = p.DotProduct(SpanModel.Direction);
         if (d > min && d < max)
         {
            return true;
         }
         return false;
      }

      private void DrawTextIndexBottom()
      {
         var setting = SpanModel.GetRebarQuantityByWidth();
         var maxBar = setting.TotalBot1;
         var spacing = (SpanModel.Width - BeamRebarRevitData.Instance.BeamRebarCover * 2 - 20.MmToFoot() - 10.MmToFoot() * 2) / (maxBar - 1);

         if (maxBar == 1)
         {
            spacing = 0;
         }

         for (int i = 0; i < setting.TotalBot1; i++)
         {
            var p = SpanModel.TopLeftCorner.EditZ(SpanModel.BotElevation - 25.MmToFoot());

            if (maxBar == 1)
            {
               p = p.Add(SpanModel.XVecForStirrupBox.Normalize() * SpanModel.Width / 2);
            }
            else
            {
               p = p.Add(SpanModel.XVecForStirrupBox.Normalize() * (BeamRebarRevitData.Instance.BeamRebarCover + 20.MmToFoot()));
            }

            var pp = p.Add(SpanModel.XVecForStirrupBox.Normalize() * i * spacing);

            var windowPoint = SpanModel.ConvertToWindowPointForSection(pp, topLeftUi, scale);
            Canvas.Children.Add(DrawText(windowPoint, (i + 1).ToString()));
         }
      }

      private void DrawTextIndexLeftSide()
      {

         //main bot
         if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars.Count > 0)
         {
            var lineMax = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars[0].Curves
               .Maxima(x => x.Length).FirstOrDefault();

            var pointRevit = SpanModel.BotLeftCorner.EditZ(lineMax.SP().Z).Add(SpanModel.XVecForStirrupBox * -25.MmToFoot());

            var windowPoint = SpanModel.ConvertToWindowPointForSection(pointRevit, topLeftUi, scale);
            Canvas.Children.Add(DrawText(windowPoint, (1).ToString(), 2, "BOT", pointRevit));
         }

         //add bot

         if (BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars.Count > 0)
         {
            var bars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel.AllBars;
            var group = bars.GroupBy(x => x.Layer.Layer);
            foreach (var g in group)
            {
               if (g.Key > 1)
               {
                  var lineMax = g.First().Curves.Maxima(x => x.Length).FirstOrDefault();

                  var pointRevit = SpanModel.BotLeftCorner.EditZ(lineMax.SP().Z).Add(SpanModel.XVecForStirrupBox * -25.MmToFoot());

                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pointRevit, topLeftUi, scale);
                  Canvas.Children.Add(DrawText(windowPoint, g.Key.ToString(), 2, "BOT", pointRevit));
               }
            }
         }

         //bottom bar left index
         if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars.Count > 0)
         {
            var lineMax = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars[0].Curves
               .Maxima(x => x.Length).FirstOrDefault();

            var pointRevit = SpanModel.BotLeftCorner.EditZ(lineMax.SP().Z).Add(SpanModel.XVecForStirrupBox * -25.MmToFoot());

            var windowPoint = SpanModel.ConvertToWindowPointForSection(pointRevit, topLeftUi, scale);
            Canvas.Children.Add(DrawText(windowPoint, 1.ToString(), 2, "TOP", pointRevit));
         }


         if (BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars.Count > 0)
         {
            var bars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars;
            var group = bars.GroupBy(x => x.Layer.Layer);
            foreach (var g in group)
            {
               if (g.Key > 1)
               {
                  if (!g.First().Curves.Any())
                  {
                     g.ForEach(x => x.DrawPath());
                  }


                  var lineMax = g.First().Curves.Maxima(x => x.Length).FirstOrDefault();

                  var pointRevit = SpanModel.BotLeftCorner.EditZ(lineMax.SP().Z).Add(SpanModel.XVecForStirrupBox * -25.MmToFoot());

                  var windowPoint = SpanModel.ConvertToWindowPointForSection(pointRevit, topLeftUi, scale);
                  Canvas.Children.Add(DrawText(windowPoint, g.Key.ToString(), 2, "TOP", pointRevit));

               }

            }
         }
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="p"></param>
      /// <param name="s"></param>
      /// <param name="type">TYPE =1 MOC DUNG , TYPE=2 MOC NGANG</param>
      /// <returns></returns>
      private Label DrawText(Point p, string s, int type = 1, string location = "BOT", XYZ revitPoint = null)
      {
         //Convert to window point
         var tbMid = new Label() { Content = s, FontSize = 12, Foreground = Brushes.Blue };
         tbMid.SetValue(CenterOnPoint.CenterPointProperty, p);

         var ex = new LabelPointExtension()
         {
            Point = p,
            Type = type,
            Location = location,
            RevitPoint = revitPoint
         };

         tbMid.Tag = ex;

         if (type == 2)
         {
            if (dicLabelPointExtensions.ContainsKey(location + s) == false)
            {
               dicLabelPointExtensions.Add(location + s, ex);
            }
         }

         return tbMid;
      }

      public void ToggleMainStirrup(bool isShow)
      {
         if (MainStirrupPath != null)
         {
            if (isShow)
            {
               MainStirrupPath.Visibility = Visibility.Visible;
            }
            else
            {
               MainStirrupPath.Visibility = Visibility.Hidden;
            }
         }
      }

      private void ClearAll()
      {
         Canvas.Children.Clear();
      }
   }
}