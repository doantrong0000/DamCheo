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
using BimSpeedUtils.Models;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CurvedBeamRebarCmd : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);

            var beamRef = AC.Selection.PickObject(ObjectType.Element, new BeamSelectionFilter(), "Beams...");
            Element beamElement = AC.Document.GetElement(beamRef);

            // Kiểm tra và ép kiểu sang FamilyInstance
            FamilyInstance beam = beamElement as FamilyInstance;

            RebarBarType _barDiameter = 8.GetRebarBarTypeByNumber();


            var beamCurve = beam.GetCurve();
            List<Curve> curves = new List<Curve>();
            curves.Add(beamCurve);

            double param = 0.5; // Điểm giữa đường cong
            XYZ tangent = beamCurve.ComputeDerivatives(param, true).BasisX.Normalize();

            // Tính vector pháp tuyến
            XYZ normal = tangent.CrossProduct(XYZ.BasisZ);


            var shapes = new FilteredElementCollector(AC.Document).OfClass(typeof(RebarShape)).Cast<RebarShape>().ToList();

            var stirrupShape = shapes.Where(x => x.RebarStyle == RebarStyle.StirrupTie)
                .FirstOrDefault(x => x.Name == "M_00");

            double spacing = 0.2 / 304.8; // Khoảng cách 200mm (chuyển sang feet)
            double cover = 0.05 / 304.8; // Lớp bảo vệ 50mm (chuyển sang feet)
            double width = 0.3 / 304.8; // Chiều rộng dầm (feet)
            double height = 0.5 / 304.8; // Chiều cao dầm (feet)
            int stirrupCount = (int)(beamCurve.Length / spacing); // Số lượng thép đai

            using (Transaction tx = new Transaction(AC.Document, "Create Rebar"))
            {
                tx.Start(); // Bắt đầu giao dịch
                try
                {
                    var newRebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, _barDiameter, null, null, beamElement, normal, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                    tx.Commit(); // Lưu thay đổi
                }
                catch (Exception ex)
                {
                    tx.RollBack(); // Hủy bỏ nếu có lỗi
                    throw new InvalidOperationException("Failed to create rebar: " + ex.Message);
                }
            }
            
            return Result.Succeeded;
        }
    }

    public class BeamModel
    {
        public double H;
        public double B;
        public double L;
        public FamilyInstance FamilyInstance;
        public GeometryElement BeamGeometryElement;
        public XYZ SPTop;
        public XYZ EPTop;
        public XYZ SPBot;
        public XYZ EPBot;
        public XYZ XVecto;
        public XYZ ZVecto;

        public BeamModel(Element Beam, List<FamilyInstance> columns)
        {
            FamilyInstance = Beam as FamilyInstance;
            var solib = FamilyInstance.GetAllSolids().OrderByDescending(x => x.Volume).FirstOrDefault();
            var bb = solib.GetBoundingBox();
            H = bb.Max.Z - bb.Min.Z;
            B = bb.Max.Y - bb.Min.Y;

            XVecto = FamilyInstance.GetTransform().BasisX;
            ZVecto = FamilyInstance.GetTransform().BasisZ;

            var curve = (Beam.Location as LocationCurve).Curve;
            var clone = curve.Clone();
            foreach (var family in columns)
            {
                var columnsolid = family.GetAllSolids().OrderByDescending(x => x.Volume).FirstOrDefault();
                var rs = columnsolid.IntersectWithCurve(curve,
                    new SolidCurveIntersectionOptions()
                        { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside });
                clone = rs.GetCurveSegment(0);
            }
        }

    }
}
