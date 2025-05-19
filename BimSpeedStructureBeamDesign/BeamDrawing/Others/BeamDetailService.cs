using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedUtils;
using MoreLinq.Extensions;
using ElementGeometry = BimSpeedStructureBeamDesign.BeamDrawing.Model.ElementGeometry;
using MessageBox = System.Windows.MessageBox;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Others
{
    public class BeamDetailService
    {
        public ViewSection ViewSection { get; set; }

        public bool IsDimDamMong { get; set; } = false;

        public List<XYZ> ListPointFaceVers { get; set; } = new List<XYZ>();

        public BeamDetailSetting Setting { get; set; }
        private List<Line> beamSegmentLines = new List<Line>();
        private BPlane plane;
        private List<ElementGeometryModel> supportElements = new List<ElementGeometryModel>();
        private List<ElementGeometryModel> gridGeometryModels = new List<ElementGeometryModel>();
        private double beamTopElevation;
        private double beamBotElevation;
        private List<ElementGeometryModel> topSupports = new List<ElementGeometryModel>();
        private List<ElementGeometryModel> botSupports = new List<ElementGeometryModel>();
        private List<FamilyInstance> beamSupport = new List<FamilyInstance>();
        private XYZ left;
        private FamilySymbol dauThep = null;
        public List<BeamGeometry> BeamGeometries { get; set; }

        public List<ElementGeometry> ListDamGoi { get; set; }
    
        public BeamDrawingSettingViewModel BeamDrawingSettingViewModel { get; set; }

        public BeamDetailService(ViewSection viewSection, BeamDrawingSettingViewModel beamDrawingSettingViewModel,
            List<BeamGeometry> beamGeometries = null, List<Element> supports = null)
        {
            ViewSection = viewSection;
            BeamGeometries = beamGeometries;
            if (supports != null)
            {
                beamSupport = supports.Cast<FamilyInstance>().ToList();
            }

            Setting = beamDrawingSettingViewModel.BeamDrawingSetting.BeamDetailSetting;

            if (BeamGeometries == null || BeamGeometries.Count == 0)
            {
                GetMainBeamInView();
            }
            else
            {
                if (beamSupport.Count > 0)
                {
                    foreach (var familyInstance in beamSupport)
                    {

                        var egm = new ElementGeometryModel(familyInstance, ViewSection);

                        supportElements.Add(egm);
                    }
                }
            }

            BeamDrawingSettingViewModel = beamDrawingSettingViewModel;

            GetData();

            var detailItems = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents).Cast<FamilySymbol>()
                .Where(x => x.Family.FamilyPlacementType == FamilyPlacementType.ViewBased).ToList();

            dauThep = detailItems.FirstOrDefault(x => x.Name == Define.FamilySymbolDauThep) ??
                      detailItems.FirstOrDefault();
        }

        public BeamDetailService(ViewSection viewSection, List<BeamGeometry> beamGeometries = null)
        {
            ViewSection = viewSection;

            BeamGeometries = beamGeometries;
            if (BeamGeometries == null || BeamGeometries.Count == 0)
            {
                GetMainBeamInView();
            }

            GetData();

            var detailItems = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents).Cast<FamilySymbol>()
                .Where(x => x.Family.FamilyPlacementType == FamilyPlacementType.ViewBased).ToList();

            dauThep = detailItems.FirstOrDefault(x => x.Name == Define.FamilySymbolDauThep) ??
                      detailItems.FirstOrDefault();
        }

        #region MainFunctions

        public void Run(bool isSetName = true)
        {
            DeleteOldDetail();

            SetViewTemplate();

            CommonService.SetRebarDetailLevel(ViewSection);

            if (Setting.IsDrawBreakLine)
            {
                CreateBreakLines();
            }

            beamSegmentLines = GetBeamSegmentLines();

            SetCropBox();

            SetGridsExtend();

            try
            {
                ViewSection.Scale = Setting.Scale;
            }
            catch
            {
                //
            }

            if(Setting.IsDrawTagRebar || Setting.IsDrawStick)
            {
                TagRebars();
            }

            SetViewTemplate();

            if (isSetName)
            {
                SetNameForDetail();
            }

            HideRebarsNotInHost();

            if(Setting.IsDrawDim  || Setting.IsDrawTagElevation)
            {
                if (IsDimDamMong)
                {
                    CreateDimensionsBeamFoundation();
                }
                else
                {
                    CreateDimensions();
                }
            }

     

        }

        private void DeleteOldDetail()
        {
            var ListDelete = new List<ElementId>();

            if (Setting.IsDrawDim)
            {
                var Dim = new FilteredElementCollector(AC.Document, ViewSection.Id)
                .OfCategory(BuiltInCategory.OST_Dimensions)
                .Where(x => x.IsValidObject)
                .Select(x => x.Id)
                .ToList();

                var rebarTagIds = new FilteredElementCollector(AC.Document, ViewSection.Id)
               .OfCategory(BuiltInCategory.OST_RebarTags) // Lọc theo Rebar Tag
               .WhereElementIsNotElementType()
               .Where(x => x.IsValidObject)
               .Select(x => x.Id)
               .ToList();

                var DimIds = Dim.Except(rebarTagIds).ToList();
                ListDelete.AddRange(DimIds);
            }


            if (Setting.IsDrawTagRebar)
            {
                var rebarTagIds = new FilteredElementCollector(AC.Document, ViewSection.Id)
                  .OfCategory(BuiltInCategory.OST_RebarTags) // Lọc theo Rebar Tag
                  .WhereElementIsNotElementType()
                  .Where(x => x.IsValidObject)
                  .Select(x => x.Id)
                  .ToList();
                ListDelete.AddRange(rebarTagIds);
            }



            if (Setting.IsDrawStick)
            {
                var detaiComponentsIds = new FilteredElementCollector(AC.Document, ViewSection.Id)
                  .OfCategory(BuiltInCategory.OST_DetailComponents)
                  .WhereElementIsNotElementType()
                  .Where(x => x.IsValidObject).Where(x => (x as FamilyInstance)?.Symbol.Family.Name == "BS_DAUTHEP")
                  .Select(x => x.Id)
                  .ToList();
                ListDelete.AddRange(detaiComponentsIds);
            }

            if (Setting.IsDrawBreakLine)
            {
                var Breakline = new FilteredElementCollector(AC.Document, ViewSection.Id)
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
                var spotIds = new FilteredElementCollector(AC.Document, ViewSection.Id)
              .OfCategory(BuiltInCategory.OST_SpotElevations)
              .WhereElementIsNotElementType()
              .Where(x => x.IsValidObject)
              .Select(x => x.Id)
              .ToList();
                var Lines = new FilteredElementCollector(AC.Document, ViewSection.Id)
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

        // Tao dau ngat thep 2d
        private void CreateMocThepForAdditionalBarsBeamFoundation(List<Rebar> rebars)
        {
            var centerZ = (beamTopElevation + beamBotElevation) / 2;

            foreach (var rebar in rebars)
            {
                var curves = rebar.GetRebarCurves();

                bool flag = true;

                var arc = curves.Where(x => x is Arc).ToList();

                if (arc.Count > 0)
                    flag = false;

                if (flag)
                {
                    Curve curveHoz = null;

                    //Get max curve
                    curveHoz = rebar.GetRebarCurves().Where(x => x.Direction().IsParallel(ViewSection.RightDirection))
                        .Maxima(x => x.Length).FirstOrDefault();

                    if (curveHoz != null && curveHoz is Line)
                    {
                        if (curveHoz.Direction().DotProduct(ViewSection.RightDirection) < 0)
                        {
                            curveHoz = curveHoz.CreateReversed();
                        }

                        if (ListPointFaceVers.Any())
                        {
                            if (curveHoz.SP() != null && IsPointNearSupports(ListPointFaceVers, curveHoz.SP()) == false)
                            {
                                if (curveHoz.SP().Z < centerZ)
                                {
                                    //Bot
                                    DauMocThepByPoint(10.MmToFoot(), curveHoz.SP(), true, false);
                                }
                                else
                                {
                                    //top
                                    DauMocThepByPoint(10.MmToFoot(), curveHoz.SP(), true, true);
                                }
                            }

                            if (curveHoz.EP() != null && IsPointNearSupports(ListPointFaceVers, curveHoz.EP()) == false)
                            {
                                if (curveHoz.EP().Z < centerZ)
                                {
                                    //Bot
                                    DauMocThepByPoint(10.MmToFoot(), curveHoz.EP(), false, false);
                                }
                                else
                                {
                                    //top
                                    DauMocThepByPoint(10.MmToFoot(), curveHoz.EP(), false, true);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateMocThepForAdditionalBars(List<Rebar> rebars)
        {
            var centerZ = (beamTopElevation + beamBotElevation) / 2;

            foreach (var rebar in rebars)
            {
                var curves = rebar.GetRebarCurves();

                if (curves.Count <= 0) return;

                Curve curveHoz = null;

                //Get max curve
                curveHoz = rebar.GetRebarCurves().Where(x => x.Direction().IsParallel(ViewSection.RightDirection))
                    .Maxima(x => x.Length).FirstOrDefault();

                if (curveHoz == null) return;

                var listPoints = new List<XYZ>();

                var hoz = curveHoz;

                var newCurves = curves.Where(x =>
                    Math.Abs(x.Length - hoz.Length) > 0.01 && !x.Direction().IsParallel(hoz.Direction())).ToList();

                foreach (var curve in newCurves)
                {
                    foreach (var xyze in curve.Tessellate())
                    {
                        listPoints.Add(xyze);
                    }
                }

                if (curveHoz is Line)
                {
                    if (curveHoz.Direction().DotProduct(ViewSection.RightDirection) < 0)
                    {
                        curveHoz = curveHoz.CreateReversed();
                    }

                    if (curveHoz.SP() != null && IsPointNearSupports(supportElements, curveHoz.SP()) == false)
                    {
                        bool flag1 = true;

                        foreach (var point in listPoints)
                        {
                            //if (Math.Abs(point.DotProduct(ViewSection.RightDirection) -
                            //             curveHoz.SP().DotProduct(ViewSection.RightDirection)) <= 100.MmToFoot())
                            //    flag1 = false;
                            if (point.DistanceTo(curveHoz.SP()) <= 50.MmToFoot())
                                flag1 = false;
                        }

                        if (flag1)
                        {
                            if (curveHoz.SP().Z < centerZ)
                            {
                                //Bot
                                DauMocThepByPoint(10.MmToFoot(), curveHoz.SP(), true, false);
                            }
                            else
                            {
                                //top
                                DauMocThepByPoint(10.MmToFoot(), curveHoz.SP(), true, true);
                            }
                        }
                    }

                    if (curveHoz.EP() != null && IsPointNearSupports(supportElements, curveHoz.EP()) == false)
                    {
                        bool flag1 = true;

                        foreach (var point in listPoints)
                        {
                            //if (Math.Abs(point.DotProduct(ViewSection.RightDirection) -
                            //             curveHoz.EP().DotProduct(ViewSection.RightDirection)) <= 200.MmToFoot())
                            //    flag1 = false;

                            if (point.DistanceTo(curveHoz.EP()) <= 50.MmToFoot())
                                flag1 = false;
                        }

                        if (flag1)
                            if (curveHoz.EP().Z < centerZ)
                            {
                                //Bot
                                DauMocThepByPoint(10.MmToFoot(), curveHoz.EP(), false, false);
                            }
                            else
                            {
                                //top
                                DauMocThepByPoint(10.MmToFoot(), curveHoz.EP(), false, true);
                            }
                    }
                }

                #region Code old

               

                #endregion
            }
        }

        private void DauMocThepByPoint(double diameter, XYZ p, bool isLeft, bool isTop)
        {
            if (dauThep != null && dauThep.Name == Define.FamilySymbolDauThep)
            {
                try
                {
                    if (dauThep == null)
                    {
                        return;
                    }

                    if (dauThep.IsActive == false)
                    {
                        dauThep.Activate();
                    }

                    var fi = AC.Document.Create.NewFamilyInstance(p, dauThep, ViewSection);
                    fi.SetParameterValueByName("DK", diameter);
                    if (isTop)
                    {
                        if (isLeft)
                        {
                            fi.flipHand();
                            ElementTransformUtils.MoveElements(AC.Document, new List<ElementId>() { fi.Id },
                                ViewSection.RightDirection * -7.7.MmToFoot());
                        }
                        else
                        {
                            ElementTransformUtils.MoveElements(AC.Document, new List<ElementId>() { fi.Id },
                                ViewSection.RightDirection * 7.7.MmToFoot());
                        }
                    }
                    else
                    {
                        fi.flipFacing(); //bot right
                        if (isLeft)
                        {
                            fi.flipHand();
                            ElementTransformUtils.MoveElements(AC.Document, new List<ElementId>() { fi.Id },
                                ViewSection.RightDirection * -7.7.MmToFoot());
                        }
                        else
                        {
                            ElementTransformUtils.MoveElements(AC.Document, new List<ElementId>() { fi.Id },
                                ViewSection.RightDirection * 7.7.MmToFoot());
                        }
                    }
                }
                catch (Exception e)
                {
                    AC.Log("Lỗi Tạo Dâu Thép" + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);
                }
            }
        }

        private void SetNameForDetail()
        {
            var mark = BeamGeometries.First().Mark;
            if (string.IsNullOrEmpty(mark))
            {
                mark = BeamGeometries.First().Beam.Id.GetElementIdValue().ToString();
            }

            int i = 1;
            string baseName = string.IsNullOrEmpty(Setting.DetailViewName) ? mark : $"{Setting.DetailViewName} {mark}";
            string name =baseName;

            while (true)
            {
                try
                {
                    ViewSection.Name = name;
                    break; // Nếu đặt tên thành công, thoát vòng lặp
                }
                catch
                {
                    name = $"{baseName} - {i}";
                    i++;
                }
            }
        }


        private void CreateDimensions()
        {
            var leftPoint2 = left.Add(-ViewSection.RightDirection *
                                    Setting.KhoangCachDimDenDamLeft);
            var leftPoint = left.Add(ViewSection.RightDirection);
            if (Setting.IsDrawDim)
            {
               
            var rapsOfGrid = gridGeometryModels.SelectMany(m => m.ReferenceAndPoints).ToList();

            var rapOfBeams = BeamGeometries
                .SelectMany(b => b.EdgeVers)
                .Where(e => e.Reference != null)
                .Select(e => new ReferenceAndPoint() { Reference = e.Reference, Point = e.SP() })
                .OrderBy(x => x.Point.DotProduct(ViewSection.RightDirection))
                .ToList();

            #region Top Dims

            var rapsGridsAndSupports = GetReferencesOfGridsAndSupports(true);
            //Get Stirrup References
            var stirrupReferences = GetStirrupReferencesToDim();
            //stirrupReferences.Clear();
            var rf2 = new ReferenceArray();

            var addiitionalTopBars = GetAdditionalTopBarReferences2();

            // Combine all references into one sequence
            var allReferences = addiitionalTopBars != null
                ? addiitionalTopBars.Concat(rapsGridsAndSupports).Concat(stirrupReferences).Concat(rapOfBeams)
                : rapsGridsAndSupports != null
                    ? rapsGridsAndSupports.Concat(stirrupReferences).Concat(rapOfBeams)
                    : stirrupReferences.Concat(rapOfBeams);

            // Filter distinct references and append to ReferenceArray
            allReferences
                .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number())
                .ForEach(x =>
                {
                    var reference = x.PlanarFace?.Reference ?? x.Reference;
                    rf2.Append(reference);
                });

            var p2 = BeamGeometries.First().BeamLine.SP().EditZ(beamTopElevation + Setting.KhoangCachDimDenDam);

            CommonService.CreateDimension(rf2, p2, ViewSection.RightDirection, Setting.DimensionTypeFixed, ViewSection);

            #endregion Top Dims

            #region Bot Dims

            //Grids Dimension:
            var sp = BeamGeometries.First().BeamLine.SP();

            var rapsGridsAndSupports2 = GetReferencesOfGridsAndSupports(false);

            var rf4 = new ReferenceArray();

            var additionBottomBarRefs = GetAdditionalBottomBarReferences();

            var additionalBottomBarReferences = additionBottomBarRefs != null
                ? additionBottomBarRefs
                    .Concat(rapsGridsAndSupports2).Concat(rapOfBeams)
                    .Concat(rapsOfGrid)
                    .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number()).ToList()
                : rapsGridsAndSupports2.Concat(rapOfBeams)
                    .Concat(rapsOfGrid)
                    .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number()).ToList();

            additionalBottomBarReferences.ForEach(x => rf4.Append(x.Reference));

            // Dim layer 1

            var p4 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam);
            var p5 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam - (6 * ViewSection.Scale).MmToFoot());
            CommonService.CreateDimension(rf4, p4, ViewSection.RightDirection, Setting.DimensionTypeFixed, ViewSection);

            // Dim layer 2
            var rapColAndBeams = rapOfBeams.Concat(rapsGridsAndSupports2)
                .OrderBy(x => x.Point.DotProduct(ViewSection.RightDirection))
                .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number())
                .ToList();

            var gridRa = new ReferenceArray();

            var rapsAll = new List<ReferenceAndPoint>()
                    { rapColAndBeams.FirstOrDefault(), rapColAndBeams.LastOrDefault() }
                .Concat(rapsOfGrid).DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number())
                .ToList();

            rapsAll.ForEach(rap => gridRa.Append(rap.Reference));

            CommonService.CreateDimension(gridRa, p5, ViewSection.RightDirection, Setting.DimensionTypeFixed,
                ViewSection);

            #endregion Bot Dims

            #region Left Dims

            var leftRa1 = new ReferenceArray();
            var egm = new ElementGeometryModel(BeamGeometries.First().Beam);
            var raps = egm.ReferenceAndPoints.Where(x => x.Normal.IsParallel(XYZ.BasisZ)).ToList();
            var allRaps = new List<ReferenceAndPoint>(raps);
            var floor = GetFloorNearAPoint(BeamGeometries.First().BeamLine.SP());
            if (floor != null)
            {
                var floorGeometryModel = new ElementGeometryModel(floor);
                var floorReferenceAndPoints = floorGeometryModel.ReferenceAndPoints
                    .Where(x => x.Normal.IsParallel(XYZ.BasisZ)).OrderBy(x => x.Point.Z).ToList();
                var floorBotRap = floorReferenceAndPoints.FirstOrDefault();
                var floorTopRap = floorReferenceAndPoints.LastOrDefault();
                if (floorTopRap != null && floorBotRap != null)
                {
                    allRaps.Add(floorTopRap);
                    allRaps.Add(floorBotRap);
                }
            }

            allRaps = allRaps.OrderBy(x => x.Point.Z).ToList();
            var topRap = allRaps.LastOrDefault();
            var botRap = allRaps.FirstOrDefault();

            if (topRap != null && botRap != null)
            {
                leftRa1.Append(topRap.Reference);
                leftRa1.Append(botRap.Reference);
                //var leftPoint1 = left.Add(-ViewSection.RightDirection *
                //                               (Setting.KhoangCachDimDenDamLeft +
                //                                Setting.KhoangCachGiua2Dim * ViewSection.Scale));

                var leftPoint1 = left.Add(-ViewSection.RightDirection *
                                          (Setting.KhoangCachDimDenDamLeft));
                CommonService.CreateDimension(leftRa1, leftPoint1, ViewSection.UpDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }

            var leftRa2 = new ReferenceArray();
            var leftRaps2 = allRaps.Distinct(new ReferenceAndPointComparerByZ());
            foreach (var referenceAndPoint in leftRaps2)
            {
                leftRa2.Append(referenceAndPoint.Reference);
            }

          
            if (leftRa2.Size > 2)
            {
                CommonService.CreateDimension(leftRa2, leftPoint2, ViewSection.UpDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }
          
            }
            if (Setting.IsDrawTagElevation)
            {
                var Distance = 200.MmToFoot();
                if (Setting.KhoangCachTagElevationDenDam > 0)
                {
                    Distance = Setting.KhoangCachTagElevationDenDam.MmToFoot();
                }
                //Create line
        

                var line = Line.CreateBound(leftPoint.Add(ViewSection.RightDirection * -Distance), leftPoint.Add(ViewSection.RightDirection * -Distance * 1.2));
                var detailLine = AC.Document.Create.NewDetailCurve(ViewSection, line);
                try
                {
                    AC.Document.Create.NewSpotElevation(AC.ActiveView, detailLine.GeometryCurve.Reference, leftPoint.Add(ViewSection.RightDirection * -Distance),
                        leftPoint.Add(ViewSection.RightDirection * -Distance),
                        leftPoint.Add(ViewSection.RightDirection *-Distance), leftPoint.Add(ViewSection.RightDirection * -Distance), true);
                }
                catch (Exception e)
                {
                    //
                }
            }

            #endregion Left Dims
        }

        private void CreateDimensionsBeamFoundation()
        {
            var rapsOfGrid = new List<ReferenceAndPoint>();

            if (gridGeometryModels.Count > 0)
                foreach (var elementGeometryModel in gridGeometryModels)
                {
                    rapsOfGrid.Add(elementGeometryModel.ReferenceAndPoints.FirstOrDefault());
                }

            //Get reference Of Beam
            var rapOfBeams = new List<ReferenceAndPoint>();

            foreach (var beamGeometry in BeamGeometries)
            {
                if (beamGeometry.EdgeVers.Count > 0)
                {
                    foreach (var edgeVer in beamGeometry.EdgeVers)
                    {
                        if (edgeVer.Reference != null)
                            rapOfBeams.Add(new ReferenceAndPoint()
                            {
                                Reference = edgeVer.Reference,
                                Point = edgeVer.SP(),
                            });
                    }
                }
            }

            #region Top Dims

            var rapsGridsAndSupports = GetRefenceAndPointBeamFoundation(false);
            //Get Stirrup References
            var stirrupReferences = GetStirrupReferencesToDim();
            //stirrupReferences.Clear();
            var rf2 = new ReferenceArray();

            if (rapsGridsAndSupports.Count > 0 || stirrupReferences.Count > 0)
            {
                var rapsAll = rapsGridsAndSupports.Concat(stirrupReferences)
                    .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number()).ToList();

                if (rapsAll.Count > 0)
                {
                    foreach (var x in rapsAll)
                    {
                        if (x.PlanarFace != null)
                        {
                            rf2.Append(x.PlanarFace.Reference);
                        }
                        else
                        {
                            rf2.Append(x.Reference);
                        }
                    }
                }
            }

            if (rf2.Size > 1)
            {
                var p2 = BeamGeometries.First().BeamLine.SP().EditZ(beamTopElevation + Setting.KhoangCachDimDenDam);

                CommonService.CreateDimension(rf2, p2, ViewSection.RightDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }

            #endregion Top Dims

            #region Bot Dims

            // Dim layer 1
            var sp = BeamGeometries.First().BeamLine.SP();

            var rapsGridsAndSupports2 = GetRefenceAndPointBeamFoundation(true);

            // lay 2 reference ngoài cùng
            var rapMaxMin = new List<ReferenceAndPoint>();

            var rapAllsList = rapsGridsAndSupports2.Concat(rapsOfGrid).Concat(rapOfBeams)
                .OrderBy(x => x.Point.DotProduct(ViewSection.RightDirection)).ToList();

            if (rapAllsList.Count > 0)
            {
                rapMaxMin.Add(rapAllsList.FirstOrDefault());
                rapMaxMin.Add(rapAllsList.LastOrDefault());
            }

            var rf4 = new ReferenceArray();

            var additionalBottomBarReferences = rapsGridsAndSupports2
                .Concat(rapsOfGrid)
                .Concat(rapMaxMin)
                .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number()).ToList();

            if (additionalBottomBarReferences.Count >= 2)
            {
                additionalBottomBarReferences.ForEach(x => rf4.Append(x.Reference));

                var p4 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam);

                CommonService.CreateDimension(rf4, p4, ViewSection.RightDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }
            // Dim layer 2

            var gridRa = new ReferenceArray();

            if (rapsOfGrid.Count >= 2)
            {
                var raps2 = rapsOfGrid.Concat(rapMaxMin)
                    .DistinctBy2(x => x.Point.DotProduct(ViewSection.RightDirection).Round2Number()).ToList();

                if (raps2.Count > 1)
                {
                    raps2.ForEach(f => gridRa.Append(f.Reference));

                    var p5 = sp.EditZ(beamBotElevation - Setting.KhoangCachDimDenDam -
                                      (6 * ViewSection.Scale).MmToFoot());

                    CommonService.CreateDimension(gridRa, p5, ViewSection.RightDirection, Setting.DimensionTypeFixed,
                        ViewSection);
                }
            }

            #endregion Bot Dims

            #region Left Dims

            var leftRa1 = new ReferenceArray();
            var egm = new ElementGeometryModel(BeamGeometries.First().Beam);
            var raps = egm.ReferenceAndPoints.Where(x => x.Normal.IsParallel(XYZ.BasisZ)).ToList();
            var allRaps = new List<ReferenceAndPoint>(raps);
            var floor = GetFloorNearAPoint(BeamGeometries.First().BeamLine.SP());
            if (floor != null)
            {
                var floorGeometryModel = new ElementGeometryModel(floor);
                var floorReferenceAndPoints = floorGeometryModel.ReferenceAndPoints
                    .Where(x => x.Normal.IsParallel(XYZ.BasisZ)).OrderBy(x => x.Point.Z).ToList();
                var floorBotRap = floorReferenceAndPoints.FirstOrDefault();
                var floorTopRap = floorReferenceAndPoints.LastOrDefault();
                if (floorTopRap != null && floorBotRap != null)
                {
                    allRaps.Add(floorTopRap);
                    allRaps.Add(floorBotRap);
                }
            }

            allRaps = allRaps.OrderBy(x => x.Point.Z).ToList();
            var topRap = allRaps.LastOrDefault();
            var botRap = allRaps.FirstOrDefault();

            if (topRap != null && botRap != null)
            {
                leftRa1.Append(topRap.Reference);
                leftRa1.Append(botRap.Reference);
                var leftPoint1 = left.Add(-ViewSection.RightDirection *
                                          (Setting.KhoangCachDimDenDamLeft +
                                           Setting.KhoangCachGiua2Dim * ViewSection.Scale));
                CommonService.CreateDimension(leftRa1, leftPoint1, ViewSection.UpDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }

            var leftRa2 = new ReferenceArray();
            var leftRaps2 = allRaps.Distinct(new ReferenceAndPointComparerByZ());
            foreach (var referenceAndPoint in leftRaps2)
            {
                leftRa2.Append(referenceAndPoint.Reference);
            }

            var leftPoint2 = left.Add(-ViewSection.RightDirection *
                                      Setting.KhoangCachDimDenDamLeft);
            if (leftRa2.Size > 2)
            {
                CommonService.CreateDimension(leftRa2, leftPoint2, ViewSection.UpDirection, Setting.DimensionTypeFixed,
                    ViewSection);
            }

            if (Setting.IsCreateSpot)
            {
                var Distance = 200.MmToFoot();
                if (Setting.KhoangCachTagElevationDenDam > 0)
                {
                    Distance = Setting.KhoangCachTagElevationDenDam.MmToFoot();
                    //Create line
                }
                var line = AC.Document.Create.NewDetailCurve(ViewSection,
                    leftPoint2.CreateLineByPointAndDirection(ViewSection.RightDirection * -Distance));

                try
                {
                    AC.Document.Create.NewSpotElevation(ViewSection, line.GeometryCurve.Reference, leftPoint2,
                        leftPoint2,
                        leftPoint2.Add(ViewSection.RightDirection * -Distance), leftPoint2, true);
                }
                catch (Exception e)
                {
                    AC.Log("không tạo đc spot elevation", e);
                }
            }

            #endregion Left Dims
        }

        private List<ReferenceAndPoint> GetReferencesOfGridsAndSupports(bool isTopSupport = true,
            bool isIncludeGrid = false)
        {
            var supports = topSupports;

            if (isTopSupport == false)
            {
                supports = botSupports;
            }

            var raps = new List<ReferenceAndPoint>();

            foreach (var support in supportElements)
            {
                if (support.IsColumn)
                {
                    if (support.BottomFace != null && support.BottomFace.Origin != null && support.BottomFace.Origin.Z < beamBotElevation)
                    {
                        foreach (var referenceAndPoint in support.ReferenceAndPoints)
                        {
                            if (referenceAndPoint.Normal.IsParallel(ViewSection.RightDirection))
                            {
                                raps.Add(referenceAndPoint);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var referenceAndPoint in support.ReferenceAndPoints)
                    {
                        //if (referenceAndPoint.Normal.CrossProduct(ViewSection.RightDirection).GetLength() < 0.0001.MmToFoot())
                        //{
                        //    //raps.Add(referenceAndPoint);
                        //}

                        if (referenceAndPoint.Normal.IsParallel(ViewSection.RightDirection))
                        {
                            raps.Add(referenceAndPoint);
                        }
                    }
                }
            }

            var gridRaps = gridGeometryModels.Select(x => x.ReferenceAndPoints.FirstOrDefault()).Where(x => x != null)
                .ToList();
            if (isIncludeGrid == false)
            {
                gridRaps.Clear();
            }

            return raps.Concat(gridRaps).ToList();
        }

        private List<ReferenceAndPoint> GetRefenceAndPointBeamFoundation(bool isBot = false)
        {
            var raps = new List<ReferenceAndPoint>();

            var listfoundations = supportElements.Where(x => x.IsFoundation).ToList();

            foreach (var support in supportElements)
            {
                if (support.IsColumn && !isBot)
                {
                    foreach (var referenceAndPoint in support.ReferenceAndPoints)
                    {
                        if (referenceAndPoint.Normal.IsParallel(ViewSection.RightDirection))
                        {
                            raps.Add(referenceAndPoint);
                        }
                    }
                }
                else if (!support.IsColumn)
                {
                    foreach (var referenceAndPoint in support.ReferenceAndPoints)
                    {
                        if (referenceAndPoint.Normal.IsParallel(ViewSection.RightDirection))
                        {
                            raps.Add(referenceAndPoint);
                        }
                    }
                }
            }

            //Get Edge Foundation 
            if (listfoundations.Count > 0 && isBot)
            {
                var maxZFace = listfoundations.Max(x => x.Transform.OfPoint(x.TopFace.Origin).Z);

                var listFoundations1 = listfoundations.Where(x =>
                    Math.Abs(x.Transform.OfPoint(x.TopFace.Origin).Z - maxZFace) <= 120.MmToFoot()).ToList();

                if (listFoundations1.Count > 0)
                {
                    foreach (var elementGeometryModel in listFoundations1)
                    {
                        var edgerVers = elementGeometryModel.EdgeVers
                            .OrderBy(x => x.SP().DotProduct(ViewSection.RightDirection)).ToList();

                        if (edgerVers.Count > 0)
                        {
                            raps.Add(new ReferenceAndPoint()
                            {
                                Reference = edgerVers.LastOrDefault()?.Reference,
                                Point = edgerVers.LastOrDefault()?.SP()
                            });

                            raps.Add(new ReferenceAndPoint()
                            {
                                Reference = edgerVers.FirstOrDefault()?.Reference,
                                Point = edgerVers.FirstOrDefault()?.SP()
                            });
                        }
                    }
                }
            }

            return raps.ToList();
        }

        private List<ReferenceAndPoint> GetReferencesOfGridsAndSupports1(bool isTopSupport = true,
            bool isIncludeGrid = false)
        {
            var supports = topSupports;

            if (isTopSupport == false)
            {
                supports = botSupports;
            }

            var raps = new List<ReferenceAndPoint>();

            foreach (var support in supportElements)
            {
                foreach (var referenceAndPoint in support.ReferenceAndPoints)
                {
                    //if (referenceAndPoint.Normal.CrossProduct(ViewSection.RightDirection).GetLength() < 0.0001.MmToFoot())
                    //{
                    //    //raps.Add(referenceAndPoint);
                    //}

                    if (referenceAndPoint.Normal.IsParallel(ViewSection.RightDirection))
                    {
                        raps.Add(referenceAndPoint);
                    }
                }
            }

            var gridRaps = gridGeometryModels.Select(x => x.ReferenceAndPoints.FirstOrDefault()).Where(x => x != null)
                .ToList();
            if (isIncludeGrid == false)
            {
                gridRaps.Clear();
            }

            return raps.Concat(gridRaps).ToList();
        }

        private void TagRebars()
        {
            #region Tag Stirupp

            AC.Document.Regenerate();
            var sts = new List<Rebar>();
            foreach (var beamGeometry in BeamGeometries)
            {
                var st = CommonService.GetStiruppsInView(beamGeometry.Beam);
                sts.AddRange(st);
            }

            sts = sts.Where(x => x != null).ToList();
            foreach (var rebar in sts)
            {
                if (rebar.GetRebarShape().Name == "M_01")
                {
                    continue;
                }

                var center = rebar.GetElementCenter();
                var number = rebar.NumberOfBarPositions;
                var spacing = rebar.MaxSpacing.FootToMm();

                var z = beamTopElevation +
                        Setting.KhoangCachDimDenDam;
                var p = center.EditZ(z);
                p = p.ProjectOnto(plane);

                if (number < 7 && spacing < 60)
                {
                    p = center;
                    TagUtils2.CreateIndependentTag(Setting.TagThepDaiTrai.Id, ViewSection.Id, rebar, false,
                        TagOrientation.Horizontal, p);

                    continue;
                }

                IndependentTag tag = TagUtils2.CreateIndependentTag(Setting.TagThepDaiTrai.Id, ViewSection.Id, rebar,
                    false,
                    TagOrientation.Horizontal, p);

                AC.Document.Regenerate();

                var bb = tag.get_BoundingBox(ViewSection);

                if (bb != null)
                {
                    var mid = bb.CenterPoint();
                    var t = (p - mid).DotProduct(ViewSection.RightDirection);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, t * ViewSection.RightDirection);
                    var tt = bb.Max.Z - z;
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id,
                        (-tt - ViewSection.Scale.MmToFoot()) * ViewSection.UpDirection);
                }
            }

            #endregion Tag Stirupp

            #region Tag Rebar Standard

            var rebars = new List<Rebar>();
            foreach (var beamGeometry in BeamGeometries)
            {
                rebars.AddRange(CommonService.GetStandardBarsInView(beamGeometry.Beam));
            }

            if (Setting.IsDrawTagRebar)
            {
                foreach (var beamSegmentLine in beamSegmentLines)
            {
                if (beamSegmentLine.Length.FootToMm() < 2000)
                {
                    var p = GetRebarsAtSegmentPoint(0.5, beamSegmentLine, rebars, out var topRebars, out var botRebars,
                        out var midRebars);
                    TagStandardRebars1(topRebars, p);
                    var tagBotMid = TagStandardRebars1(botRebars, p, false);

                    if (midRebars.Count > 0)
                    {
                        if (tagBotMid != null)
                        {
                            var bbOfTag = tagBotMid.get_BoundingBox(ViewSection);

                            var width = Math.Abs((bbOfTag.Max - bbOfTag.Min).DotProduct(ViewSection.RightDirection));

                            p = p.Add(ViewSection.RightDirection * width);
                        }
                      
                        TagStandardRebars1(midRebars, p, false);
                     
                    }
                }
                else
                {
                    var p1 = GetRebarsAtSegmentPoint(0.1, beamSegmentLine, rebars, out var topRebars1,
                        out var botRebars1, out var midRebars1);
                    var p2 = GetRebarsAtSegmentPoint(0.5, beamSegmentLine, rebars, out var topRebars2,
                        out var botRebars2, out var midRebars2);
                    var p3 = GetRebarsAtSegmentPoint(0.9, beamSegmentLine, rebars, out var topRebars3,
                        out var botRebars3, out var midRebars3);

                    //Chỉ dim additional top bar ở vị trí 1
                    //TagStandardRebars(topRebars1.GetRebarsNotIncludeInListOtherRebars(topRebars2), p1, true, false);
                    TagStandardRebars1(topRebars1, p1, true, false);
                    //Chỉ dim thép chính dưới ở vị trí 1
                    TagStandardRebars1(botRebars1, p1, false, false);
                    //TagStandardRebars(botRebars1.GetRebarsIncludeInLis2List(botRebars2), p1, false, false);

                    TagStandardRebars1(topRebars2, p2);
                    //TagStandardRebars(botRebars2.GetRebarsNotIncludeInListOtherRebars(botRebars1), p2, false);
                    var tagBotMid = TagStandardRebars1(botRebars2, p2, false);
                    //Tag mid
                    if (midRebars2.Count > 0)
                    {
                        if (tagBotMid != null)
                        {
                            var bbOfTag = tagBotMid.get_BoundingBox(ViewSection);

                            var width = Math.Abs((bbOfTag.Max - bbOfTag.Min).DotProduct(ViewSection.RightDirection));

                            p2 = p2.Add(ViewSection.RightDirection * width);
                        }

                        TagStandardRebars1(midRebars2, p2, false);
                    }

                    TagStandardRebars1(topRebars3, p3);
                    //TagStandardRebars(botRebars3.GetRebarsNotIncludeInListOtherRebars(botRebars2), p3, false);
                    TagStandardRebars1(botRebars3, p3, false);
                }
            }
            }
            if (Setting.IsDrawStick)
            {
                if (IsDimDamMong)
                {
                    CreateMocThepForAdditionalBarsBeamFoundation(rebars);
                }
                else
                {
                    CreateMocThepForAdditionalBars(rebars);
                }
            }
        

            #endregion Tag Rebar Standard
        }

        private IndependentTag TagStandardRebars1(List<Rebar> rebars, XYZ p, bool isTop = true, bool isLeft = true,
            bool isMid = false)
        {
            IndependentTag tag = null;

            if (rebars.Count < 1)
            {
                return null;
            }

            if (isTop)
            {
                rebars = rebars.DistinctBy2(x => x.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER))
                    .OrderBy(x => CommonService.GetMaxLineOfRebar(x).Midpoint().Z).ToList();
            }
            else
            {
                rebars = rebars.DistinctBy2(x => x.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER))
                    .OrderByDescending(x => CommonService.GetMaxLineOfRebar(x).Midpoint().Z).ToList();
            }

            //Tag rebar
            var zTop = beamTopElevation + Setting.KhoangCachTagDenDam;
            var zBot = beamBotElevation - Setting.KhoangCachTagDenDam;
            var pTop = p.EditZ(zTop);
            if (isTop == false)
            {
                pTop = p.EditZ(zBot);
            }

            double tagHeight = Setting.KhoangCach2Tags * ViewSection.Scale;

            var first = true;

            foreach (var rebar in rebars)
            {
                tag = CreateIndependentTagForStandardRebar(rebar, pTop, tagHeight, isLeft);

                if (isTop)
                {
                    pTop = pTop.Add(XYZ.BasisZ * tagHeight * 1);
                }
                else
                {
                    pTop = pTop.Add(-XYZ.BasisZ * tagHeight * 1);
                }
            }

            return tag;
        }

        private void TagStandardRebars(List<Rebar> rebars, XYZ p, bool isTop = true, bool isLeft = true)
        {
            if (rebars.Count < 1)
            {
                return;
            }

            List<List<Rebar>> listRebars = new List<List<Rebar>>();
            if (isTop)
            {
                rebars = rebars.DistinctBy2(x => x.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER))
                    .OrderBy(x => CommonService.GetMaxLineOfRebar(x).Midpoint().Z).ToList();
            }
            else
            {
                rebars = rebars.DistinctBy2(x => x.GetParameterValueAsString(BuiltInParameter.REBAR_NUMBER))
                    .OrderByDescending(x => CommonService.GetMaxLineOfRebar(x).Midpoint().Z).ToList();
            }

            var z = CommonService.GetMaxLineOfRebar(rebars.FirstOrDefault()).Midpoint().Z.FootToMm();
            var list = new List<Rebar>();
            foreach (var rebar in rebars)
            {
                var zRebar = Convert.ToInt32(CommonService.GetMaxLineOfRebar(rebar).Midpoint().Z.FootToMm());
                if (Math.Abs(z - zRebar) < 10)
                {
                    list.Add(rebar);
                }
                else
                {
                    listRebars.Add(new List<Rebar>(list));
                    list.Clear();
                    z = zRebar;
                    list.Add(rebar);
                }
            }

            if (list.Count > 0)
            {
                listRebars.Add(list);
            }

            //Tag rebar
            var zTop = beamTopElevation + Setting.KhoangCachTagDenDam;
            var zBot = beamBotElevation - Setting.KhoangCachTagDenDam;
            var pTop = p.EditZ(zTop);
            if (isTop == false)
            {
                pTop = p.EditZ(zBot);
            }

            double tagHeight = Setting.KhoangCach2Tags * ViewSection.Scale;
            foreach (var listRebar in listRebars)
            {
                var first = true;
                foreach (var rebar in listRebar)
                {
                    if (first)
                    {
                        CreateIndependentTagForStandardRebar(rebar, pTop, out var tagHeigh2t, isLeft);
                        first = false;
                    }
                    else
                    {
                        CreateIndependentTagForStandardRebar(rebar, pTop, out var tagHeigh3T, !isLeft);
                    }
                }

                if (isTop)
                {
                    pTop = pTop.Add(XYZ.BasisZ * tagHeight * 1);
                }
                else
                {
                    pTop = pTop.Add(-XYZ.BasisZ * tagHeight * 1);
                }
            }
        }

        private IndependentTag CreateIndependentTagForStandardRebar(Rebar rebar, XYZ p, out double tagHeight,
            bool isLeft = true)
        {
            var tagId = Setting.TagRebarStandardPhai.Id;
            if (isLeft)
            {
                tagId = Setting.TagRebarStandardTrai.Id;
            }

            var tag = TagUtils2.CreateIndependentTag(tagId, ViewSection.Id, rebar, false,
                TagOrientation.Horizontal, p);

            tagHeight = 4.MmToFoot() * ViewSection.Scale;
            AC.Document.Regenerate();
            var bb = tag.get_BoundingBox(ViewSection);
            if (bb != null)
            {
                var max = bb.Max.ProjectOnto(plane).EditZ(p.Z);
                var min = bb.Min.ProjectOnto(plane).EditZ(p.Z);
                if (max.DotProduct(ViewSection.RightDirection) < min.DotProduct(ViewSection.RightDirection))
                {
                    var temp = max;
                    max = min;
                    min = temp;
                }

                tag.HasLeader = true;
                AC.Document.Regenerate();

                if (isLeft)
                {
                    var translation = (p - max);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, ViewSection.RightDirection * -40.MmToFoot());

                    tag.SetLeaderElbow(p.Add(ViewSection.RightDirection * -10.MmToFoot()));
                }
                else
                {
                    var translation = (p - min);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, ViewSection.RightDirection * 40.MmToFoot());

                    tag.SetLeaderElbow(p.Add(ViewSection.RightDirection * 10.MmToFoot()));
                }

                tag.SetLeaderElbow(p.Add(ViewSection.RightDirection * -10.MmToFoot()));

                return tag;
            }

            return null;
        }

        private IndependentTag CreateIndependentTagForStandardRebar(Rebar rebar, XYZ p, double tagHeight, bool isLeft)
        {
            var tagId = Setting.TagRebarStandardPhai.Id;

            if (isLeft)
                tagId = Setting.TagRebarStandardTrai.Id;

            var tag = TagUtils2.CreateIndependentTag(tagId, ViewSection.Id, rebar, false,
                TagOrientation.Horizontal, p);

            AC.Document.Regenerate();

            var bb = tag.get_BoundingBox(ViewSection);
            if (bb != null)
            {
                var max = bb.Max.ProjectOnto(plane).EditZ(p.Z);
                var min = bb.Min.ProjectOnto(plane).EditZ(p.Z);
                if (max.DotProduct(ViewSection.RightDirection) < min.DotProduct(ViewSection.RightDirection))
                {
                    var temp = max;
                    max = min;
                    min = temp;
                }

                tag.HasLeader = true;
                AC.Document.Regenerate();

                if (isLeft)
                {
                    var translation = (p - max);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, ViewSection.RightDirection * -40.MmToFoot());

                    tag.SetLeaderElbow(p.Add(ViewSection.RightDirection * -10.MmToFoot()));
                }
                else
                {
                    var translation = (p - min);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, translation);
                    ElementTransformUtils.MoveElement(AC.Document, tag.Id, ViewSection.RightDirection * 40.MmToFoot());

                    tag.SetLeaderElbow(p.Add(ViewSection.RightDirection * 10.MmToFoot()));
                }

                return tag;
            }

            return null;
        }

        private void CreateBreakLines()
        {
            if (Setting.BreakLineSymbol == null)
            {
                return;
            }

            var sp = BeamGeometries.First().BeamLine.SP();
            var ep = BeamGeometries.Last().BeamLine.EP().EditZ(sp.Z);
            var beamLine = sp.CreateLine(ep);

            var extendedLine = beamLine.ExtendLineBothEnd(5);

            var p1 = extendedLine.SP()
                .ModifyVector(beamBotElevation - Setting.KhoangCachBreakLineDenDam, XYZEnum.Z);

            var p2 = extendedLine.EP()
                .ModifyVector(beamBotElevation - Setting.KhoangCachBreakLineDenDam, XYZEnum.Z);

            var botLine = Line.CreateBound(p1, p2);

            var p3 = extendedLine.SP()
                .ModifyVector(beamTopElevation + Setting.KhoangCachBreakLineDenDam, XYZEnum.Z);
            var p4 = extendedLine.EP()
                .ModifyVector(beamTopElevation + Setting.KhoangCachBreakLineDenDam, XYZEnum.Z);
            var topLine = Line.CreateBound(p3, p4);

            var columns = supportElements.Where(x => x.IsColumn).ToList();

            if (IsDimDamMong)
                botSupports = supportElements.Where(x => x.IsFoundation).ToList();

            foreach (var egm in columns)
            {
                //Bottom BreakLines
                if (true)
                {
                    var lines = GetLineIntersectWithElement(egm, botLine);
                    foreach (var line in lines)
                    {
                        var l = line.ExtendLineBothEnd(30.MmToFoot()).ProjectOn(plane) as Line;

                        var fi = AC.Document.Create.NewFamilyInstance(l, Setting.BreakLineSymbol, ViewSection);
                        AC.Document.Regenerate();
                        var bb = fi.get_BoundingBox(ViewSection);
                        if (bb != null)
                        {
                            botSupports.Add(egm);
                            if (Math.Abs(bb.Max.Z - l.SP().Z) > Math.Abs(bb.Min.Z - l.SP().Z))
                            {
                                ((LocationCurve)fi.Location).Curve = l.CreateReversed();
                            }
                        }
                    }
                }

                //Top BreakLines
                if (true)
                {
                    var lines = GetLineIntersectWithElement(egm, topLine);
                    foreach (var line in lines)
                    {
                        var l = line.ExtendLineBothEnd(30.MmToFoot()).ProjectOn(plane) as Line;

                        var fi = AC.Document.Create.NewFamilyInstance(l, Setting.BreakLineSymbol, ViewSection);

                        AC.Document.Regenerate();

                        var bb = fi.get_BoundingBox(ViewSection);

                        if (bb != null)
                        {
                            topSupports.Add(egm);
                            if (Math.Abs(bb.Max.Z - l.SP().Z) < Math.Abs(bb.Min.Z - l.SP().Z))
                            {
                                ((LocationCurve)fi.Location).Curve = l.CreateReversed();
                            }
                        }
                    }
                }
            }
        }

        private void SetCropBox()
        {
            var vectorRight = new XYZ(Math.Abs(ViewSection.RightDirection.X), Math.Abs(ViewSection.RightDirection.Y),
                Math.Abs(ViewSection.RightDirection.Z));

            var leftPoint = BeamGeometries.First().BeamLine.SP()
                .Add(vectorRight * -1 * Define.BeamDetailCropBoxOffsetLeft);
            ;
            var rightPoint = BeamGeometries.Last().BeamLine.EP()
                .Add(vectorRight * Define.BeamDetailCropBoxOffsetRight * 2);

            //lay 2 point ngoai cung
            if (ListPointFaceVers.Count > 1 && IsDimDamMong)
            {
                leftPoint = ListPointFaceVers.FirstOrDefault()
                    ?.Add(ViewSection.RightDirection * -1 * Define.BeamDetailCropBoxOffsetLeft);
                ;
                rightPoint = ListPointFaceVers.LastOrDefault()
                    ?.Add(ViewSection.RightDirection * Define.BeamDetailCropBoxOffsetRight * 2);
            }

            if ((rightPoint - leftPoint).DotProduct(vectorRight) < 0)
            {
                (leftPoint, rightPoint) = (rightPoint, leftPoint);
            }

            left = leftPoint.EditZ(beamTopElevation);
            rightPoint.EditZ(beamTopElevation);

            Element leftSupport = null;
            Element rightSupport = null;
            var rightFilter = new BoundingBoxContainsPointFilter(rightPoint, 5.MmToFoot());
            var leftFilter = new BoundingBoxContainsPointFilter(leftPoint, 5.MmToFoot());

            foreach (var ele in supportElements.Select(x => x.Element))
            {
                if (rightFilter.PassesFilter(ele))
                {
                    rightSupport = ele;
                }
                else if (leftFilter.PassesFilter(ele))
                {
                    leftSupport = ele;
                }
            }

            if (leftSupport != null)
            {
                leftPoint = GetFarthestPointByDirection(leftSupport, -vectorRight);
                left = leftPoint;
                leftPoint = leftPoint.Add(vectorRight * -1 * Define.BeamDetailCropBoxOffsetLeft);
            }

            if (rightSupport != null)
            {
                rightPoint = GetFarthestPointByDirection(rightSupport, vectorRight, ViewSection);
                rightPoint = rightPoint.Add(vectorRight * Define.BeamDetailCropBoxOffsetRight);
            }

            var zMin = beamBotElevation - Define.BeamDetailCropBoxOffsetBot;
            var zMax = beamTopElevation + Define.BeamDetailCropBoxOffsetTop;
            var topRight = new XYZ(rightPoint.X, rightPoint.Y, zMax);
            var botLeft = new XYZ(leftPoint.X, leftPoint.Y, zMin);

            var plane1 = ViewSection.ToBPlane();
            topRight = topRight.ProjectOnto(plane1);
            botLeft = botLeft.ProjectOnto(plane1);
            var topLeft = new XYZ(botLeft.X, botLeft.Y, zMax);
            var botRight = new XYZ(topRight.X, topRight.Y, zMin);

            var cropBoxCurveLoop = new CurveLoop();
            cropBoxCurveLoop.Append(Line.CreateBound(topLeft, topRight));
            cropBoxCurveLoop.Append(Line.CreateBound(topRight, botRight));
            cropBoxCurveLoop.Append(Line.CreateBound(botRight, botLeft));
            cropBoxCurveLoop.Append(Line.CreateBound(botLeft, topLeft));
            ViewSection.GetCropRegionShapeManager().SetCropShape(cropBoxCurveLoop);
        }

        private void SetGridsExtend()
        {
            foreach (var elementGeometryModel in gridGeometryModels)
            {
                try
                {
                    if (elementGeometryModel.Element is Grid grid)
                    {
                        var c = grid.GetCurvesInView(DatumExtentType.ViewSpecific, ViewSection)
                            .OrderByDescending(x => x.Length).FirstOrDefault();
                        if (c != null)
                        {
                            var sp = c.SP();
                            var ep = c.EP();
                            sp = sp.EditZ(beamBotElevation - Setting.KhoangCachGiua2Dim * ViewSection.Scale -
                                          Setting.KhoangCachDimDenDam);
                            ep = ep.EditZ(beamTopElevation + Setting.KhoangCachGiua2Dim * ViewSection.Scale +
                                Setting.KhoangCachDimDenDam - ViewSection.Scale.MmToFoot());
                            var line = Line.CreateBound(sp, ep);

                            if (grid.IsCurveValidInView(DatumExtentType.ViewSpecific, ViewSection, line))
                            {
                                grid.SetCurveInView(DatumExtentType.ViewSpecific, ViewSection, line);
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void SetViewTemplate()
        {
            if (Setting.ViewTemplate != null)
            {
                ViewSection.ViewTemplateId = Setting.ViewTemplate?.Id;
            }

            try
            {
                ViewSection.Scale = Setting.Scale;
            }
            catch
            {
                //
            }
        }

        private void HideNotNeededSections(List<ViewSection> viewSections)
        {
            var ids = viewSections.Select(x => x.Id.GetElementIdValue()).ToList();
            var idsToHide = new FilteredElementCollector(AC.Document).OfCategory(BuiltInCategory.OST_Viewers)
                .Where(x => ids.Contains(x.Id.GetElementIdValue()) == false).Select(x => x.Id).ToList();
            ViewSection.HideElements(idsToHide);
        }

        private List<Element> GetAllRebarInHost(Element ele)
        {
            var list = new List<Element>();
            var data = RebarHostData.GetRebarHostData(ele);
            list.AddRange(data.GetAreaReinforcementsInHost());
            list.AddRange(data.GetFabricAreasInHost());
            list.AddRange(data.GetFabricSheetsInHost());
            list.AddRange(data.GetPathReinforcementsInHost());
            list.AddRange(data.GetRebarContainersInHost());
            list.AddRange(data.GetRebarsInHost());
            list = list.Where(x => x != null && x.IsValidObject).ToList();
            return list;
        }

        private void HideRebarsNotInHost()
        {
            var rebars = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Rebar)).Cast<Rebar>()
                .ToList();
            var hostIds = BeamGeometries.Select(x => x.Beam.Id.GetElementIdValue()).ToList();
            var hideIds = new List<ElementId>();

            var areaRebars = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(RebarInSystem))
                .Cast<RebarInSystem>()
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
                ViewSection.HideElements(hideIds);
            }
            catch (Exception e)
            {
                AC.Log(e.Message + Environment.NewLine + "Lỗi Ẩn thép ko thuộc host dầm");
            }
        }

        #endregion MainFunctions

        #region Functions to Get Data

        private void GetData()
        {
            //Beam top and bot elevation
            beamTopElevation = BeamGeometries.Max(x => x.TopElevation);
            beamBotElevation = BeamGeometries.Min(x => x.BotElevation);

            //Plane of Viewsection
            plane = ViewSection.ToBPlane();

            //Get Beams and Walls in view

            var walls = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Wall))
                .OfCategory(BuiltInCategory.OST_Walls).ToElements();

            var columns = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns).ToElements().ToList();
            var beams = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming).ToElements().ToList();

            var foundationsFamilyInstance = new FilteredElementCollector(AC.Document, ViewSection.Id)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation).ToList();

            var foundationFloors = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Floor))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation).ToList();

            var foundations = foundationFloors.Concat(foundationsFamilyInstance).ToList();
          
                
            foreach (var element in columns)
            {
                var egm = new ElementGeometryModel(element, ViewSection) { IsColumn = true };

                supportElements.Add(egm);
            }
            foreach (var element in beams)
            {
                foreach (var beam in beams)
                {
                    // Lấy BoundingBox của dầm
                    BoundingBoxXYZ beamBoundingBox = beam.get_BoundingBox(ViewSection);
                    if (beamBoundingBox == null) continue;

                    // Biến kiểm tra xem dầm có giao với cột nào không
                    bool intersectsWithColumn = false;

                    // Duyệt qua từng cột để kiểm tra giao nhau
                    foreach (var column in columns)
                    {
                        // Lấy BoundingBox của cột
                        BoundingBoxXYZ columnBoundingBox = column.get_BoundingBox(ViewSection);
                        if (columnBoundingBox == null) continue;

                        // Kiểm tra xem hai BoundingBox có giao nhau không
                        if (DoBoundingBoxesIntersect(beamBoundingBox, columnBoundingBox))
                        {
                            intersectsWithColumn = true;
                            break; // Dừng kiểm tra nếu đã tìm thấy giao điểm
                        }
                    }

                    // Nếu dầm không giao với bất kỳ cột nào, thêm vào supportElements
                    if (!intersectsWithColumn)
                    {
                        var egm = new ElementGeometryModel(beam, ViewSection) { IsBeam = true };
                        supportElements.Add(egm);
                    }
                }
            }
            foreach (var element in walls)
            {
                var egm = new ElementGeometryModel(element, ViewSection) { IsWall = true };
                supportElements.Add(egm);
            }
  

            #region Beam Dim

            //if (Setting.IsDimDamLamGoi)
            //{
            //    var beams = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(FamilyInstance))
            //        .OfCategory(BuiltInCategory.OST_StructuralFraming).Where(x =>
            //        {
            //            var curve = x.GetCurve();

            //            if (curve != null && curve.Direction().IsParallel(ViewSection.ViewDirection))
            //            {
            //                return true;
            //            }

            //            return false;
            //        }).ToList();

            //    var pointOfBeams = new List<XYZ>();

            //    BeamGeometries.ForEach(beamGeo =>
            //    {
            //        foreach (var beamGeoEdgeVer in beamGeo.EdgeVers)
            //        {
            //            pointOfBeams.Add(beamGeoEdgeVer.SP());
            //        }
            //    });

            //    pointOfBeams.OrderBy(x => x.DotProduct(ViewSection.RightDirection)).ToList();

            //    foreach (var element in beams)
            //    {
            //        var egm = new ElementGeometryModel(element, ViewSection) { IsBeam = true };

            //        var heightSuport = Math.Abs((egm.BottomFace.Origin - egm.TopFace.Origin).DotProduct(ViewSection.UpDirection));

            //        if (heightSuport >= BeamGeometries.FirstOrDefault()?.Height)
            //        {

            //            var originFace = egm.Transform.OfPoint(egm.TopFace.Origin);

            //            var pointStart = pointOfBeams.FirstOrDefault();

            //            var pointEnd = pointOfBeams.FirstOrDefault();

            //            if (pointEnd != null && pointStart != null && (originFace.DotProduct(ViewSection.RightDirection) <= pointStart.DotProduct(ViewSection.RightDirection)
            //                                                           || originFace.DotProduct(ViewSection.RightDirection) >= pointEnd.DotProduct(ViewSection.RightDirection))
            //               )
            //                supportElements.Add(egm);
            //        }
            //    }
            //}

            #endregion

            foreach (var foundation in foundations)
            {
                var egm = new ElementGeometryModel(foundation, ViewSection, true);
                supportElements.Add(egm);
            }

            var listPointFaceVers = new List<XYZ>();

            foreach (var elementGeometryModel in supportElements)
            {
                if (elementGeometryModel.PlanarFaces.Count > 0)
                    foreach (var planarFace in elementGeometryModel.PlanarFaces)
                    {
                        if (elementGeometryModel.Transform.OfVector(planarFace.FaceNormal)
                            .IsParallel(ViewSection.RightDirection))
                        {
                            listPointFaceVers.Add(elementGeometryModel.Transform.OfPoint(planarFace.Origin));
                        }
                    }
            }

            foreach (var beamGeometry in BeamGeometries)
            {
                var firstFace = beamGeometry.PlanarFaceLeftRight.FirstOrDefault();
                var lastFace = beamGeometry.PlanarFaceLeftRight.LastOrDefault();

                if (firstFace != null) listPointFaceVers.Add(beamGeometry.Transform1.OfPoint(firstFace.Origin));
                if (lastFace != null) listPointFaceVers.Add(beamGeometry.Transform1.OfPoint(lastFace.Origin));
            }

            if (listPointFaceVers.Count > 0)
                ListPointFaceVers = listPointFaceVers.DistinctBy2(x => x.DotProduct(ViewSection.RightDirection))
                    .OrderBy(x => x.DotProduct(ViewSection.RightDirection)).ToList();

            if (foundations.Count >= 2)
                IsDimDamMong = true;

            //Grids
            GetGridInfo();
        }
        private bool DoBoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            // Kiểm tra giao nhau trên các trục X, Y, Z
            bool intersectX = box1.Max.X >= box2.Min.X && box1.Min.X <= box2.Max.X;
            bool intersectY = box1.Max.Y >= box2.Min.Y && box1.Min.Y <= box2.Max.Y;
            bool intersectZ = box1.Max.Z >= box2.Min.Z && box1.Min.Z <= box2.Max.Z;

            // Hai BoundingBox giao nhau nếu chúng giao nhau trên cả ba trục
            return intersectX && intersectY && intersectZ;
        }
        private void GetGridInfo()
        {
            var grids = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Grid)).Cast<Grid>()
                .OrderBy(x => x.Curve.SP().DotProduct(ViewSection.RightDirection))
                .ToList();

            foreach (var grid in grids)
            {
                if (!grid.IsCurved)
                {
                    var model = new ElementGeometryModel(grid);
                    model.ReferenceAndPoints.ForEach(x => x.Normal = ViewSection.RightDirection);
                    gridGeometryModels.Add(model);
                }
            }
        }

        private void GetMainBeamInView()
        {
            //Get main beam
            var beams = AC.Selection
                .PickObjects(ObjectType.Element, new BimSpeedUtils.BeamSelectionFilter(), "Beams...")
                .Select(x => x.ToElement()).Cast<FamilyInstance>().ToList();

                if (beamSupport.Count > 0)
                {
                    foreach (var familyInstance in beamSupport)
                    {
                        var egm = new ElementGeometryModel(familyInstance
                            , ViewSection);

                        supportElements.Add(egm);
                    }
                }


            if (BeamRebarCommonService.CheckBeamsValidToPutRebars(beams, out var errorMessage) == false)
            {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            BeamGeometries = beams.Select(x => new BeamGeometry(x)).ToList();
            var editBeamDirection = BeamRebarCommonService.EditBeamDirection(BeamGeometries[0].BeamLine.Direction);
            BeamGeometries.ForEach(x =>
                x.BeamLine = BeamRebarCommonService.EditLineByDirection(x.BeamLine, editBeamDirection));
            if (BeamGeometries.Count > 1)
            {
                BeamGeometries = BeamGeometries.OrderBy(x => x.MidPoint.DotProduct(editBeamDirection)).ToList();
            }
        }

        private Line EditLineByDirection(XYZ vector, Line line)
        {
            var direct = line.Direction;
            if (direct.DotProduct(vector) < 0)
            {
                return line.CreateReversed() as Line;
            }

            return line;
        }

        private XYZ GetFarthestPointByDirection(Element ele, XYZ vector)
        {
            var p = XYZ.Zero;
            var bb = ele.get_BoundingBox(null);
            if (bb != null)
            {
                var min = bb.Min;
                var max = bb.Max;
                var dMin = min.DotProduct(vector);
                var dMax = max.DotProduct(vector);
                p = dMax > dMin ? max : min;
            }

            return p;
        }

        private XYZ GetFarthestPointByDirection(Element ele, XYZ vector, ViewSection viewSection)
        {
            var p = XYZ.Zero;
            var bb = ele.get_BoundingBox(viewSection);
            if (bb != null)
            {
                var min = bb.Min;
                var max = bb.Max;
                var dMin = min.DotProduct(vector);
                var dMax = max.DotProduct(vector);
                p = dMax > dMin ? max : min;
            }

            return p;
        }

        private List<Line> GetLineIntersectWithElement(ElementGeometryModel ele, Line beamLine)
        {
            var lines = new List<Line>();
            var solids = ele.Solids;
            var option = new SolidCurveIntersectionOptions
            {
                ResultType = SolidCurveIntersectionMode.CurveSegmentsInside
            };
            foreach (var solid in solids)
            {
                var intersection = solid.IntersectWithCurve(beamLine, option);
                foreach (var c in intersection)
                {
                    if (c is Line line)
                    {
                        if (line.Length > 0.1)
                        {
                            line = EditLineByDirection(ViewSection.RightDirection, line);
                            lines.Add(line);
                        }
                    }
                }
            }

            return lines;
        }

        /// <summary>
        /// Must be set detail level coarse for rebar first
        /// </summary>
        /// <param name="rebar"> Must be set detail level coarse for rebar first</param>
        /// <returns></returns>
        private List<ReferenceAndPoint> GetStirrupReferences(Autodesk.Revit.DB.Structure.Rebar rebar)
        {
            var raps = new List<ReferenceAndPoint>();
            var lines = rebar.Lines(ViewSection);

            if (lines.Count > 0)
                foreach (var line in lines)
                {
                    if (line.Reference != null && line.Direction.IsParallel(XYZ.BasisZ) && line.SP() != null)
                    {
                        var rf = line.Reference;
                        var sp = line.SP();

                        var rap = new ReferenceAndPoint()
                        { Normal = ViewSection.RightDirection, Point = sp, Reference = rf };
                        raps.Add(rap);
                    }
                }

            //var ordered = raps.OrderBy(x => x.Point.DotProduct(ViewSection.RightDirection)).ToList();
            //if (ordered.Count > 2)
            //{
            //    var first = ordered.First();
            //    var last = ordered.Last();
            //    return new List<ReferenceAndPoint>() { first, last };
            //}
            return raps;
        }

        private List<ReferenceAndPoint> GetStirrupReferencesToDim()
        {
            var rfs = new List<ReferenceAndPoint>();

            //var rebars = new List<Autodesk.Revit.DB.Structure.Rebar>();

            //foreach (var beamGeometry in BeamGeometries)
            //{
            //    rebars.AddRange(CommonService.GetStiruppsInView(beamGeometry.Beam));
            //}

            var rebars = BeamGeometries.SelectMany(bG => CommonService.GetStiruppsInView(bG.Beam)).ToList();

            var bplane = BPlane.CreateByNormalAndOrigin(ViewSection.ViewDirection, ViewSection.Origin);

            if (rebars.Any())
            {
                #region Code cu

                //foreach (var rebar in rebars)
                //{
                //    if (rebar.GetRebarShape().Name == "M_01")
                //    {
                //        continue;
                //    }
                //    var raps = GetStirrupReferences(rebar);

                //    if (raps.Count == 2)
                //    {
                //        var first = raps.First();
                //        var last = raps.Last();
                //        var p = first.Point.EditZ(beamBotElevation);
                //        if (IsPointNearSupports(botSupports, p, 300))
                //        {
                //            rfs.Add(last);
                //            continue;
                //        }
                //        var pp = last.Point.EditZ(beamBotElevation);
                //        if (IsPointNearSupports(botSupports, pp, 300))
                //        {
                //            rfs.Add(first);
                //        }
                //    }
                //}

                #endregion

                var viewRight = ViewSection.RightDirection;

                var columnGeos = supportElements.Where(x => x.IsColumn)
                    .OrderBy(x => x.PointCenterInView.DotProduct(viewRight))
                    .ToList();

                if (columnGeos.Count > 0)
                    for (var i = 0; i < columnGeos.Count - 1; i++)
                    {
                        var point1 = columnGeos[i].PointCenterInView.ProjectOnto(bplane);
                        var point2 = columnGeos[i + 1].PointCenterInView.ProjectOnto(bplane);

                        var maxZOfBoundingBox = rebars.Max(x => x.get_BoundingBox(ViewSection).Max.Z);

                        var rebarsInNhip = rebars.Where(x =>
                            x.get_BoundingBox(ViewSection).Max.ProjectOnto(bplane).DotProduct(viewRight) >=
                            point1.DotProduct(viewRight)
                            && x.get_BoundingBox(ViewSection).Max.ProjectOnto(bplane).DotProduct(viewRight) <=
                            point2.DotProduct(viewRight)
                            && Math.Abs(x.get_BoundingBox(ViewSection).Max.Z - maxZOfBoundingBox) < 0.01
                        ).OrderBy(x => x.get_BoundingBox(ViewSection).Max.DotProduct(viewRight)).ToList();

                        if (rebarsInNhip.Count >= 2)
                        {
                            if (rebarsInNhip.FirstOrDefault()?.GetSubelements().Count > 3)
                            {
                                var rap1S = GetStirrupReferences(rebarsInNhip.FirstOrDefault())
                                    .OrderBy(x => x.Point.DotProduct(viewRight)).ToList();
                                //rfs.Add(rap1S.FirstOrDefault());
                                if (rap1S.Count > 0)
                                    rfs.Add(rap1S.LastOrDefault());
                            }

                            if (rebarsInNhip.LastOrDefault()?.GetSubelements().Count > 3)
                            {
                                var rap2S = GetStirrupReferences(rebarsInNhip.LastOrDefault())
                                    .OrderBy(x => x.Point.DotProduct(viewRight)).ToList();
                                if (rap2S.Count > 0)
                                    rfs.Add(rap2S.FirstOrDefault());
                            }
                        }
                        //else
                        //{
                        //    //Truong hop nhip dam ngan
                        //    var rap1S = GetStirrupReferences(rebarsInNhip.FirstOrDefault()).OrderBy(x => x.Point.DotProduct(viewRight)).ToList();

                        //    //rfs.Add(rap1S.FirstOrDefault());
                        //    //rfs.Add(rap1S.LastOrDefault());
                        //}
                    }
            }

            return rfs;
        }

        private ReferenceArray GetOuterReferences()
        {
            var ra = new ReferenceArray();
            var models = gridGeometryModels.Union(topSupports).Union(botSupports).ToList();
            var raps = new List<ReferenceAndPoint>();
            foreach (var elementGeometryModel in models)
            {
                foreach (var referenceAndPoint in elementGeometryModel.ReferenceAndPoints)
                {
                    raps.Add(referenceAndPoint);
                }
            }

            raps = raps.Where(x => x.Normal.IsParallel(ViewSection.RightDirection))
                .OrderBy(x => x.Point.DotProduct(ViewSection.RightDirection)).ToList();
            if (raps.Count > 1)
            {
                var first = raps.First();
                var last = raps.Last();
                ra.Append(first.Reference);
                ra.Append(last.Reference);
            }

            return ra;
        }

        private bool IsPointNearSupports2(List<ElementGeometryModel> elementGeometryModels, XYZ p,
            double toleranceMinimet)
        {
            var filter = new BoundingBoxContainsPointFilter(p, toleranceMinimet.MmToFoot(), false);

            foreach (var elementGeometryModel in elementGeometryModels)
            {
                if (filter.PassesFilter(elementGeometryModel.Element))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointNearSupports(List<ElementGeometryModel> elementGeometryModels, XYZ p,
            double toleranceMinimet = 100)
        {
            foreach (var elementGeometryModel in elementGeometryModels)
            {
                var bb = elementGeometryModel.Element.get_BoundingBox(ViewSection);
                var min = bb.Min;
                var max = bb.Max;
                var d1 = Math.Abs((p - min).DotProduct(ViewSection.RightDirection)).FootToMm();
                var d2 = Math.Abs((p - max).DotProduct(ViewSection.RightDirection)).FootToMm();
                if (d1.IsSmaller(toleranceMinimet) || d2.IsSmaller(toleranceMinimet))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointNearSupports(List<XYZ> points, XYZ p, double toleranceMinimet = 120)
        {
            var min = points.FirstOrDefault();
            var max = points.LastOrDefault();
            var d1 = Math.Abs((p - min).DotProduct(ViewSection.RightDirection)).FootToMm();
            var d2 = Math.Abs((p - max).DotProduct(ViewSection.RightDirection)).FootToMm();
            if (d1.IsSmaller(toleranceMinimet) || d2.IsSmaller(toleranceMinimet))
            {
                return true;
            }

            return false;
        }

        private Floor GetFloorNearAPoint(XYZ p)
        {
            var floors = new FilteredElementCollector(AC.Document, ViewSection.Id).OfClass(typeof(Floor)).Cast<Floor>()
                .ToList();
            var filter = new BoundingBoxContainsPointFilter(p, 500.MmToFoot());
            foreach (var floor in floors)
            {
                if (filter.PassesFilter(floor))
                {
                    return floor;
                }
            }

            return null;
        }

        private List<ReferenceAndPoint> GetAdditionalBottomBarReferences()
        {
            var list = new List<ReferenceAndPoint>();
            var raps = new List<ReferenceAndPoint>();
            var rebars = new List<Rebar>();
            foreach (var beamGeometry in BeamGeometries)
            {
                rebars.AddRange(CommonService.GetStandardBarsInView(beamGeometry.Beam));
            }

            foreach (var rebar in rebars)
            {
                var cs = rebar.ComputeRebarDrivingCurves();

                if (cs.Count == 1)
                {
                    var center = cs.FirstOrDefault().Midpoint();
                    if (center.Z > beamBotElevation && center.Z < (beamBotElevation + beamTopElevation) / 2)
                    {
                        var lines = rebar.Lines(ViewSection)
                            .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null);
                        foreach (var line in lines)
                        {
                            if (supportElements.Count > 0)
                            {
                                var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                                bool flag = false;

                                var flag1 = false;

                                var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                                foreach (var elementGeometryModel in supportElements)
                                {
                                    //Check xem point có nằm trong các suportElement
                                    if (filter1.PassesFilter(elementGeometryModel.Element))
                                    {
                                        flag = true;
                                    }

                                    if (filter2.PassesFilter(elementGeometryModel.Element))
                                    {
                                        flag1 = true;
                                    }
                                }

                                if (!flag)
                                {
                                    var rap = new ReferenceAndPoint()
                                    {
                                        Reference = line.Reference,
                                        Point = line.SP()
                                    };
                                    raps.Add(rap);
                                }

                                if (!flag1)
                                {
                                    var rap1 = new ReferenceAndPoint()
                                    {
                                        Reference = line.Reference,
                                        Point = line.EP()
                                    };
                                    raps.Add(rap1);
                                }
                            }
                            else
                            {
                                var rap = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.SP()
                                };

                                var rap2 = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.EP()
                                };

                                //AC.Document.Create.NewDetailCurve(ViewSection, line);
                                raps.Add(rap);
                                raps.Add(rap2);
                            }
                        }
                    }
                }
            }

            foreach (var referenceAndPoint in raps)
            {
                //Check not in support
                if (IsPointNearSupports2(botSupports, referenceAndPoint.Point, 100) == false)
                {
                    list.Add(referenceAndPoint);
                }
            }

            return list;
        }

        private List<ReferenceAndPoint> GetAdditionalBottomBarReferences(List<ReferenceAndPoint> referenceAndPoints)
        {
            var list = new List<ReferenceAndPoint>();
            var raps = new List<ReferenceAndPoint>();
            var rebars = new List<Rebar>();

            if (referenceAndPoints.Count > 0)
            {
                var first = referenceAndPoints.FirstOrDefault();
                var last = referenceAndPoints.LastOrDefault();

                foreach (var beamGeometry in BeamGeometries)
                {
                    rebars.AddRange(CommonService.GetStandardBarsInView(beamGeometry.Beam));
                }

                foreach (var rebar in rebars)
                {
                    var cs = rebar.ComputeRebarDrivingCurves();

                    if (cs.Count == 1)
                    {
                        var center = cs.FirstOrDefault().Midpoint();
                        if (center.Z > beamBotElevation && center.Z <= (beamBotElevation + beamTopElevation) / 2)
                        {
                            var lines = rebar.Lines(ViewSection)
                                .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null);
                            foreach (var line in lines)
                            {
                                if (supportElements.Count > 0)
                                {
                                    var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                                    bool flag = false;

                                    var flag1 = false;

                                    var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                                    foreach (var elementGeometryModel in supportElements)
                                    {
                                        //Check xem point có nằm trong các suportElement
                                        if (filter1.PassesFilter(elementGeometryModel.Element))
                                        {
                                            flag = true;
                                        }

                                        if (filter2.PassesFilter(elementGeometryModel.Element))
                                        {
                                            flag1 = true;
                                        }
                                    }

                                    if (!flag)
                                    {
                                        var rap = new ReferenceAndPoint()
                                        {
                                            Reference = line.Reference,
                                            Point = line.SP()
                                        };
                                        raps.Add(rap);
                                    }

                                    if (!flag1)
                                    {
                                        var rap1 = new ReferenceAndPoint()
                                        {
                                            Reference = line.Reference,
                                            Point = line.EP()
                                        };
                                        raps.Add(rap1);
                                    }
                                }
                                else
                                {
                                    var rap = new ReferenceAndPoint()
                                    {
                                        Reference = line.Reference,
                                        Point = line.SP()
                                    };

                                    var rap2 = new ReferenceAndPoint()
                                    {
                                        Reference = line.Reference,
                                        Point = line.EP()
                                    };

                                    //AC.Document.Create.NewDetailCurve(ViewSection, line);
                                    raps.Add(rap);
                                    raps.Add(rap2);
                                }
                            }
                        }
                    }
                }

                foreach (var referenceAndPoint in raps)
                {
                    //Check not in support
                    if (last != null
                        && first != null
                        && IsPointNearSupports2(botSupports, referenceAndPoint.Point, 100) == false
                        && Math.Abs(first.Point.DotProduct(ViewSection.RightDirection) -
                                    referenceAndPoint.Point.DotProduct(ViewSection.RightDirection)) >= 70.MmToFoot()
                        && Math.Abs(last.Point.DotProduct(ViewSection.RightDirection) -
                                    referenceAndPoint.Point.DotProduct(ViewSection.RightDirection)) >= 70.MmToFoot()
                       )
                    {
                        list.Add(referenceAndPoint);
                    }
                }
            }

            return list;
        }

        private List<ReferenceAndPoint> GetAdditionalTopBarReferences()
        {
            var list = new List<ReferenceAndPoint>();
            var raps = new List<ReferenceAndPoint>();
            var rebars = new List<Rebar>();
            foreach (var beamGeometry in BeamGeometries)
            {
                rebars.AddRange(CommonService.GetStandardBarsInView(beamGeometry.Beam));
            }

            foreach (var rebar in rebars)
            {
                var cs = rebar.ComputeRebarDrivingCurves();

                if (cs.Count == 1)
                {
                    var center = cs.FirstOrDefault().Midpoint();
                    if (center.Z > (beamBotElevation + beamTopElevation) / 2 && center.Z < beamTopElevation)
                    {
                        var lines = rebar.Lines(ViewSection)
                            .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null);
                        foreach (var line in lines)
                        {
                            bool flag1 = false;
                            bool flag2 = false;

                            var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                            var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                            foreach (var elementGeometryModel in supportElements)
                            {
                                if (filter1.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag1 = true;
                                }

                                if (filter2.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag2 = true;
                                }
                            }

                            if (!flag1)
                            {
                                var rap = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.SP()
                                };

                                raps.Add(rap);
                            }

                            if (!flag2)
                            {
                                var rap1 = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.EP()
                                };

                                raps.Add(rap1);
                            }
                        }
                    }
                }
                else if (cs.Count == 2)
                {
                    var curveHoz = cs.Where(x => x.Direction().IsParallel(ViewSection.RightDirection))
                        .Maxima(x => x.ApproximateLength).FirstOrDefault();

                    var center = curveHoz.Midpoint();
                    if (center.Z > (beamBotElevation + beamTopElevation) / 2 && center.Z < beamTopElevation)
                    {
                        var lines = rebar.Lines(ViewSection)
                            .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null);
                        foreach (var line in lines)
                        {
                            bool flag1 = false;
                            bool flag2 = false;

                            var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                            var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                            foreach (var elementGeometryModel in supportElements)
                            {
                                if (filter1.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag1 = true;
                                }

                                if (filter2.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag2 = true;
                                }
                            }

                            if (!flag1)
                            {
                                var rap = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.SP()
                                };

                                raps.Add(rap);
                            }

                            if (!flag2)
                            {
                                var rap1 = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.EP()
                                };

                                raps.Add(rap1);
                            }
                        }
                    }
                }
            }

            foreach (var referenceAndPoint in raps)
            {
                //Check not in support
                if (IsPointNearSupports2(botSupports, referenceAndPoint.Point, 100) == false)
                {
                    list.Add(referenceAndPoint);
                }

                //AC.Selection.SetElementIds(botSupports.Select(x => x.Element.Id).ToList());
            }

            return list;
        }

        private List<ReferenceAndPoint> GetAdditionalTopBarReferences2()
        {
            var list = new List<ReferenceAndPoint>();
            var raps = new List<ReferenceAndPoint>();
            var rebars = new List<Rebar>();

            rebars.AddRange(BeamGeometries.SelectMany(x => CommonService.GetStandardBarsInView(x.Beam)));

            if (rebars.Count < 0)
                return null;

            foreach (var rebar in rebars)
            {
                var curves = rebar.ComputeRebarDrivingCurves();

                var isSingleCurve = curves.Count == 1;
                var isDoubleCurve = curves.Count == 2;

                if (isSingleCurve || isDoubleCurve)
                {
                    XYZ center = isSingleCurve
                        ? curves.FirstOrDefault().Midpoint()
                        : curves.Where(x => x.Direction().IsParallel(ViewSection.RightDirection))
                            .OrderByDescending(x => x.ApproximateLength)
                            .FirstOrDefault()?.Midpoint();

                    if (center == null)
                        return null;

                    if (center.Z > (beamBotElevation + beamTopElevation) / 2 && center.Z < beamTopElevation)
                    {
                        var lines = rebar.Lines(ViewSection)
                            .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null).ToList();

                        if (!lines.Any())
                            return null;

                        foreach (var line in lines)
                        {
                            bool flag1 = false;
                            bool flag2 = false;

                            var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                            var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                            foreach (var elementGeometryModel in supportElements)
                            {
                                if (!flag1 && filter1.PassesFilter(elementGeometryModel.Element))
                                    flag1 = true;

                                if (!flag2 && filter2.PassesFilter(elementGeometryModel.Element))
                                    flag2 = true;

                                if (flag1 && flag2)
                                    break; // Short-circuit if both flags are true
                            }

                            if (!flag1)
                            {
                                var rap = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.SP()
                                };

                                raps.Add(rap);
                            }

                            if (!flag2)
                            {
                                var rap1 = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.EP()
                                };

                                raps.Add(rap1);
                            }
                        }
                    }
                }
            }

            foreach (var referenceAndPoint in raps)
            {
                //Check not in support
                if (IsPointNearSupports2(botSupports, referenceAndPoint.Point, 100) == false)
                {
                    list.Add(referenceAndPoint);
                }
            }

            return list;
        }

        private List<ReferenceAndPoint> GetAdditionalTopBarReferences(List<ReferenceAndPoint> referenceAndPoints)
        {
            var list = new List<ReferenceAndPoint>();
            var raps = new List<ReferenceAndPoint>();
            var rebars = new List<Rebar>();

            if (!list.Any())
                return list;

            var first = referenceAndPoints.FirstOrDefault();

            var last = referenceAndPoints.LastOrDefault();

            foreach (var beamGeometry in BeamGeometries)
            {
                rebars.AddRange(CommonService.GetStandardBarsInView(beamGeometry.Beam));
            }

            foreach (var rebar in rebars)
            {
                var cs = rebar.ComputeRebarDrivingCurves();

                if (cs.Count == 1)
                {
                    var center = cs.FirstOrDefault().Midpoint();
                    if (center.Z > (beamBotElevation + beamTopElevation) / 2 && center.Z < beamTopElevation)
                    {
                        var lines = rebar.Lines(ViewSection)
                            .Where(x => x.Direction.IsParallel(XYZ.BasisZ) && x.Reference != null);
                        foreach (var line in lines)
                        {
                            bool flag1 = false;
                            bool flag2 = false;

                            var filter1 = new BoundingBoxContainsPointFilter(line.SP(), 100.MmToFoot());
                            var filter2 = new BoundingBoxContainsPointFilter(line.EP(), 100.MmToFoot());

                            foreach (var elementGeometryModel in supportElements)
                            {
                                if (filter1.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag1 = true;
                                }

                                if (filter2.PassesFilter(elementGeometryModel.Element))
                                {
                                    flag2 = true;
                                }
                            }

                            if (!flag1)
                            {
                                var rap = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.SP()
                                };

                                raps.Add(rap);
                            }

                            if (!flag2)
                            {
                                var rap1 = new ReferenceAndPoint()
                                {
                                    Reference = line.Reference,
                                    Point = line.EP()
                                };

                                raps.Add(rap1);
                            }
                        }
                    }
                }
            }

            foreach (var referenceAndPoint in raps)
            {
                //Check not in support
                if (IsPointNearSupports2(botSupports, referenceAndPoint.Point, 100) == false
                    && Math.Abs(first.Point.DotProduct(ViewSection.RightDirection) -
                                referenceAndPoint.Point.DotProduct(ViewSection.RightDirection)) >= 70.MmToFoot()
                    && Math.Abs(last.Point.DotProduct(ViewSection.RightDirection) -
                                referenceAndPoint.Point.DotProduct(ViewSection.RightDirection)) >= 70.MmToFoot()
                   )
                {
                    list.Add(referenceAndPoint);
                }
            }

            return list;
        }

        private List<Line> GetBeamSegmentLines()
        {
            var columns = new FilteredElementCollector(AC.Document, ViewSection.Id)
                        .OfClass(typeof(FamilyInstance))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                        .ToElements()
                        .ToList();
            var beams = new FilteredElementCollector(AC.Document, ViewSection.Id)
                        .OfClass(typeof(FamilyInstance))
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .ToElements()
                        .ToList();

            foreach (var beam in beams)
            {
                // Lấy BoundingBox của dầm
                BoundingBoxXYZ beamBoundingBox = beam.get_BoundingBox(ViewSection);
                if (beamBoundingBox == null) continue;

                // Biến kiểm tra xem dầm có giao với cột nào không
                bool intersectsWithColumn = false;

                // Duyệt qua từng cột để kiểm tra giao nhau
                foreach (var column in columns)
                {
                    // Lấy BoundingBox của cột
                    BoundingBoxXYZ columnBoundingBox = column.get_BoundingBox(ViewSection);
                    if (columnBoundingBox == null) continue;

                    // Kiểm tra xem hai BoundingBox có giao nhau không
                    if (DoBoundingBoxesIntersect(beamBoundingBox, columnBoundingBox))
                    {
                        intersectsWithColumn = true;
                        break; // Dừng kiểm tra nếu đã tìm thấy giao điểm
                    }
                }

                // Nếu dầm không giao với bất kỳ cột nào, thêm vào supportElements
                if (!intersectsWithColumn)
                {
                    var egm = new ElementGeometryModel(beam, ViewSection) { IsBeam = true };
                    botSupports.Add(egm); // Thêm dầm không liên kết cột vào botSupports
                }
            }

            var sp = BeamGeometries.First().BeamLine.SP().EditZ(beamBotElevation - 50.MmToFoot());
            var ep = BeamGeometries.Last().BeamLine.EP().EditZ(beamBotElevation - 50.MmToFoot());
            var lines = new List<Line>() { Line.CreateBound(sp, ep) };
            foreach (var elementGeometryModel in botSupports)
            {
                foreach (var solid in elementGeometryModel.Solids)
                {
                    lines = lines.LinesDivideBySolid(solid);
                }
            }

            lines = lines.Select(x => EditLineByDirection(ViewSection.RightDirection, x)).ToList();
            return lines;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="param">0.25 0.5 0.75</param>
        /// <param name="line"></param>
        /// <param name="rebars"></param>
        /// <param name="topRebars"></param>
        /// <param name="botRebars"></param>
        /// <param name="midRebars"></param>
        private XYZ GetRebarsAtSegmentPoint(double param, Line line, List<Rebar> rebars, out List<Rebar> topRebars,
            out List<Rebar> botRebars, out List<Rebar> midRebars)
        {
            topRebars = new List<Rebar>();
            botRebars = new List<Rebar>();
            midRebars = new List<Rebar>();
            var p = line.Evaluate(param, true);
            var d = p.DotProduct(ViewSection.RightDirection);
            foreach (var rebar in rebars)
            {
                var l = rebar.GetRebarCurves().OrderByDescending(x => x.Length).FirstOrDefault();
                if (l != null && l.Direction().IsParallel(ViewSection.RightDirection))
                {
                    var bb = rebar.get_BoundingBox(ViewSection);
                    if (bb != null)
                    {
                        var min = bb.Min;
                        var max = bb.Max;
                        var dMin = min.DotProduct(ViewSection.RightDirection);
                        var dMax = max.DotProduct(ViewSection.RightDirection);
                        if (dMin > dMax)
                        {
                            var temp = dMax;
                            dMax = dMin;
                            dMin = temp;
                        }

                        if (d.IsBetweenEqual(dMin, dMax, 0.1))
                        {
                            var location =
                                CommonService.GetRebarStandardLocationInBeam(rebar, beamTopElevation, beamBotElevation);
                            switch (location)
                            {
                                case RebarLocationInBeam.Top:
                                    topRebars.Add(rebar);
                                    break;

                                case RebarLocationInBeam.Mid:
                                    midRebars.Add(rebar);
                                    break;

                                case RebarLocationInBeam.Bot:
                                    botRebars.Add(rebar);
                                    break;
                            }
                        }
                    }
                }
            }

            return p;
        }

   
        #endregion Functions to Get Data
    }
}