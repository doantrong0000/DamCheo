using System.Collections.ObjectModel;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Enums;
using BimSpeedStructureBeamDesign.BeamRebar.Model.DrawingItemModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.License;
using Visibility = System.Windows.Visibility;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public class TopAdditionalBar : ViewModelBase, IRebarModel
   {
      public IPageViewModel HostViewModel => BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel;
      private int _start;
      private int _end;
      private RebarLayer _layer;
      private int _rebarStartType = 2;
      private int _rebarEndType = 2;

      private RebarBarType _barDiameter = 20.GetRebarBarTypeByNumber();

      private List<int> _rebarStartTypes = new();
      private List<int> _rebarEndTypes = new();
      private List<RebarLayer> layers = new();
      public double PathStartX { get; set; } = double.MinValue;
      public double PathEndX { get; set; } = double.MinValue;
      private double _leftLength;
      private double leftRatio;
      private double rightRatio;
      private double rightLength;

      public void RaiseNumber()
      {
         OnPropertyChanged(nameof(SelectedNumberOfRebar));
      }

      public double LeftLength
      {
         get => _leftLength;
         set
         {
            _leftLength = value;
            GetLeftRatioByLength();
            DrawPath(true);
            OnPropertyChanged(nameof(LeftRatio));
            OnPropertyChanged();
         }
      }

      public double LeftRatio
      {
         get => leftRatio;
         set
         {
            leftRatio = value;
            GetLeftLengthByRatio();
            DrawPath(true);
            OnPropertyChanged(nameof(LeftLength));
            OnPropertyChanged();
         }
      }

      public double RightRatio
      {
         get => rightRatio;
         set
         {
            rightRatio = value;
            GetRightLengthByRatio();
            DrawPath(true);
            OnPropertyChanged(nameof(RightLength));
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
            OnPropertyChanged();
         }
      }

      public bool IsTop
      {
         get => isTop;
         set
         {
            isTop = value;
            OnPropertyChanged();
         }
      }

      public int selectedNumberOfRebar;
      private List<int> numberOfRebarList = new();

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
            if (Layer.Layer == 1 && selectedNumberOfRebar > setting.AddTop1)
            {
               setting.AddTop1 = selectedNumberOfRebar;
            }
            else if (Layer.Layer == 2 && selectedNumberOfRebar > setting.AddTop2)
            {
               setting.AddTop2 = selectedNumberOfRebar;
            }
            else if (Layer.Layer == 3 && selectedNumberOfRebar > setting.AddTop3)
            {
               setting.AddTop3 = selectedNumberOfRebar;
            }

            BeamRebarCommonService.ArrangeRebar();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
         }
      }

      public List<Rebar> Rebars { get; set; } = new();
      public List<Rebar> ConKeTheps { get; set; } = new();
      public double Z { get; set; }
      public List<Curve> Curves { get; set; } = new();

      public RebarBarType BarDiameter
      {
         get => _barDiameter;
         set
         {
            _barDiameter = value;
            GetLeftLengthByDLeft();
            GetRightLengthByDRight();
            DrawPath(true);
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged();
         }
      }

      public int Start
      {
         get => _start;
         set
         {
            _start = value;
            if (_end < _start)
            {
               _end = _start;
            }
            var positions = Service.GetAdditionalTopBarPositionsAtSpan(_start);
            SetRebarStartEndTypeByPosition(positions.FirstOrDefault());
            GetEnds();
            _rebarStartTypes = GetDanhSachKieuBatDau(_start);
            _rebarEndTypes = GetDanhSachKieuKetThuc(_start);
            DrawPath(true);

            OnPropertyChanged();
            OnPropertyChanged(nameof(RebarStartType));
            OnPropertyChanged(nameof(RebarEndType));
            OnPropertyChanged(nameof(RebarStartTypes));
            OnPropertyChanged(nameof(RebarEndTypes));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(End));

         }
      }

      public Visibility LeftRatioVisibility => RebarStartType == 1 ? Visibility.Visible : Visibility.Hidden;
      public Visibility RightRatioVisibility => RebarEndType == 1 ? Visibility.Visible : Visibility.Hidden;

      public Visibility DLeftVisibility => RebarStartType == 2 ? Visibility.Visible : Visibility.Hidden;
      public Visibility DRightVisibility => RebarEndType == 2 ? Visibility.Visible : Visibility.Hidden;

      public int End
      {
         get => _end;
         set
         {
            _end = value;
            if (_end < _start)
            {
               _start = _end;
            }
            RebarEndTypes = GetDanhSachKieuKetThuc(End);
            RebarEndType = RebarEndTypes.FirstOrDefault();
            DrawPath(true);

            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Start));

         }
      }

      public ObservableCollection<int> Starts { get; set; } = new();
      public ObservableCollection<int> Ends { get; set; } = new();
      public int MaxBar { get; set; }

      public List<int> RebarEndTypes
      {
         get => _rebarEndTypes;
         set
         {
            _rebarEndTypes = value;
            OnPropertyChanged();
         }
      }

      public List<int> RebarStartTypes
      {
         get => _rebarStartTypes;
         set
         {
            _rebarStartTypes = value;
            OnPropertyChanged();
         }
      }

      public int RebarStartType
      {
         get => _rebarStartType;
         set
         {
            if (_rebarStartType != value)
            {
               _rebarStartType = value;
               var positions = Service.GetAdditionalTopBarPositionsAtSpan(_start);
               if (_rebarStartType == _rebarEndType && _rebarStartType == 2)
               {
                  _rebarEndType = 1;
               }
               if (_rebarStartType == 1 && positions.Contains(TopRebarAdditionalType.LeftToRight) == false)
               {
                  _rebarEndType = 2;
               }

               GetEnds();
               DrawPath(true);
               OnPropertyChanged();
               OnPropertyChanged(nameof(RebarEndType));
               OnPropertyChanged(nameof(RightRatioVisibility));
               OnPropertyChanged(nameof(LeftRatioVisibility));
               OnPropertyChanged(nameof(DLeftVisibility));
               OnPropertyChanged(nameof(DRightVisibility));
            }
         }
      }

      public int RebarEndType
      {
         get => _rebarEndType;
         set
         {
            if (_rebarEndType != value)
            {
               _rebarEndType = value;
               var positions = Service.GetAdditionalTopBarPositionsAtSpan(_start);
               if (_start == _end)
               {
                  if (_rebarStartType == _rebarEndType && _rebarStartType == 2)
                  {
                     _rebarStartType = 1;
                  }
               }
               if (_rebarEndType == 1 && positions.Contains(TopRebarAdditionalType.LeftToRight) == false)
               {
                  _rebarStartType = 2;
               }
               DrawPath(true);
               OnPropertyChanged();
               OnPropertyChanged(nameof(RebarStartType));
               OnPropertyChanged(nameof(RightRatioVisibility));
               OnPropertyChanged(nameof(LeftRatioVisibility));
               OnPropertyChanged(nameof(DLeftVisibility));
               OnPropertyChanged(nameof(DRightVisibility));
            }
         }
      }

      public RebarLayer Layer
      {
         get => _layer;
         set
         {
            _layer = value;
            GetRatio();
            BeamRebarCommonService.ArrangeRebar();
            OnPropertyChanged();
            OnPropertyChanged(nameof(LeftLength));
            OnPropertyChanged(nameof(RightLength));
            OnPropertyChanged(nameof(LeftRatio));
            OnPropertyChanged(nameof(RightRatio));
         }
      }

      public List<RebarLayer> Layers
      {
         get => layers;
         set
         {
            layers = value;
            OnPropertyChanged();
         }
      }

      private string rebarPointsInSectionString;
      private List<RebarPoint> rebarPointsInSection = new();
      private bool isTop = true;

      private double dTrai;
      private double dPhai;

      public double DTrai
      {
         get => dTrai;
         set
         {
            dTrai = value;
            if (RebarStartType == 2)
            {
               //get rebar length here

               GetLeftLengthByDLeft();
               DrawPath();
            }

            OnPropertyChanged();
         }
      }

      public double DPhai
      {
         get => dPhai;
         set
         {
            dPhai = value;
            DrawPath();
            GetRightLengthByDRight();
            OnPropertyChanged();
         }
      }

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

      public string RebarPointsInSectionString
      {
         get => rebarPointsInSectionString;
         set
         {
            rebarPointsInSectionString = value;
            OnPropertyChanged();
         }
      }

      public string Name => $"Count:{SelectedNumberOfRebar}-D{BarDiameter.BarDiameter().FootToMm()}-S:{Start}-E:{End}";

      public RelayCommand PositionInSectionCommand { get; set; }
      public Guid GuidId { get; set; }
      public DimensionUiModel DimensionUiModel { get; set; } = new();


      public TopAdditionalBar(int start, int end, int layer, int rebarStartType, bool top = true)
      {
         GuidId = Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         PositionInSectionCommand = new RelayCommand(x => PositionInSection());
         _start = start;
         isTop = top;
         _end = end;
         _layer = new RebarLayer(layer, top);
         _rebarStartType = rebarStartType;
         var spanModel = start.GetSpanModelByIndex();
         var rebarSetting = spanModel.GetRebarQuantityByWidth();
         MaxBar = rebarSetting.TotalTop1;
         for (int i = 1; i <= rebarSetting.MaxBars.Max() - 2; i++)
         {
            numberOfRebarList.Add(i);
         }

         if (layer == 1)
         {
            if (rebarSetting.AddTop1 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop1 = 1;
            }
            selectedNumberOfRebar = rebarSetting.AddTop1;
         }
         else if (layer == 2)
         {
            if (rebarSetting.AddTop2 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop2 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop2;
         }
         else if (layer == 3)
         {
            if (rebarSetting.AddTop3 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop3 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop3;
         }

         GetRatio();
         GetDLeftAndRight();
      }

      public TopAdditionalBar(int start, int end, int layer, bool top = true)
      {
         GuidId = Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         PositionInSectionCommand = new RelayCommand(x => PositionInSection());
         _start = start;
         _end = end;
         isTop = top;
         _layer = new RebarLayer(layer, top);
         var spanModel = start.GetSpanModelByIndex();
         var rebarSetting = spanModel.GetRebarQuantityByWidth();
         MaxBar = rebarSetting.TotalTop1;
         for (int i = 1; i <= rebarSetting.MaxBars.Max() - 2; i++)
         {
            numberOfRebarList.Add(i);
         }

         if (layer == 1)
         {
            if (rebarSetting.AddTop1 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop1 = 1;
            }
            selectedNumberOfRebar = rebarSetting.AddTop1;
         }
         else if (layer == 2)
         {
            if (rebarSetting.AddTop2 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop2 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop2;
         }
         else if (layer == 3)
         {
            if (rebarSetting.AddTop3 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop3 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop3;
         }

         GetRatio();
         GetDLeftAndRight();
      }

      public TopAdditionalBar(int start, int layer, TopRebarAdditionalType position, bool top = true)
      {
         GuidId = Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         PositionInSectionCommand = new RelayCommand(x => PositionInSection());
         _start = start;
         _end = start;
         _layer = new RebarLayer(layer, top);
         isTop = top;
         SetRebarStartEndTypeByPosition(position);
         var spanModel = start.GetSpanModelByIndex();
         var rebarSetting = spanModel.GetRebarQuantityByWidth();
         MaxBar = rebarSetting.TotalTop1;
         for (int i = 1; i <= rebarSetting.MaxBars.Max() - 2; i++)
         {
            numberOfRebarList.Add(i);
         }
         if (layer == 1)
         {
            if (rebarSetting.AddTop1 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop1 = 1;
            }
            selectedNumberOfRebar = rebarSetting.AddTop1;
         }
         else if (layer == 2)
         {
            if (rebarSetting.AddTop2 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop2 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop2;
         }
         else if (layer == 3)
         {
            if (rebarSetting.AddTop3 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop3 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop3;
         }

         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, top));
         }
         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, !top));
         }

         GetRatio();
         GetDLeftAndRight();
         GetStarts();
         GetEnds();
         RebarStartTypes = GetDanhSachKieuBatDau(start);
         RebarEndTypes = GetDanhSachKieuKetThuc(End);
         DrawPath();

      }

      public TopAdditionalBar(int start, int end, int layer, int startType, int endType, bool top = true)
      {
         GuidId = Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         PositionInSectionCommand = new RelayCommand(x => PositionInSection());
         _start = start;
         _end = end;
         _layer = new RebarLayer(layer, top);
         isTop = top;
         _rebarStartType = startType;
         _rebarEndType = endType;
         var spanModel = start.GetSpanModelByIndex();

         var rebarSetting = spanModel.GetRebarQuantityByWidth();

         MaxBar = rebarSetting.TotalTop1;

         for (int i = 1; i <= rebarSetting.MaxBars.Max() - 2; i++)
         {
            numberOfRebarList.Add(i);
         }

         if (layer == 1)
         {
            if (rebarSetting.AddTop1 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop1 = 1;
            }

            selectedNumberOfRebar = rebarSetting.AddTop1;
         }
         else if (layer == 2)
         {
            if (rebarSetting.AddTop2 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop2 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop2;
         }
         else if (layer == 3)
         {
            if (rebarSetting.AddTop3 == 0 && !BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet)
            {
               rebarSetting.AddTop3 = 2;
            }
            selectedNumberOfRebar = rebarSetting.AddTop3;
         }

         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, top));
         }
         for (int l = layer; l <= 3; l++)
         {
            Layers.Add(new RebarLayer(l, !top));
         }

         GetRatio();
         GetDLeftAndRight();
         GetStarts();
         GetEnds();
         RebarStartTypes = GetDanhSachKieuBatDau(start);
         RebarEndTypes = GetDanhSachKieuKetThuc(End);
         DrawPath();
      }


      private void GetStarts()
      {
         List<TopAdditionalBar> bars = BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.AllBars;
         var starts = new List<int>();
         var barsInLayer = bars.Where(x => x.Layer.Layer == Layer.Layer && x.IsTop == Layer.IsTop).ToList();

         for (int i = 0; i <= BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.Count; i++)
         {
            var positions = Service.GetAdditionalTopBarPositionsAtSpan(i, Layer);
            if (positions.Count > 0 || barsInLayer.Count == 0)
            {
               starts.Add(i);
            }
         }

         Starts = new ObservableCollection<int>(starts);
      }

      private void GetEnds()
      {
         var ends = new List<int> { _start };

         for (int i = _start; i < BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.Count; i++)
         {
            var ps = Service.GetAdditionalTopBarPositionsAtSpan(i, _layer);
            if (RebarStartType == 2 || ps.Contains(TopRebarAdditionalType.LeftToRight))
            {
               var positions = Service.GetAdditionalTopBarPositionsAtSpan(i + 1, _layer);
               if (positions.Contains(TopRebarAdditionalType.Left) && positions.Contains(TopRebarAdditionalType.LeftToRight) == false)
               {
                  ends.Add(i + 1);
                  break;
               }
               if (positions.Contains(TopRebarAdditionalType.LeftToRight))
               {
                  ends.Add(i + 1);
               }
            }
         }
         ends = ends.Distinct().OrderBy(x => x).ToList();
         Ends = new ObservableCollection<int>(ends);
         _end = Ends.FirstOrDefault();
         OnPropertyChanged(nameof(Ends));
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
            BeamRebarUiServices.DrawTopAdditionalRebar(this, out var x1, out var x2);
            PathStartX = x1;
            PathEndX = x2;
            var BM = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.BeMocToiThieu.MmToFoot();
            if (BM < 80.MmToFoot())
            {   BM = 0.MmToFoot(); }
            Curves = BeamRebarServices.GetAdditionalTopBarCurves(this, BM, out var z);
         if (Curves.Count == 0)
         {
            var bb = 1;
         }
         Z = z;
         DimensionUiModel.RebarPath = Curves.ConvertCurvesToPath();
         //Draw Dimension
         DrawDimension(showDim);
         DimensionUiModel.AddToUiGrid();
         DimensionUiModel.ShowHideDim(true);
         DimensionUiModel.ShowHideRebar();
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
      }

      private void DrawDimension(bool show = false)
      {
         DimensionUiModel.RemoveFromUi(); ;

         var line1 = Curves.Where(x => x.Direction().IsHorizontal()).OrderByDescending(x => x.Length).FirstOrDefault();

         #region Horizontal Dims

         if (line1 != null)
         {
            var sp = line1.SP().ConvertToMainViewPoint();
            var ep = line1.EP().ConvertToMainViewPoint();

            var yLDimLine = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 200.MmToFoot())
                .ConvertToMainViewPoint().Y;

            var yEnd = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 50.MmToFoot())
                .ConvertToMainViewPoint().Y;
            var xs = new List<double>() { sp.X, ep.X };

            //Dim bên trái từ mép support nếu start >0 và thép neo vượt đc nhịp
            if (Start > 0 && Start < BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
            {
               var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[Start];
               var support = span.LeftSupportModel;
               var left = support.TopLeft.ConvertToMainViewPoint();
               xs.Add(left.X);
            }

            if (Start == 0)
            {
               var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[Start];
               if (span.LeftSupportModel != null)
               {
                  var left = span.LeftSupportModel.TopLeft.ConvertToMainViewPoint();
                  xs.Add(left.X);
               }
               else
               {
                  var left = span.TopLeft.ConvertToMainViewPoint();
                  xs.Add(left.X);
               }
            }

            if (End < BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
            {
               var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[End];
               var support = span.LeftSupportModel;
               if (support != null)
               {
                  var right = support.TopRight.ConvertToMainViewPoint();
                  xs.Add(right.X);
               }
            }

            if (End == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
            {
               var span = End.GetSpanModelByIndex();
               if (span.RightSupportModel != null)
               {
                  var left = span.RightSupportModel.TopLeft.ConvertToMainViewPoint();
                  xs.Add(left.X);

                  var right = span.RightSupportModel.TopRight.ConvertToMainViewPoint();
                  xs.Add(right.X);
               }
               else
               {
                  var r = span.TopRight.ConvertToMainViewPoint();
                  xs.Add(r.X);
               }
            }

            // var nums = new List<string>() { Math.Round(line1.Length.FootToMm()).ToString(CultureInfo.InvariantCulture) };
            var path = BeamRebarUiServices.DrawHorizontalDimension(xs, yEnd, yLDimLine, out var labels);
            DimensionUiModel.DimensionPaths.Add(path);
            DimensionUiModel.Labels.AddRange(labels);


         }

         #endregion Horizontal Dims

         var line2 = Curves.Where(x => x.Direction().IsParallel(XYZ.BasisZ)).OrderByDescending(x => x.Length).FirstOrDefault();
         if (line2 != null)
         {
            var y1 = line2.SP().ConvertToMainViewPoint().Y;
            var y2 = line2.EP().ConvertToMainViewPoint().Y;
            var ys = new List<double>() { y1, y2 };
            var direct = BeamRebarRevitData.Instance.BeamModel.Direction;
            var p = line2.SP().Add(direct * 300.MmToFoot()).ConvertToMainViewPoint();
            var pp = line2.SP().Add(direct * 50.MmToFoot()).ConvertToMainViewPoint();

            var g = BeamRebarUiServices.DrawVerticalDimension(ys, pp.X, p.X, out var lbs);
            DimensionUiModel.DimensionPaths.Add(g);
            DimensionUiModel.Labels.AddRange(lbs);
         }

         DimensionUiModel.AddToUiGrid();
         DimensionUiModel.ShowHideDim(show);
      }


      private List<int> GetDanhSachKieuBatDau(int index)
      {
         var positions = Service.GetAdditionalTopBarPositionsAtSpan(index, Layer);
         var kieuBatDaus = new List<int>();
         foreach (var position in positions)
         {
            if (position == TopRebarAdditionalType.LeftToRight)
            {
               kieuBatDaus.Add(1);
               kieuBatDaus.Add(2);
            }
            else if (position == TopRebarAdditionalType.Right)
            {
               kieuBatDaus.Add(2);
            }
            else if (position == TopRebarAdditionalType.Left)
            {
               kieuBatDaus.Add(1);
            }
         }
         kieuBatDaus = kieuBatDaus.Distinct().OrderBy(x => x).ToList();
         return kieuBatDaus;
      }

      private List<int> GetDanhSachKieuKetThuc(int index)
      {
         var list = new List<int>();
         var positions = Service.GetAdditionalTopBarPositionsAtSpan(index, Layer);
         if (positions.Contains(TopRebarAdditionalType.LeftToRight))
         {
            list.Add(2);
            list.Add(1);
         }
         else if (positions.Contains(TopRebarAdditionalType.Left))
         {
            list.Add(2);
         }
         else if (positions.Contains(TopRebarAdditionalType.Right))
         {
            list.Add(1);
         }

         if (index == Start)
         {
            if (RebarStartType == 2)
            {
               list = new List<int>() { 1 };
            }

            if (positions.Count == 2 && RebarStartType == 1)
            {
               list = new List<int>() { 2 };
            }
         }
         else
         {
            if (positions.Contains(TopRebarAdditionalType.LeftToRight) == false)
            {
               list = new List<int>() { 2 };
            }
         }

         return list;
      }

      private void SetRebarStartEndTypeByPosition(TopRebarAdditionalType position)
      {
         if (position == TopRebarAdditionalType.Left)
         {
            _rebarStartType = 1;
            _rebarEndType = 2;
         }
         else if (position == TopRebarAdditionalType.LeftToRight)
         {
            _rebarStartType = 1;
            _rebarEndType = 1;
         }
         else if (position == TopRebarAdditionalType.Right)
         {
            _rebarStartType = 2;
            _rebarEndType = 1;
         }
      }

      private void PositionInSection()
      {
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
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
      }

      public void RemoveUi()
      {
         DimensionUiModel.RemoveFromUi();
         rebarPointsInSection.ForEach(x => x.Paths.ForEach(y =>
           {
              BeamRebarRevitData.Instance.Grid.Children.Remove(y);
           }));
      }

      private void GetRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;

         if (_layer.Layer == 1)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepGoiLop1, 3);
         }
         if (_layer.Layer == 2)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepGoiLop2, 3);
         }
         if (_layer.Layer == 3)
         {
            leftRatio = rightRatio = Math.Round(1 / setting.Setting.ThepGoiLop3, 3);
         }

         GetLeftLengthByRatio();
         GetRightLengthByRatio();
      }

      private void GetLeftRatioByLength()
      {
         var spanStart = (Start - 1).GetSpanModelByIndex();
         if (spanStart != null)
         {
            leftRatio = (LeftLength / spanStart.Length).Round2Number();
         }
         else
         {
            leftRatio = 1;
         }
      }

      private void GetRightRatioByLength()
      {
         var spanStart = (Start).GetSpanModelByIndex();
         rightRatio = (rightLength / spanStart.Length).Round2Number();
      }

      private void GetLeftLengthByRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         var spanStart = (Start - 1).GetSpanModelByIndex();
         //Nếu là thép ở mép cuối cùng của nhịp cuối cùng
         if (spanStart != null)
         {
            _leftLength = (spanStart.Length * leftRatio).RoundMilimet(setting.Setting.Rounding) + GetLengthWithD();
         }
      }


      double GetLengthWithD()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;

         var length = 0.0;


         if (_layer.Layer == 1)
         {
            length = setting.Setting.ThepGoiLop1WithD * BarDiameter.BarDiameter();
         }
         if (_layer.Layer == 2)
         {
            length = setting.Setting.ThepGoiLop2WithD * BarDiameter.BarDiameter();
         }
         if (_layer.Layer == 3)
         {
            length = setting.Setting.ThepGoiLop3WithD * BarDiameter.BarDiameter();
         }


         return length;
      }

      private void GetRightLengthByRatio()
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         var spanStart = (Start).GetSpanModelByIndex();

         if (Start == 0)
         {
            spanStart = 0.GetSpanModelByIndex();
         }

         //Nếu là thép ở mép cuối cùng của nhịp cuối cùng
         if (spanStart != null)
         {
            rightLength = (spanStart.Length * rightRatio).RoundMilimet(setting.Setting.Rounding) + GetLengthWithD();
         }

      }

      private void GetLeftLengthByDLeft()
      {
         if (RebarStartType == 2)
         {
            _leftLength = DTrai * BarDiameter.BarDiameter();
            OnPropertyChanged(nameof(LeftLength));
         }
      }

      private void GetRightLengthByDRight()
      {
         if (RebarEndType == 2)
         {
            rightLength = DPhai * BarDiameter.BarDiameter();
            OnPropertyChanged(nameof(RightLength));
         }
      }


      public void SetPathsColor(SolidColorBrush color)
      {
         DimensionUiModel.RebarPath.Stroke = color;
      }

      void GetDLeftAndRight()
      {
         var startSpan = Start.GetSpanModelByIndex();
         if (startSpan.LeftSupportModel != null)
         {
            var cat = startSpan.LeftSupportModel.Element.Category.ToBuiltinCategory();
            if (cat == BuiltInCategory.OST_StructuralColumns)
            {
               dTrai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForColumn.Top;
            }

            else if (cat == BuiltInCategory.OST_Walls)
            {
               dTrai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForWall.Top;
            }

            else if (cat == BuiltInCategory.OST_StructuralFoundation)
            {
               dTrai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForFoundation.Top; ;
            }

            else if (cat == BuiltInCategory.OST_StructuralFraming)
            {
               dTrai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForBeam.Top; ;
            }

         }

         var endSpan = End.GetSpanModelByIndex();

         if (endSpan.LeftSupportModel != null && End < BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            var cat = endSpan.LeftSupportModel.Element.Category.ToBuiltinCategory();
            if (cat == BuiltInCategory.OST_StructuralColumns)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForColumn.Top;
            }

            else if (cat == BuiltInCategory.OST_Walls)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForWall.Top;
            }

            else if (cat == BuiltInCategory.OST_StructuralFoundation)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForFoundation.Top; ;
            }

            else if (cat == BuiltInCategory.OST_StructuralFraming)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForBeam.Top; ;
            }
         }

         if (endSpan.RightSupportModel != null && End <= BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
         {
            var cat = endSpan.RightSupportModel.Element.Category.ToBuiltinCategory();
            if (cat == BuiltInCategory.OST_StructuralColumns)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForColumn.Top;
            }

            else if (cat == BuiltInCategory.OST_Walls)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForWall.Top;
            }

            else if (cat == BuiltInCategory.OST_StructuralFoundation)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForFoundation.Top; ;
            }

            else if (cat == BuiltInCategory.OST_StructuralFraming)
            {
               dPhai = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForBeam.Top; ;
            }


         }

         if (RebarStartType == 2)
         {
            GetLeftLengthByDLeft();
         }


         if (RebarEndType == 2)
         {
            GetRightLengthByDRight();
         }



      }
   }
}