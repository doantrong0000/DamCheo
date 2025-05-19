using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AATEStCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AC.GetInformation(commandData, GetType().Name);

            var dic = new Dictionary<string, List<string>>();
            dic.Add(TemplateKeyDefine.VIEW_TEMPLATE_BEAM_ELEVATION, new List<string>()
            {
                "BS-23-Elevation-Detail-Beam PX",
            });


            JsonUtils.SaveSettingToFile(dic, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\dic.json");

            return Result.Succeeded;
        }
    }

    public static class TemplateKeyDefine
    {
        public static string VIEW_TEMPLATE_BEAM_ELEVATION = nameof(VIEW_TEMPLATE_BEAM_ELEVATION);
    }
}