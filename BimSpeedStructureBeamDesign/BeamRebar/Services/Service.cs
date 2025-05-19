using System.Windows.Controls;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamRebar.Enums;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public static class Service
   {
      public static List<Curve> CurvesFromPoints(this List<XYZ> points)
      {
         var curves = new List<Curve>();
         var ps = new List<XYZ>() { points[0] };
         var z = points[0].Z;
         var count = 0;
         for (int i = 0; i < points.Count; i++)
         {
            if (i == points.Count - 1)
            {
               if (points[i - 1].Z.IsEqual(points[i].Z, 0.01) == false)
               {
                  ps.Add(points[i - 1]);
               }

               ps.Add(points[i]);
               break;
            }
            if (points[i].Z.IsEqual(z, 0.01) == false)
            {
               if (count == 1)
               {
                  ps.Add(points[i]);
               }
               else
               {
                  ps.Add(points[i - 1]);
                  ps.Add(points[i]);
               }
               z = points[i].Z;
               count = 1;
            }
            else
            {
               count++;
            }
         }
         for (int i = 0; i < ps.Count - 1; i++)
         {
            var currentPoint = ps[i];
            var nextPoint = ps[i + 1];
            var line = Line.CreateBound(currentPoint, nextPoint);
            curves.Add(line);
         }
         return curves;
      }

      public static void GetScale(double maxLength, double maxHeight, double minHeight)
      {
         var canvasLength = Define.BeamViewerMaxLength;
         BeamRebarRevitData.XScale = canvasLength / maxLength;
         BeamRebarRevitData.Scale = Define.BeamViewerMaxLength / maxLength;


         if (maxLength > 8.MeterToFoot())
         {
            BeamRebarRevitData.Scale = Define.BeamViewerMaxLength / 8.MeterToFoot();
            BeamRebarRevitData.XScale = BeamRebarRevitData.Scale;
         }

         if (maxLength < 4.MeterToFoot())
         {
            BeamRebarRevitData.Scale = Define.BeamViewerMaxLength / 12.MeterToFoot();
            BeamRebarRevitData.XScale = BeamRebarRevitData.Scale;
         }


         var hh = 60.0;

         var viewerBeamHeight = maxHeight * BeamRebarRevitData.Scale;


         if (hh < viewerBeamHeight)
         {
            BeamRebarRevitData.XScale = hh / maxHeight;
            BeamRebarRevitData.Scale = hh / maxHeight;
         }

         BeamRebarRevitData.Instance.Grid.Width = BeamRebarRevitData.Scale * maxLength + 100;
         BeamRebarRevitData.YScale = BeamRebarRevitData.XScale;
      }

      public static void GetBreakLineY(BeamModel beamModel, Canvas grid)
      {
         var gridHeight = grid.ActualHeight;

         gridHeight = 300;
         var topElevation = beamModel.SpanModels.Max(x => x.TopElevation);
         var botElevation = beamModel.SpanModels.Min(x => x.BotElevation);
         var firstSpanTopElevation = beamModel.SpanModels[0].TopElevation;
         var maxHeight = topElevation - botElevation;
         var topY = (gridHeight - maxHeight * BeamRebarRevitData.YScale) / 2;
         var botY = topY + maxHeight * BeamRebarRevitData.YScale;

         BeamRebarRevitData.BreakLineTopY = topY - 20;
         BeamRebarRevitData.BreakLineBotY = botY + 20;

         var y = topY + (topElevation - firstSpanTopElevation) * BeamRebarRevitData.YScale;
         BeamRebarRevitData.Instance.OriginUiMainView = new System.Windows.Point(50, y);
      }

      public static string GetTypeNumberOfRebarByWidth(this double widthInMm)
      {
         foreach (var setting in BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.NumberOfRebarByWidths)
         {
            if (setting.BMin < widthInMm && setting.BMax > widthInMm)
            {
               return setting.TypeNumberOfRebarByWidth;
            }
         }
         return Define.ThreeBars;
      }

      public static SpanModel GetSpanModelByIndex(this int i)
      {
         var spanModel = BeamRebarRevitData.Instance.BeamModel.SpanModels.FirstOrDefault(x => x.Index == i);
         if (spanModel == null && i == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            return BeamRebarRevitData.Instance.BeamModel.SpanModels.LastOrDefault();
         }
         return spanModel;
      }

      public static SupportModel GetSupportModelByIndex(this int i)
      {
         if (BeamRebarRevitData.Instance.BeamModel.SpanModels.First().LeftSupportModel == null)
         {
            i--;
         }

         return BeamRebarRevitData.Instance.BeamModel.SupportModels.FirstOrDefault(x => x.Index == i);
      }

      public static SpanUiModel GetSpanUiModelByIndex(this int i)
      {
         var spanUiModel = BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.FirstOrDefault(x => x.Index == i);
         if (spanUiModel == null && i == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            return BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.LastOrDefault();
         }
         return spanUiModel;
      }

      /// <summary>
      /// Trả về số lượng thanh cốt thép ứng với từng loại cho thép chính
      /// </summary>
      /// <param name="type"></param>
      /// <param name="isBot"></param>
      /// <returns></returns>
      public static List<int> GetListNumberOfRebars(string type, bool isBot = true)
      {
         var list = new List<int>() { 1 };
         if (type == Define.OneBar)
         {
            list = new List<int> { 1 };
         }
         else if (type == Define.TwoBars)
         {
            list = new List<int> { 2 };
         }
         else if (type == Define.ThreeBars)
         {
            list = new List<int> { 2, 3 };
         }
         else if (type == Define.FourBars1)
         {
            list = new List<int> { 2, 4 };
         }
         else if (type == Define.FourBars2)
         {
            list = new List<int> { 4 };
            if (isBot == false)
            {
               list = new List<int> { 2, 4 };
            }
         }
         else if (type == Define.FiveBars1)
         {
            list = new List<int> { 4, 5 };
            if (isBot == false)
            {
               list = new List<int> { 2, 5 };
            }
         }
         else if (type == Define.FiveBars2)
         {
            list = new List<int> { 3, 5 };
         }
         else if (type == Define.SixBars)
         {
            list = new List<int> { 4, 6 };
         }
         else if (type == Define.SeventBars)
         {
            list = new List<int> { 4, 7 };
         }
         else if (type == Define.NineBars)
         {
            list = new List<int> { 4, 9 };
         }
         return list;
      }

      /// <summary>
      /// Tra ve phan tu gan cuoi cung
      /// </summary>
      /// <param name="list"></param>
      /// <returns></returns>
      public static int GetLast1(this List<int> list)
      {
         if (list.Count <= 1)
         {
            return list.Last();
         }

         return list[list.Count - 2];
      }

      public static List<int> GetMainBarQuantityByTypeWidth(this string type)
      {
         var list = new List<int>() { 1 };
         if (type == Define.OneBar)
         {
            list = new List<int> { 1 };
         }
         else if (type == Define.TwoBars)
         {
            list = new List<int> { 2 };
         }
         else if (type == Define.ThreeBars)
         {
            list = new List<int> { 2, 3 };
         }
         else if (type == Define.FourBars1)
         {
            list = new List<int> { 2, 4 };
         }
         else if (type == Define.FourBars2)
         {
            list = new List<int> { 2, 4 };
         }
         else if (type == Define.FiveBars1)
         {
            list = new List<int> { 2, 5 };
         }
         else if (type == Define.FiveBars2)
         {
            list = new List<int> { 3, 5 };
         }
         else if (type == Define.SixBars)
         {
            list = new List<int> { 4, 6 };
         }
         else if (type == Define.SeventBars)
         {
            list = new List<int> { 4, 7 };
         }
         else if (type == Define.NineBars)
         {
            list = new List<int> { 4, 9 };
         }
         return list;
      }

      public static int GetNumberOfMainBarBySpanIndex(int index, bool isBot = true)
      {
         if (index == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            index--;
         }
         if (isBot)
         {
            List<MainRebar> mainRebars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars.ToList();
            var i = mainRebars.Where(x => x.Start <= index && x.End > index).Sum(x => x.SelectedNumberOfRebar);
            return i;
         }
         else
         {
            List<MainRebar> mainRebars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars.ToList();
            var i = mainRebars.Where(x => x.Start <= index && x.End > index).Sum(x => x.SelectedNumberOfRebar);
            return i;
         }
      }

      public static int GetMaximumNumberOfAdditionalBarLayer1(string type, bool isBot)
      {
         var i = 0;
         if (type == Define.ThreeBars)
         {
            i = 1;
         }
         else if (type == Define.FourBars1)
         {
            i = 2;
         }
         else if (type == Define.FourBars2)
         {
            i = 0;
            if (isBot == false)
            {
               i = 2;
            }
         }
         else if (type == Define.FiveBars1)
         {
            i = 1;
            if (isBot == false)
            {
               i = 3;
            }
         }
         else if (type == Define.FiveBars2)
         {
            i = 2;
         }
         else if (type == Define.SixBars)
         {
            i = 2;
         }
         else if (type == Define.SeventBars)
         {
            i = 3;
         }
         else if (type == Define.NineBars)
         {
            i = 5;
         }
         return i;
      }

      public static List<int> GetListNumberOfAdditionalBottomBar(string type, int numberOfMainMarLayer1, RebarLayers layer)
      {
         var list = new List<int>();
         //Get MainBot bar In Beam
         if (layer == RebarLayers.LayerOne)
         {
            var max = GetListNumberOfRebars(type).Max();
            //True nếu thép chính đặt với số lượng tối đa
            if (max == numberOfMainMarLayer1)
            {
               return list;
            }
            var maxAdd = GetMaximumNumberOfAdditionalBarLayer1(type, true);
            list = new List<int> { maxAdd };
         }
         else if (layer == RebarLayers.LayerTwo || layer == RebarLayers.LayerThree)
         {
            if (type == Define.OneBar)
            {
               list = new List<int> { 1 };
            }
            else if (type == Define.TwoBars)
            {
               list = new List<int> { 2 };
            }
            else if (type == Define.ThreeBars)
            {
               list = new List<int> { 2, 3 };
            }
            else if (type == Define.FourBars1)
            {
               list = new List<int> { 2, 4 };
            }
            else if (type == Define.FourBars2)
            {
               list = new List<int> { 2, 4 };
            }
            else if (type == Define.FiveBars1)
            {
               list = new List<int> { 2, 4, 5 };
            }
            else if (type == Define.FiveBars2)
            {
               list = new List<int> { 2, 3, 5 };
            }
            else if (type == Define.SixBars)
            {
               list = new List<int> { 2, 4, 6 };
            }
            else if (type == Define.SeventBars)
            {
               list = new List<int> { 2, 4, 7 };
            }
            else if (type == Define.NineBars)
            {
               list = new List<int> { 2, 4, 9 };
            }
         }

         return list;
      }

      /// <summary>
      /// Trả về số lượng cốt thép có thể của thép gia cường trên theo lớp 1-2-3
      /// </summary>
      /// <param name="type"></param>
      /// <param name="numberOfMainMarLayer1"></param>
      /// <param name="layer"></param>
      /// <returns></returns>
      public static List<int> GetListNumberOfAdditionalTopBar(string type, int numberOfMainMarLayer1, RebarLayers layer)
      {
         var list = new List<int>();
         //Get MainBot bar In Beam
         if (layer == RebarLayers.LayerOne)
         {
            var max = GetListNumberOfRebars(type, false).Max();
            if (max == numberOfMainMarLayer1)
            {
               return list;
            }
            var maxAdd = GetMaximumNumberOfAdditionalBarLayer1(type, false);
            list = new List<int> { maxAdd };
         }
         else if (layer == RebarLayers.LayerTwo || layer == RebarLayers.LayerThree)
         {
            if (type == Define.OneBar)
            {
               list = new List<int> { 1 };
            }
            else if (type == Define.TwoBars)
            {
               list = new List<int> { 2 };
            }
            else if (type == Define.ThreeBars)
            {
               list = new List<int> { 2, 3 };
            }
            else if (type == Define.FourBars1)
            {
               list = new List<int> { 2, 4 };
            }
            else if (type == Define.FourBars2)
            {
               list = new List<int> { 2, 4 };
            }
            else if (type == Define.FiveBars1)
            {
               list = new List<int> { 2, 4, 5 };
            }
            else if (type == Define.FiveBars2)
            {
               list = new List<int> { 2, 3, 5 };
            }
            else if (type == Define.SixBars)
            {
               list = new List<int> { 2, 4, 6 };
            }
            else if (type == Define.SeventBars)
            {
               list = new List<int> { 2, 4, 7 };
            }
            else if (type == Define.NineBars)
            {
               list = new List<int> { 2, 4, 9 };
            }
         }

         return list;
      }

      public static List<TopRebarAdditionalType> GetAdditionalTopBarPositionsAtSpan(int index, RebarLayer layer = null)
      {
         if (layer == null)
         {
            layer = new RebarLayer(1, true);
         }
         List<TopAdditionalBar> allRebars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars;
         var list = new List<TopRebarAdditionalType>();

         //Nhịp đầu tiên
         if (index == 0)
         {
            list.Add(TopRebarAdditionalType.Right);
         }
         //Nhịp cuối cùng
         else if (index == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            list.Add(TopRebarAdditionalType.Left);
         }
         //Các nhịp ở giữa
         else
         {
            if (BeamRebarRevitData.Instance.BeamModel.SpanModels.Count > 1)
            {
               var currentSpan = BeamRebarRevitData.Instance.BeamModel.SpanModels[index];
               var nextSpan = BeamRebarRevitData.Instance.BeamModel.SpanModels[index - 1];
               if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, false))
               {
                  list.Add(TopRebarAdditionalType.LeftToRight);
                  list.Add(TopRebarAdditionalType.Left);
                  list.Add(TopRebarAdditionalType.Right);
               }
               else
               {
                  list.Add(TopRebarAdditionalType.Left);
                  list.Add(TopRebarAdditionalType.Right);
               }
            }
         }

         //Loại bỏ các vị trí đã có thép
         var bars = allRebars.Where(x => x.Layer.Layer == layer.Layer && x.IsTop == layer.IsTop).Where(x => x.Start <= index && x.End >= index).ToList();
         var isRemoveLeftToRight = false;
         foreach (var bar in bars)
         {
            //Loai bo Left
            if (bar.Start < index)
            {
               isRemoveLeftToRight = true;
               if (list.Contains(TopRebarAdditionalType.Left))
               {
                  list.Remove(TopRebarAdditionalType.Left);
               }
            }
            else
            {
               if (bar.RebarStartType == 1)
               {
                  isRemoveLeftToRight = true;
                  if (list.Contains(TopRebarAdditionalType.Left))
                  {
                     list.Remove(TopRebarAdditionalType.Left);
                  }
               }
            }
            //Loai bo right
            if (bar.End > index)
            {
               isRemoveLeftToRight = true;
               if (list.Contains(TopRebarAdditionalType.Right))
               {
                  list.Remove(TopRebarAdditionalType.Right);
               }
            }
            else
            {
               if (bar.RebarEndType == 1)
               {
                  isRemoveLeftToRight = true;
                  if (list.Contains(TopRebarAdditionalType.Right))
                  {
                     list.Remove(TopRebarAdditionalType.Right);
                  }
               }
            }
            //Loai Bo Left to right
            if (isRemoveLeftToRight)
            {
               if (list.Contains(TopRebarAdditionalType.LeftToRight))
               {
                  list.Remove(TopRebarAdditionalType.LeftToRight);
               }
            }
         }

         if (list.Contains(TopRebarAdditionalType.LeftToRight))
         {
            list.Remove(TopRebarAdditionalType.LeftToRight);
            list.Insert(0, TopRebarAdditionalType.LeftToRight);
         }
         return list;
      }
   }
}