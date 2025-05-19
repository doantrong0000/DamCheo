using Autodesk.Revit.DB;
using BimSpeedUtils;
using MoreLinq.Extensions;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
    public class BeamGeometry
    {
        private Solid _solid;
        private Solid _solid2;
        public FamilyInstance Beam { get; set; }

        public List<PlanarFace> PlanarFaceLeftRight { get; set; } = new List<PlanarFace>();

        public List<Edge> EdgeVers { get; set; } = new List<Edge>();
        public List<Edge> EdgeHozs { get; set; } = new List<Edge>();

        public List<PlanarFace> FaceCheo { get; set; } = new List<PlanarFace>();
        /// <summary>
        /// Line at center top of beam
        /// </summary>
        public Line BeamLine { get; set; }
        public Line BeamLine1 { get; set; }

        public XYZ MidPoint { get; set; }
        public Transform Transform { get; set; } = Transform.Identity;
        public Transform Transform1 { get; set; } = Transform.Identity;

        public Solid Solid
        {
            get
            {
                try
                {
                    var b = SolidUtils.Clone(_solid);
                    return b;
                }
                catch (Exception)
                {
                    var b = SolidUtils.Clone(_solid2);
                    return b;
                }
            }
            set
            {
                _solid = value;
                if (value != null)
                {
                    _solid2 = SolidUtils.Clone(value);
                }

            }
        }

        public Solid OriginalSolidTransformed { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }
        public string Mark { get; set; }
        public bool IsColumn { get; set; } = false;


        public RebarAtPositionOfSpan RebarAtPositionOfSpanStart { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanMid { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanEnd { get; set; }
        public RebarQuantityDiameter StirrupEnd { get; set; }
        public RebarQuantityDiameter StirrupMid { get; set; }

        public BeamGeometry(FamilyInstance beam)
        {
            Beam = beam;
            GetData();
        }

        public BeamGeometry()
        {

        }

        private void GetData()
        {
            Transform = Beam.GetTransform();

            if (Beam.Location is LocationCurve lc)
            {
                var line = lc.Curve as Line;
                if (line != null)
                {
                    var sp = line.SP();
                    var ep = line.EP();
                    var line1 = Line.CreateBound(sp, ep);
                    var origin = Transform.Origin;
                    var normal = Transform.OfVector(XYZ.BasisY);
                    var plane = BPlane.CreateByNormalAndOrigin(normal, origin);
                    BeamLine = line1.ProjectOntoPlane(plane);
                }
            }

            BeamLine = Line.CreateBound(BeamLine.SP(), BeamLine.EP());
            MidPoint = BeamLine.Midpoint();
            OriginalSolidTransformed = SolidUtils.CreateTransformed(GetOriginalGeometry(), Transform);
            var s = Beam.GetSolids();

            Solid = Beam.GetSolids().FirstOrDefault(x => x.Volume > 0.01);

            Mark = Beam.GetParameterValueAsString(BuiltInParameter.DOOR_NUMBER);
            //BeamLine = Line.CreateBound(BeamLine.SP().EditZ(TopElevation), BeamLine.EP().EditZ(TopElevation));

            GetDataBeamRebar();

            var solids = Beam.GetAllSolidsToDim(out Transform tf1);

            Transform1 = tf1;
            //Get Planarfaces
            foreach (var solid in solids)
            {
                if (solid.Volume > 0)
                {
                    foreach (var solidFace in solid.Faces)
                    {
                        if (solidFace is PlanarFace planarFace)
                        {
                            if (tf1.OfVector(planarFace.FaceNormal).IsParallel(BeamLine.Direction))
                            {
                                PlanarFaceLeftRight.Add(planarFace);
                            }

                            if (tf1.OfVector(planarFace.FaceNormal).IsPerpendicular(XYZ.BasisZ)
                                && !tf1.OfVector(planarFace.FaceNormal).IsParallel(XYZ.BasisX)
                                && !tf1.OfVector(planarFace.FaceNormal).IsParallel(XYZ.BasisY)
                               )
                            {
                                FaceCheo.Add(planarFace);
                            }
                        }
                    }

                    foreach (Edge edge in solid.Edges)
                    {
                        if (edge.AsCurve() is Line line)
                        {
                            if (tf1.OfVector(line.Direction).IsParallel(XYZ.BasisZ))
                            {
                                EdgeVers.Add(edge);
                            }

                            if (tf1.OfVector(line.Direction).IsParallel(BeamLine.Direction))
                            {
                                EdgeHozs.Add(edge);
                            }
                        }
                    }

                    if (FaceCheo.Count > 0)
                    {
                        foreach (var planarFace in FaceCheo)
                        {
                            foreach (EdgeArray edgeArray in planarFace.EdgeLoops)
                            {
                                foreach (Edge edge in edgeArray)
                                {
                                    if (edge.AsCurve() is Line line)
                                    {
                                        if (line.Direction.IsParallel(XYZ.BasisZ))
                                        {
                                            EdgeVers.Add(edge);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            //Xac dinh lai beamline
            if (EdgeHozs.Count > 0)
            {
                var listPoints = new List<XYZ>();

                foreach (var edge in EdgeHozs)
                {
                    listPoints.Add(tf1.OfPoint(edge.SP()));
                    listPoints.Add(tf1.OfPoint(edge.EP()));
                }

                var listPoints1 = listPoints.DistinctBy2(x => x.DotProduct(BeamLine.Direction))
                    .OrderBy(x => x.DotProduct(BeamLine.Direction));

                if(listPoints1.Any())
                {
                    var starPoint = BeamLine.SP().Add(BeamLine.Direction * (listPoints1.FirstOrDefault() - BeamLine.SP()).DotProduct(BeamLine.Direction));

                    var endPoint = BeamLine.EP().Add(BeamLine.Direction * (listPoints1.LastOrDefault() - BeamLine.EP()).DotProduct(BeamLine.Direction));

                    BeamLine1 = Line.CreateBound(starPoint, endPoint);
                }
            }
        }


        private void GetDataBeamRebar()
        {
            var first = Beam.GetParameterValueByNameAsString("First Section");
            var mid = Beam.GetParameterValueByNameAsString("Middle Section");
            var last = Beam.GetParameterValueByNameAsString("Last Section");
            var stirrup = Beam.GetParameterValueByNameAsString("Stirrup");

            RebarAtPositionOfSpanStart = new RebarAtPositionOfSpan(first);
            RebarAtPositionOfSpanMid = new RebarAtPositionOfSpan(mid);
            RebarAtPositionOfSpanEnd = new RebarAtPositionOfSpan(last);

            RebarAtPositionOfSpanStart.PositionType = RebarPositionTypeInSpan.Start;
            RebarAtPositionOfSpanMid.PositionType = RebarPositionTypeInSpan.Mid;
            RebarAtPositionOfSpanEnd.PositionType = RebarPositionTypeInSpan.End;


            StirrupMid = new RebarQuantityDiameter()
            {
                Diameter = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
                Spacing = 200,
            };

            StirrupEnd = new RebarQuantityDiameter()
            {
                Diameter = 8.GetRebarBarTypeByNumber(findBestMatchIfNull: true),
                Spacing = 100,
            };
        }

        public Solid GetOriginalSolidTransformed(double extent = 0.16404199475)
        {
            return SolidUtils.CreateTransformed(GetOriginalGeometry(extent), Transform);
        }

        private Solid GetOriginalGeometry(double extent = 0.16404199475)
        {
            var geometryElement = Beam.GetOriginalGeometry(new Options());
            var solid1 = geometryElement.Flatten().FirstOrDefault(x => x is Solid) as Solid;

            var solid1Bb = solid1.GetBoundingBox();
            var min = solid1Bb.Min;
            var max = solid1Bb.Max;
            var bbTf = solid1Bb.Transform;
            TopElevation = Transform.OfPoint(bbTf.OfPoint(max)).Z;
            BotElevation = Transform.OfPoint(bbTf.OfPoint(min)).Z;
            Height = max.Z - min.Z;
            Width = max.Y - min.Y;
            var sp = Transform.Inverse.OfPoint(BeamLine.SP().Add(BeamLine.Direction * -extent));
            var direct = Transform.Inverse.OfVector(BeamLine.Direction);
            var p1 = min;
            var p2 = p1.Add(XYZ.BasisZ * Height);
            var p3 = p2.Add(XYZ.BasisY * Width);
            var p4 = p1.Add(XYZ.BasisY * Width);
            p1 = p1.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
            p2 = p2.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
            p3 = p3.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
            p4 = p4.ModifyVector(sp.X, BimSpeedUtils.XYZEnum.X);
            var l1 = Line.CreateBound(p1, p2);
            var l2 = Line.CreateBound(p2, p3);
            var l3 = Line.CreateBound(p3, p4);
            var l4 = Line.CreateBound(p4, p1);
            var cl = new CurveLoop();
            cl.Append(l1);
            cl.Append(l2);
            cl.Append(l3);
            cl.Append(l4);
            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { cl }, direct,
                BeamLine.Length + 2 * extent);
            return solid;
        }
    }
}