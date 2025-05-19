using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BimSpeedLicense.LicenseManager;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamDrawing.Others;
using BimSpeedStructureBeamDesign.BeamDrawing.View;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing
{
    [Transaction(TransactionMode.Manual)]
    public class BeamDrawingCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name); ;
            SharedData.Instance = new SharedData();
            var viewModel = new BeamDrawingSettingViewModel();
            var settingView = new BeamDrawingView() { DataContext = viewModel };

            //try
            {
                if (settingView.ShowDialog() == true)
                {
                    //var beams = new List<FamilyInstance>();

                    List<FamilyInstance> beams;
                    List<Element> supports = new List<Element>();
                    try
                    {
                        beams = AC.Selection.PickObjects(ObjectType.Element, new BimSpeedUtils.BeamSelectionFilter(), "Beams...")
                            .Select(x => x.ToElement())
                            .Cast<FamilyInstance>()
                            .ToList();
                    }
                    catch
                    {
                        "BEAMDRAWINGCMD_MESSAGE1".NotificationError(this);

                        return Result.Cancelled;
                    }

                    if (beams.Count < 1)
                    {
                        "BEAMDRAWINGCMD_MESSAGE2".NotificationError(this);
                        return Result.Cancelled;
                    }

                    if (BeamRebarCommonService.CheckBeamsValidToPutRebars(beams, out var errorMessage) == false)
                    {
                        MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return Result.Cancelled;
                    }

                    #region Create View 3D

                    View3D view3D = null;
                    if (viewModel.IsCreate3DView)
                    {
                        using var tx3d = new Transaction(AC.Document, "Create 3D beam view");
                        tx3d.Start();

                        viewModel.ActiveSymbols();

                        var beamExtension3D = new BeamExtension(beams, supports) { BeamDrawingSetting = viewModel.BeamDrawingSetting };
                        view3D = beamExtension3D.Create3DView();

                        tx3d.Commit();
                    }

                    #endregion

                    if (viewModel.IsMultiCrossSection)
                    {
                        using var tx = new Transaction(AC.Document, "Beam drawing");
                        tx.Start();
                        viewModel.ActiveSymbols();
                        if (viewModel.PickSupportBeam)
                        {
                            supports = AC.Selection
                                .PickObjects(ObjectType.Element, new BimSpeedUtils.BeamSelectionFilter(), "Pick beam as a support")
                                .Select(x => x.ToElement()).ToList();
                     
                        }
                        var viewSections = new List<ViewSection>();
                        foreach (var beam in beams)
                        {
                            var beamExtension = new BeamExtension(new List<FamilyInstance>() { beam },supports) { BeamDrawingSetting = viewModel.BeamDrawingSetting, OnlyMidSection = true };

                            beamExtension.Run();

                            viewModel.BeamDrawingSetting.BeamSectionSetting.BreakLineSymbol =
                               viewModel.BeamDrawingSetting.BeamDetailSetting.BreakLineSymbol;

                            //Beam Section
                            var sections = new List<ViewSection>();
                            foreach (var beamExtensionBeamSegment in beamExtension.BeamSegments)
                            {
                                foreach (var section in beamExtensionBeamSegment.BeamSections.Where(x => x.ViewSection.IsValidObject))
                                {
                                    sections.Add(section.ViewSection);
                                    var sectionHandle = new BeamSectionService(section.ViewSection, viewModel.BeamDrawingSetting.BeamSectionSetting, section.FamilyInstance);
                                    sectionHandle.Run();
                                }
                            }

                            viewSections.AddRange(sections);
                        }

                        //AddBot Views To Sheet
                        var drawingService = new Others.BeamDrawingService(viewModel.BeamDrawingSetting) { ViewSections = viewSections, DetailView = null , View3D = view3D };

                        drawingService.Run();

                        tx.Commit();
                        AC.UiDoc.ActiveView = drawingService.ViewSheet;
                    }
                    else
                    {
                        if (viewModel.PickSupportBeam)
                        {
                            supports = AC.Selection
                                .PickObjects(ObjectType.Element, new BimSpeedUtils.BeamSelectionFilter(), "Pick beam as a support")
                                .Select(x => x.ToElement()).ToList();

                        }
                        var beamExtension = new BeamExtension(beams,supports) { BeamDrawingSetting = viewModel.BeamDrawingSetting };

                        using var tx = new Transaction(AC.Document, "Beam drawing");
                        tx.Start();
                        viewModel.ActiveSymbols();

                        beamExtension.Run();
                        //Beam Detail
                        var service = new BeamDetailService(beamExtension.BeamDetail, viewModel, beamExtension.BeamGeometries, supports);
                        service.Run();
                        viewModel.BeamDrawingSetting.BeamSectionSetting.BreakLineSymbol =
                           viewModel.BeamDrawingSetting.BeamDetailSetting.BreakLineSymbol;

                        //Beam Section
                        var sections = new List<ViewSection>();
                        foreach (var beamExtensionBeamSegment in beamExtension.BeamSegments)
                        {
                            foreach (var section in beamExtensionBeamSegment.BeamSections.Where(x => x.ViewSection.IsValidObject))
                            {
                                sections.Add(section.ViewSection);

                                //var name = "";

                                //name = name + "MM1";

                                //section.ViewSection.Name = name;

                                var sectionHandle = new BeamSectionService(section.ViewSection, viewModel.BeamDrawingSetting.BeamSectionSetting, section.FamilyInstance);
                                sectionHandle.Run();
                            }
                        }

                        //AddBot Views To Sheet
                        var drawingService = new Others.BeamDrawingService(viewModel.BeamDrawingSetting) { ViewSections = sections, DetailView = beamExtension.BeamDetail , View3D = view3D };
                        drawingService.Run();

                        tx.Commit();
                        AC.UiDoc.ActiveView = drawingService.ViewSheet;
                    }
                }
            }
            //catch (Exception e)
            //{
            //   MessageBox.Show(e.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //   return Result.Succeeded;
            //}

            return Result.Succeeded;
            //return LicenseCheck.CheckCommandCanExecute(GetType().Name); ;
        }

        List<ViewSection> ProcessBeamsWithSections(List<FamilyInstance> beams, BeamDrawingSettingViewModel viewModel, bool isMultiSection)
        {
            var viewSections = new List<ViewSection>();

            foreach (var beam in beams)
            {
                var beamExtension = new BeamExtension(new List<FamilyInstance>() { beam })
                {
                    BeamDrawingSetting = viewModel.BeamDrawingSetting,
                    OnlyMidSection = isMultiSection
                };

                beamExtension.Run();

                viewModel.BeamDrawingSetting.BeamSectionSetting.BreakLineSymbol =
                    viewModel.BeamDrawingSetting.BeamDetailSetting.BreakLineSymbol;

                var sections = ProcessBeamSegments(beamExtension, viewModel);
                viewSections.AddRange(sections);
            }

            return viewSections;
        }

        List<ViewSection> ProcessBeamSegments(BeamExtension beamExtension, BeamDrawingSettingViewModel viewModel)
        {
            var sections = new List<ViewSection>();

            foreach (var beamSegment in beamExtension.BeamSegments)
            {
                foreach (var section in beamSegment.BeamSections.Where(x => x.ViewSection.IsValidObject))
                {
                    sections.Add(section.ViewSection);

                    var sectionHandle = new BeamSectionService(section.ViewSection,
                        viewModel.BeamDrawingSetting.BeamSectionSetting,
                        section.FamilyInstance);
                    sectionHandle.Run();
                }
            }

            return sections;
        }
    }
}