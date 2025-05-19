using Autodesk.Revit.DB ;

namespace BimSpeedStructureBeamDesign.Utils ;

public static class FamilyUtils
{
  public static void ActiveSymbol(IList<FamilySymbol>familySymbols )
  {
    foreach ( var familySymbol in familySymbols ) {
      if(!familySymbol.IsActive) familySymbol.Activate();
    } 
  }
}