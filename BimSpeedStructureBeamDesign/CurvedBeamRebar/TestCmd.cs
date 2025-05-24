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
    public class TestCmd : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);



            var modelCurve = AC.Selection.PickObject(ObjectType.Element, "curve").ToElement() as ModelCurve;

            var curve = modelCurve.GeometryCurve;

            var newCurve=curve.CreateOffset(200.MmToFoot(), XYZ.BasisZ);


           
            using (var tx= new Transaction(AC.Document,"C"))
            {
                tx.Start();

                Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, newCurve.GetEndPoint(0));
                SketchPlane sketchPlane = SketchPlane.Create(AC.Document, plane);

                // Tạo model curve mới
                AC.Document.Create.NewModelCurve(newCurve, sketchPlane);
                tx.Commit();
            }

            return Result.Succeeded;
        }

    }

}
