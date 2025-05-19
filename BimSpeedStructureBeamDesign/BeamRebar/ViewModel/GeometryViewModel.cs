using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
    public class GeometryViewModel : ViewModelBase, IPageViewModel
    {
        public string Name => this.GetValueByKey("BEAM_REBAR_SIDE_BAR_GEOMETRY");
        public string Image => "geometry.png";
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

        public BeamRebarViewModel BeamRebarViewModel { get; set; }

        public GeometryViewModel()
        {

        }
    }
}