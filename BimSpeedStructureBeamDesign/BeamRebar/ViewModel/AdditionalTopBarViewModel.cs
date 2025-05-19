using System.Windows.Controls;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public class AdditionalTopBarViewModel : ViewModelBase, IPageViewModel
   {
      private bool _showSection = false;
      public bool IsShowSection
      {
         get => _showSection;
         set
         {
            _showSection = value;
            OnPropertyChanged();
         }
      }
      private TopAdditionalBar selectedBar;
      private bool _isSelected = false;
      public bool IsSelected
      {
         get => _isSelected;
         set
         {
            _isSelected = value;
            OnPropertyChanged();
         }
      }
      public List<TopAdditionalBar> AllBars { get; set; } = new();

      public TopAdditionalBar SelectedBar
      {
         get => selectedBar;
         set
         {
            foreach (var bar in AllBars)
            {

               bar.DimensionUiModel.ShowHideRebar();
               bar.DimensionUiModel.ShowHideDim(false);

               bar.RebarPointsInSection.ForEach(x =>
               {
                  x.Paths.ForEach(y => y.StrokeThickness = 1);
               });
            }

            if (selectedBar != null)
            {
               if (AllBars.Contains(selectedBar))
               {
                  selectedBar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
                  selectedBar.DimensionUiModel.ShowHideRebar(true);
               }
               else
               {
                  selectedBar.DimensionUiModel.RemoveFromUi();

               }
            }
            selectedBar = value;
            if (selectedBar != null)
            {
               selectedBar.DimensionUiModel.RebarPath.Stroke = Define.RebarPreviewColor;
               selectedBar.DimensionUiModel.ShowHideDim();
               selectedBar.DimensionUiModel.ShowHideRebar();

               selectedBar.RebarPointsInSection.ForEach(x =>
               {
                  x.Paths.ForEach(y => y.StrokeThickness = 5);
               });
            }
            OnPropertyChanged(nameof(AllBars));
            OnPropertyChanged();
         }
      }

      public BeamRebarRevitData BeamRebarRevitData { get; set; } = BeamRebarRevitData.Instance;
      public RelayCommand AddRebarCommand { get; set; }
      public RelayCommand NextRebarCommand { get; set; }
      public RelayCommand DeleteRebarCommand { get; set; }
      public RelayCommand DeleteAllCommand { get; set; }
      public BeamModel BeamModel { get; set; }

      public AdditionalTopBarViewModel()
      {
         GetData();
      }

      private void GetData()
      {
         AddRebarCommand = new RelayCommand(x => AddRebar());
         NextRebarCommand = new RelayCommand(x => DrawNextRebar());
         DeleteRebarCommand = new RelayCommand(DeleteRebar);
         DeleteAllCommand = new RelayCommand(x => DeleteAllRebar());
      }

      private void AddRebar()
      {
         if (SelectedBar != null)
         {
            if (AllBars.Contains(SelectedBar) == false)
            {
               SelectedBar.SetStartsEndsLayersFixed();
               AllBars.Add(SelectedBar);
               SelectedBar = null;
            }
         }
         else
         {
            SelectedBar = GetNextPreviewRebar();
            if (selectedBar != null)
            {
               if (AllBars.Contains(SelectedBar) == false)
               {
                  SelectedBar.SetStartsEndsLayersFixed();
                  AllBars.Add(SelectedBar);
                  SelectedBar = null;
               }
            }
         }
         AllBars = new List<TopAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
         OnPropertyChanged(nameof(AllBars));
         BeamRebarCommonService.ArrangeRebar();
      }

      private void DrawNextRebar()
      {
         SelectedBar = GetNextPreviewRebar();

         BeamRebarCommonService.ArrangeRebar();
      }

      private void DeleteRebar(object obj)
      {
         if (obj is ListView lv)
         {
            selectedBar?.RemoveUi();
            var selecteds = lv.SelectedItems.Cast<TopAdditionalBar>();
            foreach (var bottomAdditionalBar in selecteds)
            {
               bottomAdditionalBar.RemoveUi();
               AllBars.Remove(bottomAdditionalBar);
               selectedBar = null;
            }

            AllBars = new List<TopAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
            OnPropertyChanged(nameof(SelectedBar));
            OnPropertyChanged(nameof(AllBars));
         }

         if (AllBars.Where(x => x.Layer.Layer == 1).ToList().Count == 0 &&
             BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet == false)
         {
            foreach (var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels.DistinctBy2(x => x.Width.Round2Number()))
            {
               var setting = spanModel.GetRebarQuantityByWidth();
               setting.AddTop2 = 2;
               setting.AddTop3 = 2;
               setting.AddTop1 = 0;
            }
         }

         BeamRebarCommonService.ArrangeRebar();
      }

      private void DeleteAllRebar()
      {
         selectedBar?.RemoveUi();
         foreach (var bottomAdditionalBar in AllBars)
         {
            bottomAdditionalBar.RemoveUi();
         }
         selectedBar = null;
         AllBars = new List<TopAdditionalBar>();

         if (AllBars.Where(x => x.Layer.Layer == 1).ToList().Count == 0 && BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet == false)
         {
            foreach (var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels.DistinctBy2(x => x.Width.Round2Number()))
            {
               var setting = spanModel.GetRebarQuantityByWidth();
               setting.AddTop2 = 2;
               setting.AddTop3 = 2;
               setting.AddTop1 = 0;
            }
         }
         BeamRebarCommonService.ArrangeRebar();
         OnPropertyChanged(nameof(SelectedBar));
         OnPropertyChanged(nameof(AllBars));
      }

      public TopAdditionalBar GetNextPreviewRebar(int spanIndex = -1, int layer = -1)
      {
         if (SelectedBar == null || AllBars.Contains(SelectedBar))
         {
            for (int i = 1; i <= 3; i++)
            {
               if (layer != -1 && layer != i)
               {
                  continue;
               }
               for (var index = 0; index <= BeamModel.SpanModels.Count; index++)
               {
                  if (spanIndex != -1 && spanIndex != index)
                  {
                     continue;
                  }
                  var positions = Service.GetAdditionalTopBarPositionsAtSpan(index, new RebarLayer(i, true));
                  if (positions.Count > 0)
                  {
                     var bar = new TopAdditionalBar(index, i, positions.FirstOrDefault());
                     return bar;
                  }
               }
            }

            for (int i = 1; i <= 3; i++)
            {
               if (layer != -1 && layer != i)
               {
                  continue;
               }
               for (var index = 0; index <= BeamModel.SpanModels.Count; index++)
               {
                  if (spanIndex != -1 && spanIndex != index)
                  {
                     continue;
                  }
                  var positions = Service.GetAdditionalTopBarPositionsAtSpan(index, new RebarLayer(i, false));
                  if (positions.Count > 0)
                  {
                     var bar = new TopAdditionalBar(index, i, positions.FirstOrDefault(), false);
                     return bar;
                  }
               }
            }
            return null;
         }
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
         return SelectedBar;
      }


      public void CheckConflictWhenMainBarUpdate()
      {
         var mainBars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.MainRebars;
         var i = 0;
         if (mainBars.Count == 0)
         {
            //Xóa hết thép và gỡ khỏi Grid
            foreach (var bottomAdditionalBar in AllBars)
            {
               bottomAdditionalBar.DimensionUiModel.RemoveFromUi();
            }
            if (SelectedBar != null)
            {
               SelectedBar.DimensionUiModel.RemoveFromUi();
            }
            SelectedBar = null;
            AllBars.Clear();
         }
         else
         {
            foreach (var mainRebar in mainBars)
            {
               if (mainRebar.SelectedNumberOfRebar == mainRebar.RebarPointsInSection.Count)
               {
                  //Remove bar
                  //var barsToRemove = new List<TopAdditionalBar>();
                  //foreach (var bar in AllBars)
                  //{
                  //    if (bar.Layer.Layer == 1 && bar.Start >= mainRebar.Start && bar.End <= mainRebar.End)
                  //    {
                  //        i++;
                  //        RevitData.Grid.Children.Remove(bar.Path);
                  //        barsToRemove.Add(bar);
                  //    }
                  //}
                  //foreach (var topAdditionalBar in barsToRemove)
                  //{
                  //    AllBars.Remove(topAdditionalBar);
                  //}
               }
            }
            AllBars = new List<TopAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
            OnPropertyChanged(nameof(AllBars));
         }
         if (i > 0)
         {
            "AdditionalTopBarViewModel01".NotificationSuccess(this);
         }
      }

      public void QuickGetRebar(QuickBeamRebarSettingViewModel vm)
      {
         DeleteAllRebar();
         var setting = vm.Setting;

         if (vm.IsUseBeamDataInput)
         {

            var spans = BeamModel.SpanModels.OrderBy(x => x.Index).ToList();
            var groupAddTopBar = new List<List<RebarAtPositionOfSpan>>();

            var listAddTopBar = new List<RebarAtPositionOfSpan>();


            foreach (var spanModel in spans)
            {

               if (!listAddTopBar.Any() || spanModel.RebarAtPositionOfSpanStart.AddTop1.IsSame(listAddTopBar.Last().AddTop1))
               {
                  listAddTopBar.Add(spanModel.RebarAtPositionOfSpanStart);

                  //thep gia cuong co xu huong di xuyen nhip - check thep gia cuong tai 3 vi tri giong nhau
                  if (spanModel.RebarAtPositionOfSpanMid.AddTop1.IsSame(listAddTopBar.Last().AddTop1) && spanModel.RebarAtPositionOfSpanEnd.AddTop1.IsSame(listAddTopBar.Last().AddTop1))
                  {
                     listAddTopBar.Add(spanModel.RebarAtPositionOfSpanEnd);
                  }
                  //thep dung tai day
                  else
                  {
                     groupAddTopBar.Add(new List<RebarAtPositionOfSpan>(listAddTopBar));
                     listAddTopBar = new List<RebarAtPositionOfSpan>
                     {
                        spanModel.RebarAtPositionOfSpanEnd
                     };
                  }

               }
               else
               {
                  groupAddTopBar.Add(new List<RebarAtPositionOfSpan>(listAddTopBar));
                  listAddTopBar = new List<RebarAtPositionOfSpan>
                  {
                     spanModel.RebarAtPositionOfSpanStart,
                  };
               }


            }

            foreach (var list in groupAddTopBar)
            {
               var first = list.First();
               var last = list.Last();
               var diameter = first.AddTop1.Diameter;
               var quantity = first.AddTop1.Quantity;

               // viet ham cat thep ra neu giat cap qua lon
               var rebarStartType = first.PositionType == RebarPositionTypeInSpan.Start ? 2 : 1;
               var rebarEndType = last.PositionType == RebarPositionTypeInSpan.Start ? 1 : 2;

               var newRebar = new TopAdditionalBar(first.Index, last.Index, 1, rebarStartType, rebarEndType, true);
               newRebar.BarDiameter = diameter;

               AllBars.Add(newRebar);

            }

         }
         else
         {
            if (vm.Setting.HasTop1)
            {
               for (int i = 0; i < 20; i++)
               {
                  var bar = GetNextPreviewRebar();
                  if (bar == null)
                  {
                     break;
                  }
                  if (bar.Layer.Layer == 1 && setting.HasTop1)
                  {
                     bar.BarDiameter = setting.TopAdditionalBarDiameter1;
                     AllBars.Add(bar);
                  }
                  else if (bar.Layer.Layer == 2 && setting.HasTop2)
                  {
                     bar.BarDiameter = setting.TopAdditionalBarDiameter2;
                     AllBars.Add(bar);
                  }
                  else
                  {
                     bar.DimensionUiModel.RemoveFromUi();
                  }
               }
            }

            if (vm.Setting.HasTop2)
            {
               for (int i = 0; i < 20; i++)
               {
                  var bar = GetNextPreviewRebar(layer: 2);
                  if (bar == null)
                  {
                     break;
                  }

                  if (bar.Layer.Layer == 2 && setting.HasTop2 && bar.IsTop)
                  {
                     bar.BarDiameter = setting.TopAdditionalBarDiameter2;
                     AllBars.Add(bar);
                  }
                  else
                  {
                     bar.DimensionUiModel.RemoveFromUi();
                  }
               }
            }
         }


         foreach (var topAdditionalBar in AllBars)
         {
            topAdditionalBar.SetPathsColor(Define.RebarColor);
         }

         //Remove Thép gia cường ở công sôn
         var firtSpan = BeamRebarRevitData.BeamModel.SpanModels.FirstOrDefault();
         var secondSpan = BeamRebarRevitData.BeamModel.SpanModels.LastOrDefault();

         if (firtSpan != null && firtSpan.LeftSupportModel == null)
         {
            var removeBars = AllBars.Where(x => x.Start == 0 && x.End == 0).ToList();
            foreach (var topAdditionalBar in removeBars)
            {
               topAdditionalBar.DimensionUiModel.RemoveFromUi();
               AllBars.Remove(topAdditionalBar);
            }

            {
               var additionalTopbars = AllBars.Where(x => x.Start == 1 && x.RebarStartType == 1).ToList();

               foreach (var additionalTopbar in additionalTopbars)
               {
                  SelectedBar = new TopAdditionalBar(0, 1, additionalTopbar.Layer.Layer)
                  {
                     RebarStartType = 2,
                     RebarEndType = additionalTopbar.RebarEndType,
                     RebarStartTypes = new List<int> { 2 },
                     RebarEndTypes = additionalTopbar.RebarEndTypes,
                     selectedNumberOfRebar = additionalTopbar.SelectedNumberOfRebar,
                     NumberOfRebarList = additionalTopbar.NumberOfRebarList
                  };

                  SelectedBar.SetStartsEndsLayersFixed();
                  AddRebar();
                  AllBars.Remove(additionalTopbar);
                  additionalTopbar.DimensionUiModel.RemoveFromUi();
               }
            }
         }

         if (secondSpan != null && secondSpan.RightSupportModel == null)
         {
            var removeBars = AllBars.Where(x => x.Start == BeamRebarRevitData.BeamModel.SpanModels.Count && x.End == BeamRebarRevitData.BeamModel.SpanModels.Count).ToList();
            foreach (var topAdditionalBar in removeBars)
            {
               topAdditionalBar.DimensionUiModel.RemoveFromUi();

               AllBars.Remove(topAdditionalBar);
            }

            var additionalTopbars = AllBars.Where(x => x.Start == BeamRebarRevitData.BeamModel.SpanModels.Count - 1 && x.RebarEndType == 1).ToList();
            foreach (var additionalTopbar in additionalTopbars)
            {
               additionalTopbar.DimensionUiModel.RemoveFromUi();
               AllBars.Remove(additionalTopbar);

               SelectedBar = new TopAdditionalBar(BeamRebarRevitData.BeamModel.SpanModels.Count - 1, BeamRebarRevitData.BeamModel.SpanModels.Count, additionalTopbar.Layer.Layer, additionalTopbar.RebarStartType)
               {
                  RebarEndType = 2,
                  RebarStartTypes = new List<int>() { additionalTopbar.RebarStartType },
                  RebarEndTypes = new List<int>() { 2 },
                  SelectedNumberOfRebar = additionalTopbar.SelectedNumberOfRebar,
                  NumberOfRebarList = additionalTopbar.NumberOfRebarList
               };
               SelectedBar.DrawPath();
               AddRebar();
            }
         }
      }

      public string Name => "BEAM_REBAR_SIDE_BAR_ADDTOPBAR".GetValueInResources(this);
      public string Image => "Images/Tabs/TAB-ADD-REBAR-TOP.png";
   }
}