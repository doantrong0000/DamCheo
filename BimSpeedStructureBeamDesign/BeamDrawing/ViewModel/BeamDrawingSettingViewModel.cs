using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.Model.Json;
using BimSpeedStructureBeamDesign.BeamDrawing.View;
using BimSpeedStructureBeamDesign.Utils;
using BimSpeedStructureBeamDesign.Utils.View;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace BimSpeedStructureBeamDesign.BeamDrawing.ViewModel
{
    public class BeamDrawingSettingViewModel : ViewModelBase
    {

        #region declare
        public bool IsCreate3DView { get; set; } = false;
        public ObservableCollection<BeamDrawingSetting> BeamDrawingSettings { get; set; } = new();
        public static List<BeamDrawingSettingJson> BeamDrawingSettingRepositories { get; set; }

        public BeamDrawingSetting BeamDrawingSetting
        {
            get => beamDrawingSetting;
            set
            {
                beamDrawingSetting = value;
                OnPropertyChanged();
            }
        }

        public bool IsMultiCrossSection
        {
            get => _multiCrossSection;
            set
            {
                if (value == _multiCrossSection) return;
                _multiCrossSection = value;
                OnPropertyChanged();
            }
        }

        private string _filterString { get; set; }

        public string FilterString
        {
            get => _filterString;
            set
            {
                _filterString = value;
                CollectionView collectionView = (CollectionView)CollectionViewSource.GetDefaultView(ViewSheets);
                collectionView.Refresh();
            }
        }

        public List<ViewSheet> ViewSheets { get; set; } = new();
        public ViewSheet SheetSelected { get; set; }
        public List<FamilySymbol> IndependentTagSymbols { get; set; } = new();
        public List<MultiReferenceAnnotationType> MultiReferenceAnnotationTypes { get; set; } = new();
        public List<DimensionType> DimensionTypes { get; set; } = new();
        public List<FamilySymbol> BreakLineSymbols { get; set; } = new();
        public List<SpotDimensionType> SpotDimensionTypes { get; set; } = new();
        public List<ViewSection> ViewTemplates { get; set; } = new();
        public List<ViewFamilyType> ViewFamilyTypes { get; set; } = new();
        public List<ElementType> ViewportTypes { get; set; } = new();

        public SheetFinderView SheetFinderView { get; set; }

        private BeamDrawingSetting beamDrawingSetting;
        private bool _multiCrossSection = false;
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CmAddToCurrentSheet { get; set; }
        public RelayCommand BtnSearchSheet { get; set; }
        public RelayCommand AddSetting { get; set; }
        public RelayCommand DeleteSetting { get; set; }
        public RelayCommand UpdateSetting { get; set; }
        public RelayCommand ExportSetting { get; set; }
        public RelayCommand LoadSetting { get; set; }
        public List<string> SheetNumbers { get; set; } = new();
        public List<string> SheetNames { get; set; } = new();
        public List<FamilySymbol> TitleBlocks { get; set; } = new();

        public bool IsDrawDim
        { 
            get => BeamDrawingSetting.BeamDetailSetting.IsDrawDim && BeamDrawingSetting.BeamSectionSetting.IsDrawDim;   
            set
            {
                BeamDrawingSetting.BeamDetailSetting.IsDrawDim = value;
                BeamDrawingSetting.BeamSectionSetting.IsDrawDim = value;
                OnPropertyChanged(nameof(IsDrawDim));
            }
        }
        public bool IsDrawTagElevation
        {
            get => BeamDrawingSetting.BeamDetailSetting.IsDrawTagElevation && BeamDrawingSetting.BeamSectionSetting.IsDrawTagElevation;
            set
            {
                BeamDrawingSetting.BeamDetailSetting.IsDrawTagElevation = value;
                BeamDrawingSetting.BeamSectionSetting.IsDrawTagElevation = value;
                OnPropertyChanged(nameof(IsDrawTagElevation));
            }
        }
        public bool IsDrawStick
        {
            get => BeamDrawingSetting.BeamDetailSetting.IsDrawStick && BeamDrawingSetting.BeamSectionSetting.IsDrawStick;
            set
            {
                BeamDrawingSetting.BeamDetailSetting.IsDrawStick = value;
                BeamDrawingSetting.BeamSectionSetting.IsDrawStick = value;
                OnPropertyChanged(nameof(IsDrawStick));
            }
        }
        public bool IsDrawBreakLine
        {
            get => BeamDrawingSetting.BeamDetailSetting.IsDrawBreakLine && BeamDrawingSetting.BeamSectionSetting.IsDrawBreakLine;
            set
            {
                BeamDrawingSetting.BeamDetailSetting.IsDrawBreakLine = value;
                BeamDrawingSetting.BeamSectionSetting.IsDrawBreakLine = value;
                OnPropertyChanged(nameof(IsDrawBreakLine));
            }
        }
        public double DistanElevation
        {
            get => BeamDrawingSetting.BeamDetailSetting.KhoangCachTagElevationDenDam;
            set
            {
                BeamDrawingSetting.BeamDetailSetting.KhoangCachTagElevationDenDam = value;
                BeamDrawingSetting.BeamSectionSetting.KhoangCachTagElevationDenDam = value;
                OnPropertyChanged(nameof(DistanElevation));
            }
        }
        public bool PickSupportBeam { get; set; }
        #endregion

        public BeamDrawingSettingViewModel()
        {
            GetData();
            BtnSearchSheet = new RelayCommand(x => ShowFormFindSheet());
            OkCommand = new RelayCommand(Ok, x => BeamDrawingSetting != null);
            AddSetting = new RelayCommand(Add, x => string.IsNullOrEmpty((x as TextBox)?.Text) == false);
            UpdateSetting = new RelayCommand(Update, x => string.IsNullOrEmpty((x as TextBox)?.Text) == false);
            DeleteSetting = new RelayCommand(x => Delete(), x => BeamDrawingSetting != null);
            ExportSetting = new RelayCommand(Export);
            LoadSetting = new RelayCommand(x => Load());
            CmAddToCurrentSheet = new RelayCommand(x => AddSheetNumberName());
        }

        private void GetData()
        {
            //ViewSheets
            ViewSheets = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
                .OrderBy(x => x.SheetNumber).ToList();

            SharedData.Instance.ViewSheets = ViewSheets;

            CollectionView collectionView = (CollectionView)CollectionViewSource.GetDefaultView(ViewSheets);

            collectionView.Filter = UserFilter;

            //foreach (var viewSheet in ViewSheets)
            //{
            //    SheetNumbers.Add(viewSheet.SheetNumber);
            //    SheetNames.Add(viewSheet.Name);
            //}
            SheetNumbers.Sort();
            SheetNames.Sort();
            //IndependentTags
            IndependentTagSymbols = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_RebarTags).Cast<FamilySymbol>().OrderBy(x => x.Name).ToList();
            //Multitags
            MultiReferenceAnnotationTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(MultiReferenceAnnotationType)).Cast<MultiReferenceAnnotationType>().OrderBy(x => x.Name).ToList();
            //Dimensions
            DimensionTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(x => x.StyleType == DimensionStyleType.Linear || x.StyleType == DimensionStyleType.LinearFixed).OrderBy(x => x.Name).ToList();
            //BreakLines
            BreakLineSymbols = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_DetailComponents).Cast<FamilySymbol>().Where(x => x.Family?.FamilyPlacementType == FamilyPlacementType.CurveBasedDetail).ToList();
            //Spot Elevations
            SpotDimensionTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(SpotDimensionType)).Cast<SpotDimensionType>().OrderBy(x => x.Name).ToList();
            //Viewtemplate
            ViewTemplates = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewSection)).Cast<ViewSection>().Where(x => x.IsTemplate).OrderBy(x => x.Name).ToList();
            //Loai section cut
            ViewFamilyTypes = new FilteredElementCollector(AC.Document).OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(x => x.ViewFamily == ViewFamily.Section || x.ViewFamily == ViewFamily.Detail)
                .OrderBy(x => x.Name)
                .ToList();
            //TitleBlocks
            TitleBlocks = new FilteredElementCollector(AC.Document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>().OrderBy(x => x.Name)
                .ToList();
            //Viewport
            var viewport = new FilteredElementCollector(AC.Document).OfClass(typeof(Viewport)).Cast<Viewport>().FirstOrDefault();
            if (viewport != null)
            {
                var validTypeIds = viewport.GetValidTypes();
                ViewportTypes = validTypeIds.Select(x => x.ToElement()).Cast<ElementType>().OrderBy(x => x.Name)
                    .ToList();
            }

            SetupDefaultSetting();

            beamDrawingSetting = BeamDrawingSettings.FirstOrDefault();
        }

        private void ShowFormFindSheet()
        {
            SheetFinderView = new SheetFinderView();
            SheetFinderView.DataContext = this;
            SheetFinderView.ShowDialog();
        }
        private bool UserFilter(object item)
        {
            bool flag = false;

            ViewSheet vs = item as ViewSheet;

            if (string.IsNullOrEmpty(_filterString))
            {
                flag = true;
            }
            else
            {
                flag = vs.Name.IsContainFilter(_filterString) || vs.SheetNumber.IsContainFilter(_filterString);
            }
            return flag;
        }

        private void AddSheetNumberName()
        {
            BeamDrawingSetting.BeamSheetSetting.SheetNumber = SheetSelected.SheetNumber;
            BeamDrawingSetting.BeamSheetSetting.SheetName = SheetSelected.Name;

            OnPropertyChanged(nameof(BeamDrawingSetting.BeamSheetSetting.SheetNumber));
            OnPropertyChanged(nameof(BeamDrawingSetting.BeamSheetSetting.SheetName));
            SheetFinderView.Close();
        }

        private void SetupDefaultSetting(List<BeamDrawingSettingJson> data=null )
        {
            var setting = GetDefaultFromJson();
            OnPropertyChanged(nameof(BeamDrawingSettings));
            if (BeamDrawingSettingRepositories == null)
            {
                BeamDrawingSettingRepositories = JsonUtils.GetSettingFromFile<List<BeamDrawingSettingJson>>(AC.BimSpeedSettingPath + "//BeamDrawingSetting.json")?.Where(x => x.IsValid()).ToList();

                if (data != null)
                {
                    if (BeamDrawingSettingRepositories == null)
                    {
                        BeamDrawingSettingRepositories = new List<BeamDrawingSettingJson>();
                    }

                    BeamDrawingSettingRepositories = BeamDrawingSettingRepositories.Concat(data.Where(x => x.Name != "Default")).DistinctBy2(x => x.Name).ToList();
                }
            }

            if (BeamDrawingSettingRepositories == null)
            {
                BeamDrawingSettings = new ObservableCollection<BeamDrawingSetting>(setting);
            }
            else
            {
                if (BeamDrawingSettingRepositories.Count == 0)
                {
                    BeamDrawingSettings = new ObservableCollection<BeamDrawingSetting>(setting);
                }
                else
                {
                    if (data != null)
                    {
                        BeamDrawingSettingRepositories = BeamDrawingSettingRepositories.Concat(data.Where(x => x.Name != "Default")).DistinctBy2(x => x.Name).ToList();
                    }
                    foreach (var beamDrawingSettingRepository in BeamDrawingSettingRepositories)
                    {
                        if (BeamDrawingSettings.Any(x => x.Name == beamDrawingSettingRepository.Name))
                        {
                            continue;
                        }

                        BeamDrawingSettings.Add(beamDrawingSettingRepository.GetBeamDrawingSetting(this));
                    }
                }
            }

            if (setting.FirstOrDefault()?.BeamDetailSetting.BreakLineSymbol == null
                   || setting.FirstOrDefault()?.BeamDetailSetting.TagRebarStandardPhai == null
                   || setting.FirstOrDefault()?.BeamDetailSetting.TagThepDaiTrai == null
                   || setting.FirstOrDefault()?.BeamDetailSetting.ViewTemplate == null
                   || setting.FirstOrDefault()?.BeamSectionSetting.ViewTemplate == null)
            {
                "BEAMDRAWINGSETTINGVIEWMODEL_MESSAGE1".NotificationError(nameof(BimSpeedStructureBeamDesign));
                var setting1 = GetDefault();
            }

            foreach (var drawingSetting in BeamDrawingSettings)
            {
                if (drawingSetting.BeamDetailSetting.ViewFamilyType == null)
                {
                    drawingSetting.BeamDetailSetting.ViewFamilyType = ViewFamilyTypes.FirstOrDefault();
                }

                if (drawingSetting.BeamSectionSetting.ViewFamilyType == null)
                {
                    drawingSetting.BeamSectionSetting.ViewFamilyType = ViewFamilyTypes.FirstOrDefault();
                }
            }


            BeamDrawingSettings = new ObservableCollection<BeamDrawingSetting>(BeamDrawingSettings.Where(x => x.Name != "Default"));
        }

        private BeamDrawingSetting GetDefault()
        {
            var setting = new BeamDrawingSetting { Name = "Default" };
            //Sheet Setting
            var sheetSetting = setting.BeamSheetSetting;
            sheetSetting.ViewSheet = ViewSheets.FirstOrDefault();
            SharedData.Instance.ViewSheets = ViewSheets;
            if (sheetSetting.ViewSheet != null)
            {
                sheetSetting.SheetNumber = sheetSetting.ViewSheet.SheetNumber;
                sheetSetting.SheetNumber = sheetSetting.ViewSheet.Name;
            }

            var titleBlock = AC.Document.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_TitleBlocks));
            sheetSetting.TitleBlock = TitleBlocks.FirstOrDefault(x => x.Id.GetElementIdValue() == titleBlock?.GetElementIdValue());

            sheetSetting.TitleBlock = TitleBlocks.FirstOrDefault();

            //Beam Detail Setting

            #region Detail Setting

            var beamDetailSetting = setting.BeamDetailSetting;
            var spotDimensionType = AC.Document.GetDefaultElementTypeId(ElementTypeGroup.SpotElevationType).ToElement() as SpotDimensionType;
            beamDetailSetting.SpotDimensionType =
                SpotDimensionTypes.FirstOrDefault(x => x.Id.GetElementIdValue() == spotDimensionType?.Id.GetElementIdValue()) ??
                SpotDimensionTypes.FirstOrDefault();

            beamDetailSetting.TagRebarStandardPhai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStandardBarPhai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            beamDetailSetting.TagRebarStandardTrai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStandardBarTrai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            beamDetailSetting.TagThepDaiTrai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStirrupTraiDetailView && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            beamDetailSetting.TagThepDaiPhai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStirrupPhaiDetailView && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            var detailItems = new FilteredElementCollector(AC.Document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents).Cast<FamilySymbol>().Where(x => x.Family.FamilyPlacementType == FamilyPlacementType.ViewBased).ToList();
            beamDetailSetting.DauMocThep = detailItems.FirstOrDefault(x => x.Name == Define.FamilySymbolDauThep) ?? detailItems.FirstOrDefault();
            beamDetailSetting.ViewportType = ViewportTypes.FirstOrDefault(x => x.Name == Define.ViewportTypeHorizontalSection) ??
                                             ViewportTypes.FirstOrDefault();
            beamDetailSetting.Scale = 35;
            //Default Dimensiontype
            var dimensionType = AC.Document.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType).ToElement() as DimensionType;
            beamDetailSetting.BreakLineSymbol = BreakLineSymbols.FirstOrDefault(x => x.Name.Contains(Define.FamilySymbolBreakLine));
            beamDetailSetting.ViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name == Define.ViewTemplateSection);
            beamDetailSetting.ViewFamilyType = ViewFamilyTypes.FirstOrDefault();
            beamDetailSetting.ViewportType = ViewportTypes.FirstOrDefault();
            beamDetailSetting.DimensionTypeGap = DimensionTypes.FirstOrDefault(x => x.Name == Define.DimensionGap) ?? DimensionTypes.FirstOrDefault();
            beamDetailSetting.DimensionTypeFixed = DimensionTypes.FirstOrDefault(x => x.Name == Define.DimensionFixed) ?? DimensionTypes.FirstOrDefault();

            #endregion Detail Setting

            //Beam Section Setting
            var beamSectionSetting = setting.BeamSectionSetting;
            beamSectionSetting.TagThepNhomPhai = MultiReferenceAnnotationTypes.FirstOrDefault(x => x.Name == Define.MultiTagForStandardBarPhai) ?? MultiReferenceAnnotationTypes.FirstOrDefault();
            beamSectionSetting.TagThepNhomTrai = MultiReferenceAnnotationTypes.FirstOrDefault(x => x.Name == Define.MultiTagForStandardBarTrai) ?? MultiReferenceAnnotationTypes.FirstOrDefault(); ;

            beamSectionSetting.TagThepDaiPhai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStirrupPhai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();
            beamSectionSetting.TagThepDaiTrai = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStirrupTrai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            beamSectionSetting.IndependentTagRebarStandardLeft = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStandardBarTrai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();
            beamSectionSetting.IndependentTagRebarStandardRight = IndependentTagSymbols.FirstOrDefault(x => x.Name == Define.IndependentTagForStandardBarPhai && x.Family.Name.Contains("BS")) ?? IndependentTagSymbols.FirstOrDefault();

            beamSectionSetting.DimensionType = DimensionTypes.FirstOrDefault(x => x.Id.GetElementIdValue() == dimensionType?.Id.GetElementIdValue());
            beamSectionSetting.BreakLineSymbol = BreakLineSymbols.FirstOrDefault(x => x.Name == Define.FamilySymbolBreakLine);
            beamSectionSetting.SpotDimensionType =
                SpotDimensionTypes.FirstOrDefault(x => x.Id.GetElementIdValue() == spotDimensionType?.Id.GetElementIdValue());

            beamSectionSetting.ViewTemplate = ViewTemplates.FirstOrDefault(x => x.Name == Define.ViewTemplateCrossSection);

            beamSectionSetting.ViewFamilyType = ViewFamilyTypes.FirstOrDefault();
            beamSectionSetting.ViewportType = ViewportTypes.FirstOrDefault();
            beamSectionSetting.DimensionTypeGap = DimensionTypes.FirstOrDefault(x => x.Name == Define.DimensionGap) ?? DimensionTypes.FirstOrDefault();
            beamSectionSetting.DimensionTypeFixed = DimensionTypes.FirstOrDefault(x => x.Name == Define.DimensionFixed) ?? DimensionTypes.FirstOrDefault();
            beamSectionSetting.ViewportType = ViewportTypes.FirstOrDefault(x => x.Name == Define.ViewportTypeCrossSection) ??
                                             ViewportTypes.FirstOrDefault();
            beamSectionSetting.Scale = 25;
            return setting;
        }

        private List<BeamDrawingSetting> GetDefaultFromJson()
        {
            var setting = new List<BeamDrawingSetting>();

            List<BeamDrawingSettingJson> data = new List<BeamDrawingSettingJson>();

            var dts = BimSpeedStructureBeamDesign.Properties.Resources.beamdrawingsetting;

            Stream stream = new MemoryStream(dts);
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();

            var dataJsons = (JsonConvert.DeserializeObject<List<BeamDrawingSettingJson>>(result) ?? new List<BeamDrawingSettingJson>()).Where(x => x.IsValid()).ToList();

            foreach (var dataJson in dataJsons)
            {
                setting.Add(dataJson.GetBeamDrawingSetting(this));
            }

            return setting;
        }

        private void Ok(object x)
        {
            if (x is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }

            BeamDrawingSettingRepositories = BeamDrawingSettings.Select(y => new BeamDrawingSettingJson(y)).ToList();
            JsonUtils.SaveSettingToFile(BeamDrawingSettingRepositories, AC.BimSpeedSettingPath + "//BeamDrawingSetting.json");
        }

        private void Add(object x)
        {
            if (x is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text) == false)
                {
                    if (BeamDrawingSettings.FirstOrDefault(xx => xx.Name == tb.Text) == null)
                    {
                        if (beamDrawingSetting == null)
                        {
                            var clone = new BeamDrawingSetting(beamDrawingSetting, tb.Text);
                            BeamDrawingSettings.Add(clone);
                            tb.Text = "";
                        }
                        else
                        {
                            var clone = new BeamDrawingSetting(beamDrawingSetting, tb.Text);
                            BeamDrawingSettings.Add(clone);
                            tb.Text = "";
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(BeamDrawingSettings));
            OnPropertyChanged(nameof(BeamDrawingSetting));
        }

        private void Update(object x)
        {
            if (x is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text) == false)
                {
                    if (beamDrawingSetting == null)
                    {
                    }
                    else
                    {
                        beamDrawingSetting.Name = tb.Text;
                        tb.Text = "";
                    }
                }
            }
            OnPropertyChanged(nameof(BeamDrawingSettings));
            OnPropertyChanged(nameof(BeamDrawingSetting));
        }

        private void Delete()
        {
            BeamDrawingSettings.Remove(BeamDrawingSetting);
            BeamDrawingSetting = BeamDrawingSettings.FirstOrDefault();
            OnPropertyChanged(nameof(BeamDrawingSettings));
            OnPropertyChanged(nameof(BeamDrawingSetting));
        }

        private void Export(object x)
        {
            if (x is BeamDrawingView window)
            {
                var d = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    Title = "Where do you want to save the file?",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (d.ShowDialog() == true)
                {
                    var path = d.FileName;
                    var data = window.lv.SelectedItems.Cast<BeamDrawingSetting>().Select(y => new BeamDrawingSettingJson(y)).ToList();
                    JsonUtils.SaveSettingToFile(data, path);
                }
            }
        }

        private void Load()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse Setting Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "json",
                Filter = "json files (*.json)|*.json",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                var path = openFileDialog1.FileName;
                if (File.Exists(path) == false)
                {
                    "BEAMDRAWINGSETTINGVIEWMODEL_MESSAGE2".NotificationError(this);
                }

                var data = JsonUtils.GetSettingFromFile<List<BeamDrawingSettingJson>>(path)?.Where(x => x.IsValid()).ToList();

                if (data != null)
                {
                    SetupDefaultSetting(data);
                }
            }
            //OnPropertyChanged(nameof(BeamDrawingSettings));
        }

        public void ActiveSymbols()
        {
            foreach (var symbol in IndependentTagSymbols)
            {
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
            foreach (var symbol in BreakLineSymbols)
            {
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
            foreach (var symbol in TitleBlocks)
            {
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }
            }

            if (beamDrawingSetting.BeamDetailSetting.DauMocThep != null)
            {
                if (!beamDrawingSetting.BeamDetailSetting.DauMocThep.IsActive)
                {
                    beamDrawingSetting.BeamDetailSetting.DauMocThep.Activate();
                }
            }
        }
    }
}