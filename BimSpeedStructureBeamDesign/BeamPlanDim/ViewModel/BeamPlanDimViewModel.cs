using System.Windows;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamPlanDim.Model;
using BimSpeedStructureBeamDesign.BeamPlanDim.View;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamPlanDim.ViewModel
{
    public class BeamPlanDimViewModel : ViewModelBase
    {
        #region Checkbox

        public bool DimLoMo { get; set; } = true;

        private bool centerBeam { get; set; } = false;

        public bool CenterBeam
        {
            get => centerBeam;
            set
            {
                centerBeam = value;
                if (centerBeam == false)
                {
                    allOptionBeam = false;
                }
                else
                {
                    if (_leftEadgeBeam == true && _rightEdgeBeam == true)
                        allOptionBeam = true;
                }

                OnPropertyChanged(nameof(AllOptionBeams));
                OnPropertyChanged(nameof(CenterBeam));
            }
        }

        private bool _leftEadgeBeam { get; set; } = false;

        public bool LeftEdgeBeam
        {
            get => _leftEadgeBeam;

            set
            {
                _leftEadgeBeam = value;
                if (_leftEadgeBeam == false)
                {
                    allOptionBeam = false;
                }
                else
                {
                    if (_rightEdgeBeam == true && centerBeam == true)
                        allOptionBeam = true;
                }

                OnPropertyChanged(nameof(AllOptionBeams));
                OnPropertyChanged(nameof(LeftEdgeBeam));
            }
        }

        private bool _rightEdgeBeam { get; set; } = false;

        public bool RightEdgeBeam
        {
            get => _rightEdgeBeam;

            set
            {
                _rightEdgeBeam = value;
                if (_rightEdgeBeam == false)
                {
                    allOptionBeam = false;
                }
                else
                {
                    if (centerBeam == true && _leftEadgeBeam == true)
                        allOptionBeam = true;
                }

                OnPropertyChanged(nameof(AllOptionBeams));
                OnPropertyChanged(nameof(RightEdgeBeam));
            }
        }

        public bool allOptionBeam { get; set; } = true;

        public bool AllOptionBeams
        {
            get => allOptionBeam;
            set
            {
                allOptionBeam = value;
                if (allOptionBeam == true)
                {
                    LeftEdgeBeam = true;
                    RightEdgeBeam = true;
                    CenterBeam = true;
                }
                else
                {
                    LeftEdgeBeam = false;
                    RightEdgeBeam = false;
                    CenterBeam = false;
                }

                OnPropertyChanged(nameof(allOptionBeam));
            }
        }

        private bool allOptionFloors = true;

        public bool AllOptionFloors
        {
            get => allOptionFloors;
            set
            {
                allOptionFloors = value;

                if (allOptionFloors == true)
                {
                    DimViTriNgoaiSan = true;
                    DimViTri2SanChenhCot = true;
                }

                if (allOptionFloors == false)
                {
                    DimViTriNgoaiSan = false;
                    DimViTri2SanChenhCot = false;
                }

                OnPropertyChanged(nameof(AllOptionFloors));
            }
        }

        private bool dimViTriNgoaiSan;

        public bool DimViTriNgoaiSan
        {
            get => dimViTriNgoaiSan;
            set
            {
                dimViTriNgoaiSan = value;
                OnPropertyChanged(nameof(DimViTriNgoaiSan));
            }
        }

        private bool dimViTri2SanChenhCot;

        public bool DimViTri2SanChenhCot
        {
            get => dimViTri2SanChenhCot;
            set
            {
                dimViTri2SanChenhCot = value;
                OnPropertyChanged(nameof(DimViTri2SanChenhCot));
            }
        }

        public bool DimTruc { get; set; } = true;

        public bool TichHopDimOffset { get; set; } = true;

        #endregion

        public FloorInfo FloorInfo { get; set; }

        public BeamPlanDimView MainView { get; set; }

        public XYZ Point { get; set; }

        public List<Grid> Grids { get; set; }

        public List<DimensionType> DimensionTypes { get; set; } = new();

        private DimensionType selectedDimType { get; set; }

        public DimensionType SelectedDimType
        {
            get => selectedDimType;

            set
            {
                selectedDimType = value;
                OnPropertyChanged(nameof(SelectedDimType));
            }
        }

        public List<Opening> Openings = new List<Opening>();


        public List<FamilyInstance> Beams = new List<FamilyInstance>();

        public List<Floor> Floors = new List<Floor>();

        public OutLineInfo OutLineInfo { get; set; }
        public RelayCommand Ok { get; set; }

        public BeamPlanDimViewModel()
        {
            Ok = new RelayCommand(x => Run());
            GetData();
            SelectedDimType = DimensionTypes.FirstOrDefault();
            var json = JsonUtils.GetSettingFromFile<BeamPlanDimJson>(Define.BeamPlanDimSettingPath);

            if (json != null)
            {
                AllOptionFloors = json.AllOptionFloors;
                AllOptionBeams = json.AllOptionBeams;
                CenterBeam = json.CenterBeam;
                LeftEdgeBeam = json.LeftEdgeBeam;
                RightEdgeBeam = json.RightEdgeBeam;
                DimTruc = json.DimTruc;
                TichHopDimOffset = json.TichHopDimOffset;
                dimViTriNgoaiSan = json.DimViTriNgoaiSan;
                DimLoMo = json.DimLoMo;
                DimViTri2SanChenhCot = json.DimViTri2SanChenhCot;
                SelectedDimType = DimensionTypes?.FirstOrDefault(x => x.Name == json.DimType) ??
                                  DimensionTypes.FirstOrDefault();
            }
        }

        private void Run()
        {
            try
            {
                while (true)
                {
                    SelectRectangle();

                    XYZ mainDirect = XYZ.BasisY;
                    if (OutLineInfo.distanceAB > OutLineInfo.distanceAD)
                    {
                        mainDirect = XYZ.BasisX;
                    }

                    ReferenceArray referenceArray = new ReferenceArray();

                    var listReferenceAndPoint = new List<ReferenceAndPoint>();

                    if (Beams.Count > 0)
                    {
                        foreach (var beam in Beams)
                        {
                            var beamInfo = new BeamInfo(beam);

                            var referenceAndPointCenterBeam = new ReferenceAndPoint();
                            if (beamInfo.Direction.IsParallel(mainDirect.CrossProduct(XYZ.BasisZ)))
                            {
                                var refCenterFrontBack = beam.GetReferences(FamilyInstanceReferenceType.CenterFrontBack)
                                    .FirstOrDefault();

                                referenceAndPointCenterBeam = new ReferenceAndPoint()
                                {
                                    Reference = refCenterFrontBack,
                                    Point = beamInfo.PointCenter
                                };

                                var face = beamInfo.PlanarFaces
                                    .Where(x => x.FaceNormal.IsParallel(mainDirect))
                                    .DistinctBy2(x => x.Origin.DotProduct(mainDirect).Round2Number());

                                if (AllOptionBeams)
                                {
                                    foreach (var planarFace in face)
                                    {
                                        var referenceAndPoint = new ReferenceAndPoint()
                                        {
                                            Reference = planarFace.Reference,
                                            Point = planarFace.Origin
                                        };
                                        listReferenceAndPoint.Add(referenceAndPoint);
                                    }

                                    if (beamInfo.Direction.IsParallel(mainDirect.CrossProduct(XYZ.BasisZ)))
                                        listReferenceAndPoint.Add(referenceAndPointCenterBeam);
                                }
                                else
                                {
                                    if (LeftEdgeBeam)
                                    {
                                        face.OrderBy(x => x.Origin.DotProduct(mainDirect));
                                        var referenceAndPoint = new ReferenceAndPoint()
                                        {
                                            Reference = face.ElementAt(0).Reference,
                                            Point = face.ElementAt(0).Origin
                                        };
                                        listReferenceAndPoint.Add(referenceAndPoint);
                                    }

                                    if (RightEdgeBeam)
                                    {
                                        face.OrderBy(x => x.Origin.DotProduct(mainDirect));
                                        var referenceAndPoint = new ReferenceAndPoint()
                                        {
                                            Reference = face.ElementAt(1).Reference,
                                            Point = face.ElementAt(1).Origin
                                        };
                                        listReferenceAndPoint.Add(referenceAndPoint);
                                    }

                                    if (CenterBeam)
                                    {
                                        if (beamInfo.Direction.IsParallel(mainDirect.CrossProduct(XYZ.BasisZ)))
                                            listReferenceAndPoint.Add(referenceAndPointCenterBeam);
                                    }
                                }
                            }
                        }
                    }

                    if (AllOptionFloors)
                    {
                        if (Floors.Count > 0)
                        {
                            var floors = Floors;
                            if (dimViTri2SanChenhCot == false)
                            {
                                double maxZ = 0;
                                foreach (var floor in floors)
                                {
                                    if (maxZ < floor.get_BoundingBox(AC.ActiveView).Max.Z)
                                        maxZ = floor.get_BoundingBox(AC.ActiveView).Max.Z;
                                }

                                floors = Floors.Where(x => x.get_BoundingBox(AC.ActiveView).Max.Z.IsEqual(maxZ))
                                    .ToList();
                            }

                            foreach (var floor in floors)
                            {
                                FloorInfo = new FloorInfo(floor);

                                var face = FloorInfo.PlanarFaces.Where(x => x.FaceNormal.IsParallel(mainDirect))
                                    .DistinctBy2(x => x.Origin.DotProduct(mainDirect).Round2Number());

                                face = face.OrderBy(x => x.Origin.DotProduct(mainDirect));

                                listReferenceAndPoint.Add(
                                    new ReferenceAndPoint()
                                    {
                                        Reference = face.ElementAt(0).Reference,
                                        Point = face.ElementAt(0).Origin
                                    }
                                );

                                listReferenceAndPoint.Add(
                                    new ReferenceAndPoint()
                                    {
                                        Reference = face.ElementAt(face.Count() - 1).Reference,
                                        Point = face.ElementAt(face.Count() - 1).Origin
                                    }
                                );
                            }
                        }
                    }

                    if (DimLoMo)
                    {
                        if (Floors.Count > 0)
                        {
                            foreach (var floor in Floors)
                            {
                                var openingInfo = new OpeningInfo(floor);

                                var face = openingInfo.PlanarFaces.Where(x => x.FaceNormal.IsParallel(mainDirect))
                                    .DistinctBy2(x => x.Origin.DotProduct(mainDirect).Round2Number());

                                var newFace = face.Where(x => x.FaceNormal.IsParallel(mainDirect));

                                foreach (var opening in openingInfo.ListOpening)
                                {
                                    var isOrverLap = IsRectangleOverlap(OutLineInfo.PointMin, OutLineInfo.PointMax,
                                        opening.PointMin, opening.PointMax);

                                    var listLine = opening.ListLine
                                        .Where(x => x.Direction.IsParallel(mainDirect.CrossProduct(XYZ.BasisZ)))
                                        .ToList();

                                    if (isOrverLap)
                                        foreach (var line in listLine)
                                        {
                                            foreach (var planarFace in newFace)
                                            {
                                                if (line.Origin.DotProduct(mainDirect)
                                                    .IsEqual(planarFace.Origin.DotProduct(mainDirect)))
                                                {
                                                    var referenceAandPoint = new ReferenceAndPoint()
                                                    {
                                                        Reference = planarFace.Reference,
                                                        Point = planarFace.Origin
                                                    };
                                                    listReferenceAndPoint.Add(referenceAandPoint);
                                                }
                                            }
                                        }
                                }
                            }
                        }
                    }

                    if (Openings.Count > 0 && DimLoMo)
                    {
                        foreach (var opening in Openings)
                        {
                            var openingInfo = new OpeningInfo(opening);
                            var face = openingInfo.PlanarFaces.Where(x => x.FaceNormal.IsParallel(mainDirect))
                                .DistinctBy2(x => x.Origin.DotProduct(mainDirect).Round2Number());
                            var bb = opening.get_BoundingBox(AC.ActiveView);

                            var isOverLap = IsRectangleOverlap(OutLineInfo.PointMin, OutLineInfo.PointMax,
                                bb.Min, bb.Max);
                            if (isOverLap)
                                foreach (var planarFace in face)
                                {
                                    var referenceAandPoint = new ReferenceAndPoint()
                                    {
                                        Reference = planarFace.Reference,
                                        Point = planarFace.Origin
                                    };
                                    listReferenceAndPoint.Add(referenceAandPoint);
                                }
                        }
                    }

                    if (DimTruc)
                    {
                        if (Grids.Any())
                        {
                            foreach (var grid in Grids)
                            {
                                var line = grid.Curve as Line;

                                if (line.Direction.IsParallel(mainDirect.CrossProduct(XYZ.BasisZ)))
                                {
                                    var origin = line.Origin;

                                    if (origin.Y > OutLineInfo.PointMin.Y && origin.Y < OutLineInfo.PointMax.Y ||
                                        origin.X > OutLineInfo.PointMin.X && origin.X < OutLineInfo.PointMax.X)
                                    {
                                        var referenceAndPoint = new ReferenceAndPoint()
                                        {
                                            Reference = new Reference(grid),
                                            Point = (grid.Curve as Line).SP()
                                        };
                                        listReferenceAndPoint.Add(referenceAndPoint);
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Không có grids nao");
                        }
                    }

                    var newListRef =
                        listReferenceAndPoint.DistinctBy2(x => x.Point.DotProduct(mainDirect).Round2Number());

                    Point = AC.Selection.PickPoint();

                    if (Point != null && listReferenceAndPoint.Count > 0)
                    {
                        var line = Point.CreateLineByPointAndDirection(mainDirect);

                        foreach (var referenceAndPoint in newListRef)
                        {
                            referenceArray.Append(referenceAndPoint.Reference);
                        }

                        using (Transaction trans = new Transaction(AC.Document, "Create Dim Beam"))
                        {
                            trans.Start();

                            var dim = AC.Document.Create.NewDimension(AC.ActiveView, line, referenceArray,
                                SelectedDimType);

                            // if (TichHopDimOffset)
                            // {
                            //     new DimOffsetService(dim).SetMoveForCorrect();
                            // }

                            trans.Commit();
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }

            MainView.ShowDialog();
        }

        private void SelectRectangle()
        {
            MainView.Hide();

            Beams = new List<FamilyInstance>();
            Floors = new List<Floor>();
            Openings = new List<Opening>();
            var box = AC.Selection.PickBox(Autodesk.Revit.UI.Selection.PickBoxStyle.Crossing);

            OutLineInfo = new OutLineInfo(box.Max, box.Min);

            var outline =
                new Outline(OutLineInfo.PointMin,
                    OutLineInfo.PointMax);

            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector = new FilteredElementCollector(AC.Document, AC.ActiveView.Id);

            Openings = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(Opening))
                .OfCategory(BuiltInCategory.OST_ShaftOpening).Cast<Opening>().ToList();

            Grids = new FilteredElementCollector(AC.Document, AC.ActiveView.Id)
                .OfClass(typeof(Grid)).Cast<Grid>().ToList();

            collector.WherePasses(filter);
            var listele = collector.WherePasses(filter).ToElements();

            foreach (var ele in listele)
            {
                if (ele is FamilyInstance && (ele as FamilyInstance).Category.ToBuiltinCategory() ==
                    BuiltInCategory.OST_StructuralFraming && (ele as FamilyInstance).Category.ToBuiltinCategory() !=
                    BuiltInCategory.OST_StructuralColumns)
                {
                    var beam = ele as FamilyInstance;
                    Beams.Add(beam);
                }

                if (ele is Floor)
                {
                    var floor = ele as Floor;
                    Floors.Add(floor);
                }
            }
        }

        public bool IsRectangleOverlap(XYZ point1, XYZ point2, XYZ point3, XYZ point4)
        {
            if (point1.X == point2.X || point1.Y == point2.Y || point3.X == point4.X || point3.Y == point4.Y)
                return false;

            if (point1.X > point4.X || point3.X > point2.X)
                return false;

            if (point1.Y > point4.Y || point2.Y < point3.Y)
                return false;

            return true;
        }

        private void GetData()
        {
            DimensionTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .Where(x => x.StyleType == DimensionStyleType.Linear ||
                            x.StyleType == DimensionStyleType.LinearFixed).OrderBy(x => x.Name)
                .DistinctBy2(x => x.Name).ToList();

            if (AllOptionBeams)
            {
                LeftEdgeBeam = true;
                RightEdgeBeam = true;
                CenterBeam = true;
            }

            if (allOptionFloors)
            {
                DimViTriNgoaiSan = true;
                DimViTri2SanChenhCot = true;
            }
        }
    }
}