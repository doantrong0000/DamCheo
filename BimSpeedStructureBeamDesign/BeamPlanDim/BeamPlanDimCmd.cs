using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamPlanDim.View;
using BimSpeedStructureBeamDesign.BeamPlanDim.ViewModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim;

[Transaction(TransactionMode.Manual)]
public class BeamPlanDimCmd: IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;

        UIDocument uidoc = uiapp.ActiveUIDocument;
        AC.GetInformation(uidoc);
        
        var vm = new BeamPlanDimViewModel();
        var view = new BeamPlanDimView() { DataContext = vm };
        vm.MainView = view;
        view.ShowDialog();

        return LicenseCheck.CheckCommandCanExecute(GetType().Name);
   }
}