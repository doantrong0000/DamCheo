using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
   public class ElementGeometry
   {
      public Element Element { get; set; }
      public List<Solid> Solids { get; set; }
      public Transform Transform { get; set; } = Transform.Identity;
      public List<PlanarFace> PlanarFaces { get; set; } = new List<PlanarFace>();
      public PlanarFace TopPlanarFace { get; set; }
      public PlanarFace BotPlanarFace { get; set; }
      public PlanarFace LeftPlanarFace { get; set; }
      public PlanarFace RightPlanarFace { get; set; }
      private Autodesk.Revit.DB.View view;
      public BoundingBoxXYZ BoundingBoxXyz { get; set; }

      public double ZMax { get; set; }
      public double ZMin { get; set; }
      public double Width { get; set; }

      public ElementGeometry(Element ele, Autodesk.Revit.DB.View view)
      {
         this.Element = ele;
         this.view = view;
         BoundingBoxXyz = Element.get_BoundingBox(view);
         GetData();
      }

      private void GetData()
      {
         Solids = Element.GetAllSolids();
         if (Element is FamilyInstance fi)
         {
            var faces = Element.FacesBySymbol();
            if (faces.Count > 0)
            {
               Transform = fi.GetTransform();
            }
         }
         foreach (var solid in Solids)
         {
            foreach (var solidFace in solid.Faces)
            {
               var planarF = solidFace as PlanarFace;
               if (planarF == null) continue;
               if (planarF.Reference == null) continue;
               PlanarFaces.Add(planarF);
            }
         }

         var horizontalFaces = PlanarFaces.Where(x => x.FaceNormal.IsParallel(XYZ.BasisZ)).OrderBy(x => x.Origin.Z).ToList();
         TopPlanarFace = horizontalFaces.LastOrDefault();
         BotPlanarFace = horizontalFaces.FirstOrDefault();
         LeftPlanarFace = (view.RightDirection).FirstFace(PlanarFaces, Transform, true);
         RightPlanarFace = (-view.RightDirection).FirstFace(PlanarFaces, Transform, true);
         ZMax = Element.get_Parameter(BuiltInParameter.STRUCTURAL_ELEVATION_AT_TOP).AsDouble();
         ZMin = Element.get_Parameter(BuiltInParameter.STRUCTURAL_ELEVATION_AT_BOTTOM).AsDouble();

         if (TopPlanarFace != null && TopPlanarFace.Origin.Z > ZMax)
         {

            ZMax = TopPlanarFace.Origin.Z;
         }

         if (BotPlanarFace != null && BotPlanarFace.Origin.Z < ZMin)
         {
            ZMin = BotPlanarFace.Origin.Z;
         }


         var z = AC.Document.ActiveProjectLocation.GetTotalTransform().Inverse.OfPoint(new XYZ(0, 0, ZMin)).Z;


         if (LeftPlanarFace != null && RightPlanarFace != null)
         {
            Width = LeftPlanarFace.ToBPlane().SignedDistanceTo(RightPlanarFace.Origin);
         }
         else
         {
            Width = 200.MmToFoot();
         }
      }
   }
}