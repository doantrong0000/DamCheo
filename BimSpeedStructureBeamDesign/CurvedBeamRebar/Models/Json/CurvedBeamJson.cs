using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BimSpeedStructureBeamDesign.CurvedBeamRebar.Models.Json
{
    public class CurvedBeamJson
    {
        public double MainTop { get; set; }
        public double MainBot { get; set; }
        public int MainBotNumber { get; set; }
        public int MainTopNumber { get; set; }
        public double Layer2Top { get; set; }
        public double Layer2Bot { get; set; }
        public int Layer2TopNumber { get; set; }
        public int Layer2BotNumber { get; set; }
        public double HookXBot { get; set; }
        public double HookYBot { get; set; }
        public double HookXTop { get; set; }
        public double HookYTop { get; set; }
        public double StirrupMidSpacing { get; set; }
        public double StirrupEndSpacing { get; set; }
        public double Stirrup { get; set; }
        public double Cover { get; set; }
       
    }
}
