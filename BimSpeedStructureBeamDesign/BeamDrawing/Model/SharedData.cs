using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.BeamDrawing.ViewModel;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class SharedData
   {
      public static SharedData Instance;
      public List<ViewSheet> ViewSheets { get; set; } = new List<ViewSheet>();
      public BeamDrawingSettingViewModel ViewModel { get; set; }

      public SharedData()
      {
         SharedData.Instance = this;
      }
   }
}