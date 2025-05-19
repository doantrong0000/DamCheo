using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model.DrawingItemModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public class BottomAdditionalBar : ViewModelBase, IRebarModel
   {
      public IPageViewModel HostViewModel => BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalBottomBarViewModel;

      private int _start;
      private int _end;
      private RebarLayer _layer;
      private int _rebarStartType = 1;
      private int _rebarEndType = 1;
      private RebarBarType _barDiameter = 20.GetRebarBarTypeByNumber();
      private double leftLength;
      private double leftRatio;
      private double rightRatio;
      private double rightLength;

      public double LeftLength
      {
         get => leftLength;
         set
         {
            leftLength = value;
            GetLeftRatioByLength();
            DrawPath(true);
            OnPropertyChanged(nameof(AnchorLeft));
            OnPropertyChanged(nameof(LeftRatio));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public double LeftRatio
      {
         get => leftRatio;
         set
         {
            leftRatio = value;
            if (value > 0.5 || value < 0.0)
            {
               leftRatio = 0.25;
               "BOTTOMADDITIONBAR01_MESSAGE".NotificationError(this);
            }
            GetLeftLengthByRatio();
            DrawPath(true);
            OnPropertyChanged(nameof(LeftLength));
            OnPropertyChanged(nameof(AnchorLeft));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public double RightRatio
      {
         get => rightRatio;
         set
         {
            rightRatio = value;
            if (value > 0.5 || value < 0.0)
            {
               leftRatio = 0.25;
               "BOTTOMADDITIONBAR01_MESSAGE".NotificationError(this);
            }
            GetRightLengthByRatio();
            DrawPath(true);
            OnPropertyChanged(nameof(RightLength));
            OnPropertyChanged(nameof(AnchorRight));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public double RightLength
      {
         get => rightLength;
         set
         {
            rightLength = value;
            GetRightRatioByLength();
            DrawPath(true);
            OnPropertyChanged(nameof(RightRatio));
            OnPropertyChanged(nameof(AnchorRight));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public double TotalLength
      {
         get => totalLength;
         set
         {
            totalLength = value;
            LeftLength = totalLength / 2;
            RightLength = totalLength / 2;
            OnPropertyChanged();
         }
      }

      public double AnchorRight
      {
         get => anchorRight;
         set
         {
            anchorRight = value;
            GetRightWhenAnchorRightChanged();
            DrawPath(true);
            OnPropertyChanged(nameof(RightLength));
            OnPropertyChanged(nameof(RightRatio));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public double AnchorLeft
      {
         get => anchorLeft;
         set
         {
            anchorLeft = value;
            GetLeftWhenAnchorLeftChanged();
            DrawPath(true);
            OnPropertyChanged(nameof(LeftLength));
            OnPropertyChanged(nameof(LeftRatio));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged();
         }
      }

      public List<int> NumberOfRebarList
      {
         get => numberOfRebarList;
         set
         {
            numberOfRebarList = value;
            OnPropertyChanged();
         }
      }

      public int SelectedNumberOfRebar
      {
         get => selectedNumberOfRebar;
         set
         {
            selectedNumberOfRebar = value;
            var span = Start.GetSpanModelByIndex();
            var setting = span.GetRebarQuantityByWidth();
            if (Layer.Layer == 1 && selectedNumberOfRebar > setting.AddBot1)
            {
               span.GetRebarQuantityByWidth().AddBot1 = value;
            }
            else if (Layer.Layer == 2 && selectedNumberOfRebar > setting.AddBot2)
            {
               span.GetRebarQuantityByWidth().AddBot2 = value;
            }
            else if (Layer.Layer == 2 && selectedNumberOfRebar > setting.AddBot3)
            {
               span.GetRebarQuantityByWidth().AddBot3 = value;
            }

            BeamRebarCommonService.ArrangeRebar();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged();
         }
      }

      public RebarBarType BarDiameter
      {
         get => _barDiameter;
         set
         {
            _barDiameter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
         }
      }

      public int Start
      {
         get => _start;
         set
         {
            _start = value;
            DrawPath(true);
            OnPropertyChanged(nameof(End));
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
         }
      }

      public int End
      {
         get => _end;
         set
         {
            _end = value;
            DrawPath(true);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Start));
            OnPropertyChanged(nameof(Name));
         }
      }

      public string Name => $"Count:{SelectedNumberOfRebar}-D{BarDiameter.BarDiameter().FootToMm()}-S:{Start}-E:{End}";

      public ObservableCollection<int> Starts { get; set; } = new();
      public ObservableCollection<int> Ends { get; set; } = new();
      public string TypeNumberOfRebarByWidth { get; set; }
      public int TotalBar { get; set; }
      public List<int> RebarEndTypes { get; set; } = new() { 1, 2, 3 };
      public List<Rebar> Rebars { get; set; } = new();
      public List<Rebar> ConKeTheps { get; set; } = new();
      public double Z { get; set; }
      public List<Curve> Curves { get; set; } = new();

      public int RebarStartType
      {
         get => _rebarStartType;
         set
         {
            _rebarStartType = value;
            DrawPath(true);
            OnPropertyChanged();
         }
      }

      public int RebarEndType
      {
         get => _rebarEndType;
         set
         {
            _rebarEndType = value;
            DrawPath(true);
            OnPropertyChanged();
         }
      }

      public RebarLayer Layer
      {
         get => _layer;
         set
         {
            if (_layer != null)
            {
               _layer = value;
               BeamRebarCommonService.ArrangeRebar();
               DrawPath(true);
               OnPropertyChanged();
            }
         }
      }

      public List<RebarLayer> Layers { get; set; } = new();
      public DimensionUiModel DimensionUiModel { get; set; } = new();
      private string rebarPointsInSectionString;
      private List<RebarPoint> rebarPointsInSection = new();
      public int selectedNumberOfRebar;
      private List<int> numberOfRebarList = new();
      private double anchorRight;
      private double anchorLeft;
      private double totalLength;

      /// <summary>
      /// Các vị trí để đặt thép chính đi theo số lượng thép chính
      /// </summary>
      public List<RebarPoint> RebarPointsInSection
      {
         get => rebarPointsInSection;
         set
         {
            rebarPointsInSection = value;
            var s = "";
            var first = true;
            foreach (var rebarPoint in RebarPointsInSection)
            {
               if (rebarPoint.Checked)
               {
                  if (first)
                  {
                     s += rebarPoint.Index;
                     first = false;
                  }
                  else
                  {
                     s += "," + rebarPoint.Index;
                  }
               }
            }

            rebarPointsInSectionString = s;
            OnPropertyChanged(nameof(RebarPointsInSectionString));
            OnPropertyChanged();
         }
      }

      public List<Path> PathsCircleInSection { get; set; } = new();

      public string RebarPointsInSectionString
      {
         get => rebarPointsInSectionString;
         set
         {
            rebarPointsInSectionString = value;
            OnPropertyChanged();
         }
      }

      public RelayCommand PositionInSectionCommand { get; set; }
      public Guid GuidId { get; set; }

      public BottomAdditionalBar(int start, int end, int layer, bool isBot = true)
      {
         GuidId = System.Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         PositionInSectionCommand = new RelayCommand(PositionInSection);
         _end = start + 1;
         _start = start;
         _layer = new RebarLayer(layer, !isBot);
         for (int i = start; i < end; i++)
         {
            Starts.Add(i);
         }
         for (int i = start + 1; i <= end; i++)
         {
            Ends.Add(i);
         }
         var spanModel = start.GetSpanModelByIndex();
         var rebarSetting = spanModel.GetRebarQuantityByWidth();
         TypeNumberOfRebarByWidth = spanModel.TypeNumberOfRebarByWidth;
         TotalBar = Service.GetListNumberOfRebars(TypeNumberOfRebarByWidth).Max();
         for (int i = 1; i <= rebarSetting.MaxBars.Max() - 2; i++)
         {
            numberOfRebarList.Add(i);
         }

         if (layer == 1)
         {
            if (rebarSetting.AddBot1 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddBot1 = 1;
            }
            selectedNumberOfRebar = rebarSetting.AddBot1;
         }
         else if (layer == 2)
         {
            if (rebarSetting.AddBot2 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddBot2 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddBot2;
         }
         else if (layer == 3)
         {
            if (rebarSetting.AddBot3 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddBot3 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddBot3;
         }
         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, !isBot));
         }
         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, isBot));
         }

         GetRatio();
         DrawPath();
      }

      private void PositionInSection(object obj)
      {
         if (obj is ListView lv)
         {
            var selectedItems = lv.SelectedItems;

            var view = new BarPositionView() { DataContext = this };
            view.ShowDialog();

            var s = "";
            var first = true;
            foreach (var rebarPoint in RebarPointsInSection)
            {
               if (rebarPoint.Checked)
               {
                  if (first)
                  {
                     s += rebarPoint.Index;
                     first = false;
                  }
                  else
                  {
                     s += "," + rebarPoint.Index;
                  }
               }
            }
            rebarPointsInSectionString = s;
            OnPropertyChanged(nameof(RebarPointsInSectionString));
            RebarPointsInSection = RebarPointsInSection.OrderBy(x => x.Index).ToList();

            if (selectedItems.Count > 1)
            {
               CopyPositionValue(selectedItems.Cast<BottomAdditionalBar>().ToList());
            }
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
         }
      }

      private void CopyPositionValue(List<BottomAdditionalBar> bars)
      {
         foreach (var bottomAdditionalBar in bars)
         {
            bottomAdditionalBar.RebarPointsInSection = RebarPointsInSection.Select(x => new RebarPoint(x.Index, x.Checked)).ToList();
            bottomAdditionalBar.RebarPointsInSectionString = RebarPointsInSectionString;
         }
      }

      public void SetStartsEndsLayersFixed()
      {
         Starts = new ObservableCollection<int>() { Start };
         Ends = new ObservableCollection<int>() { End };
         Layers = new List<RebarLayer>() { Layer };
      }

      public void DrawPath(bool showDim = false)
      {
         DimensionUiModel.RemoveFromUi();
         Curves = BeamRebarServices.GetCurvesAdditionalBottomBar(this, out var z);
         Z = z;
         DimensionUiModel.RebarPath = Curves.ConvertCurvesToPath();

         //Draw Dimension
         DrawDimension(showDim);

         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel1.DrawMidBar(new List<BottomAdditionalBar>() { this });
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel2.DrawMidBar(new List<BottomAdditionalBar>() { this });
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel3.DrawMidBar(new List<BottomAdditionalBar>() { this });
      }

      private void DrawDimension(bool show = false)
      {
         var line1 = Curves.FirstOrDefault();
         var line2 = Curves.LastOrDefault();
         if (line1 != null)
         {
            if (line1.Direction().IsHorizontal())
            {
               var sp = line1.SP().ConvertToMainViewPoint();
               var ep = line1.EP().ConvertToMainViewPoint();
               var yLDimLine = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 300.MmToFoot())
                   .ConvertToMainViewPoint().Y;
               var yEnd = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 50.MmToFoot())
                   .ConvertToMainViewPoint().Y;
               var xs = new List<double>() { sp.X, ep.X };
               //Dim giữa nhịp của nhịp thứ nhất
               if (Start >= 0)
               {
                  var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[Start];
                  var left = span.TopLine.Midpoint().ConvertToMainViewPoint();
                  xs.Add(span.TopLeft.ConvertToMainViewPoint().X);
                  xs.Add(span.TopRight.ConvertToMainViewPoint().X);
                  xs.Add(left.X);
               }

               //Dim giữa nhịp của nhịp thứ 2 nếu vượt nhịp
               if (End > Start + 1)
               {
                  var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[End - 1];
                  var left = span.TopLine.Midpoint().ConvertToMainViewPoint();
                  xs.Add(span.TopRight.ConvertToMainViewPoint().X);
                  xs.Add(left.X);
               }


               DimensionUiModel.DimensionPaths.Add(BeamRebarUiServices.DrawHorizontalDimension(xs, yEnd, yLDimLine, out var labels, isDimOverall: false));
               DimensionUiModel.Labels = labels;

               DimensionUiModel.ShowHideDim(show);
            }
         }


         DimensionUiModel.AddToUiGrid();
      }

      public void RaiseNumber()
      {
         OnPropertyChanged(nameof(SelectedNumberOfRebar));
      }

      public void RemoveUi()
      {
         DimensionUiModel.RemoveFromUi();
         rebarPointsInSection.ForEach(x => x.Paths.ForEach(y => { BeamRebarRevitData.Instance.Grid.Children.Remove(y); }));
      }

      private void GetRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         if (_layer.Layer == 1)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepNhip, 3);
         }
         if (_layer.Layer == 2)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepNhip, 3);
         }
         if (_layer.Layer == 3)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepNhip, 3);
         }
         GetLeftLengthByRatio();
         GetRightLengthByRatio();

         totalLength = leftLength + rightLength;
      }


      private double GetLengthWithD()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;

         var length = 0.0;

         length = setting.Setting.ThepNhipWithD * BarDiameter.BarDiameter();

         return length;
      }
      private void GetLeftRatioByLength()
      {
         var spanStart = (Start).GetSpanModelByIndex();

         leftRatio = ((spanStart.Length / 2 - LeftLength) / spanStart.Length).RoundMilimet(BeamRebarRevitData.Instance.BeamRebarSettingJson.Rounding);

         anchorLeft = leftLength - spanStart.Length / 2;

         totalLength = leftLength + rightLength;
      }

      private void GetRightRatioByLength()
      {
         var spanStart = (End - 1).GetSpanModelByIndex();
         rightRatio = ((spanStart.Length / 2 - rightLength) / spanStart.Length).RoundMilimet(BeamRebarRevitData.Instance.BeamRebarSettingJson.Rounding);
         anchorRight = rightLength - spanStart.Length / 2;

         totalLength = leftLength + rightLength;
      }

      private void GetLeftLengthByRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         var spanStart = Start.GetSpanModelByIndex();
         //Nếu là thép ở mép cuối cùng của nhịp cuối cùng
         if (spanStart != null)
         {
            anchorLeft = (spanStart.Length * leftRatio).RoundMilimet(setting.Setting.Rounding);

            leftLength = spanStart.Length * 0.5 - anchorLeft - GetLengthWithD();
         }

         totalLength = leftLength + rightLength;
      }

      private void GetRightLengthByRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         var spanStart = (End - 1).GetSpanModelByIndex();

         anchorRight = (spanStart.Length * rightRatio).RoundMilimet(setting.Setting.Rounding);

         rightLength = spanStart.Length * 0.5 - anchorRight - GetLengthWithD(); ;

         totalLength = leftLength + rightLength;
      }

      private void GetLeftWhenAnchorLeftChanged()
      {
         var spanStart = Start.GetSpanModelByIndex();
         //Nếu là thép ở mép cuối cùng của nhịp cuối cùng
         if (spanStart != null)
         {
            leftLength = spanStart.Length / 2 + anchorLeft;
            leftRatio = ((spanStart.Length / 2 - LeftLength) / spanStart.Length).RoundMilimet(BeamRebarRevitData.Instance.BeamRebarSettingJson.Rounding);
         }

         totalLength = leftLength + rightLength;
      }

      private void GetRightWhenAnchorRightChanged()
      {
         var spanStart = (End - 1).GetSpanModelByIndex();
         //Nếu là thép ở mép cuối cùng của nhịp cuối cùng
         if (spanStart != null)
         {
            rightLength = spanStart.Length / 2 + anchorRight;
            rightRatio = ((spanStart.Length / 2 - rightLength) / spanStart.Length).RoundMilimet(BeamRebarRevitData.Instance.BeamRebarSettingJson.Rounding);
         }

         totalLength = leftLength + rightLength;
      }
   }
}