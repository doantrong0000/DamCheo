using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.Models
{
    public static class SplitArcUtils
    {
        public static List<Curve> SplitArc(this Arc arc, double spacing)
        {
            var curves = new List<Curve>();

            // Đổi spacing từ mm sang feet
            double spacingFeet = spacing.MmToFoot();
            double arcLength = arc.ApproximateLength;

            // 1. Tính số đoạn nguyên (floor)
            int segmentCount = (int)Math.Floor(arcLength / spacingFeet);

            if (segmentCount < 1)
            {
                // Cung quá ngắn, không chia được
                curves.Add(arc);
                return curves;
            }

            // 2. Tính phần dư
            double usedLength = segmentCount * spacingFeet;
            double remainder = arcLength - usedLength;

            // 3. Ngưỡng nhỏ nhất (ví dụ 50% spacing)
            double minRemainder = spacingFeet * 0.5;

            // 4. Tính tổng số đoạn thực tế
            bool hasSmallRemainder = remainder > minRemainder;
            int totalSegments = segmentCount + (hasSmallRemainder ? 1 : 0);

            // Lấy thông số góc
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            // 5. Tính góc cho mỗi đoạn đều nhau (tránh đoạn quá nhỏ)
            double segmentAngle = totalAngle / totalSegments;

            // 6. Sinh các đoạn con
            for (int i = 0; i < totalSegments; i++)
            {
                double currentStart = startAngle + i * segmentAngle;
                double currentEnd = (i == totalSegments - 1 && !hasSmallRemainder)
                                      ? endAngle  // đảm bảo đúng đến cuối cung
                                      : currentStart + segmentAngle;

                Arc segment = Arc.Create(
                    arc.Center,
                    arc.Radius,
                    currentStart,
                    currentEnd,
                    arc.XDirection,
                    arc.Normal);

                curves.Add(segment);
            }
            return curves;
        }
        public static List<Arc> SplitArcIntoThreeWithRatio(this Arc arc)
        {
            var curves = new List<Arc>();

            // Lấy thông số góc
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            // Tính góc chia theo tỷ lệ 0.25 : 0.5 : 0.25
            double angle1 = startAngle + totalAngle * 0.25;
            double angle2 = startAngle + totalAngle * 0.75; // hoặc angle1 + totalAngle*0.5

            // Tạo 3 cung con với xDirection và normal của arc gốc
            Arc arc1 = Arc.Create(
                arc.Center,
                arc.Radius,
                startAngle,
                angle1,
                arc.XDirection,   // <-- giữ nguyên
                arc.Normal);      // <-- giữ nguyên

            Arc arc2 = Arc.Create(
                arc.Center,
                arc.Radius,
                angle1,
                angle2,
                arc.XDirection,
                arc.Normal);

            Arc arc3 = Arc.Create(
                arc.Center,
                arc.Radius,
                angle2,
                endAngle,
                arc.XDirection,
                arc.Normal);
            if (!IsSubArc(arc, arc2, 1e-6))
            {
                arc1 = Arc.Create(
                     arc.Center,
                     arc.Radius,
                     startAngle,
                     angle1,
                     arc.XDirection,   // <-- giữ nguyên
                     -arc.Normal);      // <-- giữ nguyên

                arc2 = Arc.Create(
                     arc.Center,
                     arc.Radius,
                     angle1,
                     angle2,
                     arc.XDirection,
                     -arc.Normal);

                arc3 = Arc.Create(
                    arc.Center,
                    arc.Radius,
                    angle2,
                    endAngle,
                    arc.XDirection,
                    -arc.Normal);
            }
            curves.AddRange(new[] { arc1, arc2, arc3 });
            return curves;
        }
        public static List<Arc> SplitArcGetEndWithRatio(this Arc arc, double Lap)
        {
            var curves = new List<Arc>();

            // Lấy thông số góc
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            double totalAngle = endAngle - startAngle;

            // Tính góc chia theo tỷ lệ 0.25 : 0.5 : 0.25
            double angle1 = startAngle + totalAngle * Lap;
            double angle2 = startAngle + totalAngle * (1-Lap); // hoặc angle1 + totalAngle*0.5

            // Tạo 3 cung con với xDirection và normal của arc gốc
            Arc arc1 = Arc.Create(
                arc.Center,
                arc.Radius,
                startAngle,
                angle1,
                arc.XDirection,   // <-- giữ nguyên
                arc.Normal);      // <-- giữ nguyên

            Arc arc2 = Arc.Create(
                arc.Center,
                arc.Radius,
                angle1,
                angle2,
                arc.XDirection,
                arc.Normal);

            Arc arc3 = Arc.Create(
                arc.Center,
                arc.Radius,
                angle2,
                endAngle,
                arc.XDirection,
                arc.Normal);
            if (!IsSubArc(arc, arc2, 1e-6))
            {
                arc1 = Arc.Create(
                     arc.Center,
                     arc.Radius,
                     startAngle,
                     angle1,
                     arc.XDirection,   // <-- giữ nguyên
                     -arc.Normal);      // <-- giữ nguyên

                arc2 = Arc.Create(
                     arc.Center,
                     arc.Radius,
                     angle1,
                     angle2,
                     arc.XDirection,
                     -arc.Normal);

                arc3 = Arc.Create(
                    arc.Center,
                    arc.Radius,
                    angle2,
                    endAngle,
                    arc.XDirection,
                    -arc.Normal);
            }
            curves.AddRange(new[] { arc1, arc3 });
            return curves;
        }
        public static bool IsSubArc(Arc parentArc, Arc subArc, double tolerance = 1e-6)
        {
            // 1. So sánh đường tròn gốc
            if (!parentArc.Center.IsAlmostEqualTo(subArc.Center, tolerance)) return false;
            if (Math.Abs(parentArc.Radius - subArc.Radius) > tolerance) return false;

            // 2. So sánh mặt phẳng và chiều tham số
            if (!parentArc.XDirection.IsAlmostEqualTo(subArc.XDirection, tolerance)) return false;
            if (!parentArc.Normal.IsAlmostEqualTo(subArc.Normal, tolerance)) return false;

            // 3. Lấy tham số góc bắt đầu/ kết thúc
            double pStart = parentArc.GetEndParameter(0);
            double pEnd = parentArc.GetEndParameter(1);
            double sStart = subArc.GetEndParameter(0);
            double sEnd = subArc.GetEndParameter(1);

            // Chuẩn hóa để pEnd > pStart (không xét wrap‐around)
            if (pEnd < pStart)
                pEnd += 2 * Math.PI;

            // Chuẩn hóa các góc subArc vào cùng khoảng
            if (sStart < pStart) sStart += 2 * Math.PI;
            if (sEnd < pStart) sEnd += 2 * Math.PI;

            // 4. Kiểm tra subStart ≥ pStart và subEnd ≤ pEnd
            if (sStart + tolerance < pStart) return false;
            if (sEnd - tolerance > pEnd) return false;

            return true;
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
    }
}
