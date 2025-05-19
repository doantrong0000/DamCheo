using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class StirrupForSpan : ViewModelBase, ICloneable
   {
      private RebarBarType _barDiameter;
      private RebarBarType _barDiameterDaiMoc;
      private string _typeOfStirrup;
      private int _kieuPhanBoThepDai;
      private int startIndex;
      private int endIndex;
      public SpanModel SpanModel { get; set; }

      public int KieuPhanBoThepDai
      {
         get => _kieuPhanBoThepDai;
         set
         {
            _kieuPhanBoThepDai = value;
            if (_kieuPhanBoThepDai == 1)
            {
               ImageSpacing = Define.PathStirrupSpacing1;
               end1Length = 0;
               end2Length = 0;
            }
            else
            {
               ImageSpacing = Define.PathStirrupSpacing2;
               if (SpanModel != null)
               {
                  end1Length = (SpanModel.Length / 4).RoundMilimet(50);
                  end2Length = end1Length;
               }
            }

            OnPropertyChanged(nameof(End1Length));
            OnPropertyChanged(nameof(End2Length));
            OnPropertyChanged(nameof(ImageSpacing));
            OnPropertyChanged();
            BeamRebarRevitData.Instance.BeamRebarViewModel?.StirrupTabViewModel?.DimensionStirrups();
         }
      }

      public int ShapeDaiChinh
      {
         get => _shapeDaiChinh;
         set
         {
            if (value == _shapeDaiChinh) return;
            _shapeDaiChinh = value;
            OnPropertyChanged();
         }
      }

      public int ShapeDaiPhuChuNhat
      {
         get => _shapeDaiPhuChuNhat;
         set
         {
            if (value == _shapeDaiPhuChuNhat) return;
            _shapeDaiPhuChuNhat = value;
            OnPropertyChanged();
         }
      }

      public int ShapeDaiPhuChuC
      {
         get => _shapeDaiPhuChuC;
         set
         {
            if (value == _shapeDaiPhuChuC) return;
            _shapeDaiPhuChuC = value;
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
         }
      }

      public RebarBarType BarDiameterDaiMoc
      {
         get => _barDiameterDaiMoc;
         set
         {
            _barDiameterDaiMoc = value;
            OnPropertyChanged();
         }
      }

      public double SpacingAtEnd { get; set; } = 100.MmToFoot();

      public double SpacingAtMid { get; set; } = 200.MmToFoot();

      public List<string> TypeOfStirrups { get; set; } = new List<string>();
      public string ImageSpacing { get; set; }

      /// <summary>
      /// Thép đai chính
      /// </summary>
      public List<Rebar> MainStirrupEnd1 { get; set; } = new List<Rebar>();

      public List<Rebar> MainStirrupMid { get; set; } = new List<Rebar>();
      public List<Rebar> MainStirrupEnd2 { get; set; } = new List<Rebar>();

      /// <summary>
      /// Thép đai phụ, đai móc, đang lồng bên trong
      /// </summary>
      public List<Rebar> SecondaryStirrupEnd1 { get; set; } = new List<Rebar>();

      public List<Rebar> SecondaryStirrupMid { get; set; } = new List<Rebar>();
      public List<Rebar> SecondaryStirrupEnd2 { get; set; } = new List<Rebar>();

      public List<int> Indexes
      {
         get
         {
            indexes.Clear();
            for (int i = 1; i <= SpanModel.GetRebarQuantityByWidth().TotalBot1; i++)
            {
               indexes.Add(i);
            }
            return indexes;
         }
         set
         {
            indexes = value;
            OnPropertyChanged();
         }
      }

      public int Max { get; set; }

      public double End2Length
      {
         get => end2Length;
         set
         {
            end2Length = value;
            BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.DimensionStirrups();
            OnPropertyChanged();
         }
      }

      public double End1Length
      {
         get => end1Length;
         set
         {
            end1Length = value;
            BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.DimensionStirrups();
            OnPropertyChanged();
         }
      }

      public int StartIndex
      {
         get => startIndex;
         set
         {
            startIndex = value;
            OnPropertyChanged();
         }
      }

      public int EndIndex
      {
         get => endIndex;
         set
         {
            endIndex = value;
            OnPropertyChanged();
         }
      }

      public List<StirrupModel> StirrupModels
      {
         get => stirrupModels;
         set
         {
            stirrupModels = value;
            OnPropertyChanged();
         }
      }

      public StirrupModel StirrupModel
      {
         get => stirrupModel;
         set
         {
            stirrupModel = value;
            foreach (var model in stirrupModels)
            {
               model.Paths.ForEach(x => x.StrokeThickness = 1);
            }

            if (value != null)
            {
               stirrupModel.Paths.ForEach(x => x.StrokeThickness = 5);
            }

            OnPropertyChanged();
         }
      }

      private int stirrupRadio = 1;
      private List<StirrupModel> stirrupModels = new List<StirrupModel>();
      private StirrupModel stirrupModel;
      private List<int> indexes = new List<int>();
      private double end2Length;
      private double end1Length;
      private int _shapeDaiChinh = 1;
      private int _shapeDaiPhuChuNhat = 1;
      private int _shapeDaiPhuChuC = 1;

      public int StirrupRadio
      {
         get => stirrupRadio;
         set
         {
            stirrupRadio = value;
            OnPropertyChanged();
         }
      }

      public RelayCommand AddStirrupCommand { get; set; }
      public RelayCommand RemoveStirrupCommand { get; set; }
      public RelayCommand ModifyStirrupCommand { get; set; }

      public StirrupForSpan()
      {
         BarDiameter = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true);
         BarDiameterDaiMoc = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true);
         KieuPhanBoThepDai = 1;
         AddStirrupCommand = new RelayCommand(x => AddStirrup(startIndex, endIndex));
         RemoveStirrupCommand = new RelayCommand(x => Remove(), x => stirrupModel != null);
         ModifyStirrupCommand = new RelayCommand(x => Modify(), x => stirrupModel != null);
      }

      public void AddStirrup(int start, int end = -1, bool isByClick = false)
      {
         try
         {
            StartIndex = start;
            EndIndex = end;
            //Check if stirrup exist
            if (stirrupRadio == 1)
            {
               var matched = StirrupModels.FirstOrDefault(x => x.IsDaiMoc && x.StartIndex == startIndex);
               if (matched != null)
               {
                  if (isByClick)
                  {
                     stirrupModels.Remove(matched);
                     StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                     SpanModel.DrawSection();
                     return;
                  }
                  "StirrupForSpan01_MESSAGE".NotificationError(this);
               }
               else
               {
                  var model = new StirrupModel(startIndex);
                  StirrupModels.Add(model);
                  StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                  SpanModel.DrawSection();
               }
            }
            else
            {
               var matched = StirrupModels.FirstOrDefault(x =>
                   x.IsDaiMoc == false && x.StartIndex == startIndex && x.EndIndex == EndIndex);
               if (matched != null)
               {
                  if (isByClick)
                  {
                     stirrupModels.Remove(matched);
                     StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                     SpanModel.DrawSection();
                     return;
                  }
                  "StirrupForSpan02_MESSAGE".NotificationError(this);
               }
               else
               {
                  var model = new StirrupModel(startIndex, endIndex);
                  StirrupModels.Add(model);
                  StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                  SpanModel.DrawSection();
               }
            }


            }
         catch (Exception e)
         {
            AC.Log("Lỗi tạo đai", e);
         }
      }


      public void AddStirrupHorizontal(LabelPointExtension extension, bool isByClick = false)
      {
         try
         {
            StartIndex = extension.Index;
            EndIndex = extension.Index;
            //Check if stirrup exist
            if (stirrupRadio == 3)
            {
               var matched = StirrupModels.FirstOrDefault(x => x.IsDaiMoc && x.StartIndex == startIndex && x.IsHorizontal && x.Location == extension.Location);
               if (matched != null)
               {
                  if (isByClick)
                  {
                     stirrupModels.Remove(matched);
                     StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                     SpanModel.DrawSection();
                     return;
                  }
                  "StirrupForSpan01_MESSAGE".NotificationError(this);
               }
               else
               {
                  var model = new StirrupModel(startIndex, true, extension.Location);
                  StirrupModels.Add(model);
                  StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
                  SpanModel.DrawSection();
               }
            }

            if (startIndex == indexes.Max())
            {
               StartIndex = 1;
            }
            else
            {
               StartIndex++;
            }
         }
         catch (Exception e)
         {
            AC.Log("Lỗi tạo đai", e);
         }
      }

      private void Remove()
      {
         if (StirrupModel != null)
         {
            stirrupModels.Remove(StirrupModel);
            StirrupModels = StirrupModels.OrderBy(x => x.StartIndex).ToList();
            SpanModel.DrawSection();
         }
      }

      private void Modify()
      {
         if (stirrupModel == null)
         {
            Remove();
            AddStirrup(startIndex, endIndex);
         }
      }

      public object Clone()
      {
         return this.MemberwiseClone();
      }
   }
}