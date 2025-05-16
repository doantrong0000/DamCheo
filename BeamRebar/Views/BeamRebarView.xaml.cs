using BeamRebar.ViewModels;

namespace BeamRebar.Views
{
    public sealed partial class BeamRebarView
    {
        public BeamRebarView(BeamRebarViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}