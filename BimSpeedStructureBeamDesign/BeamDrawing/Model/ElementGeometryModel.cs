using Autodesk.Revit.DB;
using BimSpeedUtils;
using System.Windows.Media;
using BimSpeedStructureBeamDesign.Utils;
using MoreLinq;
using Transform = Autodesk.Revit.DB.Transform;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class ElementGeometryModel
    {
        public Element Element { get; set; }
        public List<ReferenceAndPoint> ReferenceAndPoints { get; set; } = new List<ReferenceAndPoint>();
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public Transform Transform { get; set; }
        public bool IsColumn { get; set; } = false;
        public bool IsWall { get; set; } = false;
        public bool IsBeam { get; set; } = false;
        public bool IsFoundation { get; set; } = false;

        public bool IsBeamSupport { get; set; }

        public List<Edge> EdgeVers { get; set; } = new List<Edge>();

        public List<PlanarFace> PlanarFaces = new List<PlanarFace>();

        public Line BeamLine { get; set; }

        public PlanarFace TopFace { get; set; }
        public PlanarFace BottomFace { get; set; }
        public FamilyInstance FamilyInstance { get; set; }

        public BoundingBoxXYZ BoundingBoxInView { get; set; }
        public XYZ PointCenterInView { get; set; }
        /// <summary>
        /// Use this constructor for walls,columns... not Grid...
        /// </summary>
        /// <param name="ele"></param>
        public ElementGeometryModel(Element ele, Autodesk.Revit.DB.View view = null, bool isFoundation = false)
        {
            Element = ele;

            IsFoundation = isFoundation;

            if (ele is FamilyInstance familyInstance)
            {
                BoundingBoxInView = familyInstance.get_BoundingBox(view);

                PointCenterInView = (BoundingBoxInView.Max + BoundingBoxInView.Min) / 2;

                FamilyInstance = familyInstance;

                BeamLine = (familyInstance.Location as LocationCurve)?.Curve as Line;

                var solids = familyInstance.GetAllSolidsToDim(out Transform transform);

                Transform = transform;

                if (solids.Count > 0)
                    if (IsFoundation)
                    {
                        foreach (var solid in solids)
                        {
                            if (solid.Volume > 0)
                            {
                                foreach (var solidFace in solid.Faces)
                                {
                                    if (solidFace is PlanarFace planarFace)
                                        PlanarFaces.Add(planarFace);
                                }

                                foreach (Edge edge in solid.Edges)
                                {
                                    if (edge.AsCurve() is Line line)
                                        if (transform.OfVector(line.Direction).IsParallel(XYZ.BasisZ))
                                            EdgeVers.Add(edge);
                                }

                                Solids.Add(solid);
                            }
                        }
                    }
                    else
                    {
                        foreach (var solid in solids)
                        {
                            if (solid.Volume > 0)
                            {
                                foreach (var solidFace in solid.Faces)
                                {
                                    if (solidFace is PlanarFace planarFace)
                                        PlanarFaces.Add(planarFace);
                                }

                                Solids.Add(solid);
                            }
                        }

                        foreach (var planarFace in PlanarFaces)
                        {
                            var reference = planarFace.Reference;
                            if (reference != null)
                            {
                                var normal = transform.OfPoint(planarFace.FaceNormal);
                                var point = transform.OfPoint(planarFace.Origin);
                                var rfp = new ReferenceAndPoint() { Normal = normal, Point = point, PlanarFace = planarFace, Reference = reference };
                                ReferenceAndPoints.Add(rfp);
                            }

                        }
                    }

                if (PlanarFaces.Count > 0)
                {
                    PlanarFace bottomFace = null;
                    PlanarFace topFace = null;
                    double minZ = double.MaxValue;
                    double maxZ = double.MinValue;

                    foreach (var planarFace in PlanarFaces)
                    {
                        if (transform.OfVector(planarFace.FaceNormal).IsParallel(XYZ.BasisZ))
                        {
                            double z = transform.OfPoint(planarFace.Origin).Z;
                            if (z < minZ)
                            {
                                minZ = z;
                                bottomFace = planarFace;
                            }
                            if (z > maxZ)
                            {
                                maxZ = z;
                                topFace = planarFace;
                            }
                        }
                    }

                    BottomFace = bottomFace;
                    TopFace = topFace;
                }
            }
            else if (ele is Floor floor)
            {
                #region Code cu

                //var faces = ele.AllFaces(out var transform, true);
                //if (view != null)
                //{
                //    faces = ele.FacesByView(view);
                //}

                //var ff = faces.Where(x => x is PlanarFace).Cast<PlanarFace>().ToList();
                //foreach (var planarFace in ff)
                //{
                //    var reference = planarFace.Reference;
                //    if (reference != null)
                //    {
                //        var normal = transform.OfPoint(planarFace.FaceNormal);
                //        var point = transform.OfPoint(planarFace.Origin);
                //        var rfp = new ReferenceAndPoint() { Normal = normal, Point = point, PlanarFace = planarFace, Reference = reference };
                //        ReferenceAndPoints.Add(rfp);
                //    }
                //}
                //Solids = Element.GetAllSolids(true);

                #endregion
                BoundingBoxInView = floor.get_BoundingBox(view);

                PointCenterInView = (BoundingBoxInView.Max + BoundingBoxInView.Min) / 2;

                var solids = floor.GetAllSolidsToDim(out Transform transform);

                Transform = transform;

                var planarFaces = new List<PlanarFace>();
                if (solids.Count > 0)
                    if (IsFoundation)
                    {
                        foreach (var solid in solids)
                        {
                            if (solid.Volume > 0)
                            {
                                foreach (var solidFace in solid.Faces)
                                {
                                    if (solidFace is PlanarFace planarFace)
                                        PlanarFaces.Add(planarFace);
                                }

                                foreach (Edge edge in solid.Edges)
                                {
                                    if (edge.AsCurve() is Line line)
                                        if (transform.OfVector(line.Direction).IsParallel(XYZ.BasisZ))
                                            EdgeVers.Add(edge);
                                }

                                Solids.Add(solid);
                            }
                        }
                    }
                    else
                    {
                        foreach (var solid in solids)
                        {
                            if (solid.Volume > 0)
                            {
                                foreach (var solidFace in solid.Faces)
                                {
                                    if (solidFace is PlanarFace planarFace)
                                        PlanarFaces.Add(planarFace);
                                }

                                Solids.Add(solid);
                            }
                        }

                        foreach (var planarFace in PlanarFaces)
                        {
                            var reference = planarFace.Reference;
                            if (reference != null)
                            {
                                var normal = transform.OfPoint(planarFace.FaceNormal);
                                var point = transform.OfPoint(planarFace.Origin);
                                var rfp = new ReferenceAndPoint() { Normal = normal, Point = point, PlanarFace = planarFace, Reference = reference };
                                ReferenceAndPoints.Add(rfp);
                            }

                        }
                    }

                if (PlanarFaces.Count > 0)
                {
                    var minZ = PlanarFaces.Where(x => x.FaceNormal.IsParallel(XYZ.BasisZ)).Min(x => x.Origin.Z);

                    BottomFace = PlanarFaces.FirstOrDefault(x => Math.Abs(x.Origin.Z - minZ) < 0.001);

                    var maxZ = PlanarFaces.Where(x => x.FaceNormal.IsParallel(XYZ.BasisZ)).Max(x => x.Origin.Z);

                    TopFace = PlanarFaces.FirstOrDefault(x => Math.Abs(x.Origin.Z - maxZ) < 0.001);
                }
            }
        }

        public ElementGeometryModel(Grid grid)
        {
            Element = grid;
            var rf = new Reference(grid);
            var line = grid.Curve;
            var rfp = new ReferenceAndPoint
            {
                Point = line.SP(),
                Reference = rf
            };
            ReferenceAndPoints.Add(rfp);
        }
    }
}