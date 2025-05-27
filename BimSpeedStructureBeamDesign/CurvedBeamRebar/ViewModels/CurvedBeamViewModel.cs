using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamRebar.Enums;
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
        public List<int> Numbers { get; set; } = new() { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public List<int> FillNumbers { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public RebarData RebarData { get; set; }


        public bool CheckFill2Top { get; set; } = true;
        public bool CheckFill2Bot { get; set; } = true;

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



        public RebarBarType Stirrup { get; set; }
        public double StirrupMidSpacing { get; set; }
        public double StirrupEndSpacing { get; set; }

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
            MainTopNumber = Numbers.FirstOrDefault();
            Layer2Top = RebarData.BarDiameters.FirstOrDefault();
            Layer2Bot = RebarData.BarDiameters.FirstOrDefault();
            Layer2TopNumber = Numbers.FirstOrDefault();
            Layer2BotNumber = Numbers.FirstOrDefault();
            HookXBot = 200;
            HookYBot = 0;
            HookXTop = 200;
            HookYTop = 100;
            StirrupMidSpacing = 200;
            StirrupEndSpacing = 100;
            Stirrup = RebarData.BarDiameters.FirstOrDefault();
            Cover = 25;
        }


        public void CreateRebar(object obj)
        {
            if (obj is Window window)
            {
                window.Close();

                ZLayer1Top = CurvedBeamGeometry.TopElevation - Cover.MmToFoot() - Stirrup.BarDiameter() - MainTop.BarDiameter();
                ZLayer1Bot = CurvedBeamGeometry.BotElevation + Cover.MmToFoot() + Stirrup.BarDiameter() + MainBot.BarDiameter();
                if (Layer2Top != null)
                {
                    ZLayer2Top = CurvedBeamGeometry.TopElevation - 2 * Cover.MmToFoot() - Stirrup.BarDiameter() - MainTop.BarDiameter() - Layer2Top.BarDiameter() / 2;
                }
                if (Layer2Bot != null)
                {
                    ZLayer2Bot = CurvedBeamGeometry.BotElevation + 2 * Cover.MmToFoot() + Stirrup.BarDiameter() + MainBot.BarDiameter() + Layer2Bot.BarDiameter() / 2;
                }
                double XHookLayerBot = HookXBot  - MainBot.BarDiameter() / 2 - Layer2Bot.BarDiameter() / 2;
                double YHookLayerBot = 0;
                double XHookLayerTop = HookXTop  - MainTop.BarDiameter() / 2 - Layer2Top.BarDiameter() / 2;
                double YHookLayerTop = 0;

                if (HookYBot != 0) 
                {
                    XHookLayerBot = HookXBot - Cover - MainBot.BarDiameter() / 2 - Layer2Bot.BarDiameter() / 2;
                    YHookLayerBot = HookYBot + Cover + MainTop.BarDiameter() / 2 + Layer2Top.BarDiameter() / 2;

                }
                if (HookYTop != 0)
                {
                    XHookLayerTop = HookXTop - Cover - MainTop.BarDiameter() / 2 - Layer2Top.BarDiameter() / 2;
                    YHookLayerTop = HookYTop + Cover + MainTop.BarDiameter() / 2 + Layer2Top.BarDiameter() / 2;
                }

            
               
                using (Transaction tx = new Transaction(AC.Document, "Create Free Form Rebar"))
                {
                    tx.Start();
                 
                    Curve beamCurve = CurvedBeamGeometry.BeamCurved;
                    var CurveStirrup = SetCurveElevation(beamCurve, (CurvedBeamGeometry.TopElevation + CurvedBeamGeometry.BotElevation)/2);

                    if (CurveStirrup is Arc arc)
                    {
        
                        var Paths = SplitArcIntoThreeWithRatio(arc);
                        for (int i = 0; i < Paths.Count; i++)
                        {
                            if (i == 0 || i == 2)
                            {
                                var splitCurves = SplitArc(Paths[i], StirrupEndSpacing);
                                foreach (var minicurve in splitCurves)
                                {
                                    CreateStirrupsAlongCurve(minicurve, BeamEle, Stirrup);
                                }
                            }
                            else
                            {
                                var splitCurves = SplitArc(Paths[i], StirrupMidSpacing);
                                foreach (var minicurve in splitCurves)
                                {
                                    CreateStirrupsAlongCurve(minicurve, BeamEle, Stirrup);
                                }
                            }
                        }
                    }

                    double CoverMain = Cover.MmToFoot()+ Stirrup.BarDiameter();
                    double offsetTop = CurvedBeamGeometry.Width / 2 - Cover.MmToFoot() - Stirrup.BarDiameter() -MainTop.BarDiameter()/2;
                    double offsetBottom = CurvedBeamGeometry.Width / 2 - Cover.MmToFoot() - Stirrup.BarDiameter() - MainBot.BarDiameter()/2;
                    var CurveNewTop = beamCurve.CreateOffset(-offsetTop, XYZ.BasisZ);
                    var CurveNewBot = beamCurve.CreateOffset(-offsetBottom, XYZ.BasisZ);
                    TopRebarLayout(CurveNewTop, MainTop, MainTopNumber, Cover, HookXTop, ZLayer1Top, HookYTop);
                    if (CheckFill2Top)
                    {
                        TopRebarLayout(CurveNewTop, Layer2Top, Layer2TopNumber, Cover, XHookLayerTop, ZLayer2Top, YHookLayerTop);
                    }
                    BotRebarLayout(CurveNewBot, MainBot, MainBotNumber, Cover, HookXBot, ZLayer1Bot, HookYBot);
                    if (CheckFill2Bot)
                    {
                        BotRebarLayout(CurveNewBot, Layer2Bot, Layer2BotNumber, Cover, XHookLayerBot, ZLayer2Bot, YHookLayerBot);
                    }
                    tx.Commit();
                }
            }
        }

        public void TopRebarLayout(Curve curve, RebarBarType barType, int number, double cover, double hookX, double zLayer, double hookY)
        {
            double offset = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()- 2*Stirrup.BarDiameter() - barType.BarDiameter()) / (number - 1);

            double extensionLength = hookX.MmToFoot();
            double extensionHeight = hookY.MmToFoot();

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
                CreateMainRebar(curves, barType);
            }

        }
        public void BotRebarLayout( Curve curve, RebarBarType barType, int number, double cover, double hookX, double zLayer, double hookY)
        {
            double offset = (CurvedBeamGeometry.Width - 2 * cover.MmToFoot()- 2*Stirrup.BarDiameter() - barType.BarDiameter()) / (number - 1);

            double extensionLength = hookX.MmToFoot();
            double extensionHeight = -hookY.MmToFoot();

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
                CreateMainRebar(curves, barType);
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
        public void CreateMainRebar(IList<IList<Curve>> curves, RebarBarType barType)
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
        public static List<Arc> SplitArcIntoThreeWithRatio(Arc arc)
        {
            List<Arc> curves = new List<Arc>();

            // Lấy thông số góc bắt đầu và kết thúc của cung
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            var vectoR = GetVectorFromCenterToArcMidpoint(arc); // Tính khoảng cách từ tâm đến điểm giữa theo trục X

            // Tính các điểm chia theo tỷ lệ 0.25 : 0.5 : 0.25
            double angle1 = startAngle + totalAngle * 0.25;  // 25% từ đầu
            double angle2 = angle1 + totalAngle * 0.5;       // 50% tiếp theo (tổng 75% từ đầu)

            // Tạo 3 cung con với cùng hệ tọa độ cục bộ (arc.XDirection và arc.Normal)
            Arc arc1 = Arc.Create(arc.Center, arc.Radius, startAngle, angle1, vectoR, arc.Normal);
            Arc arc2 = Arc.Create(arc.Center, arc.Radius, angle1, angle2, vectoR, arc.Normal);
            Arc arc3 = Arc.Create(arc.Center, arc.Radius, angle2, endAngle, vectoR, arc.Normal);

            // Thêm vào danh sách
            curves.Add(arc1);
            curves.Add(arc2);
            curves.Add(arc3);

            return curves;
        }


        public void CreateStirrupsAlongCurve(Curve curve, Element host, RebarBarType barType)
        {
            Document doc = AC.Document;
            // Tìm RebarShape kiểu đai
            var shape = new FilteredElementCollector(doc)
                .OfClass(typeof(RebarShape))
                .Cast<RebarShape>()
                .FirstOrDefault(x => x.Name == "M_T1"); // Đổi nếu bạn dùng shape khác
            var cover = Cover.MmToFoot(); // Bán kính cover
            var b = CurvedBeamGeometry.Width;
            var h = CurvedBeamGeometry.Height;

            XYZ tieptuyen = GetCurveTangent(curve, 0.5); // Tangent vector at 50% of curve
            XYZ vectoR = GetCurveNormalAtParameter(curve, 0.5); // Normal vector at 50% of curve
            XYZ phaptuyen = tieptuyen.CrossProduct(vectoR).Normalize(); // vuông góc R và Tiếp tuyến

            var MidPoint = GetCurveMidPoint(curve); // Midpoint of the arc
            var Yvec = tieptuyen;  // Tangent direction vector
            var Zvec = phaptuyen;  // Normal direction vector
            var Xvec = vectoR;     // Perpendicular direction vector

            // Calculate 4 corners of the rectangle based on MidPoint and vectors
            var RightTop = MidPoint.Add(Xvec * (b / 2 - cover)).Add(Zvec * (h / 2 - cover));
            var LeftTop = MidPoint.Add(Xvec * (-b / 2 + cover)).Add(Zvec * (h / 2 - cover));
            var RightBot = MidPoint.Add(Xvec * (b / 2 - cover)).Add(Zvec * (-h / 2 + cover));
            var LeftBot = MidPoint.Add(Xvec * (-b / 2 + cover)).Add(Zvec * (-h / 2 + cover));

            var XRebar = RightTop - LeftTop; // Vector theo chiều dài thép
            var YRebar = RightTop - RightBot; // Vector theo chiều cao thép
            // Dựng thép đai
            Rebar rebar = Rebar.CreateFromRebarShape(
                    doc,
                    shape,
                    barType,
                    host,
                    LeftTop, // Vector vuông góc mặt đai
                    -Xvec, // Hướng đặt thép (tiếp tuyến)
                    -Zvec);
           rebar.RebarScaleToBox(LeftTop, XRebar,-YRebar);
        }
        public XYZ GetCurveTangent(Curve curve, double parameter)
        {

            // Lấy vector tiếp tuyến tại điểm
            XYZ tangent = curve.ComputeDerivatives(parameter, normalized: true).BasisX;

            return tangent;
        }
        public XYZ GetCurveNormalAtParameter(Curve curve, double parameter)
        {
            // Kiểm tra xem curve có phải là Arc không
            if (!(curve is Arc arc))
            {
                throw new ArgumentException("Curve must be an Arc.");
            }


            // Lấy vector tiếp tuyến tại điểm
            XYZ tangent = curve.ComputeDerivatives(parameter, normalized: true).BasisX;

            // Lấy pháp tuyến của mặt phẳng chứa cung
            XYZ planeNormal = arc.Normal;

            // Tìm vecto R đi từ tâm ra của cung
            XYZ pointNormal = planeNormal.CrossProduct(tangent).Normalize();

            return pointNormal;
        }
        public XYZ GetCurveMidPoint(Curve curve)
        {
            // Kiểm tra xem curve có phải là Arc không
            if (!(curve is Arc))
            {
                throw new ArgumentException("Curve must be an Arc.");
            }

            // Lấy tham số đầu và cuối của cung
            double startParam = curve.GetEndParameter(0);
            double endParam = curve.GetEndParameter(1);

            // Tính tham số tại điểm giữa
            double midParam = (startParam + endParam) / 2.0;

            // Lấy tọa độ điểm giữa bằng Evaluate
            XYZ midPoint = curve.Evaluate(midParam, normalized: false);

            return midPoint;
        }
        public static XYZ GetArcMidPoint(Arc arc)
        {
            // Lấy tham số đầu và cuối của cung
            double startParam = arc.GetEndParameter(0);
            double endParam = arc.GetEndParameter(1);

            // Tính tham số tại điểm giữa
            double midParam = (startParam + endParam) / 2.0;

            // Lấy tọa độ điểm giữa bằng Evaluate
            XYZ midPoint = arc.Evaluate(midParam, normalized: false);

            return midPoint;
        }
        public static XYZ GetVectorFromCenterToArcMidpoint(Arc arc)
        {
            // Lấy tham số bắt đầu và kết thúc của cung (trong khoảng 0 đến 1)
            double startParam = arc.GetEndParameter(0); // thường là 0
            double endParam = arc.GetEndParameter(1);   // thường là 1

            // Tính tham số giữa (midpoint) trên cung
            double midParam = (startParam + endParam) / 2.0;

            // Tính điểm giữa trên cung theo tham số midParam
            XYZ midPoint = arc.Evaluate(0.5, true);

            // Lấy tâm của cung
            XYZ center = arc.Center;

            // Tính vector từ tâm tới điểm giữa
            XYZ vectorFromCenterToMid = midPoint - center;
            XYZ axis = vectorFromCenterToMid.Normalize();
            return axis;
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
        public List<Curve> SplitArc(Arc arc, double spacing)
        {
            List<Curve> curves = new List<Curve>();
            var numberOfSegments= arc.ApproximateLength / spacing.MmToFoot();
            // Lấy thông số góc bắt đầu và kết thúc của cung
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            // Tính độ dài góc của mỗi đoạn
            double segmentAngle = totalAngle / numberOfSegments;

            // Tạo các cung con
            for (int i = 0; i < numberOfSegments; i++)
            {
                double currentStartAngle = startAngle + i * segmentAngle;
                double currentEndAngle = currentStartAngle + segmentAngle;

                // Tạo cung con
                Arc segment = Arc.Create(
                    arc.Center,
                    arc.Radius,
                    currentStartAngle,
                    currentEndAngle,
                    arc.XDirection,
                    arc.Normal);

                curves.Add(segment);
            }

            return curves;
        }



    }
}
