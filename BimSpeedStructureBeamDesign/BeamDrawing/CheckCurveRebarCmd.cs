using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing;
[Transaction(TransactionMode.Manual)]
public class CheckCurveRebarCmd: IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        AC.GetInformation(commandData, GetType().Name);

        var rebar = AC.Selection.PickObject(ObjectType.Element).ElementId.ToElement() as Rebar;
        
        var cs = rebar.ComputeRebarDrivingCurves();

        MessageBox.Show(cs.Count().ToString());
        
        return Result.Succeeded;
    }
}