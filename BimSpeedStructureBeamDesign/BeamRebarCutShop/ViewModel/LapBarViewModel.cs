using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.ViewModel;

public class LapBarViewModel : ViewModelBase
{
   private LengthOrDiameterLap lengthOrDiameter;

   public LengthOrDiameterLap LengthOrDiameter
   {
      get => lengthOrDiameter;
      set
      {
         lengthOrDiameter = value;
         OnPropertyChanged();
      }
   }

   public LapBarViewModel()
   {
      LengthOrDiameter = new LengthOrDiameterLap();
   }
}