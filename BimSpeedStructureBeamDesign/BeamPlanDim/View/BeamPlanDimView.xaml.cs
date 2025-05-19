using System.Windows;
using BimSpeedStructureBeamDesign.BeamPlanDim.ViewModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.View;

public partial class BeamPlanDimView : Window
{
    public BeamPlanDimView()
    {
        InitializeComponent();
    }

    private void BeamPlanDimView_OnClosed(object sender, EventArgs e)
    {
        if (DataContext is BeamPlanDimViewModel viewModel)
        {
            var json = new BeamPlanDimJson();

            json.AllOptionBeams = viewModel.AllOptionBeams;
            json.AllOptionFloors = viewModel.AllOptionFloors;
            json.CenterBeam = viewModel.CenterBeam;
            json.LeftEdgeBeam = viewModel.LeftEdgeBeam;
            json.RightEdgeBeam = viewModel.RightEdgeBeam;
            json.DimLoMo = viewModel.DimLoMo;
            json.DimViTriNgoaiSan = viewModel.DimViTriNgoaiSan;
            json.DimTruc = viewModel.DimTruc;
            json.TichHopDimOffset = viewModel.TichHopDimOffset;
            json.DimType = viewModel.SelectedDimType.Name;
            json.DimViTri2SanChenhCot = viewModel.DimViTri2SanChenhCot;
            
            JsonUtils.SaveSettingToFile(json, Define.BeamPlanDimSettingPath);
        }
    }
}