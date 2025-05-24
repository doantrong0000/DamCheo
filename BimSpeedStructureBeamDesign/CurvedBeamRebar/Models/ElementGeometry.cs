using Autodesk.Revit.DB;
using BimSpeedUtils;

namespace BimSpeedRebar.CurvedBeamRebar.Models
{
    public class ElementGeometry
    {
        public Element Element { get; set; }
        public Solid Solid { get; set; }
        public Solid OriginalSolid { get; set; }

        public ElementGeometry(Element ele)
        {
            this.Element = ele;
            Solid = ele.GetSingleSolid();
        }
    }
}