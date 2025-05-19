using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.ViewModel;

public class CrackedBarViewModel
{
   public Rebar RbMain { get; set; }

   public LengthOrDiameterCracked LengthOrDiameterA { get; set; }

   public LengthOrDiameterCracked LengthOrDiameterB { get; set; }

   public CrackedBarViewModel()
   {

   }

}