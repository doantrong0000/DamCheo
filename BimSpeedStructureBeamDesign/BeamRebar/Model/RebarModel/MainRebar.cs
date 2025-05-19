using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model.DrawingItemModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public class MainRebar : ViewModelBase, IRebarModel
   {
      private int _start;
      private int _end;
      private RebarBarType _barDiameter = 20.GetRebarBarTypeByNumber();
      private int _layer = 1;
      private string rebarPointsInSectionString;
      private List<RebarPoint> rebarPointsInSection = new();
      public int selectedNumberOfRebar;
      private double lxTrai;
      private double lxPhai;
      private double lyTrai;
      private double lyPhai;
      public List<int> NumberOfRebarList { get; set; } = new();

      public int SelectedNumberOfRebar
      {
         get => selectedNumberOfRebar;
         set
         {
            selectedNumberOfRebar = value;
            var span = Start.GetSpanModelByIndex();
            var setting = span.GetRebarQuantityByWidth();
            if (Layer == 1)
            {
               if (IsTop)
               {
                  setting.MainTop1 = selectedNumberOfRebar;
               }
               else
               {
                  setting.MainBot1 = selectedNumberOfRebar;
               }
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
            DrawPath();
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
            if (_start > _end)
            {
               _end = _start + 1;
            }
            DrawPath();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(End));
         }
      }

      public int End
      {
         get => _end;
         set
         {
            _end = value;
            if (_start > value)
            {
               _start = _end - 1;
            }
            DrawPath();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Start));
         }
      }

      public int Layer
      {
         get => _layer;
         set
         {
            _layer = value;
            DrawPath();
            OnPropertyChanged();
         }
      }

      public List<int> Layers { get; set; } = new() { 1, 2, 3, 4, 5 };
      public ObservableCollection<int> Starts { get; set; } = new();
      public ObservableCollection<int> Ends { get; set; } = new();
      public bool IsTop { get; set; } = false;
      public List<Rebar> Rebars { get; set; } = new();
      public List<Curve> Curves { get; set; } = new();

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

            selectedNumberOfRebar = rebarPointsInSection.Count(x => x.Checked);
            OnPropertyChanged(nameof(SelectedNumberOfRebar));
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

      public RelayCommand PositionInSectionCommand { get; set; }

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

      public DimensionUiModel DimensionUiModel { get; set; } = new();
      public double LxTrai
      {
         get => lxTrai;
         set
         {
            lxTrai = value;
            DrawPath();
            OnPropertyChanged();
         }
      }

      public double LxPhai
      {
         get => lxPhai;
         set
         {
            lxPhai = value;
            DrawPath();
            OnPropertyChanged();
         }
      }

      public double LyTrai
      {
         get => lyTrai;
         set
         {
            lyTrai = value;
            DrawPath();
            OnPropertyChanged();
         }
      }

      public double LyPhai
      {
         get => lyPhai;
         set
         {
            lyPhai = value;
            DrawPath();
            OnPropertyChanged();
         }
      }

      public string Name => $"Count:{SelectedNumberOfRebar}-D{BarDiameter.BarDiameter().FootToMm()}-S:{Start}-E:{End}";

      public IPageViewModel HostViewModel => IsTop ? BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel : BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel;
      public System.Guid GuidId { get; set; }

      public MainRebar(int start, int end, int layer, bool isTop = false)
      {
         GuidId = System.Guid.NewGuid();
         DimensionUiModel.RebarModel = this;
         BarDiameter = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.TopMainBarDiameter;

         if (!isTop)
         {
            BarDiameter = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.BotMainBarDiameter;
         }
         IsTop = isTop;
         _end = end;
         _start = start;
         _layer = layer;
         for (int i = start; i < End; i++)
         {
            Starts.Add(i);
         }
         for (int i = start + 1; i <= End; i++)
         {
            Ends.Add(i);
         }
         var spanModel = start.GetSpanModelByIndex();
         var rebarSetting = spanModel.GetRebarQuantityByWidth();
         foreach (var n in rebarSetting.MaxBars)
         {
            if (n >= 1)
            {
               NumberOfRebarList.Add(n);
            }
         }

         GetDataLength(IsTop);
         DrawPath();
         SetIndexes();
         selectedNumberOfRebar = rebarPointsInSection.Count(x => x.Checked);
         PositionInSectionCommand = new RelayCommand(x => PositionInSection());
      }

      public void DrawPath()
      {
         DimensionUiModel.RemoveFromUi();
         if (End > 0)
         {
            Curves = IsTop ? BeamRebarServices.GetMainBarCurves(this, false, -1) : BeamRebarServices.GetMainBarCurves(this, false, 1);
            DimensionUiModel.RebarPath = Curves.ConvertCurvesToPath();

            DrawDimension(true);
         }
      }

      private void SetIndexes()
      {
         rebarPointsInSection.Clear();
         BeamRebarCommonService.ArrangeRebar();
      }

      public void RaiseNumber()
      {
         OnPropertyChanged(nameof(SelectedNumberOfRebar));
      }

      private void DrawDimension(bool show = false)
      {
         DimensionUiModel.RemoveFromUi();
         var line1 = Curves.FirstOrDefault();
         var line2 = Curves.LastOrDefault();

         var sp = line1.SP().ConvertToMainViewPoint();
         var ep = line2.EP().ConvertToMainViewPoint();
         var yLDimLine = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 300.MmToFoot())
             .ConvertToMainViewPoint().Y;
         var yEnd = new XYZ(0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 50.MmToFoot())
             .ConvertToMainViewPoint().Y;
         var xs = new List<double>() { sp.X, ep.X };

         var startSupport = Start.GetSupportModelByIndex();
         if (startSupport != null)
         {
            var right = startSupport.TopRight.ConvertToMainViewPoint();
            var left = startSupport.TopLeft.ConvertToMainViewPoint();
            xs.Add(right.X);
            xs.Add(left.X);
         }

         var endSupport = End.GetSupportModelByIndex();
         if (endSupport != null)
         {
            var left = endSupport.TopLeft.ConvertToMainViewPoint();
            var right = endSupport.TopRight.ConvertToMainViewPoint();
            xs.Add(right.X);
            xs.Add(left.X);
         }
         xs.Sort();

         DimensionUiModel.DimensionPaths.Add(BeamRebarUiServices.DrawHorizontalDimension(xs, yEnd, yLDimLine, out var labels, isDimOverall: false));
         DimensionUiModel.Labels.AddRange(labels);

         //Draw Dim dọc

         if (line1.Direction().IsParallel(XYZ.BasisZ))
         {
            //
            var ys = new List<double>();
            ys.Add(line1.SP().ConvertToMainViewPoint().Y);
            ys.Add(line1.EP().ConvertToMainViewPoint().Y);
            var x = line1.SP().ConvertToMainViewPoint().X - 20;
            var xDimLine = x - 20;
            DimensionUiModel.DimensionPaths.Add(BeamRebarUiServices.DrawVerticalDimension(ys, x, xDimLine, out var labels2, isDimOverall: false));
            DimensionUiModel.Labels.AddRange(labels2);
         }
         if (line2.Direction().IsParallel(XYZ.BasisZ))
         {
            //
            var ys = new List<double>();
            ys.Add(line2.SP().ConvertToMainViewPoint().Y);
            ys.Add(line2.EP().ConvertToMainViewPoint().Y);
            var x = line2.SP().ConvertToMainViewPoint().X + 20;
            var xDimLine = x + 20;
            DimensionUiModel.DimensionPaths.Add(BeamRebarUiServices.DrawVerticalDimension(ys, x, xDimLine, out var labels2, isDimOverall: false));
            DimensionUiModel.Labels.AddRange(labels2);
         }

         DimensionUiModel.AddToUiGrid();

         DimensionUiModel.ShowHideDim(show);
      }

      private void GetDataLength(bool isTop)
      {
         lxTrai = 0.0;
         lyTrai = 0.0;
         lxPhai = 0.0;
         lyPhai = 0.0;
        
         var startSupport = Start.GetSupportModelByIndex();
         var endSupport = End.GetSupportModelByIndex();
         var startSpan = Start.GetSpanModelByIndex();
         var endSpan = (End - 1).GetSpanModelByIndex();
         var DStirr = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.StirrupBarDiameter.BarDiameter();
         var cover = BeamRebarRevitData.Instance.BeamRebarCover;
         var n = BeamRebarServices.GetNBySupport(startSpan.LeftSupportModel, IsTop);
         var diameterFoot = BarDiameter.BarDiameter();
         var nD = (n * diameterFoot);
         var nDchaythang = 6* diameterFoot;
         var isNeoThepTheoQuyDinh = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.NeoThepTheoQuyDinh;
         double BeMoc = 0;
         var BM = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.BeMocToiThieu.MmToFoot();
         if (BM != null)
         {
            BeMoc = BM;
         }

        if (startSupport == null || startSpan.LeftSupportModel == null)
        {
            // 🟧 Gối trái không tồn tại → đầu tự do
            if (IsTop) // 🔼 Trường hợp THÉP LỚP TRÊN tại đầu trái
            {
                lxTrai = -cover - diameterFoot / 2;
                lyTrai = startSpan.Height - 2 * cover - 2 *diameterFoot;
            }
            else // 🔽 Trường hợp THÉP LỚP DƯỚI tại đầu trái
            {
                lxTrai = -cover - diameterFoot / 2;
                lyTrai = 0;
            }
        }
        else
        {
            if (isNeoThepTheoQuyDinh)
            {
                if (startSupport.Width >= nD + cover)
                {
                    // 🟩 Gối đủ rộng để NÉO THÉP tiêu chuẩn
                    lxTrai = nD.SetLengthLessThanByCover(startSupport.Width, cover);
                    if (lyTrai < BeMoc)
                    {
                        var x = BeMoc - lyTrai;
                        lyTrai = BeMoc;
                        lxTrai -= x;
                        if (lxTrai < nDchaythang)
                        {
                            lxTrai = nDchaythang;
                        }
                    }
                }
                else
                {
                    // 🟨 Gối hẹp → kiểm tra có thể kéo thép xuyên qua nhịp trái?
                    if (startSpan.CoTheNhanThepTopAtLeft && IsTop || startSpan.CoTheNhanThepBottomAtLeft && !IsTop)
                    {
                        lxTrai = nD;
                    }
                    else
                    {
                        if (isTop)
                        {
                            lxTrai = startSupport.Width - cover - diameterFoot / 2;
                        }
                        else
                        {
                            lxTrai = startSupport.Width - cover - diameterFoot;
                        }
                        lyTrai = (nD - lxTrai).SetZeroIfLess();
                        if (lyTrai < BeMoc)
                        {
                            var x = BeMoc - lyTrai;
                            lyTrai = BeMoc;
                            lxTrai -= x;
                            if (lxTrai < nDchaythang)
                            {
                                lxTrai = nDchaythang;
                            }
                        }
                    }
                }
            }
            else
            {
                // 🟫 Trường hợp KHÔNG NÉO theo quy định, lấy cấu tạo
                if (IsTop) // 🔼 THÉP LỚP TRÊN tại gối trái
                {
                    lxTrai = startSupport.Width - cover - diameterFoot / 2;
                    lyTrai = startSpan.Height - 2 * cover - diameterFoot * 2 ;
                }
                else // 🔽 THÉP LỚP DƯỚI tại gối trái
                {
                    lxTrai = (startSupport.Width - cover - diameterFoot/2);
                    lyTrai = 0;

                }
            }
        }

        // 🔁 Tương tự cho gối phải:
        if (endSupport == null || endSpan.RightSupportModel == null)
        {
            // 🟧 Gối phải không tồn tại → đầu tự do
            if (IsTop) // 🔼 THÉP LỚP TRÊN tại đầu phải
            {
                lxPhai = -cover - diameterFoot / 2;
                lyPhai = (startSpan.Height - 2 * cover - 2 * diameterFoot).SetZeroIfLess();
            }
            else // 🔽 THÉP LỚP DƯỚI tại đầu phải
            {
                lxPhai = -cover - diameterFoot/2;
                lyPhai = 0;
            }
        }
        else
        {
            if (isNeoThepTheoQuyDinh)
            {
                if (endSupport.Width > nD + cover)
                {
                    // 🟩 Gối phải đủ rộng để neo tiêu chuẩn
                    lxPhai = nD.SetLengthLessThanByCover(endSupport.Width, cover);
                    if (lyPhai < BeMoc)
                    {
                        var x = BeMoc - lyPhai;
                        lyPhai = BeMoc;
                        lxPhai -= x;
                        if (lxPhai < nDchaythang)
                        {
                            lxPhai = nDchaythang;
                        }
                    }
                }
                else
                {
                    // 🟨 Gối phải hẹp → kiểm tra có thể kéo xuyên phải
                    if (endSpan.CoTheNhanThepTopAtRight && IsTop || endSpan.CoTheNhanThepBottomAtRight && !IsTop)
                    {
                        lxPhai = nD;
                        if (lyPhai < BeMoc)
                        {
                            var x = BeMoc - lyPhai;
                            lyPhai = BeMoc;
                            lxPhai -= x;
                            if (lxPhai < nDchaythang)
                            {
                                lxPhai = nDchaythang;
                            }
                        }
                    }
                    else
                    {
                        if (isTop)
                        {
                            lxPhai = (endSupport.Width - cover - diameterFoot / 2);
                        }
                        else
                        {
                            lxPhai = (endSupport.Width - cover - diameterFoot );
                        }
                        lyPhai = (nD - lxPhai).SetZeroIfLess();
                        if (lyPhai < BeMoc)
                        {
                            var x = BeMoc - lyPhai;
                            lyPhai = BeMoc;
                            lxPhai -= x;
                            if (lxPhai < nDchaythang)
                            {
                                lxPhai = nDchaythang;
                            }
                        }
                    }
                }
            }
            else
            {
                if (IsTop) // 🔼 THÉP LỚP TRÊN tại gối phải
                {
                    lxPhai = endSupport.Width - cover - diameterFoot / 2;
                    lyPhai = startSpan.Height - 2 * cover - 2 * diameterFoot;
                }
                else // 🔽 THÉP LỚP DƯỚI tại gối phải
                {
                    lxPhai = (endSupport.Width - cover - diameterFoot /2);
                    lyPhai = 0;
                }
            }
        }

         OnPropertyChanged(nameof(LxTrai));
         OnPropertyChanged(nameof(LxPhai));
         OnPropertyChanged(nameof(LyTrai));
         OnPropertyChanged(nameof(LyPhai));
      }
   }
}