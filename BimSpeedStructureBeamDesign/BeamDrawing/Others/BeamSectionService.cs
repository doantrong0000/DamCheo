using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;
using CommunityToolkit.Mvvm.DependencyInjection;
using MoreLinq.Extensions;
using System.Windows.Documents;
using ElementGeometry = BimSpeedStructureBeamDesign.BeamDrawing.Model.ElementGeometry;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
    public class BeamSectionService
    {
        public RebarsInBeam RebarsInBeam { get; set; }
        private List<ElementGeometry> floors = new();
        private ElementGeometry beamGeometry;
        private BPlane viewPlane;
        private FamilySymbol breakLineSymbol;
        private Autodesk.Revit.DB.View view;
        private MultiReferenceAnnotationType mraRight;
        private MultiReferenceAnnotationType mraLeft;
        private FamilySymbol fsIndependentTagRight;
        private FamilySymbol fsIndependentTagLeft;
        private List<Element> leftIndependentTags = new();
        private List<Element> rightIndependentTags = new();
        private List<Element> leftMultiReferenceAnnotations = new();
        private List<Element> rightMultiReferenceAnnotations = new();
        private Dictionary<RebarsInBeam.RebarInBeamLocation, List<Element>> dicTags = new();
        private XYZ leftPointOfTagLeft = null;
        private XYZ rightPointOfTagLeft = null;
        private string mark = "";
        public BeamSectionSetting Setting { get; set; }
        public List<BeamGeometry> BeamGeometries { get; set; }

        public BeamSectionService(Autodesk.Revit.DB.View view, BeamSectionSetting setting, FamilyInstance beam = null)
        {
            this.view = view;
            if (beam != null)
            {
                beamGeometry = new ElementGeometry(beam, view);
            }
            else
            {
                beam = GetMainBeamInView();
                beamGeometry = new ElementGeometry(beam, view);
            }
            Setting = setting;
            mark = beam.GetMark();
            GetData();
        }

        public void Run()
        {
            DeleteOldDetail();


            if (breakLineSymbol != null && breakLineSymbol.IsValidObject && breakLineSymbol.Id.GetElementIdValue() == -1 && !breakLineSymbol.IsActive)
            {
                breakLineSymbol.Activate();
            }

            if (fsIndependentTagRight != null && fsIndependentTagRight.IsValidObject && fsIndependentTagRight.Id.GetElementIdValue() == -1 && !fsIndependentTagRight.IsActive)
            {
                fsIndependentTagRight.Activate();
            }

            if (fsIndependentTagLeft != null && fsIndependentTagLeft.IsValidObject && fsIndependentTagLeft.Id.GetElementIdValue() == -1 && !fsIndependentTagLeft.IsActive)
            {
                fsIndependentTagLeft.Activate();
            }



            if (Setting.IsDrawBreakLine)
            {
                CreateBreakLines();
            }

            if (Setting.IsDrawTagRebar)
            {
                CreateTags();
            }

            if (Setting.IsDrawDim || Setting.IsDrawTagElevation)
            {
                CreateDimsAndSpot();
            }
            view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE).Set(0);
      
       
            if (Setting.ViewTemplate != null)
            {
                view.ViewTemplateId = Setting.ViewTemplate.Id;
            }
            try
            {
                view.Scale = Setting.Scale;
            }
            catch
            {
                //
            }
            if (Setting.IsCrossSection)
            {
                SetNameForDetail();
            }
            //HideRebarsNotInHost();
            view.SetParameterValueByName(BuiltInParameter.SECTION_COARSER_SCALE_PULLDOWN_METRIC, 100);
        }

        private void DeleteOldDetail()
        {
            var ListDelete = new List<ElementId>();


            if (Setting.IsDrawDim)
            {
             var DimIds = new FilteredElementCollector(AC.Document, view.Id)
             .OfCategory(BuiltInCategory.OST_Dimensions)
             .Where(x => x.IsValidObject)
              .Where(x => x.LookupParameter("Label") != null)
             .Select(x => x.Id)
             .ToList();

                ListDelete.AddRange(DimIds);
            }
          

            if (Setting.IsDrawTagRebar)
            {
              var  rebarTagIds = new FilteredElementCollector(AC.Document, view.Id)
                .OfCategory(BuiltInCategory.OST_RebarTags) // Lọc theo Rebar Tag
                .WhereElementIsNotElementType()
                .Where(x => x.IsValidObject)
                .Select(x => x.Id)
                .ToList();
                ListDelete.AddRange(rebarTagIds);
            }



            if (Setting.IsDrawStick)
            {
               var detaiComponentsIds = new FilteredElementCollector(AC.Document, view.Id)
                 .OfCategory(BuiltInCategory.OST_DetailComponents)
                 .WhereElementIsNotElementType()
                 .Where(x => x.IsValidObject).Where(x=> (x as FamilyInstance)?.Symbol.Family.Name =="BS_DAUTHEP")
                 .Select(x => x.Id)
                 .ToList();
                ListDelete.AddRange(detaiComponentsIds);
            }

            if (Setting.IsDrawBreakLine)
            {
               var Breakline = new FilteredElementCollector(AC.Document, view.Id)
                    .OfCategory(BuiltInCategory.OST_DetailComponents)
                    .WhereElementIsNotElementType()
                    .Where(x => x.IsValidObject)
                    .Where(x => x.LookupParameter("Depth") != null && x.LookupParameter("Depth").AsDouble() > 0)
                    .Select(x => x.Id)
                    .ToList();
          
                ListDelete.AddRange(Breakline);
            }
            if (Setting.IsDrawTagElevation)
            {
               var spotIds = new FilteredElementCollector(AC.Document, view.Id)
                .OfCategory(BuiltInCategory.OST_SpotElevations)
                .WhereElementIsNotElementType()
                .Where(x => x.IsValidObject)
                .Select(x => x.Id)
                .ToList();
                var Lines = new FilteredElementCollector(AC.Document, view.Id)
                  .OfCategory(BuiltInCategory.OST_Lines)
                  .Where(x => x.IsValidObject)
                  .Select(x => x.Id)
                  .ToList();
                ListDelete.AddRange(spotIds);
                ListDelete.AddRange(Lines);
            } 
            try
            {
              
                if (ListDelete != null && ListDelete.Any())
                {
                    foreach (var Id in ListDelete)
                    {
                        var element = AC.Document.GetElement(Id);
                        if (element != null && element.IsValidObject)
                        {
                            AC.Document.Delete(Id);
                        }
                    }
                }

              
            }
            catch (Exception ex)
            {
                
            }
        }

        private void GetData()
        {
            //View
            viewPlane = BPlane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin);
            //Get family or load
            GetSomeFamily();
            //Get Element Geometry
            GetElementGeometry();
            //Rebar in Beam
            RebarsInBeam = RebarsInBeam.Instance;
            RebarsInBeam.GetInfo(beamGeometry, view);
        }

        private FamilyInstance GetMainBeamInView()
        {
            var beams = new FilteredElementCollector(AC.Document, view.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>().ToList();
            foreach (var fi in beams)
            {
                var line = GetBeamLine(fi);
                if (line.Direction.IsParallel(view.ViewDirection))
                {
                    return fi;
                }
            }
            return null;
        }

        private Line GetBeamLine(FamilyInstance fi)
        {
            Line line = null;
            var beamCenter = fi.GetElementCenter(null);

            if (fi.Location is LocationCurve lc)
            {
                var c = lc.Curve;
                line = c as Line;
                if (line != null)
                {
                    var plane = BPlane.CreateByNormalAndOrigin(line.Direction.CrossProduct(XYZ.BasisZ), beamCenter);
                    var sp = line.SP().ProjectOnto(plane);
                    var ep = line.EP().ProjectOnto(plane);
                    line = Line.CreateBound(sp, ep);
                }
            }
            return line;
        }

        private void HideRebarsNotInHost()
        {
            var rebars = new FilteredElementCollector(AC.Document, view.Id).OfClass(typeof(Rebar)).Cast<Rebar>()
                .ToList();
            var hostIds = new List<int>() { beamGeometry.Element.Id.GetElementIdValue()};
            var hideIds = new List<ElementId>();
            var areaRebars = new FilteredElementCollector(AC.Document, view.Id).OfClass(typeof(RebarInSystem)).Cast<RebarInSystem>()
                .ToList();
            foreach (var rebar in rebars)
            {
                var id = rebar.GetHostId();
                if (id != null)
                {
                    if (hostIds.Contains(id.GetElementIdValue()) == false)
                    {
                        hideIds.Add(rebar.Id);
                    }
                }
            }
            foreach (var rebar in areaRebars)
            {
                var id = rebar.GetHostId();
                if (id != null)
                {
                    if (hostIds.Contains(id.GetElementIdValue()) == false)
                    {
                        hideIds.Add(rebar.Id);
                    }
                }
            }
            try
            {
                view.HideElements(hideIds);
            }
            catch (Exception)
            {
                //
            }
        }

        private void CreateBreakLines()
        {
            //Right BreakLines
            var rightCenter = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane).Add(view.RightDirection * Setting.KhoangCachBreakLine);

            var rightLine = Line.CreateBound(rightCenter.Add(view.UpDirection * 20), rightCenter.Add(view.UpDirection * -20));

            var floorSolids = floors.Select(x => x.Solids).ToList().Flatten().Cast<Solid>().ToList();
            var insideLines = rightLine.GetInsideLinesIntersectSolids(floorSolids);
            foreach (var insideLine in insideLines)
            {
                var extendLine = ExtendLine(insideLine, 20, view.UpDirection);
                var fi = AC.Document.Create.NewFamilyInstance(extendLine, breakLineSymbol, view);
                var bb = fi.get_BoundingBox(view);
            }

            //Left BreakLines
            var leftCenter = beamGeometry.LeftPlanarFace.FaceCenter().ProjectOnto(viewPlane).Add(view.RightDirection * (-80).MmToFoot());
            var leftLine = Line.CreateBound(leftCenter.Add(view.UpDirection * 20), leftCenter.Add(view.UpDirection * -20));
            var insideLinesLeft = leftLine.GetInsideLinesIntersectSolids(floorSolids);
            foreach (var insideLine in insideLinesLeft)
            {
                var extendLine = ExtendLine(insideLine, 20, -view.UpDirection);
                AC.Document.Create.NewFamilyInstance(extendLine, breakLineSymbol, view);
            }
        }
        private void CreateDimsAndSpot()
        {
                //Left Dim
            var raLeft = new ReferenceArray();
            var topPlanarFaces = floors.Select(x => x.TopPlanarFace).ToList();
            topPlanarFaces.Insert(0, beamGeometry.TopPlanarFace);
            topPlanarFaces.Add(beamGeometry.BotPlanarFace);
            topPlanarFaces = topPlanarFaces.Where(x => x != null).OrderByDescending(x => x.Origin.Z).ToList();
            var topFace = topPlanarFaces.FirstOrDefault();
            var botFace = topPlanarFaces.LastOrDefault();
            if (topFace != null) raLeft.Append(topFace.Reference);
            if (botFace != null) raLeft.Append(botFace.Reference);

            var end = beamGeometry.RightPlanarFace.Origin.EditZ(beamGeometry.ZMax).ProjectOnto(viewPlane)
                .Add(view.RightDirection * beamGeometry.Width / -2);

            //if (Setting.SpotDimensionType != null && Setting.IsCreateSpot)
            //{
            //    //Find Top Face Slab,Find Top Face Beam if null create a line to dim
            //    Reference rf;
            //    if (topFace != null)
            //    {
            //        rf = topFace.Reference;
            //        var origin = topFace.Origin;
            //        var midPlane = Plane.CreateByNormalAndOrigin(beamGeometry.LeftPlanarFace.FaceNormal, beamGeometry.Transform.Origin);
            //        origin = origin.ProjectOnto(midPlane);
            //        try
            //        {
            //            var b = beamGeometry.RightPlanarFace.Origin.EditZ(origin.Z).ProjectOnto(viewPlane)
            //                .Add(view.RightDirection * 100.MmToFoot());
            //            if (rightPointOfTagLeft != null)
            //            {
            //                end = rightPointOfTagLeft.EditZ(origin.Z).Add(view.RightDirection * 100.MmToFoot());
            //                b = end.Add(view.RightDirection * -10.MmToFoot());
            //            }
            //            var spot = AC.Document.Create.NewSpotElevation(view, rf, origin, b,
            //                end, origin, true);
            //            spot.ChangeTypeId(Setting.SpotDimensionType.Id);
            //        }
            //        catch (Exception e)
            //        {
            //            AC.Log("Lỗi tạo spot elevation" + Environment.NewLine + e.Message);
            //        }
            //    }
            //}




            if (Setting.IsDrawDim)
            {
                var leftNum = Setting.KhoangCachSideDimDenDam;
            var leftSp = beamGeometry.LeftPlanarFace.Origin.ProjectOnto(viewPlane).Add(view.RightDirection * -leftNum);
            var leftLine = leftSp.CreateLineByPointAndDirection(view.UpDirection);
            if (raLeft.Size > 1)
            {
                if(Setting.IsDrawDim)
                {
                    var leftDimension = AC.Document.Create.NewDimension(view, leftLine, raLeft, Setting.DimensionTypeFixed);
                }
               
                //Move Dim Left nếu tag bên trái
                //if (leftPointOfTagLeft != null)
                //{
                //    var origin = leftDimension.Origin;
                //    if (origin.DotProduct(view.RightDirection) > leftPointOfTagLeft.DotProduct(view.RightDirection))
                //    {
                //        var d = Math.Abs((origin - leftPointOfTagLeft).DotProduct(view.RightDirection)) + 1.MmToFoot() * view.Scale;
                //        ElementTransformUtils.MoveElement(AC.Document, leftDimension.Id, view.RightDirection * -d);
                //    }
                //}
            }

            //Bot Dim
            var raBot = new ReferenceArray();
            raBot.Append(beamGeometry.RightPlanarFace.Reference);
            raBot.Append(beamGeometry.LeftPlanarFace.Reference);
            var minZ = beamGeometry.BotPlanarFace.Origin.Z - (RebarsInBeam.B1Rebars.Count - 1) * 160.MmToFoot() -
                       Setting.KhoangCachBotDimDenDam;
            var botCenter = beamGeometry.BotPlanarFace.FaceCenter().ProjectOnto(viewPlane);
            botCenter = new XYZ(botCenter.X, botCenter.Y, minZ);
            var botLine = botCenter.CreateLineByPointAndDirection(view.RightDirection);
          
                AC.Document.Create.NewDimension(view, botLine, raBot, Setting.DimensionTypeFixed);
            }

            if (Setting.IsDrawTagElevation)
            {
                double Distance = 200.MmToFoot();
                if (Setting.KhoangCachTagElevationDenDam > 200 )
                {
                    Distance = Setting.KhoangCachTagElevationDenDam.MmToFoot();
                }
                if (Setting.KhoangCachTagElevationDenDam < -200)
                {
                    Distance = Setting.KhoangCachTagElevationDenDam.MmToFoot();
                }
                // Tạo một Detail Line tạm thời
                var line = Line.CreateBound(end.Add(view.RightDirection * -Distance), end.Add(view.RightDirection * -Distance*1.2));
                var detailLine = AC.Document.Create.NewDetailCurve(view, line);

                if (detailLine != null)
                {
                    // Tạo Spot Elevation
                    try
                    {
                        AC.Document.Create.NewSpotElevation(view, detailLine.GeometryCurve.Reference, end.Add(view.RightDirection * -Distance), end.Add(view.RightDirection * -Distance * 1),
                            end.Add(view.RightDirection * -Distance * 1), end.Add(view.RightDirection * -Distance), true);
                    }
                    catch (Exception e)
                    {
                        AC.Log("Spot elevation", e);
                    }

            
                }
            }
        }

        private void CreateTags()
        {
            TagRebar(RebarsInBeam.RebarInBeamLocation.T1);
            TagRebar(RebarsInBeam.RebarInBeamLocation.T2);
            TagRebar(RebarsInBeam.RebarInBeamLocation.T3);
            TagRebar(RebarsInBeam.RebarInBeamLocation.B1);
            TagRebar(RebarsInBeam.RebarInBeamLocation.B2);
            TagRebar(RebarsInBeam.RebarInBeamLocation.B3);
            TagMidBar();
            TagStirrups();
        }

        private void GetSomeFamily()
        {
            //BreakLine
            breakLineSymbol = Setting.BreakLineSymbol;
            //Tag type
            mraRight = Setting.TagThepNhomPhai;
            mraLeft = Setting.TagThepNhomTrai;
            fsIndependentTagRight = Setting.IndependentTagRebarStandardRight;
            fsIndependentTagLeft = Setting.IndependentTagRebarStandardLeft;
        }

        private void GetElementGeometry()
        {
            //Floors
            floors = new FilteredElementCollector(AC.Document, view.Id).OfCategory(BuiltInCategory.OST_Floors).Select(x => new ElementGeometry(x, view)).ToList();
            //Beam
            if (beamGeometry == null)
            {
                beamGeometry = new FilteredElementCollector(AC.Document, view.Id).OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming).Select(x => new ElementGeometry(x, view)).FirstOrDefault();
                if (beamGeometry == null)
                {
                    "BEAMSECTIONSERVICE_MESSAGE".NotificationError(this);
                }

            }
        }

        private void TagRebar(RebarsInBeam.RebarInBeamLocation locaion)
        {
            var rightVector = view.RightDirection;
            var upVecTor = view.UpDirection;
            double z = 0.0;
            XYZ p = XYZ.Zero;
            List<Rebar> rebars = RebarsInBeam.GetRebars(locaion).Where(IsRebarDirectionParallelBeamDirection).ToList();

            switch (locaion)
            {
                case RebarsInBeam.RebarInBeamLocation.T1:
                    z = beamGeometry.ZMax + 70.MmToFoot();
                    p = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    break;

                case RebarsInBeam.RebarInBeamLocation.T2:
                    z = beamGeometry.ZMax - 120.MmToFoot();
                    p = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    upVecTor = -upVecTor;
                    break;

                case RebarsInBeam.RebarInBeamLocation.T3:
                    z = beamGeometry.BoundingBoxXyz.Max.Z - 200.MmToFoot();
                    upVecTor = -upVecTor;
                    rightVector = -rightVector;
                    p = beamGeometry.LeftPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    break;

                case RebarsInBeam.RebarInBeamLocation.B1:
                    z = beamGeometry.ZMin - 70.MmToFoot();
                    p = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    upVecTor = -upVecTor;
                    break;

                case RebarsInBeam.RebarInBeamLocation.B2:
                    z = beamGeometry.ZMin + 120.MmToFoot();
                    p = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    break;

                case RebarsInBeam.RebarInBeamLocation.B3:
                    z = beamGeometry.ZMin + 200.MmToFoot();
                    rightVector = -rightVector;
                    p = beamGeometry.LeftPlanarFace.FaceCenter().ProjectOnto(viewPlane);
                    break;

            }

            p = new XYZ(p.X, p.Y, z);

            var isRight = rightVector.IsOppositeDirectionTo(-view.RightDirection);
            rebars = rebars.OrderByDescending(RebarTotalSpacing).ToList();
            var rebarMores = rebars.Select(x => new RebarMore(x)).GroupBy(x => x.RebarNumber);
            foreach (var g in rebarMores)
            {
                Element tag;
                var quantity = g.Sum(x => x.Rebar.Quantity);
                if (quantity == 1)
                {
                    tag = CreateIndependentTag(g.First().Rebar, p, isRight);
                    p = p.Add(upVecTor * 6 * view.Scale.MmToFoot());
                    if (isRight)
                    {
                        rightIndependentTags.Add(tag);
                    }
                    else
                    {
                        leftIndependentTags.Add(tag);
                    }
                }
                else
                {
                    var ids = g.Select(x => x.Rebar.Id).ToList();
                    tag = CreateMultiReferenceAnnotation(isRight, ids, p, p);
                    p = p.Add(upVecTor * 6 * view.Scale.MmToFoot());

                    if (isRight)
                    {
                        rightMultiReferenceAnnotations.Add(tag);
                    }
                    else
                    {
                        leftMultiReferenceAnnotations.Add(tag);
                    }
                }

                if (dicTags.ContainsKey(locaion))
                {
                    dicTags[locaion].Add(tag);
                }
                else
                {
                    dicTags.Add(locaion, new List<Element> { tag });
                }
            }
        }

        private bool IsRebarDirectionParallelBeamDirection(Rebar rebar)
        {
            var c = rebar.GetRebarCurves().OrderBy(x => x.Length).LastOrDefault();
            if (c == null)
            {
                return false;
            }

            if (c.Direction().IsParallel(view.ViewDirection))
            {
                return true;
            }

            return false;
        }

        private void TagMidBar()
        {
            foreach (var midBar in RebarsInBeam.MidBars)
            {
                var center = midBar.RebarCenterPoint().ProjectOnto(viewPlane);
                var rightPlane =
                    BPlane.CreateByNormalAndOrigin(view.RightDirection, beamGeometry.RightPlanarFace.Origin);

                if (midBar.IsStandardRebar())
                {
                    var right = center.ProjectOnto(rightPlane).Add(view.UpDirection * (-35).MmToFoot()).ProjectOnto(viewPlane);
                    var quantity = midBar.Quantity;
                    if (quantity == 1)
                    {
                        CreateIndependentTag(midBar, right, true);
                    }
                    else
                    {
                        var ids = new List<ElementId>() { midBar.Id };
                        CreateMultiReferenceAnnotation(true, ids, right, right);
                    }
                }
                else
                {
                    var right = center.ProjectOnto(rightPlane).Add(view.UpDirection * 35.MmToFoot()).ProjectOnto(viewPlane);
                    CreateIndependentTag(midBar, right, true);
                }
            }
        }

        private void TagStirrups()
        {
            var ds = RebarsInBeam.Stirrups.DistinctBy2(x => x.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER)).ToList();

            var center = beamGeometry.RightPlanarFace.FaceCenter().ProjectOnto(viewPlane).EditZ(beamGeometry.ZMax * 0.5 + beamGeometry.ZMin * 0.5);
            var isHasStirrupNgang = false;
            for (var index = 0; index < ds.Count; index++)
            {
                var stirrup = ds[index];
                var type = CheckStirrupType(stirrup);
                if (type == StirrupType.DaiChuNhat)
                {
                    //Có thép gia cường dưới ko có ra cường trên
                    if (RebarsInBeam.B2Rebars.Count > 0 && RebarsInBeam.T2Rebars.Count == 0)
                    {
                        center = center.Add(XYZ.BasisZ * (beamGeometry.ZMax - beamGeometry.ZMin) * 0.15);
                        CreateIndependentTag(stirrup, center, index >= 0, true);
                    }
                    else if (RebarsInBeam.B2Rebars.Count == 0 && RebarsInBeam.T2Rebars.Count > 0)
                    {
                        center = center.Add(XYZ.BasisZ * (beamGeometry.ZMax - beamGeometry.ZMin) * -0.15);
                        CreateIndependentTag(stirrup, center, index >= 0, true);
                    }
                    else if (RebarsInBeam.B2Rebars.Count == 0 && RebarsInBeam.T2Rebars.Count == 0)
                    {
                        if (index == 0)
                        {
                            center = center.Add(XYZ.BasisZ * (beamGeometry.ZMax - beamGeometry.ZMin) * -0.15);
                            CreateIndependentTag(stirrup, center, true, true);
                        }
                        else
                        {
                            center = center.Add(XYZ.BasisZ * (beamGeometry.ZMax - beamGeometry.ZMin) * 0.3);
                            CreateIndependentTag(stirrup, center, true, true);
                        }

                    }
                    else
                    {
                        if (index == 0)
                        {
                            CreateIndependentTag(stirrup, center, true, true);
                        }
                        else
                        {
                            CreateIndependentTag(stirrup, center, true, true);
                        }

                    }
                }
                else
                {
                    var maxCurve = stirrup.GetRebarCurves().OrderByDescending(x => x.Length).FirstOrDefault();
                    var mid = maxCurve.Midpoint().ProjectOnto(viewPlane);

                    if (type == StirrupType.DaiMocNgang)
                    {
                        if (IsNearTop(mid))
                        {
                            mid = mid.Add(XYZ.BasisZ * -1 * 100.MmToFoot());
                        }
                        else
                        {
                            mid = mid.Add(XYZ.BasisZ * 1 * 100.MmToFoot());
                        }

                        isHasStirrupNgang = true;
                        CreateIndependentTag(stirrup, mid, false, true, true);
                    }
                    else
                    {
                        if (isHasStirrupNgang)
                        {
                            mid = mid.Add(XYZ.BasisZ * -1 * 35.MmToFoot());
                        }
                        CreateIndependentTag(stirrup, mid, false, true);
                    }
                }

            }
        }

        bool IsNearTop(XYZ point)
        {
            return Math.Abs(point.Z - beamGeometry.ZMax) < Math.Abs(point.Z - beamGeometry.ZMin);
        }


        private StirrupType CheckStirrupType(Rebar rebar)
        {
            var shape = rebar.GetRebarShape().Name;
            var mainCurve = rebar.GetRebarCurves().OrderByDescending(x => x.Length).FirstOrDefault();
            var direct = mainCurve.Direction();
            if (shape.Equals("M_01"))
            {
                if (Math.Abs(direct.DotProduct(XYZ.BasisZ)) < 0.1)
                {
                    return StirrupType.DaiMocNgang;
                }

                return StirrupType.DaiMocDung;
            }

            return StirrupType.DaiChuNhat;
        }

        enum StirrupType
        {
            DaiMocNgang = 1,
            DaiMocDung = 2,
            DaiChuNhat = 3
        }

           private void SetNameForDetail()
        {
            if (string.IsNullOrEmpty(mark))
            {
                if (BeamGeometries != null && BeamGeometries.Any())
                {
                    mark = BeamGeometries.First().Beam.Id.GetElementIdValue().ToString();
                }
                else
                {
                    mark = "Section"; // Giá trị mặc định nếu không có Beam
                }
            }
            int i = 1;
            string baseName = string.IsNullOrEmpty(Setting.DetailSectionName) ? mark : $"{Setting.DetailSectionName} {mark}";
            string name = baseName;

            while (true)
            {
                try
                {
                    view.Name = name;
                    break; // Nếu đặt tên thành công, thoát vòng lặp
                }
                catch
                {
                    name = $"{baseName} - {i}";
                    i++;
                }
            }

        }
        private IndependentTag CreateIndependentTag(Rebar rebar, XYZ p, bool isRight, bool isStirrup = false, bool isMove90 = false)
        {
            var tagSymbolId = fsIndependentTagRight.Id;
            if (isStirrup)
            {
                tagSymbolId = Setting.TagThepDaiPhai?.Id;
            }

            if (tagSymbolId==null)
            {
               return null;
            }

            if (isRight == false)
            {
                tagSymbolId = fsIndependentTagLeft.Id;
                if (isStirrup)
                {
                    tagSymbolId = Setting.TagThepDaiTrai.Id;
                }
            }

            var centerPoint = CommonService.GetMaxLineOfRebar(rebar).Midpoint().ProjectOnto(viewPlane);
            var leaderElbow = new XYZ(centerPoint.X, centerPoint.Y, p.Z);
            var pp = isRight ? p.Add(view.RightDirection * -2) : p.Add(view.RightDirection * 2);

            var independentTag =
                TagUtils2.CreateIndependentTag(tagSymbolId, view.Id, rebar, false, TagOrientation.Horizontal, pp);


            MoveTag(independentTag, isRight);


            if (!rebar.IsContainRebar(RebarsInBeam.Stirrups) || isMove90)
            {
                independentTag.LeaderEndCondition = LeaderEndCondition.Free;
                independentTag.SetLeaderElbow(leaderElbow);
                independentTag.SetLeaderEnd(centerPoint);
            }

            if (isRight == false)
            {
                AC.Document.Regenerate();
                var bb = independentTag.get_BoundingBox(view);
                if (bb != null)
                {
                    var min = bb.Min;
                    var max = bb.Max;
                    if (min.DotProduct(view.RightDirection) < max.DotProduct(view.RightDirection))
                    {
                        leftPointOfTagLeft = min;
                    }
                    else
                    {
                        leftPointOfTagLeft = max;
                    }
                }
            }
            else
            {
                AC.Document.Regenerate();
                var bb = independentTag.get_BoundingBox(view);
                if (bb != null)
                {
                    var min = bb.Min;
                    var max = bb.Max;
                    if (min.DotProduct(view.RightDirection) > max.DotProduct(view.RightDirection))
                    {
                        rightPointOfTagLeft = min;
                    }
                    else
                    {
                        rightPointOfTagLeft = max;
                    }
                }
            }
            return independentTag;
        }

        private void MoveTag(IndependentTag independentTag, bool isRight = true)
        {
            AC.Document.Regenerate();
            independentTag.HasLeader = false;
            AC.Document.Regenerate();
            var bb = independentTag.get_BoundingBox(view);
            if (bb != null)
            {
                var min = bb.Min;
                var max = bb.Max;
                if (isRight)
                {
                    var first = view.RightDirection.FirstPointByDirection(new List<XYZ> { min, max });
                    var d = (first - beamGeometry.RightPlanarFace.Origin).DotProduct(view.RightDirection);
                    ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, view.RightDirection * (Setting.KhoangCachTagDenDam - d));
                }
                else
                {
                    var first = (-view.RightDirection).FirstPointByDirection(new List<XYZ> { min, max });
                    var d = (first - beamGeometry.LeftPlanarFace.Origin).DotProduct(view.RightDirection);
                    ElementTransformUtils.MoveElement(AC.Document, independentTag.Id, -view.RightDirection * (Setting.KhoangCachTagDenDam + d));
                }
            }

            if (isRight)
            {
                AC.Document.Regenerate();
                bb = independentTag.get_BoundingBox(view);
                if (bb != null)
                {
                    var min = bb.Min;
                    var max = bb.Max;
                    rightPointOfTagLeft = min.DotProduct(view.RightDirection) > max.DotProduct(view.RightDirection) ? min : max;
                }
            }
            independentTag.HasLeader = true;
        }

        private MultiReferenceAnnotation CreateMultiReferenceAnnotation(bool isRight, List<ElementId> ids, XYZ lineOrigin, XYZ tagHeadPosition)
        {
            try
            {
                var type = isRight ? mraRight : mraLeft;
                var direction = isRight ? view.RightDirection : -view.RightDirection;
                var option = new MultiReferenceAnnotationOptions(type);
                option.SetElementsToDimension(ids);
                option.DimensionPlaneNormal = view.ViewDirection;
                option.DimensionLineDirection = direction;
                option.DimensionLineOrigin = lineOrigin;
                var p = isRight ? tagHeadPosition.Add(view.RightDirection * -2) : tagHeadPosition.Add(view.RightDirection * 2);
                option.TagHeadPosition = p;
                if (MultiReferenceAnnotation.AreReferencesValidForLinearDimension(AC.Document, view.Id, option))
                {
                    var multiTag = MultiReferenceAnnotation.Create(AC.Document, view.Id, option);
                    if (multiTag.TagId.ToElement() is IndependentTag indepentTag)
                    {
                        MoveTag(indepentTag, isRight);
                    }
                }
            }
            catch (Exception)
            {
                //
            }

            return null;
        }

        #region Helper Funtions

        private Line ExtendLine(Line line, double num, XYZ vector)
        {
            var direct = line.Direction;
            var p1 = line.SP();
            var p2 = line.EP();
            p1 = p1.Add(direct * (-num).MmToFoot());
            p2 = p2.Add(direct * num.MmToFoot());
            var l = Line.CreateBound(p1, p2);
            if (vector.IsOppositeDirectionTo(direct))
            {
                l = Line.CreateBound(p2, p1);
            }
            return l;
        }

        private double RebarTotalSpacing(Rebar rebar)
        {
            var num = 0.0;
            var quantity = rebar.Quantity;
            if (quantity != 1)
            {
                var maxSpacing = rebar.MaxSpacing;
                num = (quantity - 1) * maxSpacing;
            }
            return num;
        }

        #endregion Helper Funtions
    }

    public class RebarMore
    {
        public Rebar Rebar { get; set; }
        public string RebarNumber { get; set; }

        public RebarMore(Rebar rebar)
        {
            Rebar = rebar;
            RebarNumber = rebar.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER);
        }

    }
}