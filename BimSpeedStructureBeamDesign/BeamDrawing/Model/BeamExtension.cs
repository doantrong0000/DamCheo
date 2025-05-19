using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.Model;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;
using System.Xml.Linq;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using MoreLinq.Extensions;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.Utils;
using NamingViewModel = BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel.NamingViewModel;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class BeamExtension
    {
        public List<FamilyInstance> Beams { get; set; }
        public List<Rebar> Rebars { get; set; } = new();
        public List<BeamSegment> BeamSegments { get; set; } = new();
        public ViewSection BeamDetail { get; set; }
        public XYZ Direction { get; set; }
        public bool IsValid { get; set; }
        public BeamDrawingSetting BeamDrawingSetting { get; set; }
        public List<BeamGeometry> BeamGeometries { get; set; } = new();
        public List<Element> BeamSupports { get; set; } = new();
        private List<Line> beamLines = new();
        private BeamDetailViewModel beamDetailViewModel;

        public BeamDetailViewModel BeamDetailViewModel
        {
            get => beamDetailViewModel;
            set => beamDetailViewModel = value;
        }

        public BeamSectionViewModel BeamSectionViewModel { get; set; }

        public bool OnlyMidSection { get; set; } = false;

        public BeamExtension(List<FamilyInstance> beams, List<Element> supports = null)
        {
            Beams = beams;
            foreach (var familyInstance in Beams)
            {
                BeamGeometries.Add(new BeamGeometry(familyInstance));
            }
            Direction = BeamRebarCommonService.EditBeamDirection(BeamGeometries[0].BeamLine.Direction);
            BeamGeometries.ForEach(x => x.BeamLine = BeamRebarCommonService.EditLineByDirection(x.BeamLine, Direction));
            if (BeamGeometries.Count > 1)
            {
                BeamGeometries = BeamGeometries.OrderBy(x => x.MidPoint.DotProduct(Direction)).ToList();
            }
            BeamSupports = supports;
        }

        #region Create Sections and get data

        public void Run()
        {
            //get position in rebarsetting

            var rebarSetting = new BeamRebarSettingViewModel();

            if (rebarSetting.Setting != null)
            {
                BeamDrawingSetting.BeamSectionSetting.ViTri1 = rebarSetting.Setting.Position1;
                BeamDrawingSetting.BeamSectionSetting.ViTri2 = rebarSetting.Setting.Position2;
                BeamDrawingSetting.BeamSectionSetting.ViTri3 = rebarSetting.Setting.Position3;
            }

            beamLines = GetBeamLines(Beams);

            if (beamLines == null)
            {
                return;
            }
            CreateViewParallelToBeam();

            if (BeamDrawingSetting.BeamDetailSetting.ViewTemplate != null)
            {
                BeamDetail.ViewTemplateId = BeamDrawingSetting.BeamDetailSetting.ViewTemplate.Id;
            }

            CreateSections();

            GetRebar();

            SetNameForViewSection(1);

            var sections = new List<Autodesk.Revit.DB.View>();
            foreach (var beamSegment in BeamSegments)
            {
                foreach (var beamSegmentBeamSection in beamSegment.BeamSections)
                {
                    sections.Add(beamSegmentBeamSection.ViewSection);
                }
            }
            AC.Document.Regenerate();


            ModifySameSectionThenCreateReferencedView();

            //CommonService.HideViewersNotUse(BeamDetail, sections);
        }

        private void CreateSections()
        {
            Direction = beamLines.First().Direction;
            if (Direction.Y < 0)
            {
                Direction = -Direction;
            }

            if (Direction.Y.IsEqual(0, 0.00001))
            {
                Direction = new XYZ(Math.Abs(Direction.X), 0, Direction.Z);
            }



            var columns = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns).ToElements();
            var walls = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(Wall))
                .OfCategory(BuiltInCategory.OST_Walls).ToElements();
            var foundationFloors = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(Floor))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation).ToElements();
            var foundationFamilyInstance = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation).ToElements();

            var foundations = foundationFloors.Concat(foundationFamilyInstance).ToList();

  

            var filters = Beams.Select(b => CreateBoundingBoxFilter(b.get_BoundingBox(null)))
                .Where(f => f != null)
                .ToList();

            var solids = new List<Solid>();


            bool flag = true;

            //foreach (var beam in BeamSupports)
            //{
            //    foreach (var boundingBoxIntersectsFilter in filters)
            //    {
            //        if (boundingBoxIntersectsFilter.PassesFilter(beam))
            //        {
            //            foreach (var solid in beam.GetAllSolids(true))
            //            {
            //                if (solid.Volume > 0)
            //                {
            //                    solids.Add(solid);
            //                }
            //            }
            //            break;
            //        }
            //    }
            //}
            //check xem section co mong hay khong
            foreach (var foundation in foundations)
            {
                foreach (var boundingBoxIntersectsFilter in filters)
                {
                    if (boundingBoxIntersectsFilter.PassesFilter(foundation))
                    {
                        flag = false;

                        foreach (var solid in foundation.GetAllSolids(true))
                        {
                            if (solid.Volume > 0)
                            {
                                solids.Add(solid);
                            }
                        }
                        break;
                    }
                }
            }

            if (flag)
                foreach (var column in columns.Concat(walls))
                {
                    foreach (var boundingBoxIntersectsFilter in filters)
                    {
                        if (boundingBoxIntersectsFilter.PassesFilter(column))
                        {
                            foreach (var solid in column.GetAllSolids(true))
                            {
                                if (solid.Volume > 0)
                                {
                                    solids.Add(solid);
                                }
                            }
                            break;
                        }
                    }
                }

            var i = 0;
            
            foreach (var beamGeometry in BeamGeometries)
            {
                var segmentLines = new List<Line> { beamGeometry.BeamLine };
             
                foreach (var solid in solids)
                {
                    var centroid = solid.ComputeCentroid();
                    var vector = (beamGeometry.BeamLine.SP().Z - centroid.Z) * XYZ.BasisZ;
                    var solid2 = SolidUtils.CreateTransformed(solid, Transform.CreateTranslation(vector));
                    //BeamRebarCommonService.CreateDirectShape(solid2);
                    if (OnlyMidSection == false)
                    {
                        segmentLines = segmentLines.LinesDivideBySolid(solid2);
                    }
                }

                segmentLines = segmentLines.OrderByDescending(x => x.Midpoint().DotProduct(Direction)).ToList();
                if (segmentLines.Count == 0)
                {
                    segmentLines = new List<Line> { beamGeometry.BeamLine };
                }

                if (BeamSupports.Count > 0)
                {
                    segmentLines = DivideSegmentsByBeams(segmentLines, BeamSupports);
                }
               
                foreach (var segmentLine in segmentLines)
                {
                    if (segmentLine != null)
                    {
                        var sp = segmentLine.SP().ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);
                        var ep = segmentLine.EP().ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);

                        //Move vao mep dau dam trong truoong hop dam mong
                        if (!flag && segmentLines.Count < 2)
                        {
                            if (segmentLine == segmentLines.FirstOrDefault())
                            {
                                ep = segmentLine.EP()
                                    .Add(Direction *
                                         (beamGeometry.BeamLine1.EP() - segmentLine.EP()).DotProduct(Direction))
                                    .ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);
                            }

                            // Move vao mep cuoi dam
                            if (segmentLine == segmentLines.LastOrDefault())
                            {
                                sp = segmentLine.SP().Add(Direction *
                                                          (beamGeometry.BeamLine1.SP() - segmentLine.SP()).DotProduct(Direction)).ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);
                            }

                        }

                        var beamSegment = new BeamSegment { LineSegment = Line.CreateBound(sp, ep), BeamGeometry = beamGeometry, SegmentIndex = i };
                        beamSegment = CreateViewPerpendicularToBeamSegment(beamSegment);

                        i++;
                        BeamSegments.Add(beamSegment);
                    }
                }
            }
           
        }

        private BoundingBoxIntersectsFilter CreateBoundingBoxFilter(BoundingBoxXYZ bb)
        {
            if (bb == null) return null;
            var outline = new Outline(bb.Min, bb.Max);
            return new BoundingBoxIntersectsFilter(outline, 0.1);
        }
        private List<Line> DivideSegmentsByBeams(List<Line> segmentLines, List<Element> beams)
        {
            var dividedSegments = new List<Line>();
            var beamsInstance = beams.Cast<FamilyInstance>().ToList();
            var beamsLines = GetBeamLines(beamsInstance);

            foreach (var segmentLine in segmentLines)
            {
                // Lấy tọa độ Z của segmentLine hiện tại
                double segmentLineZ = segmentLine.SP().Z;

                // Danh sách các điểm giao nhau
                var intersectionPoints = new List<XYZ>();

                // Tính toán giao điểm dựa trên X và Y
                foreach (var beamLine in beamsLines)
                {
                    // Tìm giao điểm trong không gian 2D (X và Y)
                    if (FindLineIntersection(segmentLine, beamLine, out var intersectionPoint2D))
                    {
                        // Gán tọa độ Z của segmentLine vào giao điểm
                        var intersectionPoint3D = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, segmentLineZ);
                        intersectionPoints.Add(intersectionPoint3D);
                    }
                }

                // Chia segmentLine dựa trên các giao điểm
                var dividedLines = DivideBeamLineByPoints(segmentLine, intersectionPoints);
                dividedSegments.AddRange(dividedLines);
            }

            return dividedSegments;

        }
        public static List<Line> DivideBeamLineByPoints(Line line, List<XYZ> points)
        {
            var dividedLines = new List<Line>();

            // Sắp xếp các điểm theo khoảng cách từ điểm bắt đầu của đường thẳng
            var sortedPoints = points.OrderBy(p => (p - line.SP()).GetLength()).ToList();

            // Thêm điểm bắt đầu và kết thúc của đường thẳng vào danh sách
            sortedPoints.Insert(0, line.SP());
            sortedPoints.Add(line.EP());

            // Tạo các đoạn đường từ các điểm đã sắp xếp
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var startPoint = sortedPoints[i];
                var endPoint = sortedPoints[i + 1];
                dividedLines.Add(Line.CreateBound(startPoint, endPoint));
            }

            return dividedLines;
        }
        private bool FindLineIntersection(Line line1, Line line2, out XYZ intersectionPoint)
        {
            intersectionPoint = null;

            // Lấy tọa độ X và Y của các điểm đầu và cuối của hai đường thẳng
            var p1 = new XYZ(line1.SP().X, line1.SP().Y, 0);
            var p2 = new XYZ(line1.EP().X, line1.EP().Y, 0);
            var p3 = new XYZ(line2.SP().X, line2.SP().Y, 0);
            var p4 = new XYZ(line2.EP().X, line2.EP().Y, 0);

            // Tính vector hướng của hai đường thẳng
            var dir1 = p2 - p1;
            var dir2 = p4 - p3;

            // Tính định thức
            double det = dir1.X * dir2.Y - dir1.Y * dir2.X;

            // Nếu định thức bằng 0, hai đường thẳng song song hoặc trùng nhau
            if (Math.Abs(det) < 1e-10)
                return false;

            // Tính vector từ điểm bắt đầu của đường thẳng 1 đến điểm bắt đầu của đường thẳng 2
            var diff = p3 - p1;

            // Tính tham số t và u
            double t = (diff.X * dir2.Y - diff.Y * dir2.X) / det;
            double u = (diff.X * dir1.Y - diff.Y * dir1.X) / det;

            // Kiểm tra xem giao điểm có nằm trên cả hai đoạn thẳng không
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                // Tính giao điểm
                intersectionPoint = p1 + t * dir1;
                return true;
            }

            return false;
        
        }
        public List<Line> GetBeamLines(List<FamilyInstance> beams)
        {
            var list = new List<Line>();

                foreach (var familyInstance in beams)
                {
                    var l = GetBeamLine(familyInstance);
                    list.Add(l);
                }

                var direct = list.First().Direction;
                list = list.OrderBy(x => x.Midpoint().DotProduct(direct)).ToList();
            
            return list;
        }

        private Line GetBeamLine(FamilyInstance beam)
        {
            var line = (beam.Location as LocationCurve)?.Curve as Line;
            if (line == null)
            {
                return null;
            }
            var tf = beam.GetTransform();
            var origin = tf.Origin;
            var normal = line.Direction.CrossProduct(XYZ.BasisZ);
            var plane = BPlane.CreateByNormalAndOrigin(normal, origin);
            var l = line.ProjectOn(plane) as Line;
            l = EditLine(l);
            return l;
        }

        private void GetRebar()
        {
            foreach (var familyInstance in Beams)
            {
                var data = RebarHostData.GetRebarHostData(familyInstance);
                if (data != null)
                {
                    Rebars.AddRange(data.GetRebarsInHost().ToList());
                }
            }
        }

        private Line EditLine(Line line)
        {
            var rsLine = line.Clone() as Line;
            var a = line.SP();
            var b = line.EP();
            if (a.X > b.X)
            {
                rsLine = line.CreateReversed() as Line;
            }
            else if (Math.Abs(a.X - b.X) < 0.00001)
            {
                if (a.Y > b.Y)
                {
                    rsLine = line.CreateReversed() as Line;
                }
            }
            return rsLine;
        }

        #endregion Create Sections and get data

        private BeamSegment CreateViewPerpendicularToBeamSegment(BeamSegment beamSegment)
        {
            var beamLine = beamSegment.LineSegment;
            if (beamLine.Length.FootToMm() > 2000)
            {
                var p1 = beamLine.Evaluate(BeamDrawingSetting.BeamSectionSetting.ViTri1, true);
                var p2 = beamLine.Evaluate(BeamDrawingSetting.BeamSectionSetting.ViTri2, true);
                var p3 = beamLine.Evaluate(BeamDrawingSetting.BeamSectionSetting.ViTri3, true);
                var isCreate2End = beamSegment.LineSegment.Length.FootToMm() > 2 && !OnlyMidSection;
                var origin = p1;
                var viewDir = beamLine.Direction;
                var up = XYZ.BasisZ;
                var right = up.CrossProduct(viewDir);

                var tf = Transform.Identity;
                tf.Origin = origin;
                tf.BasisX = right;
                tf.BasisY = up;
                tf.BasisZ = viewDir;

                var box = new BoundingBoxXYZ
                {
                    Transform = tf,
                    Min = new XYZ(-beamSegment.BeamGeometry.Width / 2 - 150.MmToFoot(), -200.MmToFoot(), 0),
                    Max = new XYZ(beamSegment.BeamGeometry.Width / 2 + 150.MmToFoot(), beamSegment.BeamGeometry.Height + 200.MmToFoot(), 200.MmToFoot())
                };

                ViewSection vs1 = null;
                if (isCreate2End)
                {
                    if (BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.ViewFamily == ViewFamily.Section)
                    {
                        vs1 = ViewSection.CreateSection(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }
                    else
                    {
                        vs1 = ViewSection.CreateDetail(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }
                }

                tf.Origin = p2;
                box.Transform = tf;
                ViewSection vs2 = null;

                if (BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.ViewFamily == ViewFamily.Section)
                {
                    vs2 = ViewSection.CreateSection(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                }
                else
                {
                    vs2 = ViewSection.CreateDetail(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                }

                tf.Origin = p3;
                box.Transform = tf;

                ViewSection vs3 = null;

                box = new BoundingBoxXYZ
                {
                    Transform = tf,
                    Min = new XYZ(-beamSegment.BeamGeometry.Width / 2 - 150.MmToFoot(), -200.MmToFoot(), 0),
                    Max = new XYZ(beamSegment.BeamGeometry.Width / 2 + 150.MmToFoot(), beamSegment.BeamGeometry.Height + 200.MmToFoot(), 100.MmToFoot())
                };

                if (isCreate2End)
                {
                    if (BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.ViewFamily == ViewFamily.Section)
                    {
                        vs3 = ViewSection.CreateSection(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }
                    else
                    {
                        vs3 = ViewSection.CreateDetail(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }
                }

                //Set viewTemplate and scale :
                if (isCreate2End)
                {
                    SetTemplateAndScale(vs1, BeamDrawingSetting.BeamSectionSetting.ViewTemplate as ViewSection, false);

                    SetTemplateAndScale(vs3, BeamDrawingSetting.BeamSectionSetting.ViewTemplate as ViewSection, false);
                }
                SetTemplateAndScale(vs2, BeamDrawingSetting.BeamSectionSetting.ViewTemplate as ViewSection, false);

                if (isCreate2End)
                {
                    var bs1 = new BeamSection
                    {
                        ViewSection = vs1,
                        Origin = p1,
                        FamilyInstance = beamSegment.BeamGeometry.Beam,
                        BsLocation = BeamSection.BeamSectionLocation.Start
                    };
                    bs1.GetRebar();
                    bs1.SegmentIndex = beamSegment.SegmentIndex;
                    beamSegment.BeamSections.Add(bs1);
                }

                var bs2 = new BeamSection
                {
                    Origin = p2,
                    ViewSection = vs2,
                    FamilyInstance = beamSegment.BeamGeometry.Beam,
                    BsLocation = BeamSection.BeamSectionLocation.Mid
                };
                bs2.GetRebar();
                bs2.SegmentIndex = beamSegment.SegmentIndex;

                if (isCreate2End)
                {
                    var bs3 = new BeamSection
                    {
                        ViewSection = vs3,
                        Origin = p3,
                        FamilyInstance = beamSegment.BeamGeometry.Beam,
                        BsLocation = BeamSection.BeamSectionLocation.End
                    };
                    bs3.GetRebar();
                    bs3.SegmentIndex = beamSegment.SegmentIndex;
                    beamSegment.BeamSections.Add(bs3);
                }

                beamSegment.BeamSections.Add(bs2);
            }
            else
            {
                var isCreate2End = beamSegment.LineSegment.Length.FootToMm() > 2 && OnlyMidSection;
                var pointCenter = beamLine.Evaluate(BeamDrawingSetting.BeamSectionSetting.ViTri2, true);
   
                var origin = pointCenter;
                var viewDir = beamLine.Direction;
                var up = XYZ.BasisZ;
                var right = up.CrossProduct(viewDir);

                var tf = Transform.Identity;
                tf.Origin = origin;
                tf.BasisX = right;
                tf.BasisY = up;
                tf.BasisZ = viewDir;

                ViewSection vs1 = null;


                    var box = new BoundingBoxXYZ
                    {
                        Transform = tf,
                        Min = new XYZ(-beamSegment.BeamGeometry.Width / 2 - 150.MmToFoot(), -200.MmToFoot(), 0),
                        Max = new XYZ(beamSegment.BeamGeometry.Width / 2 + 150.MmToFoot(), beamSegment.BeamGeometry.Height + 200.MmToFoot(), 200.MmToFoot())
                    };

                    if (BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.ViewFamily == ViewFamily.Section)
                    {
                        vs1 = ViewSection.CreateSection(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }
                    else
                    {
                        vs1 = ViewSection.CreateDetail(AC.Document, BeamDrawingSetting.BeamSectionSetting.ViewFamilyType.Id, box);
                    }


                    var bs2 = new BeamSection
                    {
                        Origin = pointCenter,
                        ViewSection = vs1,
                        FamilyInstance = beamSegment.BeamGeometry.Beam,
                        BsLocation = BeamSection.BeamSectionLocation.Mid
                    };
                    bs2.GetRebar();
                    bs2.SegmentIndex = beamSegment.SegmentIndex;

                    beamSegment.BeamSections.Add(bs2);
             }

     

            return beamSegment;
        }

        private ViewSection CreateViewParallelToBeam()
        {
            ViewSection vs = null;
            var zBottom = BeamGeometries.Select(x => x.BotElevation).Min();
            var b = BeamGeometries.Select(x => x.Width).Max();
            var h = BeamGeometries.Select(x => x.Height).Max();
            var p0 = BeamGeometries.First().BeamLine.SP().EditZ(zBottom);
            var p1 = BeamGeometries.Last().BeamLine.EP().EditZ(zBottom);

            XYZ a1 = p0;

            XYZ a2 = p1;

            var length = (p1 - p0).GetLength();

            var min = new XYZ(-0.5 * length - 150.MmToFoot(), -500.MmToFoot(), -b / 2 + b / 5);
            var max = new XYZ(0.5 * length + 150.MmToFoot(), h + 500.MmToFoot(), b / 2 + 100.MmToFoot());

            var mid = (p0 + p1) / 2;
            var beamDirection = (a2 - a1).Normalize();
            var up = XYZ.BasisZ;
            var viewDir = -beamDirection.CrossProduct(up);
            var tf = Transform.Identity;
            tf.Origin = mid;
            tf.BasisX = -beamDirection;
            tf.BasisY = up;
            tf.BasisZ = viewDir;

            var sectionBox = new BoundingBoxXYZ
            {
                Transform = tf,
                Min = min,
                Max = max
            };

            if (BeamDrawingSetting.BeamDetailSetting.ViewFamilyType.ViewFamily == ViewFamily.Detail)
            {
                vs = ViewSection.CreateDetail(AC.Document, BeamDrawingSetting.BeamDetailSetting.ViewFamilyType.Id, sectionBox);
            }

            if (BeamDrawingSetting.BeamDetailSetting.ViewFamilyType.ViewFamily == ViewFamily.Section)
            {
                vs = ViewSection.CreateSection(AC.Document, BeamDrawingSetting.BeamDetailSetting.ViewFamilyType.Id, sectionBox);
            }

            SetTemplateAndScale(vs, BeamDrawingSetting.BeamDetailSetting.ViewTemplate as ViewSection);
            BeamDetail = vs;
            return null;
        }

        private bool Compare2ListRebar(List<Rebar> rebars1, List<Rebar> rebars2)
        {
            var rs = false;
            if (rebars1.Count == rebars2.Count)
            {
                var rb1s = rebars1.Select(x => x.GetParameterValueByNameAsString("Rebar Number")).ToList();
                var rb2s = rebars2.Select(x => x.GetParameterValueByNameAsString("Rebar Number")).ToList();
                rb1s.Sort();
                rb2s.Sort();
                if (rb1s.SequenceEqual(rb2s))
                {
                    rs = true;
                }
            }
            return rs;
        }

        private bool Compair2Section(BeamSection s1, BeamSection s2)
        {
            if (Compare2ListRebar(s1.Rebars, s2.Rebars))
            {
                return true;
            }

            return false;
        }

        private void ModifySameSectionThenCreateReferencedView()
        {
            var beamSections = BeamSegments.Select(x => x.BeamSections).Flatten().Cast<BeamSection>().ToList();

            var dist = beamSections.Distinct().ToList();
            var maxH = BeamGeometries.Max(x => x.Height);

            foreach (var beamSection in dist)
            {
                // if(ignoreList.Contains(section)) continue;
                var same = beamSections.Where(x => x.Equals(beamSection)).Where(x => !(x.SegmentIndex == beamSection.SegmentIndex && x.BsLocation == beamSection.BsLocation)).ToList();
                foreach (var beamSection1 in same)
                {
                    try
                    {
                        if (beamSection1.ViewSection.IsValidObject)
                        {
                            AC.Document.Delete(beamSection1.ViewSection.Id);
                        }

                        //Create Reference View
                        var head = beamSection1.Origin.Add(BeamDetail.UpDirection * (200 + maxH.FootToMm()).MmToFoot());
                        var tail = beamSection1.Origin.Add(-BeamDetail.UpDirection * 200.MmToFoot());
                        ViewSection.CreateReferenceSection(AC.Document, BeamDetail.Id, beamSection.ViewSection.Id, head, tail);
                        beamSection1.IsReference = true;
                        beamSection1.ReferencedSection = beamSection.ViewSection;
                    }
                    catch (Exception)
                    {

                    }
                    //Delete
                }
            }
        }

        private void SetNameForViewSection(int number)
        {
            var mark = Beams.First().GetMark();
            if (mark == "")
            {
                mark = Beams.First().Id.GetElementIdValue().ToString();
            }
            foreach (var beamSegment in BeamSegments)
            {
                foreach (var beamSection in beamSegment.BeamSections)
                {
                    //if (beamSection.IsReference == false)
                    {
                        number++;
                        while (true)
                        {
                            try
                            {
                                beamSection.ViewSection.Name = mark + "-" + number;
                                break;
                            }
                            catch
                            {
                                number++;
                            }
                        }
                    }
                }
            }
        }

        private void SetTemplateAndScale(ViewSection vs, ViewSection template, bool isDetail = true)
        {
            if (vs != null)
            {
                if (template != null)
                {
                    vs.ViewTemplateId = template.Id;
                }
            }

            try
            {
                if (isDetail)
                {
                    vs.Scale = BeamDrawingSetting.BeamDetailSetting.Scale;
                }
                else
                {
                    vs.Scale = BeamDrawingSetting.BeamSectionSetting.Scale;
                }
            }
            catch
            {
            }
        }

        #region Create View 3D

        public View3D Create3DView()
        {
            // Tạo một view 3D mới
            ViewFamilyType viewFamilyType = new FilteredElementCollector(AC.Document)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            if (viewFamilyType == null)
                return null;

            // Tạo view 3D
            View3D view3D = View3D.CreateIsometric(AC.Document, viewFamilyType.Id);

            // Đặt tên cho view
            string mark = BeamGeometries.First().Mark;
            if (string.IsNullOrEmpty(mark))
            {
                mark = BeamGeometries.First().Beam.Id.GetElementIdValue().ToString();
            }

            // Thử đặt tên, xử lý trường hợp trùng tên
            int counter = 1;
            string baseName = $"3D REBAR {mark}";
            string viewName = baseName;

            while (true)
            {
                try
                {
                    view3D.Name = viewName;
                    break;
                }
                catch
                {
                    counter++;
                    viewName = $"{baseName} ({counter})";
                }
            }

            // Thiết lập các thuộc tính của view
            view3D.DetailLevel = ViewDetailLevel.Fine;

            // Tạo section box xung quanh dầm
            BoundingBoxXYZ sectionBox = GetBeamsBoundingBox();
            view3D.SetSectionBox(sectionBox);

            // Thiết lập hướng view cố định 
            SetFixedViewOrientation(view3D);

            // Khóa view để tránh người dùng xoay góc nhìn
            view3D.SaveOrientationAndLock();

            // Làm cho dầm trong suốt để nhìn thấy thép rõ hơn
            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
            overrideSettings.SetSurfaceTransparency(60); // Làm dầm trong suốt 60%

            // Áp dụng cài đặt trong suốt cho tất cả dầm
            foreach (var beam in Beams)
            {
                view3D.SetElementOverrides(beam.Id, overrideSettings);
            }

            // Tạo tag cho thép
            CreateRebarTags(view3D);

            // Tạo dim cho view 3D nếu cần
            CreateDimensionsIn3DView(view3D);

            return view3D;
        }

        private void SetFixedViewOrientation(View3D view3D)
        {
            try
            {
                // Lấy dầm đầu tiên để tính hướng
                FamilyInstance firstBeam = Beams.FirstOrDefault();
                if (firstBeam == null) return;

                // Xác định hướng dầm
                XYZ beamDirection = Direction;

                // Tạo eye position (vị trí camera)
                XYZ eyePosition = GetViewPosition(beamDirection);

                // Tạo target position (vị trí nhìn vào - trung tâm của dầm)
                XYZ targetPosition = GetBeamsCenterPoint();

                // Tính vector hướng nhìn từ mắt đến trung tâm
                XYZ viewDirection = (targetPosition - eyePosition).Normalize();

                // Up vector luôn là trục Z
                XYZ upVector = XYZ.BasisZ;

                // Đảm bảo upVector vuông góc với viewDirection
               
                double dot = upVector.DotProduct(viewDirection);
                XYZ upVectorOrthogonal = (upVector - viewDirection * dot).Normalize();

                // Tạo ViewOrientation3D với các vector đã được điều chỉnh
                ViewOrientation3D orientation = new ViewOrientation3D(eyePosition, viewDirection, upVectorOrthogonal);

                // Thiết lập orientation cho view
                view3D.SetOrientation(orientation);

                // Khóa orientation
                view3D.SaveOrientationAndLock();
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                TaskDialog.Show("Error", $"Không thể thiết lập hướng view: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        // Xác định vị trí điểm nhìn
        private XYZ GetViewPosition(XYZ beamDirection)
        {
            // Tính center point của tất cả dầm
            XYZ centerPoint = GetBeamsCenterPoint();

            // Tạo một vector ngang vuông góc với beamDirection
            XYZ horizontalVector;

            if (Math.Abs(beamDirection.Z) > 0.9) // Nếu dầm gần như thẳng đứng
            {
                horizontalVector = XYZ.BasisX; // Sử dụng trục X
            }
            else
            {
                // Tạo vector ngang vuông góc với beamDirection
                horizontalVector = XYZ.BasisZ.CrossProduct(beamDirection).Normalize();
            }

            // Khoảng cách từ trung tâm dầm
            double distance = 20.0.FootToMm().MmToFoot();

            // Tạo vị trí camera
            XYZ viewPosition = centerPoint
                - beamDirection * distance * 0.8    // Lùi lại so với hướng dầm
                + XYZ.BasisZ * distance * 0.7       // Nâng cao hơn so với dầm
                + horizontalVector * distance * 0.6; // Dịch sang một bên

            return viewPosition;
        }

        // Tính trung tâm của tất cả dầm
        private XYZ GetBeamsCenterPoint()
        {
            if (Beams == null || Beams.Count == 0)
                return XYZ.Zero;

            XYZ sum = XYZ.Zero;
            int count = 0;

            foreach (var beam in Beams)
            {
                var bb = beam.get_BoundingBox(null);
                if (bb != null)
                {
                    sum += (bb.Min + bb.Max) / 2;
                    count++;
                }
            }

            return count > 0 ? sum / count : XYZ.Zero;
        }
        // Phương thức tạo tag thép trong View3D - đã cập nhật
        private void CreateRebarTags(View3D view3D)
        {
            try
            {
                // Khởi tạo Revit API
                AC.Document.Regenerate();

                // Lấy tất cả thép trong dầm - phân nhóm theo loại thép
                var rebarsInBeam = RebarsInBeam.Instance;
                rebarsInBeam.GetInfo(new ElementGeometry(Beams.First(), view3D), view3D);

                // Tag thép đai
                TagRebarGroup(rebarsInBeam.Stirrups, "Stirrup", view3D);

                // Tag thép dưới
                TagRebarGroup(rebarsInBeam.B1Rebars, "BOT", view3D);

                // Tag thép trên
                TagRebarGroup(rebarsInBeam.T1Rebars, "TOP", view3D);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Lỗi khi tạo tag: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        // Tạo tag cho một nhóm thép
        private void TagRebarGroup(List<Rebar> rebars, string groupType, View3D view3D)
        {
            if (rebars == null || rebars.Count == 0) return;

            // Lọc thép đại diện - chỉ lấy một số thép để tag
            var selectedRebars = rebars
                .DistinctBy2(r => r.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER))
                .Take(3) // Giới hạn số lượng tag
                .ToList();

            foreach (var rebar in selectedRebars)
            {
                try
                {
                    // Tìm tag symbol phù hợp cho loại thép này
                    FamilySymbol tagSymbol = FindRebarTagSymbolByType(groupType);
                    if (tagSymbol == null)
                    {
                        AC.Log($"Không tìm thấy tag symbol phù hợp cho loại thép: {groupType}");
                        continue;
                    }

                    // Đảm bảo tag symbol được kích hoạt
                    if (!tagSymbol.IsActive)
                    {
                        tagSymbol.Activate();
                        AC.Document.Regenerate();
                    }

                    // Tính vị trí tag dựa trên loại thép
                    XYZ rebarPoint = GetRebarTaggingPoint(rebar);
                    XYZ tagPoint = GetTagPositionByRebarType(rebar, rebarPoint, groupType);

                    // Tạo tag
                    Reference rebarRef = GetValidRebarReference(rebar);
                    if (rebarRef == null) continue;

                    IndependentTag tag = TagUtils2.CreateIndependentTag(
                        tagSymbol.Id,
                        view3D.Id,
                        rebar,
                        true, // Sử dụng leader
                        TagOrientation.Horizontal,
                        tagPoint
                    );

                    // Điều chỉnh tag và leader
                    AdjustTagAndLeader(tag, rebarPoint, tagPoint, groupType, view3D);

                    // Re-generate để đảm bảo tag được hiển thị đúng
                    AC.Document.Regenerate();
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng tiếp tục với các thép khác
                    AC.Log($"Lỗi khi tạo tag cho thép {rebar.Id}: {ex.Message}");
                }
            }
        }

        // Tìm biểu tượng tag phù hợp dựa trên loại thép
        private FamilySymbol FindRebarTagSymbolByType(string rebarType)
        {
            // Tìm theo mẫu tên cụ thể như trong ảnh của bạn
            string tagNamePattern = rebarType.ToUpper().Contains("BOT") ? "BOT" :
                                  rebarType.ToUpper().Contains("TOP") ? "TOP" :
                                  rebarType.ToUpper().Contains("STIRRUP") ? "DAI" : "";

            // Tạo danh sách các khả năng tên tag
            var possibleNames = new List<string>
    {
        $"@BSA1-RT_{tagNamePattern}",
        $"RT_{tagNamePattern}",
        $"@BS_{tagNamePattern}",
        $"A1_P_RT_{tagNamePattern}",
        $"A1_T_RT_{tagNamePattern}"
    };

            // Tìm tag theo danh sách tên
            foreach (var tagName in possibleNames)
            {
                var tag = new FilteredElementCollector(AC.Document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_RebarTags)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(t => t.Name.ToUpper().Contains(tagName));

                if (tag != null) return tag;
            }

            // Nếu không tìm thấy tag cụ thể, trả về tag thép đầu tiên
            return new FilteredElementCollector(AC.Document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RebarTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault();
        }

        // Lấy điểm để tag thép
        private XYZ GetRebarTaggingPoint(Rebar rebar)
        {
            // Lấy đường cong trung tâm của thép
            IList<Curve> curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0);

            if (curves.Count > 0)
            {
                // Tìm đường thẳng dài nhất của thép
                Curve longestCurve = curves.OrderByDescending(c => c.Length).First();

                // Lấy điểm giữa của đường cong
                return longestCurve.Evaluate(0.5, true);
            }

            // Nếu không có đường cong, sử dụng điểm giữa bounding box
            BoundingBoxXYZ bb = rebar.get_BoundingBox(null);
            return (bb.Min + bb.Max) / 2;
        }

        // Tính vị trí đặt tag dựa trên loại thép
        private XYZ GetTagPositionByRebarType(Rebar rebar, XYZ rebarPoint, string rebarType)
        {
            // Offset tùy thuộc vào loại thép
            double horizontalOffset = 250.MmToFoot(); // Khoảng cách ngang
            double verticalOffset = 100.MmToFoot(); // Khoảng cách dọc

            if (rebarType.ToUpper().Contains("BOT"))
            {
                // Tag cho thép dưới - đặt ở dưới phải
                return rebarPoint + new XYZ(horizontalOffset, 0, -verticalOffset);
            }
            else if (rebarType.ToUpper().Contains("TOP"))
            {
                // Tag cho thép trên - đặt ở trên phải
                return rebarPoint + new XYZ(horizontalOffset, 0, verticalOffset);
            }
            else if (rebarType.ToUpper().Contains("STIRRUP"))
            {
                // Tag cho thép đai - đặt ở giữa bên phải
                return rebarPoint + new XYZ(horizontalOffset, 0, 0);
            }

            // Mặc định đặt bên phải
            return rebarPoint + new XYZ(horizontalOffset, 0, 0);
        }

        // Lấy tham chiếu hợp lệ của thép để tạo tag
        private Reference GetValidRebarReference(Rebar rebar)
        {
       
            Reference directRef = new Reference(rebar);


            if (directRef == null)
            {
                try
                {
                    var element = AC.Document.GetElement(rebar.Id);
                    directRef = new Reference(element);
                }
                catch { }
            }

            if (directRef == null)
            {
                Options options = new Options();
                options.ComputeReferences = true;
                options.DetailLevel = ViewDetailLevel.Fine;

                GeometryElement geomElem = rebar.get_Geometry(options);
                if (geomElem != null)
                {
                    foreach (GeometryObject geomObj in geomElem)
                    {
                        if (geomObj is Solid solid && solid.Faces.Size > 0)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                if (face.Reference != null)
                                    return face.Reference;
                            }
                        }
                    }
                }
            }

            return directRef;
        }

        // Điều chỉnh tag và leader
        private void AdjustTagAndLeader(IndependentTag tag, XYZ rebarPoint, XYZ tagPoint, string rebarType, Autodesk.Revit.DB.View view3D)
        {
            // Tái tạo để có bounding box
            AC.Document.Regenerate();

            try
            {
                // Thiết lập leader
                tag.LeaderEndCondition = LeaderEndCondition.Free;

                // Tính toán điểm khuỷu tay (elbow point)
                XYZ elbowPoint;
                if (rebarType.ToUpper().Contains("BOT"))
                {
                    // Điểm khuỷu tay cho thép dưới
                    elbowPoint = rebarPoint + new XYZ(100.MmToFoot(), 0, -50.MmToFoot());
                }
                else if (rebarType.ToUpper().Contains("TOP"))
                {
                    // Điểm khuỷu tay cho thép trên
                    elbowPoint = rebarPoint + new XYZ(100.MmToFoot(), 0, 50.MmToFoot());
                }
                else
                {
                    // Điểm khuỷu tay mặc định
                    elbowPoint = rebarPoint + new XYZ(100.MmToFoot(), 0, 0);
                }

                // Đặt điểm khuỷu tay và điểm cuối
                tag.SetLeaderElbow(elbowPoint);
                tag.SetLeaderEnd(rebarPoint);

                // Đảm bảo tag không bị chồng lên thép - di chuyển tag nếu cần
                BoundingBoxXYZ tagBB = tag.get_BoundingBox(view3D);
                if (tagBB != null)
                {
                    // Điều chỉnh vị trí tag nếu cần
                    double moveDistance = 50.MmToFoot();
                    if (rebarType.ToUpper().Contains("BOT"))
                    {
                        ElementTransformUtils.MoveElement(AC.Document, tag.Id, new XYZ(moveDistance, 0, -moveDistance));
                    }
                    else if (rebarType.ToUpper().Contains("TOP"))
                    {
                        ElementTransformUtils.MoveElement(AC.Document, tag.Id, new XYZ(moveDistance, 0, moveDistance));
                    }
                    else
                    {
                        ElementTransformUtils.MoveElement(AC.Document, tag.Id, new XYZ(moveDistance, 0, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                AC.Log($"Lỗi khi điều chỉnh tag: {ex.Message}");
            }
        }

        // Lọc một số thép đại diện để tag
        private List<Rebar> FilterRepresentativeRebars(List<Rebar> allRebars)
        {
            // Nhóm thép theo kích thước và loại
            var groupedRebars = allRebars
                .GroupBy(r => new {
                    Number = r.get_Parameter(BuiltInParameter.REBAR_NUMBER)?.AsString(),
                    Diameter = r.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble()
                })
                .Select(g => g.First())
                .Take(5)  // Chỉ lấy 5 nhóm đại diện
                .ToList();

            return groupedRebars;
        }

        // Tìm tag symbol phù hợp
        private FamilySymbol FindRebarTagSymbol()
        {
            return new FilteredElementCollector(AC.Document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RebarTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault();
        }

        // Lấy điểm giữa của thép
        private XYZ GetRebarCenter(Rebar rebar)
        {
            IList<Curve> curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);

            if (curves.Count > 0)
            {
                // Tìm đường thẳng dài nhất
                Curve longestCurve = curves.OrderByDescending(c => c.Length).First();
                return longestCurve.Evaluate(0.5, true);  // Lấy điểm giữa của đường cong
            }

            // Fallback nếu không tìm được đường cong
            return (rebar.get_BoundingBox(null).Min + rebar.get_BoundingBox(null).Max) / 2;
        }

        // Điều chỉnh vị trí tag nếu cần
        private void AdjustTagPosition(IndependentTag tag, XYZ basePoint, View3D view)
        {
            // Thêm offset để tag không bị chồng lên thép
            XYZ offset = new XYZ(0, 0, 0.1.FootToMm().MmToFoot());
            ElementTransformUtils.MoveElement(AC.Document, tag.Id, offset);

            // Thiết lập leader và điểm đầu leader
            tag.HasLeader = true;
            tag.LeaderEndCondition = LeaderEndCondition.Free;
            tag.SetLeaderElbow(basePoint);
            tag.SetLeaderEnd(basePoint);
        }

        // Tạo kích thước trong view 3D nếu cần
        private void CreateDimensionsIn3DView(View3D view3D)
        {
            // Trong Revit, thường không thể tạo kích thước trong view 3D
            // Nhưng có thể tạo spot coordinates hoặc spot elevations

            // Ví dụ: tạo spot elevation cho điểm đầu và điểm cuối của dầm
            try
            {
                // Nếu có API hỗ trợ, có thể thêm code tại đây
            }
            catch (Exception ex)
            {
                AC.Log($"Không thể tạo dim trong view 3D: {ex.Message}");
            }
        }

        private BoundingBoxXYZ GetBeamsBoundingBox()
        {

            double padding = 2.0.FootToMm().MmToFoot(); 
            BoundingBoxXYZ result = null;

            foreach (var beam in Beams)
            {
                var bb = beam.get_BoundingBox(null);
                if (bb == null) continue;

                if (result == null)
                {
                    result = new BoundingBoxXYZ();
                    result.Min = new XYZ(bb.Min.X - padding, bb.Min.Y - padding, bb.Min.Z - padding);
                    result.Max = new XYZ(bb.Max.X + padding, bb.Max.Y + padding, bb.Max.Z + padding);
                }
                else
                {
                    result.Min = new XYZ(
                        Math.Min(result.Min.X, bb.Min.X - padding),
                        Math.Min(result.Min.Y, bb.Min.Y - padding),
                        Math.Min(result.Min.Z, bb.Min.Z - padding)
                    );
                    result.Max = new XYZ(
                        Math.Max(result.Max.X, bb.Max.X + padding),
                        Math.Max(result.Max.Y, bb.Max.Y + padding),
                        Math.Max(result.Max.Z, bb.Max.Z + padding)
                    );
                }
            }

            return result;
        }

        #endregion

        #region AutoGenerateTools

        public void CreateSectionsForAutoGenerate(bool isCreateCrossSection = true)
        {
            beamLines = GetBeamLines(Beams);
            if (beamLines == null)
            {
                return;
            }
            CreateViewParallelToBeamAuto();
            var i = 1;
            if (isCreateCrossSection)
            {
                CreateSectionsAuto();
                SetNameForViewSectionAuto(0);
            }
        }

        private void CreateViewParallelToBeamAuto()
        {
            ViewSection vs = null;
            var cropModel = beamDetailViewModel.CropModel;
            var zBottom = BeamGeometries.Select(x => x.BotElevation).Min();
            var b = BeamGeometries.Select(x => x.Width).Max();
            var h = BeamGeometries.Select(x => x.Height).Max();
            var p0 = BeamGeometries.First().BeamLine.SP().EditZ(zBottom);
            var p1 = BeamGeometries.Last().BeamLine.EP().EditZ(zBottom);
            XYZ a1 = p0;
            XYZ a2 = p1;
            var length = (p1 - p0).GetLength();
            var min = new XYZ(-0.5 * length - cropModel.LeftOffset, -cropModel.BotOffset, -b / 2 + beamDetailViewModel.SectionOffsetFromSideFace);
            var max = new XYZ(0.5 * length + cropModel.RightOffset, h + cropModel.TopOffset, b / 2 + 100.MmToFoot());
            var mid = (p0 + p1) / 2;
            var beamDirection = (a2 - a1).Normalize();
            var up = XYZ.BasisZ;
            var viewDir = -beamDirection.CrossProduct(up);
            var tf = Transform.Identity;
            tf.Origin = mid;
            tf.BasisX = -beamDirection;
            tf.BasisY = up;
            tf.BasisZ = viewDir;
            var sectionBox = new BoundingBoxXYZ
            {
                Transform = tf,
                Min = min,
                Max = max
            };

            var sectionTypeModel = beamDetailViewModel.InclinedBeamSectionTypeModel;
            if (Direction.IsParallel(XYZ.BasisY))
            {
                sectionTypeModel = beamDetailViewModel.VerticalBeamSectionTypeModel;
            }
            else if (Direction.IsParallel(XYZ.BasisX))
            {
                sectionTypeModel = beamDetailViewModel.HorizontalBeamSectionTypeModel;
            }
            if (sectionTypeModel.ViewFamilyType.ViewFamily == ViewFamily.Detail)
            {
                vs = ViewSection.CreateDetail(AC.Document, sectionTypeModel.ViewFamilyType.Id, sectionBox);
            }

            if (sectionTypeModel.ViewFamilyType.ViewFamily == ViewFamily.Section)
            {
                vs = ViewSection.CreateSection(AC.Document, sectionTypeModel.ViewFamilyType.Id, sectionBox);
            }
            if (vs != null)
            {
                vs.SetParameterValueByName(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR, cropModel.FarClipOffset);
                vs.SetParameterValueByName(BuiltInParameter.SECTION_COARSER_SCALE_PULLDOWN_METRIC, cropModel.HideScale);
            }
            SetViewTemplateAuto(vs, sectionTypeModel);
            var name = GetNameAuto(beamDetailViewModel.NamingViewModel, BeamGeometries.FirstOrDefault()?.Beam);
            SetNameForViewAuto(vs, name);
        }

        private string GetNameAuto(NamingViewModel vm, FamilyInstance fi)
        {
            var s = "";
            var recordModels = vm.RecordModels;
            foreach (var recordModel in recordModels)
            {
                if (recordModel.IsParam)
                {
                    var value = GetParamValue(fi, recordModel.Text);
                    s += value;
                }
                else
                {
                    s += recordModel.Text;
                }
            }
            return s;
        }

        private void SetNameForViewAuto(ViewSection vs, string name)
        {
            if (vs == null)
            {
                return;
            }
            var i = 1;
            var s = name;
            while (true)
            {
                if (i > 100)
                {
                    break;
                }
                try
                {
                    vs.Name = s;
                    break;
                }
                catch
                {
                    s = name + "(" + i + ")";
                    i++;
                }
            }
        }

        private string GetParamValue(FamilyInstance fi, string param)
        {
            var p = fi.LookupParameter(param);
            if (p == null)
            {
                p = fi.Symbol.LookupParameter(param);
            }

            var s = "";
            if (p != null)
            {
                if (p.StorageType == StorageType.Double)
                {
                    s = p.AsValueString();
                    if (string.IsNullOrEmpty(s))
                    {
                        s = p.AsDouble().Round2Number().ToString();
                    }
                }
                if (p.StorageType == StorageType.Integer)
                {
                    s = p.AsInteger().ToString();
                }
                if (p.StorageType == StorageType.String)
                {
                    s = p.AsString();
                }
                if (p.StorageType == StorageType.ElementId)
                {
                    s = p.AsValueString();
                }
            }

            if (s == null)
            {
                s = "";
            }

            return s;
        }

        private void SetViewTemplateAuto(ViewSection vs, SectionTypeModel model)
        {
            if (vs == null)
            {
                return;
            }
            if (model.ViewTemplate.Element != null && model.ViewTemplate.ElementId != null)
            {
                vs.ViewTemplateId = model.ViewTemplate.ElementId;
            }
        }

        private void CreateSectionsAuto()
        {
            Direction = beamLines.First().Direction;
            if (Direction.Y < 0)
            {
                Direction = -Direction;
            }

            if (Direction.Y.IsEqual(0, 0.00001))
            {
                Direction = new XYZ(Math.Abs(Direction.X), 0, Direction.Z);
            }

            var columns = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns).ToElements();
            var walls = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(Wall))
                .OfCategory(BuiltInCategory.OST_Walls).ToElements();

            var solids = new List<Solid>();
            var filters = new List<BoundingBoxIntersectsFilter>();
            foreach (var familyInstance in Beams)
            {
                var bb = familyInstance.get_BoundingBox(null);
                if (bb != null)
                {
                    var ol = new Outline(bb.Min, bb.Max);
                    var filter = new BoundingBoxIntersectsFilter(ol, 0.1);
                    filters.Add(filter);
                }
            }
            foreach (var column in columns.Concat(walls))
            {
                foreach (var boundingBoxIntersectsFilter in filters)
                {
                    if (boundingBoxIntersectsFilter.PassesFilter(column))
                    {
                        foreach (var solid in column.GetAllSolids(true))
                        {
                            if (solid.Volume > 0)
                            {
                                solids.Add(solid);
                            }
                        }
                        break;
                    }
                }
            }
            var i = 0;

            foreach (var beamGeometry in BeamGeometries)
            {
                var segmentLines = new List<Line> { beamGeometry.BeamLine };
                foreach (var solid in solids)
                {
                    var centroid = solid.ComputeCentroid();
                    var vector = (beamGeometry.BeamLine.SP().Z - centroid.Z) * XYZ.BasisZ;
                    var solid2 = SolidUtils.CreateTransformed(solid, Transform.CreateTranslation(vector));
                    segmentLines = segmentLines.LinesDivideBySolid(solid2);
                }
                segmentLines = segmentLines.OrderByDescending(x => x.Midpoint().DotProduct(Direction)).ToList();
                foreach (var segmentLine in segmentLines)
                {
                    var sp = segmentLine.SP().ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);
                    var ep = segmentLine.EP().ModifyVector(beamGeometry.BotElevation, XYZEnum.Z);

                    var beamSegment = new BeamSegment { LineSegment = Line.CreateBound(sp, ep), BeamGeometry = beamGeometry, SegmentIndex = i };
                    beamSegment = CreateViewPerpendicularToBeamSegmentAuto(beamSegment);
                    i++;
                    BeamSegments.Add(beamSegment);
                }
            }
        }

        private void SetNameForViewSectionAuto(int number)
        {
            var name = GetNameAuto(BeamSectionViewModel.NamingViewModel, BeamGeometries.First().Beam);

            foreach (var beamSegment in BeamSegments)
            {
                foreach (var beamSection in beamSegment.BeamSections)
                {
                    {
                        number++;
                        var count = 0;
                        while (true)
                        {
                            count++;
                            if (count > 100)
                            {
                                break;
                            }
                            try
                            {
                                beamSection.ViewSection.Name = name + "-" + number;
                                break;
                            }
                            catch
                            {
                                number++;
                            }
                        }
                    }
                }
            }
        }

        private BeamSegment CreateViewPerpendicularToBeamSegmentAuto(BeamSegment beamSegment)
        {
            var beamLine = beamSegment.LineSegment;
            var p1 = beamLine.Evaluate(beamDetailViewModel.Position1, true);
            var p2 = beamLine.Evaluate(beamDetailViewModel.Position2, true);
            var p3 = beamLine.Evaluate(beamDetailViewModel.Position3, true);
            var isCreate2End = beamSegment.LineSegment.Length.FootToMm() > beamDetailViewModel.Length3Sections;
            var origin = p1;
            var viewDir = beamLine.Direction;
            var up = XYZ.BasisZ;
            var right = up.CrossProduct(viewDir);

            var tf = Transform.Identity;
            tf.Origin = origin;
            tf.BasisX = right;
            tf.BasisY = up;
            tf.BasisZ = viewDir;

            var box = new BoundingBoxXYZ
            {
                Transform = tf,
                Min = new XYZ(-beamSegment.BeamGeometry.Width / 2 - 150.MmToFoot(), -200.MmToFoot(), 0),
                Max = new XYZ(beamSegment.BeamGeometry.Width / 2 + 150.MmToFoot(), beamSegment.BeamGeometry.Height + 200.MmToFoot(), 200.MmToFoot())
            };
            ViewSection vs1 = null;
            var type = BeamSectionViewModel.SectionTypeModel;
            if (isCreate2End)
            {
                if (type.ViewFamilyType.ViewFamily == ViewFamily.Section)
                {
                    vs1 = ViewSection.CreateSection(AC.Document, type.ViewFamilyType.Id, box);
                }
                else
                {
                    vs1 = ViewSection.CreateDetail(AC.Document, type.ViewFamilyType.Id, box);
                }
            }

            tf.Origin = p2;
            box.Transform = tf;
            ViewSection vs2 = null;
            if (type.ViewFamilyType.ViewFamily == ViewFamily.Section)
            {
                vs2 = ViewSection.CreateSection(AC.Document, type.ViewFamilyType.Id, box);
            }
            else
            {
                vs2 = ViewSection.CreateDetail(AC.Document, type.ViewFamilyType.Id, box);
            }

            tf.Origin = p3;
            box.Transform = tf;

            ViewSection vs3 = null;
            if (isCreate2End)
            {
                if (type.ViewFamilyType.ViewFamily == ViewFamily.Section)
                {
                    vs3 = ViewSection.CreateSection(AC.Document, type.ViewFamilyType.Id, box);
                }
                else
                {
                    vs3 = ViewSection.CreateDetail(AC.Document, type.ViewFamilyType.Id, box);
                }
            }

            //Set viewTemplate and scale :

            if (isCreate2End)
            {
                var bs1 = new BeamSection
                {
                    ViewSection = vs1,
                    Origin = p1,
                    FamilyInstance = beamSegment.BeamGeometry.Beam,
                    BsLocation = BeamSection.BeamSectionLocation.Start
                };
                bs1.GetRebar();
                bs1.SegmentIndex = beamSegment.SegmentIndex;
                beamSegment.BeamSections.Add(bs1);
            }

            var bs2 = new BeamSection
            {
                Origin = p2,
                ViewSection = vs2,
                FamilyInstance = beamSegment.BeamGeometry.Beam,
                BsLocation = BeamSection.BeamSectionLocation.Mid,
                SegmentIndex = beamSegment.SegmentIndex
            };
            beamSegment.BeamSections.Add(bs2);
            if (isCreate2End)
            {
                var bs3 = new BeamSection
                {
                    ViewSection = vs3,
                    Origin = p3,
                    FamilyInstance = beamSegment.BeamGeometry.Beam,
                    BsLocation = BeamSection.BeamSectionLocation.End,
                    SegmentIndex = beamSegment.SegmentIndex
                };
                beamSegment.BeamSections.Add(bs3);
            }

            SetViewTemplateAuto(vs1, type);
            SetViewTemplateAuto(vs2, type);
            SetViewTemplateAuto(vs3, type);

            return beamSegment;
        }

        public void CreateCrossSectionBy2Point(XYZ a, XYZ b)
        {
            var plane1 = BPlane.CreateByNormalAndOrigin(Direction, a);
            var plane2 = BPlane.CreateByNormalAndOrigin(Direction, b);
            var beamGeo = BeamGeometries.FirstOrDefault();
            if (beamGeo == null)
            {
                return;
            }
            a = beamGeo.BeamLine.Origin.ProjectOnto(plane1).EditZ(beamGeo.BotElevation);
            b = beamGeo.BeamLine.Origin.ProjectOnto(plane2).EditZ(beamGeo.BotElevation);
            if (a.DistanceTo(b) < 50.MmToFoot())
            {
                "BEAMETENXION_MESSAGE".NotificationError(this);
                return;
            }
            var line = a.CreateLine(b);
            var p1 = line.Evaluate(beamDetailViewModel.Position1, true);
            var p2 = line.Evaluate(beamDetailViewModel.Position2, true);
            var p3 = line.Evaluate(beamDetailViewModel.Position3, true);
            var isCreate2End = line.Length.FootToMm() > beamDetailViewModel.Length3Sections;
            if (isCreate2End)
            {
                CreateCrossSectionByPoint(p1);
                CreateCrossSectionByPoint(p3);
            }
            CreateCrossSectionByPoint(p2);
        }

        public void CreateCrossSectionByPoint(XYZ p)
        {
            var plane = BPlane.CreateByNormalAndOrigin(Direction, p);
            var beamGeo = BeamGeometries.FirstOrDefault();
            if (beamGeo == null)
            {
                return;
            }
            p = beamGeo.BeamLine.Origin.ProjectOnto(plane);
            var origin = p.EditZ(beamGeo.BotElevation);
            var viewDir = Direction;
            var up = XYZ.BasisZ;
            var right = up.CrossProduct(viewDir);
            var tf = Transform.Identity;
            tf.Origin = origin;
            tf.BasisX = right;
            tf.BasisY = up;
            tf.BasisZ = viewDir;
            var box = new BoundingBoxXYZ
            {
                Transform = tf,
                Min = new XYZ(-beamGeo.Width / 2 - 150.MmToFoot(), -200.MmToFoot(), 0),
                Max = new XYZ(beamGeo.Width / 2 + 150.MmToFoot(), beamGeo.Height + 200.MmToFoot(), 200.MmToFoot())
            };
            ViewSection vs1;
            var type = BeamSectionViewModel.SectionTypeModel;

            if (type.ViewFamilyType.ViewFamily == ViewFamily.Section)
            {
                vs1 = ViewSection.CreateSection(AC.Document, type.ViewFamilyType.Id, box);
            }
            else
            {
                vs1 = ViewSection.CreateDetail(AC.Document, type.ViewFamilyType.Id, box);
            }
            SetViewTemplateAuto(vs1, type);
        }

        #endregion AutoGenerateTools
    }

    public class BeamSegment
    {
        public List<BeamSection> BeamSections { get; set; } = new();
        public List<Rebar> Rebars { get; set; } = new();
        public Line LineSegment { get; set; }
        public int SegmentIndex { get; set; }
        public BeamGeometry BeamGeometry { get; set; }

        public BeamSegment()
        {
        }
    }

    public class BeamSection
    {
        public ViewSection ViewSection { get; set; }
        public List<Rebar> Rebars { get; set; } = new();
        public FamilyInstance FamilyInstance { get; set; }
        public int DetailNumber { get; set; }
        public BeamSectionLocation BsLocation { get; set; }
        public bool IsReference { get; set; } = false;
        public ViewSection ReferencedSection { get; set; }
        public XYZ Origin { get; set; }
        public List<string> RebarNumbers { get; set; }
        public List<double> Spacing { get; set; }
        public int SegmentIndex { get; set; }

        public BeamSection()
        {
        }

        public void AutoDimTag()
        {
            if (IsReference == false)
            {
                //BeamSectionController.Instance.Run(ViewSection);
            }
        }

        public void GetRebar()
        {
            var rebarInView = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Rebar))
                .Cast<Rebar>().ToList();
            var data = RebarHostData.GetRebarHostData(FamilyInstance);
            if (data != null)
            {
                var rbs = data.GetRebarsInHost().Select(x => x.Id.GetElementIdValue()).ToList();
                foreach (var rebar in rebarInView)
                {
                    if (rbs.Contains(rebar.Id.GetElementIdValue()))
                    {
                        Rebars.Add(rebar);
                    }
                }
            }


            RebarNumbers = Rebars.Select(x => x.GetParameterValueByNameAsString("Rebar Number")).ToList();

            RebarNumbers.Sort();

            //Compare stirrup Spacing
            Spacing = Rebars.Where(x => x.IsStandardRebar() == false).Select(x => x.MaxSpacing.Round2Number()).OrderBy(x => x).ToList();
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object obj)
        {
            return obj is BeamSection && Compair2Section(this, (BeamSection)obj);
        }

        private bool Compare2ListRebar(List<Rebar> rebars1, List<Rebar> rebars2)
        {
            var rs = false;
            if (rebars1.Count == rebars2.Count)
            {
                var rb1S = rebars1.Select(x => x.GetParameterValueByNameAsString("Rebar Number")).ToList();

                var rb2S = rebars2.Select(x => x.GetParameterValueByNameAsString("Rebar Number")).ToList();

                rb1S.Sort();
                rb2S.Sort();

                if (rb1S.SequenceEqual(rb2S))
                {
                    rs = true;
                }
                else
                {
                    var aa = 1;
                }
            }

            //Compare stirrup Spacing
            var s1 = rebars1.Where(x => x.IsStandardRebar() == false).Select(x => x.MaxSpacing.Round2Number()).ToList();
            var s2 = rebars2.Where(x => x.IsStandardRebar() == false).Select(x => x.MaxSpacing.Round2Number()).ToList();

            foreach (var d in s1)
            {
                if (s2.Any(x => x.IsEqual(d, 20.MmToFoot())) == false)
                {
                    rs = false;
                    break;
                }
            }

            return rs;
        }

        private bool Compair2Section(BeamSection s1, BeamSection s2)
        {
            if (Compare2ListRebar(s1.Rebars, s2.Rebars))
            {
                return true;
            }
            return false;
        }

        public enum BeamSectionLocation
        {
            Start,
            Mid,
            End
        }
    }
}