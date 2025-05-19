using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.Utils
{
  public static class DocumentUtils
  {
    public static void Transaction( this Document document, string transactionName, Action action  )
    {
      using var transaction = new Transaction( document) ;
      transaction.Start( transactionName ) ;

      action.Invoke() ;

      transaction.Commit() ;
    }

    public static DimensionType GetDimensionType( this Document document, string name )
    {
      return new FilteredElementCollector( document )
             .OfClass( typeof( DimensionType ) )
             .WhereElementIsElementType()
             .ToElements()
             .Cast<DimensionType>()
             .Where( d => d.StyleType == DimensionStyleType.Linear )
             .FirstOrDefault( x => x.Name == name ) ;
    }

    public static ViewFamilyType GetViewFamilyType( this Document document, string name )
    {
      var viewFamilyTypes = new FilteredElementCollector( document )
                            .OfClass( typeof( ViewFamilyType ) )
                            .Cast<ViewFamilyType>() ;

      return viewFamilyTypes.FirstOrDefault( x => x.Name == name ) ; ;
    }

    public static List<string> GetAllViewTypes( this Document document )
    {
      return new FilteredElementCollector( document )
             .OfClass( typeof( ViewFamilyType ) )
             .Cast<ViewFamilyType>()
             .Where( v => v.ViewFamily is ViewFamily.StructuralPlan or ViewFamily.FloorPlan or ViewFamily.CeilingPlan )
             .Select( x => x.Name )
             .ToList() ;
    }

    public static Autodesk.Revit.DB.View GetViewTemplate( this Document document, string name )
    {
      return new FilteredElementCollector( document )
             .OfClass( typeof( Autodesk.Revit.DB.View ) )
             .WhereElementIsNotElementType()
             .Cast<Autodesk.Revit.DB.View>()
             .Where( v => v.IsTemplate )
             .FirstOrDefault( v => v.Name == name ) ;
    }

    public static Element GetScopeBox( this Document document, string name)
    {
      return new FilteredElementCollector( document )
          .OfCategory( BuiltInCategory.OST_VolumeOfInterest )
          .WhereElementIsNotElementType()
          .FirstOrDefault( e => e.Name == name ) ;
    }

    public static FamilySymbol GetTitleBlock( this Document document, string name )
    {
      return new FilteredElementCollector( document )
             .OfCategory( BuiltInCategory.OST_TitleBlocks )
             .WhereElementIsElementType()
             .Cast<FamilySymbol>()
             .FirstOrDefault( x => x.Name == name ) ;
    }

    public static string MakeUniqueViewPlanName( this Document document, string name )
    {
      int count = new FilteredElementCollector( document )
                  .OfClass( typeof( ViewPlan ) )
                  .Cast<ViewPlan>()
                  .Where( v => !v.IsTemplate )
                  .Count( v => v.Name.Contains( name ) ) ;

      return count > 0 ? name + $" ({count})" : name ;
    }

    public static (string, int) MakeUniqueViewSheetNumber( this Document document, string prefix, int number)
    {
      string name = prefix + number.ToString( "00" ) ;
      while ( document.IsViewSheetNumberExisted(name) ) {
        number++ ;
        name = prefix + number.ToString( "00" ) ;
      }
      return ( name, number ) ;
    }

    private static bool IsViewSheetNumberExisted( this Document document, string name )
    {
      var viewSheetNumbers = new FilteredElementCollector( document )
                             .OfClass( typeof( ViewSheet ) )
                             .Cast<ViewSheet>()
                             .Where( v => !v.IsTemplate && !v.Name.Contains("Unnamed") )
                             .Select( v => v.SheetNumber )
                             .Distinct() ;
      return viewSheetNumbers.Contains( name ) ;
    }
  }
}