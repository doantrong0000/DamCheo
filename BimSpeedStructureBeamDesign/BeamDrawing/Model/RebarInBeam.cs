using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;
using BimSpeedUtils.ComparerUtils;
using System.Windows.Shapes;

namespace BimSpeedStructureBeamDesign.BeamDrawing.Model
{
    public class RebarsInBeam
    {
        public ElementGeometry Beam { get; set; }
        private FamilyInstance beam;
        public Autodesk.Revit.DB.View View { get; set; }
        public List<Rebar> AllRebars { get; set; } = new();
        public List<Rebar> TRebars { get; set; } = new();
        public List<Rebar> T1Rebars { get; set; } = new();
        public List<Rebar> T2Rebars { get; set; } = new();
        public List<Rebar> T3Rebars { get; set; } = new();
        public List<Rebar> BRebars { get; set; } = new();
        public List<Rebar> B1Rebars { get; set; } = new();
        public List<Rebar> B2Rebars { get; set; } = new();
        public List<Rebar> B3Rebars { get; set; } = new();
        public List<Rebar> Stirrups { get; set; } = new();
        public List<Rebar> MidBars { get; set; } = new();

        private static RebarsInBeam _instance;

        private RebarsInBeam()
        {
        }

        public static RebarsInBeam Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RebarsInBeam();
                }
                return _instance;
            }
        }

        public void GetInfo(ElementGeometry beam, Autodesk.Revit.DB.View view = null)
        {
            this.beam = beam.Element as FamilyInstance;
            Beam = beam;
            if (view != null)
            {
                this.View = view;
            }
            var rebarsInHost = RebarHostData.GetRebarHostData(this.beam).GetRebarsInHost().ToList();
            if (view != null)
            {
                var rebarsInView = new FilteredElementCollector(AC.Document, view.Id).OfClass(typeof(Rebar))
                    .Cast<Rebar>().ToList();
                AllRebars = rebarsInView.Intersect(rebarsInView, new RebarComparer()).ToList();
            }
            else
            {
                AllRebars = rebarsInHost;
            }

            Stirrups = FindStirrups(AllRebars);
            BRebars = FindBottomRebars(AllRebars);
            B1Rebars = FindBottom1(BRebars);
            B2Rebars = FindBottom2(BRebars, B1Rebars);
            B3Rebars = FindBottom3(BRebars, B2Rebars);
            TRebars = FindTopRebars(AllRebars);
            T1Rebars = FindTop1(TRebars);
            T2Rebars = FindTop2(TRebars, T1Rebars);
            T3Rebars = FindTop3(TRebars, T2Rebars);
            MidBars = FindMidRebars(AllRebars);
        }

        #region Funtions to find location of rebars

        public static List<Rebar> FindStirrups(List<Rebar> allRebars)
        {
            var stirrups = new List<Rebar>();
            foreach (var rebar in allRebars)
            {

                var shape = rebar.GetRebarShape();

                if (rebar.IsStirrupOrTie())
                {
                    //Ignore midbar #06A
                    if (shape.Name != "#6A")
                    {
                        stirrups.Add(rebar);
                    }
                }
                else
                {
                    if (shape.Name == "M_01")
                    {
                        stirrups.Add(rebar);
                    }
                }
            }
            return stirrups;
        }

        public List<Rebar> FindBottomRebars(List<Rebar> allRebars)
        {
            var rebars = new List<Rebar>();

            var bbHeight = Beam.ZMax - Beam.ZMin;
            foreach (var rebar in allRebars)
            {
                if (rebar.IsStandardRebar())
                {
                    var centerPoint = rebar.RebarCenterPoint();
                    if (centerPoint.Z < Beam.ZMin + bbHeight / 2.5)
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindBottom1(List<Rebar> botRebars)
        {
            var rebars = new List<Rebar>();
            var botRebar = botRebars.OrderBy(x => x.RebarCenterPoint().Z).FirstOrDefault();
            if (botRebar != null)
            {
                var botZ = botRebar.RebarCenterPoint().Z;
                foreach (var rebar in botRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - botZ);
                    if (num < DoubleUtils.MmToFoot(25))
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindBottom2(List<Rebar> botRebars, List<Rebar> bot1Rebars = null)
        {
            var rebars = new List<Rebar>();
            var botRebar = botRebars.OrderBy(x => x.RebarCenterPoint().Z).FirstOrDefault();
            var b1Rebars = bot1Rebars ?? FindBottom1(botRebars);
            if (botRebar != null)
            {
                var botZ = botRebar.RebarCenterPoint().Z;
                foreach (var rebar in botRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - botZ);
                    if (num < 60.MmToFoot() && !rebar.IsContainRebar(b1Rebars))
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindBottom3(List<Rebar> botRebars, List<Rebar> bot2Rebars = null)
        {
            var rebars = new List<Rebar>();
            var b2Rebars = bot2Rebars ?? FindBottom2(botRebars);

            var botRebar = b2Rebars.OrderBy(x => x.RebarCenterPoint().Z).FirstOrDefault();
            if (botRebar != null)
            {
                var botZ = botRebar.RebarCenterPoint().Z;
                foreach (var rebar in botRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - botZ);
                    if (num < DoubleUtils.MmToFoot(60) && !rebar.IsContainRebar(b2Rebars) && !rebar.IsContainRebar(B1Rebars))
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindTopRebars(List<Rebar> allRebars)
        {
            var rebars = new List<Rebar>();

            var bbHeight = Beam.ZMax - Beam.ZMin;
            foreach (var rebar in allRebars)
            {
                if (rebar.IsStandardRebar())
                {
                    var centerPoint = rebar.RebarCenterPoint();
                    if (centerPoint.Z > Beam.ZMax - bbHeight / 2.5)
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindTop1(List<Rebar> topRebars)
        {
            var rebars = new List<Rebar>();
            var topRebar = topRebars.OrderByDescending(x => x.RebarCenterPoint().Z).FirstOrDefault();
            if (topRebar != null)
            {
                var botZ = topRebar.RebarCenterPoint().Z;
                foreach (var rebar in topRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - botZ);
                    if (num < DoubleUtils.MmToFoot(25))
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindTop2(List<Rebar> topRebars, List<Rebar> top1Rebars = null)
        {
            var rebars = new List<Rebar>();
            var topRebar = topRebars.OrderByDescending(x => x.RebarCenterPoint().Z).FirstOrDefault();
            var b1Rebars = top1Rebars ?? FindBottom1(topRebars);
            if (topRebar != null)
            {
                var topZ = topRebar.RebarCenterPoint().Z;
                foreach (var rebar in topRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - topZ);
                    if (num < 60.MmToFoot() && !rebar.IsContainRebar(b1Rebars))
                    {
                        rebars.Add(rebar);
                    }
                }
            }
            return rebars;
        }

        public List<Rebar> FindTop3(List<Rebar> topRebars, List<Rebar> top2Rebars = null)
        {
            var rebars = new List<Rebar>();
            var b2Rebars = top2Rebars ?? FindBottom2(topRebars);
            var topRebar = b2Rebars.OrderByDescending(x => x.RebarCenterPoint().Z).FirstOrDefault();
            if (topRebar != null)
            {
                var botZ = topRebar.RebarCenterPoint().Z;
                foreach (var rebar in topRebars)
                {
                    var z = rebar.RebarCenterPoint().Z;
                    var num = Math.Abs(z - botZ);
                    if (num < 60.MmToFoot() && !rebar.IsContainRebar(b2Rebars) && !rebar.IsContainRebar(T2Rebars) && !rebar.IsContainRebar(T1Rebars))
                    {
                        rebars.Add(rebar);
                    }
                }
            }

            return rebars;
        }

        public List<Rebar> FindMidRebars(List<Rebar> allRebars)
        {
            var rebars = new List<Rebar>();
            foreach (var rebar in allRebars)
            {
                if (rebar.IsStandardRebar()

                    && T1Rebars.All(x => x.Id != rebar.Id)
                    && T2Rebars.All(x => x.Id != rebar.Id)
                    && T3Rebars.All(x => x.Id != rebar.Id)
                    && B1Rebars.All(x => x.Id != rebar.Id)
                    && B2Rebars.All(x => x.Id != rebar.Id)
                    && B3Rebars.All(x => x.Id != rebar.Id)
                    )
                {
                    rebars.Add(rebar);
                }
            }
            return rebars;
        }

        #endregion Funtions to find location of rebars

        public void SetBarLocation(string param)
        {
            T1Rebars.ForEach(x => x.SetParameterValueByName(param, "T1"));
            T2Rebars.ForEach(x => x.SetParameterValueByName(param, "T2"));
            T3Rebars.ForEach(x => x.SetParameterValueByName(param, "T3"));
            B1Rebars.ForEach(x => x.SetParameterValueByName(param, "B1"));
            B2Rebars.ForEach(x => x.SetParameterValueByName(param, "B2"));
            B3Rebars.ForEach(x => x.SetParameterValueByName(param, "B3"));
            MidBars.ForEach(x => x.SetParameterValueByName(param, "Mid"));
        }

        public List<Rebar> GetRebars(RebarInBeamLocation location)
        {
            List<Rebar> rebars = new List<Rebar>();
            switch (location)
            {
                case RebarInBeamLocation.T1:
                    rebars = T1Rebars;
                    break;

                case RebarInBeamLocation.T2:
                    rebars = T2Rebars;
                    break;

                case RebarInBeamLocation.T3:
                    rebars = T3Rebars;
                    break;

                case RebarInBeamLocation.B1:
                    rebars = B1Rebars;
                    break;

                case RebarInBeamLocation.B2:
                    rebars = B2Rebars;
                    break;

                case RebarInBeamLocation.B3:
                    rebars = B3Rebars;
                    break;

                case RebarInBeamLocation.Mid:
                    rebars = MidBars;
                    break;
            }

            return rebars;
        }

        public enum RebarInBeamLocation
        {
            T1,
            T2,
            T3,
            B1,
            B2,
            B3,
            Mid,
            Stirrup
        }
    }
}