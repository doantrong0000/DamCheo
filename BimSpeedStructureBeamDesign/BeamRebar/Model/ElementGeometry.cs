using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class ElementGeometry
   {
      public Element Element { get; set; }
      public Solid Solid { get; set; }
      public Solid OriginalSolid { get; set; }

      public ElementGeometry(Element ele)
      {
        this.Element = ele;
        // chỉ lấy solid lớn nhất trong trường hợp ele là móng có 2 khối solid
        GeometryElement geometryElement = ele.get_Geometry(new Options());

        Solid largestSolid = null;
        double maxVolume = 0;

        // Lấy tất cả các solid từ GeometryElement
        foreach (GeometryObject geoObj in geometryElement)
        {
            if (geoObj is Solid solid)
            {
                double solidVolume = solid.Volume;
                // Kiểm tra nếu thể tích của solid này lớn hơn thể tích lớn nhất đã tìm thấy
                if (solidVolume > maxVolume)
                {
                    maxVolume = solidVolume;
                    largestSolid = solid; // Cập nhật solid có thể tích lớn nhất
                }
            }
        }

        // Lưu solid có thể tích lớn nhất vào thuộc tính Solid (hoặc một nơi khác nếu cần)
        this.Solid = largestSolid;
      }

        //public ElementGeometry(Element ele)
        //{
        //    this.Element = ele;
        //    Solid = ele.GetSingleSolid();
        //}
    }
}