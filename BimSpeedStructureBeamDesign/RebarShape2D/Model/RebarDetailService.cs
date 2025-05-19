using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.RebarShape2D.ViewModel;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.RebarShape2D.Model
{
    public class RebarDetailService
    {
        public RebarDetailModel RebarDetailModel { get; set; }
        public FamilySymbol RebarSymbol2D { get; set; }
        public View View { get; set; }
        public string FamilyPath { get; set; }
        public FamilyInstance Shape { get; set; }
        public Element Tag { get; set; }
        public RebarShapeSettingViewModel RebarShapeSettingViewModel { get; set; }

        public RebarDetailService(Rebar rebar, View view)
        {
            RebarDetailModel = new RebarDetailModel(rebar);
            View = view;
            FamilyPath = AC.BimSpeedResourcesFolderByLanguageAndVersion + "\\Families\\BS_REBAR_SHAPE.rfa";
            RebarShapeSettingViewModel = new RebarShapeSettingViewModel();
        }

        public void CreateDetail2D(XYZ p)
        {
            //Open family
            var path = CreateSymbol();
            LoadSymbol(path, p);

            if (RebarShapeSettingViewModel.IsCreateTag && Shape != null)
            {
                using (var tx = new Transaction(AC.Document, "Create Tag"))
                {
                    tx.Start();
                    AC.Document.Regenerate();
                    try
                    {
                        CreateTag(AC.ActiveView, RebarDetailModel.Rebar, false);
                    }
                    catch (Exception e)
                    {
                        AC.Log(e.Message + Environment.NewLine + "Tag");
                    }

                    tx.Commit();
                }
            }
        }

        private string CreateSymbol()
        {
            if (File.Exists(FamilyPath) == false)
            {
                return "";
            }
            Document familyDocument = AC.Application.OpenDocumentFile(FamilyPath);
            if (familyDocument == null)
            {
                return "";
            }
            var planView = new FilteredElementCollector(familyDocument).OfClass(typeof(View)).Cast<View>()
                .FirstOrDefault(x => x.ViewType == ViewType.FloorPlan);

            var graphicsStyles = new FilteredElementCollector(familyDocument).OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>().ToList();
            var style1 = graphicsStyles.FirstOrDefault(x => x.Name == "BS_REBAR_1");
            var style3 = graphicsStyles.FirstOrDefault(x => x.Name == "BS_REBAR_3");
            var lengthSymbol = new FilteredElementCollector(familyDocument).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(x => x.Name == "BS_REBAR_SHAPE_ANNOTATION_2.0");
            if (RebarShapeSettingViewModel.TextSize.IsEqual(1.8))
            {
                lengthSymbol = new FilteredElementCollector(familyDocument).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(x => x.Name == "BS_REBAR_SHAPE_ANNOTATION_1.8");
            }
            if (RebarShapeSettingViewModel.TextSize.IsEqual(2.2))
            {
                lengthSymbol = new FilteredElementCollector(familyDocument).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(x => x.Name == "BS_REBAR_SHAPE_ANNOTATION_2.2");
            }
            if (RebarShapeSettingViewModel.TextSize.IsEqual(2.5))
            {
                lengthSymbol = new FilteredElementCollector(familyDocument).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(x => x.Name == "BS_REBAR_SHAPE_ANNOTATION_2.5");
            }

            using (var tx = new Transaction(familyDocument, "Family Create"))
            {
                tx.Start();
                //Center Lines
                foreach (var familyCurve in RebarDetailModel.FamilyCurves)
                {
                    var detailCurve = familyDocument.FamilyCreate.NewDetailCurve(planView, familyCurve);
                    if (style1 != null)
                    {
                        detailCurve.SetParameterValueByName("Subcategory", style3.Id);
                    }
                    //Set Visible Parameter
                }
                //Create Family Length
                var i = 0;
                lengthSymbol?.Activate();
                if (lengthSymbol != null)
                {
                    for (var index = 0; index < RebarDetailModel.FamilyCurves.Count; index++)
                    {
                        if (RebarDetailModel.Is2HooksSame && index == 0)
                        {
                            i++;
                            continue;
                        }

                        var familyCurve = RebarDetailModel.FamilyCurves[index];
                        //Get Insert Point
                        if (familyCurve is Line)
                        {
                            var segmentLength = RebarDetailModel.SegmentLengths[i];
                            var mid = familyCurve.Evaluate(0.5, true);
                            var direct = familyCurve.Direction();
                            var vector = XYZ.BasisZ.CrossProduct(-direct).Normalize();

                            var scale = 25;

                            var insertPoint = mid.Add(vector * (2.3.MmToFoot() * scale));

                            if (RebarDetailModel.Transform.OfVector(familyCurve.Direction()).IsPerpendicular(XYZ.BasisZ))
                            {
                                if (RebarDetailModel.Transform.OfPoint(insertPoint).Z < RebarDetailModel.MaxCurve.SP().Z)
                                {
                                    insertPoint = mid.Add(vector * (-2.3.MmToFoot() * scale));
                                }
                            }

                            if (RebarDetailModel.Is2HooksSame && index == RebarDetailModel.FamilyCurves.Count - 1)
                            {
                                insertPoint = familyCurve.GetEndPoint(1);
                                var v = (familyCurve.GetEndPoint(1) - familyCurve.GetEndPoint(0)).Normalize();
                                insertPoint = insertPoint.Add(v * (2.3.MmToFoot() * scale));
                            }
                            var angle = familyCurve.Direction().AngleTo(XYZ.BasisX);
                            if (lengthSymbol.Family.FamilyPlacementType == FamilyPlacementType.ViewBased)
                            {
                                var fi = familyDocument.FamilyCreate.NewFamilyInstance(insertPoint, lengthSymbol, planView);
                                ElementTransformUtils.RotateElement(familyDocument, fi.Id,
                                    insertPoint.CreateLineByPointAndDirection(XYZ.BasisZ), -angle);
                                if (segmentLength.IsVariable)
                                {
                                    fi.SetParameterValueByName("BS_SEGMENT_LENGTH_MIN", segmentLength.Min);
                                    fi.SetParameterValueByName("BS_SEGMENT_LENGTH_MAX", segmentLength.Max);
                                    fi.SetParameterValueByName("BS_SEGMENT_LENGTH_VISIBLE", 0);
                                    fi.SetParameterValueByName("BS_SEGMENT_RANGE_VISIBLE", 1);
                                }
                                else
                                {
                                    fi.SetParameterValueByName("BS_SEGMENT_LENGTH", segmentLength.Length);
                                }
                                familyDocument.Regenerate();
                                if (fi.GetTransform().BasisX.IsParallel(familyCurve.Direction()) == false)
                                {
                                    ElementTransformUtils.RotateElement(familyDocument, fi.Id,
                                        insertPoint.CreateLineByPointAndDirection(XYZ.BasisZ), 2 * angle);
                                }
                            }
                            i++;
                        }
                    }
                }

                tx.Commit();
            }

            var saveOption = new SaveAsOptions() { OverwriteExistingFile = true };
            var familyName = "BS_REBAR_SHAPE_" + RebarDetailModel.Rebar.Id.GetElementIdValue() + ".rfa";
            var tempPath = Path.GetTempPath() + "\\" + familyName;
            familyDocument.SaveAs(tempPath, saveOption);
            familyDocument.Close(false);
            return tempPath;
        }

        private void LoadSymbol(string tempPath, XYZ p)
        {
            Family family;
            using (var tx = new Transaction(AC.Document, "Load"))
            {
                tx.Start();
                AC.Document.LoadFamily(tempPath, new LoadFamilyOption(), out family);
                if (family.GetFamilySymbolIds().First().ToElement() is FamilySymbol symbol)
                {
                    symbol.Activate();

                    ViewUtils.SetSketchPlane();
                    try
                    {
                        //Check pick point in range of MainBotBot line
                        if (RebarDetailModel.MaxCurve.Direction().IsParallel(View.ViewDirection))
                        {
                            var pp = p.Add(View.RightDirection * RebarDetailModel.MaxCurve.Length);
                            PlaceFamilyByPoint(p, symbol, p.CreateLine(pp));
                        }
                        else
                        {
                            var l = ProjectCurveOnPlane(RebarDetailModel.MaxCurve, View.ToBPlane());
                            if (IsHorizontalCurveInView(RebarDetailModel.MaxCurve, View))
                            {
                                if (IsInRangeOfLine(l, p, false))
                                {
                                    var planeP = BPlane.CreateByNormalAndOrigin(View.UpDirection, p);
                                    var point = RebarDetailModel.MaxCurve.SP().ProjectOnto(planeP);
                                    PlaceFamilyByPoint(point, symbol, l);
                                }
                                else
                                {
                                    PlaceFamilyByPoint(p, symbol, l);
                                }
                            }
                            else
                            {
                                if (IsInRangeOfLine(l, p))
                                {
                                    var planeP = BPlane.CreateByNormalAndOrigin(View.RightDirection, p);
                                    var point = RebarDetailModel.MaxCurve.SP().ProjectOnto(planeP);
                                    PlaceFamilyByPoint(point, symbol, l);
                                }
                                else
                                {
                                    PlaceFamilyByPoint(p, symbol, l);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AC.Log(e.Message + Environment.NewLine + "Lỗi Load family symbol");
                    }
                }

                tx.Commit();
            }
        }

        private void CreateTag(View view, Rebar rebar, bool isHasLeader = true)
        {
            var tagId = RebarShapeSettingViewModel.StirrupTag?.Id;
            if (rebar.IsStandardRebar())
            {
                tagId = RebarShapeSettingViewModel.StandardTag?.Id;
            }
            if (tagId == null)
            {
                "RebardetailService01_MESSAGE".NotificationError(this);
                return;
            }

            var list = LinesGeometry(Shape);
            var max = list.OrderByDescending(x => x.Length).FirstOrDefault();
            var maxCurve = list.Where(x => x.Length.IsEqual(max.Length, 10.MmToFoot()))
                .OrderByDescending(x => x.Midpoint().DotProduct(view.UpDirection)).First();

            //Move Tag
            if (RebarDetailModel.Rebar.IsStandardRebar())
            {
                //Đặt tag tại chính giữa cây thép
                var p = maxCurve.Evaluate(0.5, true);
                var tag = TagUtils2.CreateIndependentTag(tagId, view.Id, rebar, false, TagOrientation.Horizontal, p);
                Tag = tag;
                if (maxCurve.Direction.IsParallel(view.UpDirection))
                {
                    tag.TagOrientation = TagOrientation.Vertical;
                    AC.Document.Regenerate();
                    //Move
                    var bb = tag.get_BoundingBox(view);
                    var tagMin = bb.Min;
                    var tagMax = bb.Max;
                    var points = new List<XYZ>() { tagMin, tagMax };
                    var rightPoint = points.OrderByDescending(x => x.DotProduct(view.RightDirection)).First();
                    var plane = view.ToBPlane();
                    var d = (p.ProjectOnto(plane) - rightPoint.ProjectOnto(plane)).DotProduct(-view.RightDirection);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, (d + 25.MmToFoot()) * view.RightDirection * -1);
                }
                else if (maxCurve.Direction.IsParallel(view.RightDirection))
                {
                    tag.TagOrientation = TagOrientation.Horizontal;
                    AC.Document.Regenerate();
                    //Move
                    var bb = tag.get_BoundingBox(view);
                    var tagMin = bb.Min;
                    var tagMax = bb.Max;
                    var points = new List<XYZ>() { tagMin, tagMax };
                    var topPoint = points.OrderByDescending(x => x.DotProduct(view.UpDirection)).First();
                    var plane = view.ToBPlane();
                    var d = (p.ProjectOnto(plane) - topPoint.ProjectOnto(plane)).DotProduct(view.UpDirection);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, d * view.UpDirection);
                }
            }
            else
            {
                var p = maxCurve.Evaluate(0.75, true);
                var pp = maxCurve.Evaluate(0.25, true);

                if (maxCurve.Direction().IsParallel(view.UpDirection))
                {
                    var d1 = p.DotProduct(view.UpDirection);
                    var d2 = pp.DotProduct(view.UpDirection);
                    if (d1 < d2)
                    {
                        p = pp;
                    }
                }
                var modelP = p;
                //Find Up Point of Max Curve
                var points = new List<XYZ>() { maxCurve.SP(), maxCurve.EP() };
                var upPoint = points.OrderByDescending(x => x.DotProduct(view.UpDirection)).First();

                var tag = TagUtils2.CreateIndependentTag(tagId, view.Id, rebar, false, TagOrientation.Horizontal, modelP);
                Tag = tag;
                var bb = tag.get_BoundingBox(view);
                if (bb != null)
                {
                    var dMinUp = bb.Min.DotProduct(view.UpDirection);
                    var dMaxUp = bb.Max.DotProduct(view.UpDirection);
                    var minUp = bb.Min;
                    if (dMinUp > dMaxUp)
                    {
                        minUp = bb.Max;
                    }
                    var move = (upPoint - minUp).DotProduct(view.UpDirection);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, view.UpDirection * move);

                    var dMin = bb.Min.DotProduct(view.RightDirection);
                    var dMax = bb.Max.DotProduct(view.RightDirection);
                    var left = bb.Min;
                    if (dMin > dMax)
                    {
                        left = bb.Max;
                    }
                    var d = (modelP - left).DotProduct(view.RightDirection);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, view.RightDirection * (d + 50.MmToFoot()));
                    //Move tag up if is Dai Moc
                    if (isHasLeader)
                    {
                        AC.Document.Regenerate();
                        tag.HasLeader = true;
                        tag.LeaderEndCondition = LeaderEndCondition.Free;
                        tag.SetLeaderEnd(modelP);
                        var plane = BPlane.CreateByNormalAndOrigin(view.UpDirection, tag.TagHeadPosition);
                        tag.SetLeaderElbow(modelP.ProjectOnto(plane));
                    }
                }
            }
        }

        private List<Line> LinesGeometry(Element element)
        {
            var lines = new List<Line>();
            var op = new Options();
            op.IncludeNonVisibleObjects = false;
            op.View = View;
            var geoE = element.get_Geometry(op);
            if (geoE == null) return lines;
            foreach (var geoO in geoE)
            {
                var geoI = geoO as GeometryInstance;
                if (geoI == null) continue;
                var instanceGeoE = geoI.GetInstanceGeometry();

                foreach (var instanceGeoObj in instanceGeoE)
                {
                    var line = instanceGeoObj as Line;
                    if (line != null)
                    {
                        lines.Add(line);
                    }
                }
            }
            return lines;
        }

        private void LoadSymbolForWall(string tempPath)
        {
            AC.Document.LoadFamily(tempPath, new LoadFamilyOption(), out var family);
            if (family.GetFamilySymbolIds().First().ToElement() is FamilySymbol symbol)
            {
                symbol.Activate();
                //Check if need to pick point to place
                if (RebarDetailModel.Normal.IsParallel(View.ViewDirection))
                {
                    try
                    {
                        var l = ProjectCurveOnPlane(RebarDetailModel.MaxCurve, View.ToBPlane());
                        PlaceFamilyByPoint(RebarDetailModel.MaxCurve.SP(), symbol, l);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void PlaceFamilyByPoint(XYZ p, FamilySymbol symbol, Line l)
        {
            var fi = AC.Document.Create.NewFamilyInstance(p, symbol, View);
            AC.Document.Regenerate();
            //Rotate
            var tf = fi.GetTransform();

            var angle = tf.BasisX.AngleTo(l.Direction);
            if (tf.BasisX.DotProduct(l.Direction).IsEqual(-1))
            {
                angle = Math.PI;
            }
            ElementTransformUtils.RotateElement(AC.Document, fi.Id, tf.Origin.CreateLineByPointAndDirection(View.ViewDirection), angle);
            AC.Document.Regenerate();
            tf = fi.GetTransform();
            if (tf.BasisX.IsParallel(l.Direction) == false)
            {
                ElementTransformUtils.RotateElement(AC.Document, fi.Id, tf.Origin.CreateLineByPointAndDirection(View.ViewDirection), -2 * angle);
            }
            else
            {
                if (tf.BasisX.DotProduct(l.Direction).IsEqual(-1))
                {
                    ElementTransformUtils.RotateElement(AC.Document, fi.Id, tf.Origin.CreateLineByPointAndDirection(View.ViewDirection), Math.PI);
                }
            }
            AC.Document.Regenerate();
            tf = fi.GetTransform();
            if (tf.BasisY.DotProduct(RebarDetailModel.Transform.BasisY).IsEqual(1) == false)
            {
                var plane = Plane.CreateByNormalAndOrigin(tf.BasisY, tf.Origin);
                ElementTransformUtils.MirrorElements(AC.Document, new List<ElementId>() { fi.Id }, plane, false);
            }
            Shape = fi;
        }

        private Line ProjectCurveOnPlane(Curve c, BPlane plane)
        {
            var sp = c.SP().ProjectOnto(plane);
            var ep = c.EP().ProjectOnto(plane);
            return Line.CreateBound(sp, ep);
        }

        private bool IsHorizontalCurveInView(Curve curve, View view)
        {
            var c = ProjectCurveOnPlane(curve, view.ToBPlane());
            var right = view.RightDirection;
            var direct = c.Direction();
            var angle = direct.AngleTo(right).RadiansToDegrees();
            if (angle > -45 && angle < 45 || angle > 135 && angle < 225)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsInRangeOfLine(Line line, XYZ p, bool isVertical = true)
        {
            if (isVertical)
            {
                var d1 = line.SP().DotProduct(View.UpDirection);
                var d2 = line.EP().DotProduct(View.UpDirection);
                var d = p.DotProduct(View.UpDirection);
                var max = Math.Max(d1, d2);
                var min = Math.Min(d1, d2);
                if (d > min && d < max)
                {
                    return true;
                }
            }
            else
            {
                var d1 = line.SP().DotProduct(View.RightDirection);
                var d2 = line.EP().DotProduct(View.RightDirection);
                var d = p.DotProduct(View.RightDirection);
                var max = Math.Max(d1, d2);
                var min = Math.Min(d1, d2);
                if (d > min && d < max)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class LoadFamilyOption : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source,
            out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}