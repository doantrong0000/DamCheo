using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.Others;
using BimSpeedStructureBeamDesign.BeamDrawing.View;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing
{
    [Transaction(TransactionMode.Manual)]
    public class BeamDetailCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);
            //try
            {
                var view = AC.ActiveView as ViewSection;
                if (view == null)
                {

                    "BeamDetail01_MESSAGE".NotificationError(this);

                    return Result.Succeeded;
                }
                SharedData.Instance = new SharedData();
                var viewModel = new BeamDrawingSettingViewModel();
                var settingView = new BeamDrawingView() { DataContext = viewModel };
                if (settingView.ShowDialog() == true)
                {
                    using var tx = new Transaction(AC.Document, "Detailing Section");
                    tx.Start();
                    viewModel.ActiveSymbols();
                    try
                    {
                        var service = new BeamDetailService(view, viewModel);
                        service.Run(false);
                    }
                    catch
                    {
                        //
                    }
                    tx.Commit();
                }
            }
            //catch (Exception e)
            //{
            //    return Result.Succeeded;
            //}


            return LicenseCheck.CheckCommandCanExecute(GetType().Name);
        }
    }
}