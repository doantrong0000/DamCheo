using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Models;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Models.RebarModels;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Views;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.ViewModels
{
    public class CurvedBeamViewModel
    {
        public CurvedBeamView MainView { get; set; }
        public List<int> Numbers { get; set; } = new() {2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public List<int> FillNumbers { get; set; } = new() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public RebarData RebarData { get; set; }

        public bool CheckFillTop { get; set; } = false;
        public bool CheckFill2Top { get; set; } = false;
        public bool CheckFillBot { get; set; } = false;
        public bool CheckFill2Bot { get; set; } = false;

        public RebarBarType MainTop { get; set; }
        public RebarBarType FillTop { get; set; }
        public RebarBarType Layer2Top { get; set; }
        public RebarBarType MainBot { get; set; }
        public RebarBarType FillBot { get; set; }
        public RebarBarType Layer2Bot { get; set; }
        public int MainTopNumber { get; set; }
        public int FillTopNumber { get; set; }
        public int Layer2TopNumber { get; set; }
        public int MainBotNumber { get; set; }
        public int FillBotNumber { get; set; }
        public int Layer2BotNumber { get; set; }
        public double HookXTop { get; set; }
        public double HookYTop { get; set; }
        public double HookXBot { get; set; }
        public double HookYBot { get; set; }
        public double Cover { get; set; }

        public double ZLayer1Top { get; set; }
        public double ZLayer2Top { get; set; }
        public double ZLayer1Bot { get; set; }
        public double ZLayer2Bot { get; set; }

        public CurvedBeamGeometry CurvedBeamGeometry { get; set; }
        public CurvedBeamModel CurvedBeamModel{ get; set; }
        public FamilyInstance Beam { get; set; }
        public Element BeamEle { get; set; }
        public RelayCommand CreateCommand { get; set; }
        public CurvedBeamViewModel(Element beam)
        {
            Beam = beam as FamilyInstance;
            BeamEle = beam;
            RebarData = RebarData.Instance;
            CurvedBeamModel= new CurvedBeamModel(Beam);
            CurvedBeamGeometry = CurvedBeamModel.CurvedBeamGeometry;
            Setdata();
            CreateCommand = new RelayCommand(CreateRebar);

        }
   
        public void Setdata()
        {
            MainTop = RebarData.BarDiameters.FirstOrDefault();
            MainBot = RebarData.BarDiameters.FirstOrDefault();
            MainBotNumber = Numbers.FirstOrDefault();
            MainTopNumber = Numbers.FirstOrDefault();
            HookXBot = 25;
            HookYBot = 25;
            HookXTop = 25;
            HookYTop = 25;
            Cover = 25;
            
        }

      
        public void CreateRebar(object obj)
        {
            if (obj is Window window)
            {
                window.Close();

                ZLayer1Top = CurvedBeamGeometry.TopElevation - Cover.MmToFoot();
                ZLayer2Top = CurvedBeamGeometry.TopElevation - 2* Cover.MmToFoot();
                ZLayer1Bot = CurvedBeamGeometry.TopElevation + Cover.MmToFoot();
                ZLayer2Bot = CurvedBeamGeometry.TopElevation + 2* Cover.MmToFoot();


                using (Transaction tx = new Transaction(AC.Document, "Create Free Form Rebar"))
                {
                    tx.Start();

                    // 1. Lấy thông tin cơ bản
                    Curve beamCurve = CurvedBeamModel.CurveBeam;
                    XYZ beamStart = beamCurve.GetEndPoint(0);
                    XYZ beamEnd = beamCurve.GetEndPoint(1);
                    List<Curve> curves = new List<Curve>();


                    double offset = CurvedBeamGeometry.Width/2 - Cover.MmToFoot();
                    double extensionLength = HookXTop.MmToFoot();

                    Curve offsetLeft = beamCurve.CreateOffset(offset, XYZ.BasisZ); // lệch trái
                    curves = ExtendCurveInXYPlane(offsetLeft, extensionLength, ZLayer1Top);
                    CreateTopRebar(curves);

                    List<Curve> curves2 = new List<Curve>();
                    Curve offsetRight = beamCurve.CreateOffset(-offset, XYZ.BasisZ); // lệch phải
                    curves2 = ExtendCurveInXYPlane(offsetRight, extensionLength, ZLayer1Top);
                    CreateTopRebar(curves2);
                    tx.Commit();
                }

            }
        }
        public List<Curve> ExtendCurveInXYPlane( Curve beamCurve, double extensionLength, double Zlayer)
        {
            List<Curve> curves = new List<Curve>();

            // Lấy điểm đầu và cuối gốc
            XYZ startPoint = beamCurve.GetEndPoint(0);
            XYZ endPoint = beamCurve.GetEndPoint(1);

            // Tính tiếp tuyến tại hai đầu (dựa trên đạo hàm của đường cong)
            Transform startDerivatives = beamCurve.ComputeDerivatives(0.0, true);
            Transform endDerivatives = beamCurve.ComputeDerivatives(1.0, true);

            XYZ tangentStart = startDerivatives.BasisX.Normalize();
            XYZ tangentEnd = endDerivatives.BasisX.Normalize();

            // Đưa tiếp tuyến về mặt phẳng XY (Z = 0)
            tangentStart = new XYZ(tangentStart.X, tangentStart.Y, 0).Normalize();
            tangentEnd = new XYZ(tangentEnd.X, tangentEnd.Y, 0).Normalize();

            // Đưa các điểm về mặt phẳng Zlayer
            startPoint = new XYZ(startPoint.X, startPoint.Y, Zlayer);
            endPoint = new XYZ(endPoint.X, endPoint.Y, Zlayer);

            // Tính điểm mở rộng mới
            XYZ newStart = startPoint - tangentStart * extensionLength;
            XYZ newEnd = endPoint + tangentEnd * extensionLength;

            // Tạo các đoạn mở rộng (luôn là Line)
            Line extensionStart = Line.CreateBound(newStart, startPoint);
            Line extensionEnd = Line.CreateBound(endPoint, newEnd);

            // Dịch chuyển toàn bộ curve gốc về cao độ Zlayer
            double offsetZ = Zlayer - beamCurve.GetEndPoint(0).Z;
            Transform moveToZ = Transform.CreateTranslation(new XYZ(0, 0, offsetZ));
            Curve movedBeamCurve = beamCurve.CreateTransformed(moveToZ);

            // Thêm vào danh sách
            curves.Add(extensionStart);
            curves.Add(movedBeamCurve);
            curves.Add(extensionEnd);

            return curves;
        }
        public void CreateTopRebar (List<Curve> curves)
        {
            var hooks = new FilteredElementCollector(AC.Document).OfClass(typeof(RebarHookType))
                      .Cast<RebarHookType>().ToList();
            // 4. Lấy RebarShape phù hợp (hoặc dùng shape có sẵn)
            var hookStart = hooks
                .FirstOrDefault(x => x.Name.Contains("Standard - 90 deg"));
            var hookEnd = hooks
                .FirstOrDefault(x => x.Name.Contains("Standard - 90 deg"));

            var shapes = new FilteredElementCollector(AC.Document).OfClass(typeof(RebarShape))
                .Cast<RebarShape>().ToList();
            // 4. Lấy RebarShape phù hợp (hoặc dùng shape có sẵn)
            RebarShape shape = shapes
                .FirstOrDefault(x => x.Name.Contains("Rebar Shape 7"));
            ElementId endTreatmentTypeIdAtStart = ElementId.InvalidElementId;

            double rotateAngle = -Math.PI / 2; // Góc xoay nếu cần thiết

            // 5. Tạo rebar tự do
            Rebar freeFormRebar = Rebar.CreateFromCurves(
                AC.Document,
                RebarStyle.Standard,
                MainTop, // RebarBarType
                hookStart, // Không dùng hook start
                hookEnd, // Không dùng hook end
                BeamEle, // Host element (beam)
                XYZ.BasisZ, // Normal vector (vuông góc với mặt phẳng đặt thép)
                curves,
                RebarHookOrientation.Left,
                RebarHookOrientation.Left,
                rotateAngle,
                rotateAngle,
                endTreatmentTypeIdAtStart,
                endTreatmentTypeIdAtStart,
                false,
                true);
        }
    }
}
