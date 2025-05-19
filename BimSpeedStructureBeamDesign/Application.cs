using System.IO;
using Autodesk.Revit.UI;
using Serilog.Events;
using BimSpeedStructureBeamDesign.BeamDrawing;
using BimSpeedStructureBeamDesign.BeamPlanDim;
using BimSpeedStructureBeamDesign.BeamRebar;
using BimSpeedStructureBeamDesign.BeamSectionGenerator;
using System.Reflection;
using System.Windows.Media.Imaging;
using BimSpeedStructureBeamDesign.BeamRebarCutShop;
using BimSpeedUtils.BimSpeedToolKit;
using BimSpeedUtils.PanelUtils;
using BimSpeedUtils.RibbonUtils;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign
{
   [UsedImplicitly]
   public class Application : BimSpeedToolkitExternal
   {
      public override void OnStartup()
      {
         CreateRibbon();
         CreateLogger();
         CoppyFamily();
      }

      private void CreateRibbon()
      {
         var pathIcons = "/BimSpeedStructureBeamDesign;component/Resources/Icons/";

         var flag = true;
         var ribbon = Autodesk.Windows.ComponentManager.Ribbon;

         if (ribbon != null)
         {
            if (ribbon.Tabs.Count > 0)
            {
               foreach (var tab in ribbon.Tabs)
               {
                  if (tab.Name == "BimSpeed")
                     flag = false;
               }
            }
         }

         RibbonPanel panel;

         if (flag)
         {
            PanelUtils.AddBimSpeedPanel(Application, "BimSpeed");
            panel = Application.CreatePanel("Beam", "BimSpeed");
         }
         else
         {
            panel = Application.CreateRibbonPanel("BimSpeed", "Beam");
         }

         //Add button
         var buttonBeamDrawing = panel.AddPushButton<BeamDrawingCmd>("Beam Drawing");
         buttonBeamDrawing.SetImage("/BimSpeedStructureBeamDesign;component/Resources/Icons/beamdrawing16.png");
         buttonBeamDrawing.SetLargeImage("/BimSpeedStructureBeamDesign;component/Resources/Icons/beamdrawing.png");

         var beamDetail = new PushButtonData(typeof(BeamDetailCmd).FullName, "Beam Detail", Assembly.GetAssembly(typeof(BeamDetailCmd)).Location, typeof(BeamDetailCmd).FullName);
         beamDetail.Image = new BitmapImage(new Uri(pathIcons + "beamsection16.png", UriKind.RelativeOrAbsolute));
         beamDetail.LargeImage = new BitmapImage(new Uri(pathIcons + "beamsection.png", UriKind.RelativeOrAbsolute));

         var beamSection = new PushButtonData(typeof(BeamSectionCmd).FullName, "Beam Section", Assembly.GetAssembly(typeof(BeamSectionCmd)).Location, typeof(BeamSectionCmd).FullName);
         beamSection.Image = new BitmapImage(new Uri(pathIcons + "beamsection16.png", UriKind.RelativeOrAbsolute));
         beamSection.LargeImage = new BitmapImage(new Uri(pathIcons + "beamsection.png", UriKind.RelativeOrAbsolute));

         panel.AddStackedItems(beamDetail, beamSection);

         var buttonBeamPlanDim = panel.AddPushButton<BeamPlanDimCmd>("Beam Plan Dim");
         buttonBeamPlanDim.SetImage(pathIcons + "sideplandim16.png");
         buttonBeamPlanDim.SetLargeImage(pathIcons + "sideplandim.png");

         var buttonBeamRebar = panel.AddPushButton<BeamRebarCmd>("Beam Rebar");
         buttonBeamRebar.SetImage(pathIcons + "beamrebar16.png");
         buttonBeamRebar.SetLargeImage(pathIcons + "beamrebar.png");


         var buttonCutThep = panel.AddPushButton<BeamRebarCutShopCmd>("Beam Rebar Shop");
         buttonCutThep.SetImage(pathIcons + "beamrebar16.png");
         buttonCutThep.SetLargeImage(pathIcons + "beamrebar.png");

         var buttonBeamRebarSectionGenerator = panel.AddPushButton<BeamSectionGeneratorCmd>("Beam Section Generator");
         buttonBeamRebarSectionGenerator.SetImage(pathIcons + "auto16.png");
         buttonBeamRebarSectionGenerator.SetLargeImage(pathIcons + "auto.png");
      }

      private static void CreateLogger()
      {
         const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

         Log.Logger = new LoggerConfiguration()
             .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
             .MinimumLevel.Debug()
             .CreateLogger();

         AppDomain.CurrentDomain.UnhandledException += (_, args) =>
         {
            var e = (Exception)args.ExceptionObject;
            Log.Fatal(e, "Domain unhandled exception");
         };
      }

      private void CoppyFamily()
      {
         var pathFile2024 = System.IO.Path.Combine(AC.BimSpeedResourcesFolder, "EN", "2024", "BimSpeedTemplate.rte");
         //Check template
         if (!File.Exists(pathFile2024))
         {
            string folderTemplate = Path.Combine(AC.BimSpeedResourcesFolder, "EN", "2024");

            if (!File.Exists(folderTemplate))
               Directory.CreateDirectory(folderTemplate);

            //Get file family
            string filePath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            string directoryPath = Path.GetDirectoryName(filePath);

            File.Copy(Path.Combine(directoryPath, "Resources", "Families", "BimSpeedTemplate.rte"), Path.Combine(folderTemplate, "BimSpeedTemplate.rte"), true);
         }
      }
   }
}