using System.IO;
using Autodesk.Revit.UI;
using Serilog.Events;
using System.Reflection;
using System.Windows.Media.Imaging;
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