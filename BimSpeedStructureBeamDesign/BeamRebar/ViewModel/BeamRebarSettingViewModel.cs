using System.Collections.ObjectModel;
using System.Windows;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedStructureBeamDesign.BeamDrawing.View;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;
using MoreLinq.Extensions;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public class BeamRebarSettingViewModel : ViewModelBase
   {
      public BeamDrawingSettingViewModel BeamDrawingSettingViewModel { get; set; }

      public List<int> Anchors { get; set; } = new() { 10, 15, 20, 25, 30, 35, 40, 45, 50 };
      public List<int> CrossSectionScales { get; set; } = new() { 20, 25, 30, 35, 40 };
      public List<int> HorizontalSectionScales { get; set; } = new() { 25, 30, 35, 40, 50, 70 };
      public BeamRebarRevitData BeamRebarRevitData { get; set; }

      private string path = AC.BimSpeedSettingPath + "\\BeamRebarSetting.json";
      private NumberOfRebarByWidth selected;

      public NumberOfRebarByWidth Selected
      {
         get => selected;
         set
         {
            selected = value;
            OnPropertyChanged();
         }
      }

      public BeamRebarSettingJson Setting { get; set; }
      public RelayCommand SaveCommand { get; set; }
      public RelayCommand ModifyCommand { get; set; }
      public RelayCommand DefaultCommand { get; set; }
      public RelayCommand SettingDrawingCommand { get; set; }

      public BeamRebarSettingViewModel()
      {
         BeamRebarRevitData = BeamRebarRevitData.Instance;
         var conKeThep = new ConKeThepModel
         {
            ConKeThepInfo = new DiameterAndSpacingModel { Diameter = 25.GetRebarBarTypeByNumber(findBestMatchIfNull: true), Spacing = 2000.MmToFoot(), DiameterInt = 25 },
            ConKeDaiMocInfo = new DiameterAndSpacingModel { Diameter = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true), Spacing = 2000.MmToFoot(), DiameterInt = 8 },
            IsConKeBangCotThep = true
         };

         Setting = JsonUtils.GetSettingFromFile<BeamRebarSettingJson>(path) ?? new BeamRebarSettingJson
         {
            AnchorRebarSettingForBeam = new AnchorRebarSetting { Bot = 10, Top = 30 },
            AnchorRebarSettingForColumn = new AnchorRebarSetting { Bot = 10, Top = 30 },
            AnchorRebarSettingForWall = new AnchorRebarSetting { Bot = 10, Top = 30 },
            AnchorRebarSettingForFoundation = new AnchorRebarSetting { Bot = 10, Top = 30 },
            ConKeThep = conKeThep,
            DuongKinhThepCauTaoLopTren = 14.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
            ChieuDaiDoanNoiThepCauTaoLopTren = 400.MmToFoot(),
            ThepCauTaoGiuaDamModel = new ThepCauTaoGiuaDamModel { BarDiameterForBarGoInColumn = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true), LengthGoInColumn = 100.MmToFoot(), BarDiameter = 14.GetRebarBarTypeByNumber(findBestMatchIfNull: true) },
            DuongKinhCotThepDaiBoXung = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
            KhoangCachCotThepDaiBoXung = 400.MmToFoot(),
            ThepNhip = 8,
            ThepGoiLop1 = 3,
            Rounding = 3,
            ThepGoiLop2 = 4,
            ThepGoiLop3 = 5,
            ThepNhipWithD = 0,
            ThepGoiLop1WithD = 0,
            ThepGoiLop2WithD = 0,
            ThepGoiLop3WithD = 0,
            KhoangCachDaiGiaCuong = 50.MmToFoot(),
            DuongKinhDaiGiaCuong = 8,
            SoLuongDaiGiaCuong = 5,
            DoanKeoDai2Ben = 300.MmToFoot(),
            SoLuongThepVaiBo = 2,
            BeMocToiThieu = 0.MmToFoot(),
            TaoThepVaiBoDamPhu = true,
            DuongKhiThepVaiBo = 16.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
            KhoangGiatCapDuocNhanThep = 50.MmToFoot(),
            NumberOfRebarByWidths = new ObservableCollection<NumberOfRebarByWidth>(BeamRebarCommonService.GetNumberOfRebarByWidthsDefault())
         };

         if (Setting.ConKeThep.ConKeThepInfo.Diameter == null)
         {
            Setting.ConKeThep.ConKeThepInfo.Diameter =
               Setting.ConKeThep.ConKeThepInfo.DiameterInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }

         if (Setting.ConKeThep.ConKeDaiMocInfo.Diameter == null)
         {
            Setting.ConKeThep.ConKeDaiMocInfo.Diameter =
               Setting.ConKeThep.ConKeDaiMocInfo.DiameterInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }


         if (Setting.DuongKhiThepVaiBo == null)
         {
            Setting.DuongKhiThepVaiBo =
               Setting.DuongKhiThepVaiBoInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }


         if (Setting.ThepCauTaoGiuaDamModel.BarDiameterForBarGoInColumn == null)
         {
            Setting.ThepCauTaoGiuaDamModel.BarDiameterForBarGoInColumn =
               Setting.ThepCauTaoGiuaDamModel.BarDiameterForBarGoInColumnInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }


         if (Setting.ThepCauTaoGiuaDamModel.BarDiameter == null)
         {
            Setting.ThepCauTaoGiuaDamModel.BarDiameter =
               Setting.ThepCauTaoGiuaDamModel.BarDiameterInt.GetRebarBarTypeByNumber(
                  findBestMatchIfNull: true);
         }


         if (Setting.AnchorRebarSettingForFoundation == null)
         {
            Setting.AnchorRebarSettingForFoundation = new AnchorRebarSetting { Bot = 10, Top = 30 };
         }


         if (Setting.NumberOfRebarByWidths.Count < 2)
         {
            Setting.NumberOfRebarByWidths = new ObservableCollection<NumberOfRebarByWidth>(BeamRebarCommonService.GetNumberOfRebarByWidthsDefault());
         }

         SaveCommand = new RelayCommand(Save);
         ModifyCommand = new RelayCommand(Modify);
         DefaultCommand = new RelayCommand(x => SetDefault());

         SettingDrawingCommand = new RelayCommand(x =>
          {
             var window = new BeamDrawingView { DataContext = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel };
             window.ShowDialog();
          });
      }

      private void Save(object w)
      {
         if (w is Window window)
         {
            window.Close();
         }

         JsonUtils.SaveSettingToFile(Setting, path);
      }

      private void Modify(object obj)
      {
         if (Selected == null)
         {
            "BeamRebarSettingViewModel01_MESSAGE".NotificationError(this);
            return;
         }
         if (obj is string s)
         {
            if (int.TryParse(s, out var n))
            {
               n = Math.Abs(n);
               var index = Setting.NumberOfRebarByWidths.IndexOf(Selected);
               if (index == 0)
               {
                  return;
               }

               if (n > Selected.BMax)
               {
                  "BeamRebarSettingViewModel02_MESSAGE".NotificationError(this);
                  return;
               }
               var previos = Setting.NumberOfRebarByWidths[index - 1];
               if (n > previos.BMin && n < previos.BMax)
               {
                  previos.BMax = n - 1;
                  selected.BMin = n;
               }
            }
            else
            {
               "BeamRebarSettingViewModel02_MESSAGE".NotificationError(this);
            }
         }
         OnPropertyChanged(nameof(Selected));
         OnPropertyChanged(nameof(Setting.NumberOfRebarByWidths));
      }

      private void SetDefault()
      {
         var b = new ObservableCollection<NumberOfRebarByWidth>(BeamRebarCommonService.GetNumberOfRebarByWidthsDefault());
         Setting.NumberOfRebarByWidths.Clear();
         b.ForEach(x => Setting.NumberOfRebarByWidths.Add(x));
      }
   }
}