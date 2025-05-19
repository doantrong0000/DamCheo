using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using BimSpeedUtils;
using MoreLinq;

namespace BimSpeedStructureBeamDesign.BeamDrawing
{
    [Transaction(TransactionMode.Manual)]
    internal class CheckPointCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);

            var rebar = AC.Selection.PickObject(ObjectType.Element).ToElement() as Rebar;

            if (rebar != null)
            {
                var refArr = new ReferenceArray();
                var raps = new List<ReferenceAndPoint>();
                var linesRebars = rebar.Lines(AC.Document.ActiveView);

                foreach (var linesRebar in linesRebars)
                {
                    if (linesRebar.Direction.IsParallel(XYZ.BasisZ))
                    {
                        raps.Add(new ReferenceAndPoint()
                        {
                            Reference = linesRebar.Reference,
                            Point = linesRebar.Origin
                        });
                    }
                }
                raps.DistinctBy(x => x.Point.DotProduct(AC.Document.ActiveView.RightDirection).Round2Number()).ForEach(rap => refArr.Append(rap.Reference));
                var point1 = new XYZ(22.450406896, -10.604645493, 24.123994816);

                using (Transaction ts = new Transaction(AC.Document,"Create dim"))
                {
                    ts.Start();

                    AC.Document.Create.NewDimension(AC.Document.ActiveView,
                        point1.CreateLine(point1.Add(AC.Document.ActiveView.RightDirection * 500.MmToFoot())), refArr);
                    ts.Commit();
                }
            }

            return Result.Succeeded;
        }
    }
}
