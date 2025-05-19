using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.Utils
{
    public static class SolidUtilsCustom
    {
        public static IList<Solid> GetAllSolidFloor(this Element ele, out Transform transform)
        {
            transform = Transform.Identity;
            List<Solid> solidList = new List<Solid>();
            if (ele == null)
                return solidList;

            GeometryElement geometryElement = ele.get_Geometry(new Options());

            List<GeometryObject> geometryObjectList = new List<GeometryObject>();

            foreach (GeometryObject geometryObject1 in geometryElement)
            {
                geometryObjectList.Add(geometryObject1);
                GeometryInstance geometryInstance = geometryObject1 as GeometryInstance;

                if (null != geometryInstance)
                {
                    foreach (GeometryObject geometryObject2 in geometryInstance.GetSymbolGeometry())
                    {
                        var tf = geometryInstance.Transform;
                        transform = tf;

                        Solid solid = geometryObject2 as Solid;
                        if (!(null == solid) && solid.Faces.Size != 0 && solid.Edges.Size != 0)
                        {
                            solidList.Add(solid);
                        }
                    }
                }

                Solid solid1 = geometryObject1 as Solid;
                if (!(null == solid1) && solid1.Faces.Size != 0 && solid1.Edges.Size != 0)
                    solidList.Add(solid1);
            }

            return solidList;
        }
    }
}
