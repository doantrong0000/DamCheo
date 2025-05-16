using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BeamRebar.ViewModels;
using BeamRebar.Views;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedUtils;
using Nice3point.Revit.Toolkit.External;
using BuiltInCategory = Autodesk.Revit.DB.BuiltInCategory;

namespace BeamRebar.Commands
{
    /// <summary>
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);

            try
            {
                Reference beamRef = AC.UiDoc.Selection.PickObject(ObjectType.Element, new BeamSelectionFilter(), "Chọn dầm");
                FamilyInstance Beam = AC.Document.GetElement(beamRef) as FamilyInstance;
                Options geomOptions = AC.Application.Create.NewGeometryOptions();
                geomOptions.ComputeReferences = true;
                var beam =new BeamGeometry(Beam);
                var curve2= beam.Beam.GetCurve();
                GeometryElement geomElem = Beam.get_Geometry(geomOptions);

                // Lấy curve từ dầm (bao gồm cả dầm nghiêng)
                Curve beamCurve = curve2;
   

                if (beamCurve == null)
                {
                    TaskDialog.Show("Lỗi", "Không tìm thấy đường dẫn của dầm!");
                    return Result.Failed;
                }

                // 3. Tạo thép dọc chủ
                using (Transaction trans = new Transaction(AC.Document, "Đặt thép dầm"))
                {
                    trans.Start();

                    // Tạo thép dọc (4 thanh)
                    var barType = new FilteredElementCollector(AC.Document)
                        .OfClass(typeof(RebarBarType))
                        .FirstOrDefault(e => e.Name == "M_00") as RebarBarType;

                    RebarShape rebarShape = new FilteredElementCollector(AC.Document)
                        .OfClass(typeof(RebarShape))
                        .FirstOrDefault(e => e.Name == "Standard") as RebarShape;

                    IList<Curve> barCurves = new List<Curve>();
                    for (int i = 0; i < 4; i++)
                    {
                        // Tạo các curve song song với dầm (tùy chỉnh offset theo kích thước dầm)
                        barCurves.Add(beamCurve.CreateOffset(0.1 * (i - 1.5), XYZ.BasisZ));
                    }

                    Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, barType, null, null,
                        Beam, XYZ.BasisY, barCurves, RebarHookOrientation.Left, RebarHookOrientation.Left, 0, 0, null, null, false, false);

                    // 4. Tạo thép đai
                    RebarBarType stirrupType = new FilteredElementCollector(AC.Document)
                        .OfClass(typeof(RebarBarType))
                        .FirstOrDefault(e => e.Name == "BS_M_T1") as RebarBarType;

                    // Khoảng cách thép đai (200mm)
                    double spacing = UnitUtils.ConvertToInternalUnits(200, new ForgeTypeId());

                    //Rebar.CreateFromRebarShape(AC.Document, RebarStyle.Standard, stirrupType, Beam, origin,
                    //    Xvec);

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class BeamSelectionFilter: ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if(element.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralFraming) return true;
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }

    }
}