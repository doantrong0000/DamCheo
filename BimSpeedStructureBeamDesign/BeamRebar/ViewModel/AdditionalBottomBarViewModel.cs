using System.Windows.Controls;
using BimSpeedStructureBeamDesign.BeamRebar.Enums;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
    public class AdditionalBottomBarViewModel : ViewModelBase, IPageViewModel
    {
        private BottomAdditionalBar selectedBar;

        public List<BottomAdditionalBar> AllBars { get; set; } = new();

        public BottomAdditionalBar SelectedBar
        {
            get => selectedBar;
            set
            {
                foreach (var bottomAdditionalBar in AllBars)
                {
                    bottomAdditionalBar.DimensionUiModel.ShowHideDim(false);
                    bottomAdditionalBar.RebarPointsInSection.ForEach(x =>
                    {
                        x.Paths.ForEach(y => y.StrokeThickness = 1);
                    });
                }
                if (selectedBar != null)
                {
                    if (AllBars.Contains(selectedBar))
                    {
                        selectedBar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
                        selectedBar.DimensionUiModel.ShowHideDim(false);

                    }
                    else
                    {
                        selectedBar?.RemoveUi();
                    }
                }
                selectedBar = value;
                if (selectedBar != null)
                {
                    selectedBar.DimensionUiModel.RebarPath.Stroke = Define.RebarPreviewColor;
                    selectedBar.DimensionUiModel.ShowHideDim();

                    selectedBar.RebarPointsInSection.ForEach(x =>
                    {
                        x.Paths.ForEach(y => y.StrokeThickness = 5);
                    });
                    var span = selectedBar.Start.GetSpanModelByIndex();
                    if (span != null)
                    {
                        BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel = span;
                    }
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(AllBars));
            }
        }

        public BeamRebarRevitData BeamRebarRevitData { get; set; } = BeamRebarRevitData.Instance;
        public RelayCommand AddRebarCommand { get; set; }
        public RelayCommand NextRebarCommand { get; set; }
        public RelayCommand DeleteRebarCommand { get; set; }
        public RelayCommand DeleteAllCommand { get; set; }
        public BeamModel BeamModel { get; set; }

        public AdditionalBottomBarViewModel()
        {
            var s = this.GetValueByKey("BEAM_REBAR_SIDE_BAR_ADDBOTBAR");
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

            AllBars = new List<BottomAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
            BeamRebarCommonService.ArrangeRebar();
            OnPropertyChanged(nameof(AllBars));
        }

        private void DrawNextRebar()
        {
            SelectedBar = GetNextPreviewRebar();

            BeamRebarCommonService.ArrangeRebar();

            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel1.DrawMidBar(new List<BottomAdditionalBar>() { SelectedBar });
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel2.DrawMidBar(new List<BottomAdditionalBar>() { SelectedBar });
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.SectionUiModel3.DrawMidBar(new List<BottomAdditionalBar>() { SelectedBar });
        }

        private void DeleteRebar(object obj)
        {
            if (obj is ListView lv)
            {
                var selecteds = lv.SelectedItems.Cast<BottomAdditionalBar>();
                foreach (var bottomAdditionalBar in selecteds)
                {
                    bottomAdditionalBar.RemoveUi();
                    AllBars.Remove(bottomAdditionalBar);
                    selectedBar = null;
                }
                BeamRebarCommonService.ArrangeRebar();
                AllBars = new List<BottomAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
                OnPropertyChanged(nameof(SelectedBar));
                OnPropertyChanged(nameof(AllBars));
            }
            if (AllBars.Where(x => x.Layer.Layer == 1).ToList().Count == 0)
            {
                foreach (var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels.DistinctBy2(x => x.Width.Round2Number()))
                {
                    var setting = spanModel.GetRebarQuantityByWidth();
                    setting.AddBot2 = 2;
                    setting.AddBot3 = 2;
                    setting.AddBot1 = 0;
                }
            }
        }

        private void DeleteAllRebar()
        {
            foreach (var bottomAdditionalBar in AllBars)
            {
                bottomAdditionalBar.RemoveUi();
            }
            selectedBar?.RemoveUi();
            selectedBar = null;
            AllBars = new List<BottomAdditionalBar>();
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
            if (BeamRebarRevitData.Instance.BeamRebarViewModel.IsFirstQuickGet == false)
            {
                BeamRebarRevitData.Instance.BeamRebarViewModel.RebarQuantityManager.RebarQuantityByWidths.ForEach(x =>
                {
                    x.AddBot1 = 0;
                });
            }

            BeamRebarCommonService.ArrangeRebar();
            OnPropertyChanged(nameof(SelectedBar));
            OnPropertyChanged(nameof(AllBars));
        }

        public BottomAdditionalBar GetNextPreviewRebar(int spanIndex = -1, int layer = -1, bool isTop = false)
        {
            if (SelectedBar == null || AllBars.Contains(SelectedBar))
            {
                if (isTop)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        if (layer != -1 && layer != i)
                        {
                            continue;
                        }
                        for (int j = 0; j < BeamModel.SpanModels.Count; j++)
                        {
                            if (spanIndex != -1 && spanIndex != j)
                            {
                                continue;
                            }
                            if (IsSpanNeedRebar(BeamModel.SpanModels[j], AllBars.ToList(), i, true))
                            {
                                var max = j + 1;
                                for (int m = j; m < BeamModel.SpanModels.Count - 1; m++)
                                {
                                    var currentSpan = BeamModel.SpanModels[m];
                                    var nextSpan = BeamModel.SpanModels[m + 1];
                                    if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, false))
                                    {
                                        max++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var bar = new BottomAdditionalBar(j, max, i, false);
                                return bar;
                            }
                        }
                    }

                    for (int i = 1; i < 4; i++)
                    {
                        if (layer != -1 && layer != i)
                        {
                            continue;
                        }
                        for (int j = 0; j < BeamModel.SpanModels.Count; j++)
                        {
                            if (spanIndex != -1 && spanIndex != j)
                            {
                                continue;
                            }
                            if (IsSpanNeedRebar(BeamModel.SpanModels[j], AllBars.ToList(), i))
                            {
                                var max = j + 1;
                                for (int m = j; m < BeamModel.SpanModels.Count - 1; m++)
                                {
                                    var currentSpan = BeamModel.SpanModels[m];
                                    var nextSpan = BeamModel.SpanModels[m + 1];
                                    if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan))
                                    {
                                        max++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var bar = new BottomAdditionalBar(j, max, i);
                                return bar;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < 4; i++)
                    {
                        if (layer != -1 && layer != i)
                        {
                            continue;
                        }
                        for (int j = 0; j < BeamModel.SpanModels.Count; j++)
                        {
                            if (spanIndex != -1 && spanIndex != j)
                            {
                                continue;
                            }
                            if (IsSpanNeedRebar(BeamModel.SpanModels[j], AllBars.ToList(), i))
                            {
                                var max = j + 1;
                                for (int m = j; m < BeamModel.SpanModels.Count - 1; m++)
                                {
                                    var currentSpan = BeamModel.SpanModels[m];
                                    var nextSpan = BeamModel.SpanModels[m + 1];
                                    if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan))
                                    {
                                        max++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var bar = new BottomAdditionalBar(j, max, i);
                                return bar;
                            }
                        }
                    }

                    for (int i = 1; i < 4; i++)
                    {
                        if (layer != -1 && layer != i)
                        {
                            continue;
                        }
                        for (int j = 0; j < BeamModel.SpanModels.Count; j++)
                        {
                            if (spanIndex != -1 && spanIndex != j)
                            {
                                continue;
                            }
                            if (IsSpanNeedRebar(BeamModel.SpanModels[j], AllBars.ToList(), i, true))
                            {
                                var max = j + 1;
                                for (int m = j; m < BeamModel.SpanModels.Count - 1; m++)
                                {
                                    var currentSpan = BeamModel.SpanModels[m];
                                    var nextSpan = BeamModel.SpanModels[m + 1];
                                    if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, false))
                                    {
                                        max++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var bar = new BottomAdditionalBar(j, max, i, false);
                                return bar;
                            }
                        }
                    }
                }
                return null;
            }
            return SelectedBar;
        }

        public bool IsSpanNeedRebar(SpanModel spanModel, List<BottomAdditionalBar> allRebars, int layer = 1, bool isTop = false)
        {
            if (layer == 1)
            {
                var numberOfMainBar = Service.GetNumberOfMainBarBySpanIndex(spanModel.Index);
                var numbers1 = Service.GetListNumberOfAdditionalBottomBar(spanModel.TypeNumberOfRebarByWidth, numberOfMainBar, RebarLayers.LayerOne);
                if (numbers1.Count == 0)
                {
                    return false;
                }
            }
            return !allRebars.Where(x => x.Layer.Layer == layer && x.Layer.IsTop == isTop).Any(x => x.Start <= spanModel.Index && x.End > spanModel.Index);
        }

        public void CheckConflictWhenMainBarUpdate()
        {
            var mainBars = BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars;
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

                    SelectedBar.DimensionUiModel.RemoveFromUi(); ;
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
                        var barsToRemove = new List<BottomAdditionalBar>();
                        foreach (var bar in AllBars)
                        {
                            if (bar.Layer.Layer == 1 && bar.Start >= mainRebar.Start && bar.End <= mainRebar.End)
                            {
                                i++;
                                bar.DimensionUiModel.RemoveFromUi();
                                barsToRemove.Add(bar);
                            }
                        }
                        foreach (var bottomAdditionalBar in barsToRemove)
                        {
                            AllBars.Remove(bottomAdditionalBar);
                        }
                    }
                }
            }
            AllBars = new List<BottomAdditionalBar>(AllBars.OrderBy(x => x.Layer.Name).ThenBy(x => x.Layer.Layer).ThenBy(x => x.Start));
            OnPropertyChanged(nameof(AllBars));
            if (i > 0)
            {
                "AdditionalBottomBarViewModel01".NotificationSuccess(this);
            }
        }

        public void QuickGetRebar(QuickBeamRebarSettingViewModel vm)
        {
            DeleteAllRebar();
            var setting = vm.Setting;
            for (int i = 0; i < 30; i++)
            {
                var bar = GetNextPreviewRebar();

                if (bar == null)
                {
                    break;
                }
                if (bar.Layer.Layer == 3)
                {
                    bar.RemoveUi();
                    break;
                }
                if (bar.Layer.Layer == 1)
                {
                    bar.BarDiameter = setting.BotAdditionBarDiameter1;
                    AllBars.Add(bar);
                }
                else if (bar.Layer.Layer == 2 && setting.HasBot2)
                {
                    bar.BarDiameter = setting.BotAdditionBarDiameter2;
                    AllBars.Add(bar);
                }
                else
                {
                    bar.DimensionUiModel.RemoveFromUi(); ;
                }
            }

            if (setting.HasBot1 == false)
            {
                var layer1Bars = AllBars.Where(x => x.Layer.Layer == 1).ToList();
                layer1Bars.ForEach(x => x.DimensionUiModel.RemoveFromUi());
                foreach (var bottomAdditionalBar in layer1Bars)
                {
                    AllBars.Remove(bottomAdditionalBar);
                }
            }

            foreach (var bar in AllBars)
            {
                bar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
            }

            //Remove Thep gia cuong duoi o congson
            var firstSpan = BeamRebarRevitData.BeamModel.SpanModels.FirstOrDefault();
            var secondSpan = BeamRebarRevitData.BeamModel.SpanModels.LastOrDefault();
            if (firstSpan != null)
            {
                if (firstSpan.LeftSupportModel == null || firstSpan.RightSupportModel == null)
                {
                    //Remove
                    var bars = AllBars.Where(x => x.Start == firstSpan.Index).ToList();
                    if (bars.Count > 0)
                    {
                        foreach (var bar in bars)
                        {
                            AllBars.Remove(bar);
                            bar.DimensionUiModel.RemoveFromUi(); ;
                        }
                    }
                }
            }
            if (secondSpan != null)
            {
                if (secondSpan.LeftSupportModel == null || secondSpan.RightSupportModel == null)
                {
                    //Remove
                    var bars = AllBars.Where(x => x.Start == secondSpan.Index).ToList();
                    if (bars.Count > 0)
                    {
                        foreach (var bar in bars)
                        {
                            AllBars.Remove(bar);
                            bar.DimensionUiModel.RemoveFromUi();
                        }
                    }
                }
            }
        }

        public string Name => "BEAM_REBAR_SIDE_BAR_ADDBOTBAR".GetValueInResources(this);
        public string Image => "Images/Tabs/TAB-ADD-REBAR-BOT.png";

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
    }
}