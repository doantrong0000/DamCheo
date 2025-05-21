using Autodesk.Revit.DB;
using BimSpeedUtils;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.CurvedBeamModel
{
    public class CurvedBeamGeometry
    {
        private Solid _solid;
        private Solid _solid2;
        public FamilyInstance Beam { get; set; }

        public List<PlanarFace> PlanarFaceLeftRight { get; set; } = new List<PlanarFace>();

        public List<Edge> EdgeVers { get; set; } = new List<Edge>();
        public List<Edge> EdgeHozs { get; set; } = new List<Edge>();

        public List<PlanarFace> FaceCheo { get; set; } = new List<PlanarFace>();

        /// <summary>
        /// Line at center top of beam
        /// </summary>
        public Curve BeamLine { get; set; }

        public XYZ MidPoint { get; set; }
        public Transform Transform { get; set; } = Transform.Identity;
        public Transform Transform1 { get; set; } = Transform.Identity;

        public Solid Solid
        {
            get
            {
                try
                {
                    var b = SolidUtils.Clone(_solid);
                    return b;
                }
                catch (Exception)
                {
                    var b = SolidUtils.Clone(_solid2);
                    return b;
                }
            }
            set
            {
                _solid = value;
                if (value != null)
                {
                    _solid2 = SolidUtils.Clone(value);
                }

            }
        }

        public Solid OriginalSolidTransformed { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }
        public string Mark { get; set; }
        public bool IsColumn { get; set; } = false;


        public RebarAtPositionOfSpan RebarAtPositionOfSpanStart { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanMid { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanEnd { get; set; }
        public RebarQuantityDiameter StirrupEnd { get; set; }
        public RebarQuantityDiameter StirrupMid { get; set; }

        
    }

}
