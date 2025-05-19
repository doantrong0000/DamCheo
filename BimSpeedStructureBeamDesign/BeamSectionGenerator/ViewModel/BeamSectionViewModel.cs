using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.Beam.BeamAutoSection;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamSectionGenerator.Model;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;
using Transaction = Autodesk.Revit.DB.Transaction;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.ViewModel
{
    public class BeamSectionViewModel : ViewModelBase
    {
        public int Operation
        {
            get => operation;
            set
            {
                operation = value;
                OnPropertyChanged();
            }
        }

        public BeamDetailViewModel BeamDetailViewModel { get; set; }
        private NamingViewModel namingViewModel;
        private int operation = 1;
        public CropModel CropModel { get; set; } = new CropModel();
        public SectionTypeModel SectionTypeModel { get; set; }
        public List<ElementModel> ViewTemplates { get; set; } = new List<ElementModel>();
        public List<ViewFamilyType> ViewFamilyTypes { get; set; } = new List<ViewFamilyType>();

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

        public BeamSectionViewModel(BeamAutoSectionJson data)
        {
            GetData(data);
            OkCommand = new RelayCommand(Ok);
        }

        private void Ok(object obj)
        {
            if (obj is Window window)
            {
                window.Close();
                if (AC.ActiveView is ViewSection viewSection)
                {
                    var beam = AC.Selection.PickObject(ObjectType.Element, new BeamSelectionFilter(), "Select Beam...").ToElement() as FamilyInstance;
                    var beamExtension = new BeamExtension(new List<FamilyInstance>() { beam }) { BeamSectionViewModel = this, BeamDetailViewModel = BeamDetailViewModel };
                    if (beamExtension.Direction.IsParallel(AC.ActiveView.RightDirection))
                    {
                        while (true)
                        {
                            try
                            {
                                using (var tx = new Transaction(AC.Document, "View Section"))
                                {
                                    tx.Start();
                                    ViewUtils.SetSketchPlane();
                                    if (operation == 1)
                                    {
                                        var p = AC.Selection.PickPoint("Point to place Section");
                                        beamExtension.CreateCrossSectionByPoint(p);
                                    }
                                    else
                                    {
                                        var p1 = AC.Selection.PickPoint("Point 1");
                                        var p2 = AC.Selection.PickPoint("Point 2");
                                        beamExtension.CreateCrossSectionBy2Point(p1, p2);
                                    }

                                    tx.Commit();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                                break;
                            }
                        }
                    }
                    else
                    {
                        "BeamSectionViewModel01_MESSAGE".NotificationError(this);
                    }
                }
                else
                {
                    "BeamSectionViewModel02_MESSAGE".NotificationError(this);
                }
            }
        }

        private void GetData(BeamAutoSectionJson data)
        {
            var views = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewSection)).Cast<ViewSection>()
                .Where(x => x.IsTemplate).OrderBy(x => x.Name);
            ViewTemplates.Add(new ElementModel() { Name = "<None>", Element = null, ElementId = ElementId.InvalidElementId });
            foreach (var viewSection in views)
            {
                var em = new ElementModel() { Element = viewSection, ElementId = viewSection.Id, Name = viewSection.Name };
                ViewTemplates.Add(em);
            }

            ViewFamilyTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(x => x.ViewFamily == ViewFamily.Section || x.ViewFamily == ViewFamily.Detail).ToList();
            SectionTypeModel = new SectionTypeModel() { ViewFamilyType = ViewFamilyTypes.FirstOrDefault(), ViewTemplate = ViewTemplates.FirstOrDefault() };

            var beam = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).FirstElement();
            var list = new List<string>();
            if (beam != null)
            {
                var ps = beam.GetOrderedParameters();
                foreach (var parameter in ps)
                {
                    list.Add(parameter.Definition.Name);
                }

                var symbol = beam.GetTypeId().ToElement();
                if (symbol != null)
                {
                    var symbolPs = symbol.GetOrderedParameters();
                    foreach (var parameter in symbolPs)
                    {
                        list.Add(parameter.Definition.Name);
                    }
                }
            }

            list = list.DistinctBy2(x => x).OrderBy(x => x).ToList();
            NamingViewModel = new NamingViewModel(list);

            if (data?.BeamSectionJson != null)
            {
                var json = data.BeamSectionJson;
                operation = json.Operation;
                CropModel = json.CropModel;
                var vft = ViewFamilyTypes.FirstOrDefault(x => x.Name == json.ViewFamilyType);
                if (vft != null)
                {
                    SectionTypeModel.ViewFamilyType = vft;
                }

                var vt = ViewTemplates.FirstOrDefault(x => x.Name == json.ViewTemplate);
                if (vt != null)
                {
                    SectionTypeModel.ViewTemplate = vt;
                }

                if (json.RecordModels != null && json.RecordModels.Count > 0)
                {
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
}