using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.View;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BeamSectionGeneratorCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);
            try
            {
                var mainViewModel = new BeamSectionGeneratorViewModel();
                var view = new BeamSectionGeneratorView() { DataContext = mainViewModel };
                view.ShowDialog();
                mainViewModel.Save();
            }
            catch (Exception e)
            {

                AC.Log(e.Message);
            }


            return LicenseCheck.CheckCommandCanExecute(GetType().Name);
      }
    }
}