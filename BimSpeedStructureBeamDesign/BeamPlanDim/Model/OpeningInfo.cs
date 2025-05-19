using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.Model;

public class OpeningInfo
{
    public List<PlanarFace> PlanarFaces = new List<PlanarFace>();

    public List<OpeningModel> ListOpening = new List<OpeningModel>();

    public OpeningInfo(Opening opening)
    {
        var solids = opening.GetSolids();

        foreach (var solid in solids)
        {
            if (solid.Volume > 0)
                foreach (var solidFace in solid.Faces)
                {
                    var planarF = solidFace as PlanarFace;
                    if (planarF == null) continue;
                    if (planarF.Reference == null) continue;
                    PlanarFaces.Add(planarF);
                }
        }
    }

    public OpeningInfo(Floor floor)
    {
        var solids = floor.GetSolids();

        foreach (var solid in solids)
        {
            if (solid.Volume > 0)
            {
                foreach (var solidFace in solid.Faces)
                {
                    var planarF = solidFace as PlanarFace;
                    if (planarF == null) continue;
                    if (planarF.Reference == null) continue;
                    PlanarFaces.Add(planarF);
                }

                var s = SolidUtils.SplitVolumes(solid);

                foreach (var solid1 in s)
                {
                    var planarFaces = new List<PlanarFace>();
                    var faces = solid1.Faces;

                    foreach (var face in faces)
                    {
                        var planarF = face as PlanarFace;
                        if (planarF == null) continue;
                        planarFaces.Add(planarF);
                    }

                    if (planarFaces.Count > 0)
                    {
                        var newPlanarFaces = planarFaces.Where(x => x.FaceNormal.IsParallel(XYZ.BasisZ))
                            .OrderBy(x => x.Origin.Z).ToList();

                        var topPlanarFace = newPlanarFaces.LastOrDefault();

                        var edgeCurveLoop = topPlanarFace.GetEdgesAsCurveLoops();
                        if (edgeCurveLoop.Count > 1)
                        {
                            var chuViLonNhat = edgeCurveLoop.Max(x => x.GetExactLength());
                            //loai bo curve loop to nhat

                            var openingChuNhats = edgeCurveLoop.Where(x =>
                                x.IsRectangular(x.GetPlane()) && x.GetExactLength() < chuViLonNhat).ToList();

                            foreach (var openingChu in openingChuNhats)
                            {
                                List<Line> lines = new List<Line>();
                                foreach (var curve in openingChu)
                                {
                                    lines.Add(curve as Line);
                                }

                                List<XYZ> listPoint = new List<XYZ>();
                                foreach (var line in lines)
                                {
                                    if (line.Direction.IsParallel(XYZ.BasisX))
                                    {
                                        listPoint.Add(line.SP());
                                        listPoint.Add(line.EP());
                                    }
                                }

                                var pointMax = listPoint.ElementAt(0);
                                var pointMin = listPoint.ElementAt(0);

                                foreach (var point in listPoint)
                                {
                                    if (point.X <= pointMin.X && point.Y <= pointMin.Y)
                                        pointMin = point;

                                    if (point.X >= pointMax.X && point.Y >= pointMax.Y)
                                        pointMax = point;
                                }

                                ListOpening.Add(new OpeningModel()
                                {
                                    ListLine = lines,
                                    PointMax = pointMax,
                                    PointMin = pointMin
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}