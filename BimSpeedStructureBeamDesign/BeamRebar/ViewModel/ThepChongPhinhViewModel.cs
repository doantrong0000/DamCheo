
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public class ThepChongPhinhViewModel : ViewModelBase, IPageViewModel
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
      private bool _isSelected = false;
      private int _soLuongLopThepChongPhinh = 0;

      public bool IsSelected
      {
         get => _isSelected;
         set
         {
            _isSelected = value;
            OnPropertyChanged();
         }
      }

      public int SoLuongLopThepChongPhinh
      {
         get => _soLuongLopThepChongPhinh;
         set
         {
            if (value == _soLuongLopThepChongPhinh) return;

            _soLuongLopThepChongPhinh = value;

            //redraw thep chong phinh
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
            OnPropertyChanged();
         }
      }

      public List<int> ListSoLuongThepChongPhinh { get; set; } = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

      public List<RebarChongPhinhModel> ListRebarChongPhinhModels { get; set; } = new();

      public List<SpanModel> SpanModels { get; set; }

      public string Name => "BEAM_REBAR_SIDE_BAR_THEPCHONGPHINH".GetValueInResources(this);
      public string Image => "Images/Tabs/ThepChongPhinh.png";

      public BeamRebarViewModel BeamRebarViewModel { get; set; }
      public ThepChongPhinhViewModel(BeamModel beamModel)
      {
         SpanModels = beamModel?.SpanModels;

         BeamRebarViewModel = BeamRebarRevitData.Instance.BeamRebarViewModel;
      }
   }
}