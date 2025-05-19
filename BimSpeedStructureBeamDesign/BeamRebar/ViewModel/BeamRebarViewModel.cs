using System.Windows;
using System.Windows.Input;
using BimSpeedStructureBeamDesign.Beam;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
    public class BeamRebarViewModel : ViewModelBase
    {

        #region Fields

        private ICommand _changePageCommand;

        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        #endregion Fields

        #region Properties / Commands

        public ICommand ChangePageCommand
        {
            get
            {
                return _changePageCommand ??= new RelayCommand(
                   p => ChangeViewModel((IPageViewModel)p),
                   p => p is IPageViewModel);
            }
        }

        public List<IPageViewModel> PageViewModels => _pageViewModels ??= new List<IPageViewModel>();

        public GeometryViewModel GeometryViewModel { get; set; }
        public IPageViewModel CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set
            {
                if (_currentPageViewModel != value)
                {
                    _currentPageViewModel = value;
                    ChangePage();
                    OnPropertyChanged();
                }
            }
        }

        //-------------------------------------------------------------------------------


        #endregion Properties / Commands

        private NumberOfRebarByWidth selectedNumberByWidth;
        public List<NumberOfRebarByWidth> NumberOfRebarByWidths { get; set; } = new();
        public List<string> TypeNumberOfRebarByWidths { get; set; } = new() { Define.OneBar, Define.TwoBars, Define.ThreeBars, Define.FourBars1, Define.FourBars2, Define.FiveBars1, Define.FiveBars2, Define.SeventBars, Define.SeventBars, Define.NineBars };

        public NumberOfRebarByWidth SelectedNumberByWidth
        {
            get => selectedNumberByWidth;
            set
            {
                selectedNumberByWidth = value;
                OnPropertyChanged();
            }
        }

        public MainBarInTopViewModel MainBarInTopViewModel { get; set; }
        public MainBarInBottomViewModel MainBarInBottomViewModel { get; set; }
        public AdditionalBottomBarViewModel AdditionalBottomBarViewModel { get; set; }
        public AdditionalTopBarViewModel AdditionalTopBarViewModel { get; set; }
        public BeamRebarRevitData BeamRebarRevitData { get; set; } = BeamRebarRevitData.Instance;
        public BeamModel BeamModel { get; set; }
        public StirrupTabViewModel StirrupTabViewModel { get; set; }
        public ThepChongPhinhViewModel ThepChongPhinhViewModel { get; set; }
        public BeamRebarSettingViewModel BeamRebarSettingViewModel { get; set; }

        private SpanModel selectedSpanModel;
        public List<SpanModel> SpanModels { get; set; }
        public RebarQuantityManager RebarQuantityManager { get; set; }

        public SpanModel SelectedSpanModel
        {
            get => selectedSpanModel;
            set
            {
                selectedSpanModel = value;
                StirrupTabViewModel.SelectedSpanModel = value;
                if (selectedSpanModel != null)
                {
                    BeamRebarUiServices.SelectSpanByIndex(selectedSpanModel.Index);
                    //Draw Section model
                    selectedSpanModel.DrawSection();
                }
                else
                {
                    BeamRebarUiServices.SelectSpanByIndex(-1);
                }
                OnPropertyChanged();
            }
        }

        #region ICommands

        public RelayCommand OkCommand { get; set; }
        public RelayCommand ToggleCommand { get; set; }

        #endregion ICommands

        public BeamRebarViewModel(BeamModel beamModel)
        {
            BeamModel = beamModel;
            BeamRebarRevitData.Instance.BeamRebarViewModel = this;
            GetData();
            SpanModels = beamModel.SpanModels;
            selectedSpanModel = SpanModels.FirstOrDefault();
            ToggleCommand = new RelayCommand(x => Toggle());

        }

        private void GetData()
        {
            BeamRebarRevitData.NumberOfRebarByWidths = BeamRebarCommonService.GetNumberOfRebarByWidthsDefault();
            SelectedNumberByWidth = NumberOfRebarByWidths.FirstOrDefault();
            GeometryViewModel = new GeometryViewModel() { BeamRebarViewModel = this };
            StirrupTabViewModel = new StirrupTabViewModel(BeamModel);
            ThepChongPhinhViewModel = new ThepChongPhinhViewModel(BeamModel);
            BeamRebarSettingViewModel = BeamRebarRevitData.BeamRebarSettingViewModel;

            BeamRebarRevitData.BeamRebarSettingViewModel = BeamRebarSettingViewModel;

            MainBarInTopViewModel = new MainBarInTopViewModel() { BeamModel = BeamModel };
            MainBarInBottomViewModel = new MainBarInBottomViewModel { BeamModel = BeamModel };
            AdditionalBottomBarViewModel = new AdditionalBottomBarViewModel() { BeamModel = BeamModel };
            AdditionalTopBarViewModel = new AdditionalTopBarViewModel() { BeamModel = BeamModel };
            //PageViewModels.Add(GeometryViewModel);
            PageViewModels.Add(MainBarInTopViewModel);
            PageViewModels.Add(MainBarInBottomViewModel);
            PageViewModels.Add(AdditionalTopBarViewModel);
            PageViewModels.Add(AdditionalBottomBarViewModel);
            PageViewModels.Add(StirrupTabViewModel);
            PageViewModels.Add(ThepChongPhinhViewModel);

            // Set starting page
            _currentPageViewModel = null;
            OkCommand = new RelayCommand(Ok);
        }

        private void Ok(object w)
        {
           
            if (w is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        public bool IsFirstQuickGet { get; set; } = true;

        public void QuickGetRebar(QuickBeamRebarSettingViewModel quickSetting)
        {
            IsFirstQuickGet = true;
            RebarQuantityManager = new RebarQuantityManager();
            StirrupTabViewModel.QuickGetStirrup(quickSetting);
            MainBarInTopViewModel.QuickGetRebar(quickSetting);
            AdditionalTopBarViewModel.QuickGetRebar(quickSetting);
            MainBarInBottomViewModel.QuickGetRebar(quickSetting);
            AdditionalBottomBarViewModel.QuickGetRebar(quickSetting);
            BeamRebarCommonService.ArrangeRebar();
            if (BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.MainTop1 == 1 &&
                BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.MainBot1 == 1
                &&
                BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.HasBot1 == false
                &&
                BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.HasTop1 == false)
            {
                //Xóa đai chính
                //Tạo đai móc
                StirrupTabViewModel.CreateStirrupForLintel();
            }
            IsFirstQuickGet = false;
            foreach (var spanModel in SpanModels)
            {
                spanModel.DrawSectionSymbol();
            }
        }

        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels
               .FirstOrDefault(vm => vm == viewModel);

            foreach (var pageViewModel in PageViewModels)
            {
                pageViewModel.IsSelected = false;
            }
            if (CurrentPageViewModel != null)
            {

                CurrentPageViewModel.IsSelected = true;
            }

        }

        private void ChangePage()
        {
            if (CurrentPageViewModel is AdditionalBottomBarViewModel)
            {
                if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.IsEnoughBar() == false)
                {
                    //"BeamRebarView202_MESSAGE".NotificationError();
                    //TabControl.SelectedIndex = 2;
                }
            }


            if (CurrentPageViewModel is AdditionalTopBarViewModel)
            {
                BeamRebarRevitData.Instance.BeamRebarViewModel.AdditionalTopBarViewModel.CheckConflictWhenMainBarUpdate();
                if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInTopViewModel.IsEnoughBar() == false)
                {
                    //"BeamRebarView202_MESSAGE".NotificationError();
                    // TabControl.SelectedIndex = 4;
                }
            }


            if (CurrentPageViewModel is MainBarInBottomViewModel)
            {
                foreach (var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.BeamModel.SpanModels)
                {
                    if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.MainRebars.Count == 0)
                    {
                        return;
                    }
                    if (BeamRebarRevitData.Instance.BeamRebarViewModel.MainBarInBottomViewModel.IsEnoughBar() == false)
                    {
                        "BeamRebarView202_MESSAGE".NotificationError(this);
                        //TabControl.SelectedIndex = 2;
                    }
                }

            }

            if (CurrentPageViewModel is StirrupTabViewModel)
            {
                BeamRebarRevitData.BeamRebarView2.GroupSection.Visibility = Visibility.Visible;
            }
            else
            {
                BeamRebarRevitData.BeamRebarView2.GroupSection.Visibility = Visibility.Collapsed;
            }

            ShowHideItemNotInTab(CurrentPageViewModel);

            if ((CurrentPageViewModel is StirrupTabViewModel) == false)
            {
                BeamRebarRevitData.BeamRebarView2.GroupSection.Visibility = CurrentPageViewModel.IsShowSection ? Visibility.Visible : Visibility.Collapsed;
            }

        }

        void Toggle()
        {
            if (CurrentPageViewModel is StirrupTabViewModel)
            {

            }
            else
            {
                CurrentPageViewModel.IsShowSection = !CurrentPageViewModel.IsShowSection;

                BeamRebarRevitData.BeamRebarView2.GroupSection.Visibility = CurrentPageViewModel.IsShowSection ? Visibility.Visible : Visibility.Collapsed;
            }

        }

        void ShowHideItemNotInTab(IPageViewModel vm)
        {
            var listToRemove = new List<UIElement>();
            foreach (UIElement item in BeamRebarRevitData.Instance.Grid.Children)
            {
                if (item.GetPropertyValue("Tag") is IRebarModel tag)
                {
                    var itemViewModel = tag.HostViewModel;
                    tag.DimensionUiModel.ShowHideDim(false);
                    if (itemViewModel.GetType().FullName == vm.GetType().FullName)
                    {
                        tag.DimensionUiModel.ShowHideRebar();

                        if (vm is MainBarInTopViewModel viewModel)
                        {
                            if (viewModel.MainRebars.All(x => x.GuidId != tag.GuidId))
                            {
                                listToRemove.Add(item);
                                continue;
                            }

                        }

                        if (vm is MainBarInBottomViewModel mainBarInBottomViewModel)
                        {
                            if (mainBarInBottomViewModel.MainRebars.All(x => x.GuidId != tag.GuidId))
                            {
                                listToRemove.Add(item);
                            }
                        }
                    }
                    else
                    {
                        tag.DimensionUiModel.ShowHideRebar(false);
                    }
                }
            }

            BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.ShowHide(false);

            if (vm is StirrupTabViewModel stirrupTabViewModel)
            {
                stirrupTabViewModel.DimensionStirrups();
            }


            listToRemove.ForEach(x => BeamRebarRevitData.Instance.Grid.Children.Remove(x));
        }

    }
}