using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
   [Transaction(TransactionMode.Manual)]
   [Regeneration(RegenerationOption.Manual)]
   public class ASumLengthCmd : IExternalCommand
   {
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         AC.GetInformation(commandData, GetType().Name);

         var curves = AC.Selection.GetElementIds().Select(x => x.ToElement())
             .Where(x => x is DetailCurve).Cast<DetailCurve>().ToList();

         RebarShopService.CutCurveByRequiredLength(curves[0].GeometryCurve, 2000.MmToFoot(), 800.MmToFoot(), out var c1, out var c2);

         using (var tx = new Transaction(AC.Document, "A"))
         {
            tx.Start();
            AC.Document.Create.NewDetailCurve(AC.ActiveView, c1.SP().CreateLineByPointAndDirection(XYZ.BasisZ));
            AC.Document.Create.NewDetailCurve(AC.ActiveView, c2.SP().CreateLineByPointAndDirection(XYZ.BasisZ));
            AC.Document.Create.NewDetailCurve(AC.ActiveView, c1);
            AC.Document.Create.NewDetailCurve(AC.ActiveView, c2);

            tx.Commit();
         }

         return Result.Succeeded;
      }
   }
}