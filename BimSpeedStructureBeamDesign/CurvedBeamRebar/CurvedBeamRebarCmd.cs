using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using BimSpeedUtils;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.ViewModels;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Views;
using BimSpeedUtils.Models;
using BimSpeedUtils.LanguageUtils;
using System.Windows;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CurvedBeamRebarCmd : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);
            RebarData.Instance = new RebarData();
            Element beam = null;
            try
            {
               beam = AC.Selection.PickObject(ObjectType.Element, new BeamSelectionFilter(), "Beams...").ToElement();
            }
            catch
            {
                "BEAMREBARCMD_MESSAGE2".NotificationSuccess(this, "You have aborted the pick operation!");
                return Result.Cancelled;
            }

            var vm = new CurvedBeamViewModel(beam);
            var view = new CurvedBeamView(){DataContext = vm};
            view.ShowDialog();
            
            return Result.Succeeded;
        }
    }

}
