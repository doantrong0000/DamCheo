using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using BimSpeedStructureBeamDesign.CurvedBeamRebar.ViewModels;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.Models.RebarModels
{
    public class TopBar
    {
        private RebarBarType _barDiameter = 20.GetRebarBarTypeByNumber();
        private int _layer = 1;
        private Curve barCurve;
        private double cover;
        
        public TopBar() { }

    }
}
