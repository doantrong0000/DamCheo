using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.Utils
{
    public static class GeometryUtils
  {
    public static XYZ MovePoint(this XYZ point, XYZ direction, double length)
    {
      var normal = direction.Normalize() ;
      return point + normal * length ;
    }

    public static Line MoveLine(this Line line, XYZ direction, double length)
    {
      var normal = direction.Normalize() ;
      var startPoint = line.GetEndPoint( 0 ) ;
      var endPoint = line.GetEndPoint( 1 ) ;
      return Line.CreateBound( startPoint.MovePoint( normal, length ), endPoint.MovePoint( direction, length ) ) ;
    }

    public static List<XYZ> IntersectWithCurves( this Curve curve, params Curve[] curves )
    {
      var result = new List<XYZ>() ;
      foreach ( var item in curves ) {
        curve.Intersect( item, out var resultArray ) ;
        if(resultArray is null) continue;
        if ( ! resultArray.IsEmpty ) {
          for ( int i = 0 ; i < resultArray.Size ; i++ ) {
            var res = resultArray.get_Item( i ) ;
            var pt = res.XYZPoint ;
            if(pt is not null) result.Add( pt );
          }
        }
      }

      return result ;
    }
  }
}