using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.Beam.BeamAutoSection;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.Model;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel
{
    public class BeamDetailViewModel : ViewModelBase
    {
        private bool isAll = true;
        private bool isX = true;
        private bool isY = true;
        private bool isInclined = true;
        private bool isSelection = true;
        private bool autoGenerateBeamCrossSection = true;
        private NamingViewModel namingViewModel;
        public CropModel CropModel { get; set; } = new CropModel();
        public BeamSectionViewModel BeamSectionViewModel { get; set; }
        public SectionTypeModel HorizontalBeamSectionTypeModel { get; set; }
        public SectionTypeModel VerticalBeamSectionTypeModel { get; set; }
        public SectionTypeModel InclinedBeamSectionTypeModel { get; set; }
        public double SectionOffsetFromSideFace { get; set; } = 50.MmToFoot();

        private Langs language { get; set; }
        public Langs Language
        {
            get => language;
            set
            {
                language = value;
                OnPropertyChanged(nameof(Language));
                Constants.Lang = (LangEnum)Language.Value;
            }
        }

        private List<Langs> langueges { get; set; }

        public List<Langs> Languages
        {

            get => langueges;

            set
            {
                langueges = value;
                OnPropertyChanged(nameof(Languages));

            }
        }

        public bool IsAll
        {
            get => isAll;
            set
            {
                isAll = value;
                if (value)
                {
                    isX = true;
                    isY = true;
                    isInclined = true;
                    isSelection = false;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelection));
                OnPropertyChanged(nameof(IsX));
                OnPropertyChanged(nameof(IsY));
                OnPropertyChanged(nameof(IsInclined));
            }
        }

        public class Langs
        {
            public string Name { get; set; }

            public int Value { get; set; }
        }


        public bool IsX
        {
            get => isX;
            set
            {
                isX = value;
                if (value)
                {
                    isSelection = false;
                }
                OnPropertyChanged(nameof(IsSelection));
                OnPropertyChanged();
            }
        }

        public bool IsY
        {
            get => isY;
            set
            {
                isY = value;
                if (value)
                {
                    isSelection = false;
                }
                OnPropertyChanged(nameof(IsSelection));
                OnPropertyChanged();
            }
        }

        public bool IsInclined
        {
            get => isInclined;
            set
            {
                isInclined = value;
                if (value)
                {
                    isSelection = false;
                }
                OnPropertyChanged(nameof(IsSelection));
                OnPropertyChanged();
            }
        }

        public bool IsSelection
        {
            get => isSelection;
            set
            {
                isSelection = value;
                if (value)
                {
                    isAll = false;
                    isX = false;
                    isY = false;
                    isInclined = false;
                }
                else
                {
                    IsAll = true;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAll));
                OnPropertyChanged(nameof(IsX));
                OnPropertyChanged(nameof(IsY));
                OnPropertyChanged(nameof(IsInclined));
            }
        }

        public List<ElementModel> ViewTemplates { get; set; } = new List<ElementModel>();
        public List<ViewFamilyType> ViewFamilyTypes { get; set; } = new List<ViewFamilyType>();

        public bool AutoGenerateBeamCrossSection
        {
            get => autoGenerateBeamCrossSection;
            set
            {
                autoGenerateBeamCrossSection = value;
                OnPropertyChanged();
            }
        }

        public double Length3Sections { get; set; } = 1000.MmToFoot();
        public double Position1 { get; set; } = 0.1;
        public double Position2 { get; set; } = 0.5;
        public double Position3 { get; set; } = 0.9;
        public RelayCommand OkCommand { get; set; }

        public NamingViewModel NamingViewModel
        {
            get => namingViewModel;
            set
            {
                namingViewModel = value;
                OnPropertyChanged();
            }
        }

        public BeamDetailViewModel(BeamAutoSectionJson data)
        {
            GetData(data);
            OkCommand = new RelayCommand(Ok);
            Languages = new List<Langs> {
                new Langs(){Name = "English", Value= 1},
                 new Langs(){Name = "Viet Nam", Value= 2}
            };
            Language = Languages.First();

            Constants.Lang = (LangEnum)Language.Value;

        }

        private void Ok(object obj)
        {
            if (obj is Window window)
            {
                window.Close();
                var beams = GetBeams();
                foreach (var familyInstance in beams)
                {
                    GenerateBeamSection(familyInstance);
                }
            }
        }

        private List<FamilyInstance> GetBeams()
        {
            var list = new List<FamilyInstance>();
            var list2 = new List<FamilyInstance>();
            if (isSelection == false)
            {
                var beams = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>().ToList();
                foreach (var familyInstance in beams)
                {
                    if (familyInstance.Location is LocationCurve lc)
                    {
                        if (lc.Curve is Line line)
                        {
                            var direct = line.Direction;
                            if (isAll)
                            {
                                list.Add(familyInstance);
                            }
                            else
                            {
                                if (isX)
                                {
                                    if (direct.IsParallel(XYZ.BasisX))
                                    {
                                        list.Add(familyInstance);
                                        continue;
                                    }
                                }
                                if (isY)
                                {
                                    if (direct.IsParallel(XYZ.BasisY))
                                    {
                                        list.Add(familyInstance);
                                        continue;
                                    }
                                }
                                if (isInclined)
                                {
                                    if (direct.IsParallel(XYZ.BasisY) == false && direct.IsParallel(XYZ.BasisX) == false)
                                    {
                                        list.Add(familyInstance);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                try
                {
                    list = AC.Selection.PickObjects(ObjectType.Element, new BeamSelectionFilter(), "Select Beams...")
                        .Select(x => x.ToElement()).Where(x => x is FamilyInstance).Cast<FamilyInstance>().ToList();
                }
                catch
                {
                    "BeamDetailViewModel01_MESSAGE".NotificationError(this);
                }
            }

            //Distint by mark
            var marks = new List<string>();
            foreach (var familyInstance in list)
            {
                var mark = familyInstance.GetParameterValueAsString(BuiltInParameter.DOOR_NUMBER);
                if (string.IsNullOrEmpty(mark))
                {
                    list2.Add(familyInstance);
                }
                else
                {
                    if (marks.Contains(mark))
                    {
                        continue;
                    }
                    marks.Add(mark);
                    list2.Add(familyInstance);
                }
            }
            return list2;
        }

        private void GenerateBeamSection(FamilyInstance fi)
        {
            using (var tx = new Transaction(AC.Document, "Auto"))
            {
                tx.Start();
                var beamExtension = new BeamExtension(new List<FamilyInstance>() { fi }) { BeamDetailViewModel = this, BeamSectionViewModel = BeamSectionViewModel };
                beamExtension.CreateSectionsForAutoGenerate(autoGenerateBeamCrossSection);
                tx.Commit();
            }
        }

        private void GetData(BeamAutoSectionJson data)
        {
            var views = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewSection)).Cast<ViewSection>()
                .Where(x => x.IsTemplate).OrderBy(x => x.Name);
            ViewTemplates.Add(new ElementModel() { Name = "<None>", Element = null, ElementId = ElementId.InvalidElementId });
            //foreach (var viewSection in views)
            //{
            //    var em = new ElementModel() { Element = viewSection, ElementId = viewSection.Id, Name = viewSection.Name };
            //    ViewTemplates.Add(em);
            //}

            //optimize
            ViewTemplates.AddRange(views.Select(viewsection => new ElementModel
                {
                    Element = viewsection,
                    ElementId = viewsection.Id,
                    Name = viewsection.Name,
                }
                ));

            ViewFamilyTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewFamilyType)).
                Cast<ViewFamilyType>().Where(x => x.ViewFamily == ViewFamily.Section || x.ViewFamily == ViewFamily.Detail).ToList();

            HorizontalBeamSectionTypeModel = new SectionTypeModel() { ViewFamilyType = ViewFamilyTypes.FirstOrDefault(), ViewTemplate = ViewTemplates.FirstOrDefault() };

            VerticalBeamSectionTypeModel = new SectionTypeModel() { ViewFamilyType = ViewFamilyTypes.FirstOrDefault(), ViewTemplate = ViewTemplates.FirstOrDefault() };

            InclinedBeamSectionTypeModel = new SectionTypeModel() { ViewFamilyType = ViewFamilyTypes.FirstOrDefault(), ViewTemplate = ViewTemplates.FirstOrDefault() };

            var beam = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).FirstElement();

            var list = new List<string>();
            if (beam != null)
            {
                //var ps = beam.GetOrderedParameters();
                //foreach (var parameter in ps)
                //{
                //    list.Add(parameter.Definition.Name);
                //}

                list.AddRange(beam.GetOrderedParameters().Select(x => x.Definition.Name));

                var symbol = beam.GetTypeId().ToElement();
                if (symbol != null)
                {
                    //var symbolPs = symbol.GetOrderedParameters();
                    //foreach (var parameter in symbolPs)
                    //{
                    //    list.Add(parameter.Definition.Name);
                    //}
                    list.AddRange(symbol.GetOrderedParameters().Select(x => x.Definition.Name));
                }
            }

            list = list.DistinctBy2(x => x).OrderBy(x => x).ToList();
            NamingViewModel = new NamingViewModel(list);

            if (data?.BeamDetailJson != null)
            {
                var json = data.BeamDetailJson;
                CropModel = json.CropModel;
                isAll = json.IsAll;
                isX = json.IsX;
                isY = json.IsY;
                isInclined = json.IsInclined;
                IsSelection = json.IsSelection;
                autoGenerateBeamCrossSection = json.AutoGenerateBeamCrossSection;
                Position1 = json.Position1;
                Position2 = json.Position2;
                Position3 = json.Position3;
                SectionOffsetFromSideFace = json.SectionOffsetFromSideFace;
                Length3Sections = json.Length3Sections;
                var xVft = ViewFamilyTypes.FirstOrDefault(x => x.Name == json.XViewFamilyType);
                if (xVft != null)
                {
                    HorizontalBeamSectionTypeModel.ViewFamilyType = xVft;
                }

                var yVft = ViewFamilyTypes.FirstOrDefault(x => x.Name == json.YViewFamilyType);
                if (yVft != null)
                {
                    VerticalBeamSectionTypeModel.ViewFamilyType = yVft;
                }

                var iVft = ViewFamilyTypes.FirstOrDefault(x => x.Name == json.InclinedViewFamilyType);
                if (iVft != null)
                {
                    InclinedBeamSectionTypeModel.ViewFamilyType = iVft;
                }

                var xViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name == json.XViewTemplate);
                if (xViewTemplate != null)
                {
                    HorizontalBeamSectionTypeModel.ViewTemplate = xViewTemplate;
                }

                var yViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name == json.YViewTemplate);
                if (yViewTemplate != null)
                {
                    VerticalBeamSectionTypeModel.ViewTemplate = yViewTemplate;
                }

                var iViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name == json.InclinedViewTemplate);

                if (iViewTemplate != null)
                {
                    InclinedBeamSectionTypeModel.ViewTemplate = iViewTemplate;
                }

                if (json.RecordModels != null)
                {
                    if (json.RecordModels.Count > 0)
                    {
                        NamingViewModel.RecordModels.Clear();
                    }
                    foreach (var jsonRecordModel in json.RecordModels)
                    {
                        if (jsonRecordModel.IsParam == false)
                        {
                            NamingViewModel.RecordModels.Add(jsonRecordModel);
                        }
                        else
                        {
                            if (namingViewModel.Parameters.Contains(jsonRecordModel.Text))
                            {
                                NamingViewModel.RecordModels.Add(jsonRecordModel);
                            }
                        }
                    }
                    namingViewModel.GetPreview();
                }

            }
        }
    }
}