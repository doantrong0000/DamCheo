using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.View;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [System.Reflection.Obfuscation(ApplyToMembers = true, Exclude = true)]
    public class BeamRebarCutShopCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);

            RebarUtils.CreateCommonRebarBarTypes();

            SharedData.Instance = new SharedData();

            BeamRebarRevitData.Instance = new BeamRebarRevitData();

            var beams = new List<FamilyInstance>();

            if (AC.ActiveView is View3D == false)
            {
                "BEAMREBARCMD_MESSAGE1".NotificationError(this, "message");
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

            var vm = new BeamRebarCutShopViewModel(beams);

            var view = new BeamRebarCutShopView() { DataContext = vm };
            view.ShowDialog();


            return LicenseCheck.CheckCommandCanExecute(GetType().Name, isFree: true);
        }
    }
}
