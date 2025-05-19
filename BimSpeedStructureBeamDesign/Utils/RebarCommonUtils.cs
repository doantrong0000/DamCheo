using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Utils;

   public static class RebarCommonUtils
   {
      public static bool IsPointNearSolid(Solid solid, XYZ p, XYZ direct, out Line lineInsideSupport)
      {
         lineInsideSupport = null;
         var center = solid.ComputeCentroid();
         p = p.EditZ(center.Z);
         var sp = p.Add(direct * 50.MmToFoot());
         var ep = p.Add(-direct * 50.MmToFoot());
         var line = Line.CreateBound(sp, ep);
         var solidCurveIntersection = solid.IntersectWithCurve(line,
            new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside });
         if (solidCurveIntersection.Any())
         {
            var sp1 = p.Add(direct * 5000.MmToFoot());
            var ep1 = p.Add(-direct * 5000.MmToFoot());
            var line1 = Line.CreateBound(sp1, ep1);
            var solidCurveIntersection1 = solid.IntersectWithCurve(line1,
               new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside });
            if (solidCurveIntersection1.Any())
            {
               lineInsideSupport = solidCurveIntersection1.GetCurveSegment(0) as Line;
               if (lineInsideSupport != null)
               {
                  lineInsideSupport = Line.CreateBound(lineInsideSupport.SP().EditZ(p.Z),
                     lineInsideSupport.EP().EditZ(p.Z));
                  lineInsideSupport = EditLineByDirection(lineInsideSupport, direct);

                  return true;
               }
            }
         }
         return false;
      }

      public static XYZ EditBeamDirection(XYZ vector)
      {
         if (vector.X < 0)
         {
            vector = -vector;
         }
         if (vector.DotProduct(XYZ.BasisX).IsEqual(0, 0.001))
         {
            if (vector.Y < 0)
            {
               vector = -vector;
            }
         }
         return vector;
      }

      public static Line EditLineByDirection(Line line, XYZ vector)
      {
         var sp = line.SP();
         var ep = line.EP();
         var direct = ep - sp;
         if (direct.DotProduct(vector) < -0.0001)
         {
            return Line.CreateBound(ep, sp);
         }
         return line;
      }

      public static List<Line> TrimLinesBySolids(Line line, List<Solid> solids)
      {
         if (solids.Count == 0)
         {
            return new List<Line> { line };
         }
         var solid = solids[0];
         if (solids.Count > 1)
         {
            var z = line.SP().Z;
            solid = SolidUtils.Clone(solid);
            for (int i = 0; i < solids.Count; i++)
            {
               var s = solids[i];
               try
               {
                  var translation = XYZ.BasisZ * (z - s.ComputeCentroid().Z);
                  var solidZ = SolidUtils.CreateTransformed(s, Transform.CreateTranslation(translation));
                  BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid, solidZ, BooleanOperationsType.Union);
               }
               catch
               {
                  //Ignore
                  var b = 1;
               }
            }
         }
         var curveIntersection = solid.IntersectWithCurve(line,
            new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside });
         var list = curveIntersection.Where(x => x is Line).Cast<Line>().ToList();

         return list;
      }


      public static List<Line> EditLinesByDirectionAndOrdering(List<Line> lines, XYZ vector)
      {
         return lines.Select(x => EditLineByDirection(x, vector)).OrderBy(x => x.Midpoint().DotProduct(vector))
            .ToList();
      }
   }