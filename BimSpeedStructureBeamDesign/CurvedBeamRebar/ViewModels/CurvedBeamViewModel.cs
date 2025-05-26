using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Models;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Models.RebarModels;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.Views;
using BimSpeedUtils;
using static Autodesk.Revit.DB.SpecTypeId;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.ViewModels
{
    public class CurvedBeamViewModel
    {
        public CurvedBeamView MainView { get; set; }
        public List<int> Numbers { get; set; } = new() { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public List<int> FillNumbers { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public RebarData RebarData { get; set; }

        public bool CheckFillTop { get; set; } = false;
        public bool CheckFill2Top { get; set; } = false;
        public bool CheckFillBot { get; set; } = false;
        public bool CheckFill2Bot { get; set; } = false;

        public RebarBarType MainTop { get; set; }
        public RebarBarType Layer2Top { get; set; }
        public RebarBarType MainBot { get; set; }
        public RebarBarType Layer2Bot { get; set; }
        public int MainTopNumber { get; set; }
        public int Layer2TopNumber { get; set; }
        public int MainBotNumber { get; set; }
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
        public CurvedBeamModel CurvedBeamModel { get; set; }
        public FamilyInstance Beam { get; set; }
        public Element BeamEle { get; set; }
        public RelayCommand CreateCommand { get; set; }

        public CurvedBeamViewModel(Element beam)
        {
            Beam = beam as FamilyInstance;
            BeamEle = beam;
            RebarData = RebarData.Instance;
            CurvedBeamModel = new CurvedBeamModel(Beam);
            CurvedBeamGeometry = CurvedBeamModel.CurvedBeamGeometry;
            Setdata();
            CreateCommand = new RelayCommand(CreateRebar);

        }

        public void Setdata()
        {
            MainTop = RebarData.BarDiameters.FirstOrDefault();
            MainBot = RebarData.BarDiameters.FirstOrDefault();
            MainBotNumber = Numbers.FirstOrDefault();
            MainTopNumber = 3;
            HookXBot = 200;
            HookYBot = 100;
            HookXTop = 200;
            HookYTop = 100;
            Cover = 25;

        }


        public void CreateRebar(object obj)
        {
            if (obj is Window window)
            {
                window.Close();

                ZLayer1Top = CurvedBeamGeometry.TopElevation - Cover.MmToFoot();
                ZLayer2Top = CurvedBeamGeometry.TopElevation - 2 * Cover.MmToFoot();
                ZLayer1Bot = CurvedBeamGeometry.BotElevation + Cover.MmToFoot();
                ZLayer2Bot = CurvedBeamGeometry.BotElevation + 2 * Cover.MmToFoot();
                var XHookLayerTop = HookXTop - Cover;
                var XHookLayerBot = HookXBot - Cover;
                var YHookLayerTop = HookXTop - Cover;
                var YHookLayerBot = HookXBot - Cover;

                using (Transaction tx = new Transaction(AC.Document, "Create Free Form Rebar"))
                {
                    tx.Start();
                 
                    // 1. Lấy thông tin cơ bản
                    Curve beamCurve = CurvedBeamGeometry.BeamCurved;
                    double offset = CurvedBeamGeometry.Width / 2 - Cover.MmToFoot();
                    var Curve = beamCurve.CreateOffset(-offset, XYZ.BasisZ);
                    if (beamCurve is Arc arc)
                    {
                        var Paths = SplitArcIntoThreeWithRatio(arc);
                        CreateStirrupsAlongCurve(Curve, BeamEle, 300, CurvedBeamGeometry.Width,
                            CurvedBeamGeometry.Height, MainTop);
                        //foreach (Curve curve in Paths)
                        //{

                        //    CreateStirrupsAlongCurve(beamCurve, BeamEle, 300, CurvedBeamGeometry.Width,
                        //        CurvedBeamGeometry.Height, MainTop);
                        //}
                 
                    }
                   
                  
                    //TopRebarLayout(Curve,MainTop, MainTopNumber, Cover, HookXTop, ZLayer1Top, HookYTop);
                    //TopRebarLayout(Curve,Layer2Top, Layer2TopNumber, Cover,XHookLayerTop, ZLayer2Top, YHookLayerTop);
                    //BotRebarLayout(Curve, MainBot, MainBotNumber, Cover, HookXBot, ZLayer1Bot, HookYBot);
                    //BotRebarLayout(Curve,Layer2Bot, Layer2BotNumber, Cover, XHookLayerBot, ZLayer2Bot, YHookLayerBot);
                    tx.Commit();
                }

            }
        }

        public void TopRebarLayout(Curve curve, RebarBarType barType, int number, double cover, double hookX, double zLayer, double hookY)
        {
            double offset = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()) / (number - 1);

            double extensionLength = hookX.MmToFoot();
            double extensionHeight = hookY.MmToFoot();
            if (number == 1)
            {
                double offsetPoint = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()) / 2;

                // Tạo curve offset sang phải
                Curve offsetRight = curve.CreateOffset(offsetPoint, XYZ.BasisZ);
                offsetRight = TrimCurveBySolids(offsetRight, CurvedBeamModel.Solids);
                // Kéo dài curve trong mặt phẳng XY (nếu cần)
                var curves = ExtendCurve(offsetRight, extensionLength, zLayer, extensionHeight);

                // Tạo rebar dựa trên curves đã offset
                CreateTopRebar(curves, barType);
            }
            else
            {
                for (int i = 0; i < number; i++)
                {
                    // Tính khoảng cách offset
                    double offsetPoint = i * offset;

                    // Tạo curve offset sang phải
                    Curve offsetRight = curve.CreateOffset(offsetPoint, XYZ.BasisZ);
                    offsetRight = TrimCurveBySolids(offsetRight, CurvedBeamModel.Solids);
                    // Kéo dài curve trong mặt phẳng XY (nếu cần)
                    IList<IList<Curve>> curves = ExtendCurve(offsetRight, extensionLength, zLayer, extensionHeight);

                    // Tạo rebar dựa trên curves đã offset
                    CreateTopRebar(curves, barType);
                }
            }

        }
        public void BotRebarLayout(Curve curve, RebarBarType barType, int number, double cover, double hookX, double zLayer, double hookY)
        {
            double offset = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()) / (number - 1);

            double extensionLength = hookX.MmToFoot();
            double extensionHeight = -hookY.MmToFoot();
            if (number == 1)
            {
                double offsetPoint = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()) / 2;

                // Tạo curve offset sang phải
                Curve offsetRight = curve.CreateOffset(offsetPoint, XYZ.BasisZ);
                offsetRight = TrimCurveBySolids(offsetRight, CurvedBeamModel.Solids);
                // Kéo dài curve trong mặt phẳng XY (nếu cần)
                var curves = ExtendCurve(offsetRight, extensionLength, zLayer, extensionHeight);

                // Tạo rebar dựa trên curves đã offset
                CreateTopRebar(curves, barType);
            }
            else
            {
                for (int i = 0; i < number; i++)
                {
                    // Tính khoảng cách offset
                    double offsetPoint = i * offset;

                    // Tạo curve offset sang phải
                    Curve offsetRight = curve.CreateOffset(offsetPoint, XYZ.BasisZ);
                    offsetRight = TrimCurveBySolids(offsetRight, CurvedBeamModel.Solids);
                    // Kéo dài curve trong mặt phẳng XY (nếu cần)
                    IList<IList<Curve>> curves = ExtendCurve(offsetRight, extensionLength, zLayer, extensionHeight);

                    // Tạo rebar dựa trên curves đã offset
                    CreateTopRebar(curves, barType);
                }
            }

        }
        public IList<IList<Curve>> ExtendCurve(Curve beamCurve, double HookX, double Zlayer, double HookY)
        {
            // Khởi tạo danh sách lồng nhau để trả về
            IList<IList<Curve>> curveSets = new List<IList<Curve>>();
            List<Curve> curves = new List<Curve>();

            // Lấy điểm đầu và cuối gốc
            XYZ startPoint = beamCurve.GetEndPoint(0);
            XYZ endPoint = beamCurve.GetEndPoint(1);

            // Dịch chuyển toàn bộ curve gốc về cao độ Zlayer
            double offsetZ = Zlayer - beamCurve.GetEndPoint(0).Z;
            Transform moveToZ = Transform.CreateTranslation(new XYZ(0, 0, offsetZ));
            Curve movedBeamCurve = beamCurve.CreateTransformed(moveToZ);

            // Nếu cả HookX và HookY đều bằng 0, chỉ trả về movedBeamCurve
            if (HookX == 0 && HookY == 0)
            {
                curves.Add(movedBeamCurve);
                curveSets.Add(curves);
                return curveSets;
            }

            // Tính tiếp tuyến tại hai đầu (chỉ cần nếu HookX hoặc HookY khác 0)
            XYZ tangentStart = null;
            XYZ tangentEnd = null;
            if (HookX != 0 || HookY != 0)
            {
                Transform startDerivatives = beamCurve.ComputeDerivatives(0.0, true);
                Transform endDerivatives = beamCurve.ComputeDerivatives(1.0, true);
                tangentStart = startDerivatives.BasisX.Normalize();
                tangentEnd = endDerivatives.BasisX.Normalize();
                // Đưa tiếp tuyến về mặt phẳng XY
                tangentStart = new XYZ(tangentStart.X, tangentStart.Y, 0).Normalize();
                tangentEnd = new XYZ(tangentEnd.X, tangentEnd.Y, 0).Normalize();
            }

            // Đưa các điểm về mặt phẳng Zlayer
            startPoint = new XYZ(startPoint.X, startPoint.Y, Zlayer);
            endPoint = new XYZ(endPoint.X, endPoint.Y, Zlayer);

            // Xử lý HookX và HookY
            XYZ newStart = startPoint;
            XYZ newEnd = endPoint;

            if (HookX != 0)
            {
                // Tính điểm mở rộng mới trên mặt phẳng XY
                newStart = startPoint - tangentStart * HookX;
                newEnd = endPoint + tangentEnd * HookX;
            }

            // Tính điểm kéo xuống theo Z nếu HookY != 0
            XYZ downStart = newStart;
            XYZ downEnd = newEnd;
            if (HookY != 0)
            {
                downStart = new XYZ(newStart.X, newStart.Y, Zlayer - HookY);
                downEnd = new XYZ(newEnd.X, newEnd.Y, Zlayer - HookY);
            }

            // Tạo các đoạn kéo xuống theo Z nếu HookY != 0
            if (HookY != 0)
            {
                Line downStartSegment = Line.CreateBound(newStart, downStart);
                curves.Add(downStartSegment);
            }

            // Tạo đoạn mở rộng đầu nếu HookX != 0
            if (HookX != 0)
            {
                Line extensionStart = Line.CreateBound(newStart, startPoint);
                curves.Add(extensionStart);
            }

            // Thêm đường cong gốc đã dịch chuyển
            curves.Add(movedBeamCurve);

            // Tạo đoạn mở rộng cuối nếu HookX != 0
            if (HookX != 0)
            {
                Line extensionEnd = Line.CreateBound(endPoint, newEnd);
                curves.Add(extensionEnd);
            }

            // Tạo đoạn kéo xuống cuối nếu HookY != 0
            if (HookY != 0)
            {
                Line downEndSegment = Line.CreateBound(newEnd, downEnd);
                curves.Add(downEndSegment);
            }

            // Thêm danh sách curves vào curveSets
            curveSets.Add(curves);

            return curveSets;
        }
        public void CreateTopRebar(IList<IList<Curve>> curves, RebarBarType barType)
        {
            RebarFreeFormValidationResult validationResult;
            Rebar rebar = Rebar.CreateFreeForm(AC.Document, barType, BeamEle, curves, out validationResult);
        }
        public static Curve TrimCurveBySolids(Curve curve, List<Solid> solids)
        {
            if (solids == null || solids.Count == 0)
            {
                return curve;
            }

            // Gộp solids lại thành 1 solid duy nhất
            Solid unionSolid = solids[0];
            if (solids.Count > 1)
            {
                unionSolid = SolidUtils.Clone(unionSolid);
                for (int i = 1; i < solids.Count; i++)
                {
                    try
                    {
                        BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(
                            unionSolid, solids[i], BooleanOperationsType.Union);
                    }
                    catch
                    {
                        // Bỏ qua nếu không union được
                    }
                }
            }

            // Lấy các đoạn nằm ngoài solid
            SolidCurveIntersection intersection = unionSolid.IntersectWithCurve(
                curve,
                new SolidCurveIntersectionOptions
                {
                    ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside
                });

            Curve longestCurve = null;
            double maxLength = 0;

            if (intersection != null && intersection.SegmentCount > 0)
            {
                for (int i = 0; i < intersection.SegmentCount; i++)
                {
                    Curve segment = intersection.GetCurveSegment(i);
                    if (segment != null)
                    {
                        double len = segment.Length;
                        if (len > maxLength)
                        {
                            maxLength = len;
                            longestCurve = segment;
                        }
                    }
                }
            }

            // Nếu không có đoạn nào nằm ngoài solid, trả về null
            return longestCurve;
        }
        public double LengthTrimExten(double R, double offset)
        {
            return ((R+offset)*offset/R).MmToFoot();
        }
        public double GetCurveRadius(Curve curve)
        {
            // Kiểm tra xem curve có phải là Arc không
            if (curve is Arc arc)
            {
                // Lấy bán kính của Arc
                return arc.Radius;
            }
            else
            {
                // Nếu không phải Arc, trả về giá trị không hợp lệ hoặc throw exception
                throw new Exception("Curve is not an Arc, so it does not have a radius.");
            }
        }
        public List<Curve> SplitArcIntoThreeWithRatio(Arc arc)
        {
            List<Curve> curves = new List<Curve>();

            // Lấy thông số góc bắt đầu và kết thúc của cung
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            // Tính các điểm chia theo tỷ lệ 0.25 : 0.5 : 0.25
            double angle1 = startAngle + totalAngle * 0.25;  // 25% từ đầu
            double angle2 = angle1 + totalAngle * 0.5;       // 50% tiếp theo (tổng 75% từ đầu)

            // Tạo 3 cung con
            Arc arc1 = Arc.Create(arc.Center, arc.Radius, startAngle, angle1, arc.XDirection, arc.Normal);
            Arc arc2 = Arc.Create(arc.Center, arc.Radius, angle1, angle2, arc.XDirection, arc.Normal);
            Arc arc3 = Arc.Create(arc.Center, arc.Radius, angle2, endAngle, arc.XDirection, arc.Normal);

            // Thêm vào danh sách
            curves.Add(arc1);
            curves.Add(arc2);
            curves.Add(arc3);

            return curves;
        }

        public void CreateStirrupsAlongCurve(Curve curve, Element host, double spacing, double width, double height, RebarBarType barType)
        {
            Document doc = AC.Document;
            double totalLength = curve.ApproximateLength;
            int numStirrups = (int)(totalLength / spacing);
            curve = TrimCurveBySolids(curve, CurvedBeamModel.Solids);
            curve = SetCurveElevation(curve, CurvedBeamGeometry.TopElevation - Cover.MmToFoot());
            // Vector tham chiếu ban đầu để dựng hệ trục (không song song với curve)
            XYZ refVec = XYZ.BasisZ;
            
            // Tìm RebarShape kiểu đai
            var shape = new FilteredElementCollector(doc)
                .OfClass(typeof(RebarShape))
                .Cast<RebarShape>()
                .FirstOrDefault(x => x.Name == "M_T1"); // Đổi nếu bạn dùng shape khác

            XYZ tangent = curve.ComputeDerivatives(0.5, true).BasisX.Normalize(); // Tiếp tuyến tại điểm giữa
            XYZ normal = XYZ.BasisZ; // Hoặc lấy từ mặt cắt ngang nếu biết

            // Hệ trục địa phương
            XYZ xVec = tangent;
            var XVecForStirrupBox = xVec * (CurvedBeamGeometry.Width - 2 * Cover.MmToFoot());
            XYZ yVec = normal.CrossProduct(xVec).Normalize(); // Vuông góc với mặt đai

            var YVecForStirrupBox = XYZ.BasisZ * (CurvedBeamGeometry.Height - 2 *Cover.MmToFoot());
            
            // Dựng thép đai
            Rebar rebar = Rebar.CreateFromRebarShape(
                    doc,
                    shape,
                    barType,
                    host,
                    curve.GetEndPoint(0), // Vector vuông góc mặt đai
                    -XVecForStirrupBox, // Hướng đặt thép (tiếp tuyến)
                    -YVecForStirrupBox);

            XYZ start = curve.SP();
            XYZ end = curve.EP() ;
            var plane = BPlane.CreateByNormalAndOrigin(start - end, start);
            var origin = curve.GetEndPoint(0).ProjectOnto(plane);
            rebar.RebarScaleToBox(origin, xVec, yVec);

            rebar.SetRebarLayoutAsMaximumSpacing(spacing.MmToFoot(), 10000.MmToFoot(), false, true, false);

        }
        public Curve SetCurveElevation(Curve originalCurve, double elevation)
        {
            // Lấy điểm đầu và điểm cuối của curve
            XYZ startPoint = originalCurve.GetEndPoint(0);
            XYZ endPoint = originalCurve.GetEndPoint(1);
            double offsetZ = elevation - originalCurve.GetEndPoint(0).Z;
            Transform moveToZ = Transform.CreateTranslation(new XYZ(0, 0, offsetZ));
            Curve movedBeamCurve = originalCurve.CreateTransformed(moveToZ);
            return movedBeamCurve;
        }

    }
}
