using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
   public class BeamSectionCmd : IExternalCommand
   {
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         AC.GetInformation(commandData, GetType().Name);
         try
         {
            var views = new List<ViewSection>();
            var view = AC.ActiveView as ViewSection;
            if (view == null)
            {
               views = AC.Selection.PickObjects(ObjectType.Element, new ViewportSelectionFilter(), "Select Viewports...").Select(x => x.ToElement()).Where(x => x is Viewport).Cast<Viewport>()
                   .Select(x => AC.Document.GetElement(x.ViewId)).Where(x => x is ViewSection).Cast<ViewSection>().ToList();
            }
            else
            {
               views.Add(view);
            }

            SharedData.Instance = new SharedData();
            var viewModel = new BeamDrawingSettingViewModel();
            var settingView = new BeamDrawingView() { DataContext = viewModel };
            if (settingView.ShowDialog() == true)
            {
               using var tx = new Transaction(AC.Document, "Beam Drawing");
               tx.Start();

               viewModel.BeamDrawingSetting.BeamSectionSetting.BreakLineSymbol =
                  viewModel.BeamDrawingSetting.BeamDetailSetting.BreakLineSymbol;
               viewModel.ActiveSymbols();
               foreach (var section in views)
               {
                  var sectionHandle = new BeamSectionService(section, viewModel.BeamDrawingSetting.BeamSectionSetting, null);
                  sectionHandle.Run();
               }
               tx.Commit();
            }
         }
         catch (Exception e)
         {
            "BEAMREBARCMD_MESSAGE2".NotificationSuccess(this, "aa");
            return Result.Succeeded;
         }

         return LicenseCheck.CheckCommandCanExecute(GetType().Name);
      }
   }
}