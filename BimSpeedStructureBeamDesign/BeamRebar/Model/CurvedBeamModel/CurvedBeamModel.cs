using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.CurvedBeamModel
{
    public class CurvedBeamModel
    {
        public XYZ Direction { get; set; }

        /// <summary>
        /// Điểm gốc của dầm tính ở phía mặt top
        /// </summary>
        public XYZ Origin { get; set; }

        public System.Windows.Point OriginUi { get; set; }
        public XYZ Last { get; set; }
        public double ZTop { get; set; }
        public List<SpanModel> SpanModels { get; set; } = new List<SpanModel>();
        public List<SupportModel> SupportModels { get; set; } = new List<SupportModel>();
        public List<CurvedBeamGeometry> BeamGeometries { get; private set; } = new List<CurvedBeamGeometry>();
        public BeamUiModel BeamUiModel { get; set; }
        public List<FamilyInstance> BeamAsSupports { get; set; }
        public List<FamilyInstance> SecondaryBeams { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }

        public CurvedBeamModel(List<FamilyInstance> beams, List<FamilyInstance> beamAsSupports, List<FamilyInstance> secondaryBeams)
        {

        }

    }
}