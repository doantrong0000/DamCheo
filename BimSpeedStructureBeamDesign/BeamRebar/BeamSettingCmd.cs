using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BeamSettingCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var s = "";
            try
            {
                AC.GetInformation(commandData, GetType().Name);
                SharedData.Instance = new SharedData();
                BeamRebarRevitData.Instance = new BeamRebarRevitData();
                var vm = new BeamRebarSettingViewModel();
                var view = new BeamRebarViewSetting() { DataContext = vm };
                view.ShowDialog();
            }
            catch (Exception e)
            {
                s = e.Message + Environment.NewLine + e.StackTrace;
            }

            return Result.Succeeded;
        }
    }
}