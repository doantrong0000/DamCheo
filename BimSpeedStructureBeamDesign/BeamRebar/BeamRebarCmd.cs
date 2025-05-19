using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.View;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
   [Transaction(TransactionMode.Manual)]
   [Regeneration(RegenerationOption.Manual)]
   public class BeamRebarCmd : IExternalCommand
   {
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         AC.GetInformation(commandData, GetType().Name);

         RebarUtils.CreateCommonRebarBarTypes();

         SharedData.Instance = new SharedData();

         BeamRebarRevitData.Instance = new BeamRebarRevitData();

         List<FamilyInstance> beams ;

         if (AC.ActiveView is View3D == false)
         {
            "BEAMREBARCMD_MESSAGE1".NotificationError(this, "message");
            return Result.Cancelled ;
         }
         try
         {
            beams = AC.Selection.PickObjects(ObjectType.Element, new BeamSelectionFilter(), "Beams...").Select(x => x.ToElement()).Cast<FamilyInstance>().ToList();

            if (BeamRebarCommonService.CheckBeamsValidToPutRebars(beams, out var errorMessage) == false)
            {
               MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK,
                   MessageBoxImage.Error);
               return Result.Cancelled;
            }
         }
         catch
         {
            "BEAMREBARCMD_MESSAGE2".NotificationSuccess(this, "You have aborted the pick operation!");
            return Result.Cancelled;
         }

         if ( !beams.Any() ) return Result.Cancelled ;

         try
         {
            using var tx = new TransactionGroup(AC.Document, "Create Beam Rebars");
            tx.Start();
            var quickSettingViewModel = new QuickBeamRebarSettingViewModel(beams);
            var view1 = new QuickBeamRebarView { DataContext = quickSettingViewModel };
            view1.ShowDialog();
            tx.Assimilate();
         }
         catch (Exception e)
         {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            AC.Log(e.Message);
         }

         BeamRebarRevitData.Instance = null;
         SharedData.Instance = null;

         return LicenseCheck.CheckCommandCanExecute(GetType().Name);
      }
   }
}