using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.RebarShop.Model
{
   public class BeamRebarShopModel
   {
      public List<CutRebarModel> TopRebars { get; set; } = new List<CutRebarModel>();
      public List<CutRebarModel> BotRebars { get; set; } = new List<CutRebarModel>();

      public List<BeamShopGeometryModel> BeamShopGeometryModels { get; set; }
      public List<FamilyInstance> Beams { get; set; }
      public View View { get; set; }
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


      public XYZ Direction { get; set; }
      public XYZ Normal { get; set; }


      public BeamRebarShopModel(List<FamilyInstance> beams, BeamModel beamModel, View view = null)
      {
         Direction = beamModel.Direction;
         Normal = beamModel.SpanModels[0].Normal;
         BeamShopGeometryModels = new List<BeamShopGeometryModel>();
         GetInfo(beams, view);
      }

      public void GetInfo(List<FamilyInstance> beams, View view = null)
      {
         if (view != null)
         {
            View = view;
         }

         foreach (var familyInstance in beams)
         {
            var model = new BeamShopGeometryModel(familyInstance);
            BeamShopGeometryModels.Add(model);
            var allBarInBeams = RebarHostData.GetRebarHostData(familyInstance).GetRebarsInHost().ToList();
            Stirrups.AddRange(FindStirrups(allBarInBeams));

            var bRebars = FindBottomRebars(allBarInBeams, model);
            var tBars = FindTopRebars(allBarInBeams, model);
            BRebars.AddRange(bRebars);

            TRebars.AddRange(tBars);

            B1Rebars.AddRange(FindBottom1(bRebars));
            B2Rebars.AddRange(FindBottom2(bRebars, B1Rebars));
            B3Rebars.AddRange(FindBottom3(bRebars, B2Rebars));

            T1Rebars.AddRange(FindTop1(tBars));
            T2Rebars.AddRange(FindTop2(tBars, T1Rebars));
            T3Rebars.AddRange(FindTop3(tBars, T2Rebars));
            MidBars.AddRange(FindMidRebars(allBarInBeams, model));
         }

         TopRebars = GetGroupCutRebarModels(T1Rebars);
         BotRebars = GetGroupCutRebarModels(B1Rebars);
      }


      List<CutRebarModel> GetGroupCutRebarModels(List<Rebar> rebarList)
      {
         var listCutRebarModels = new List<CutRebarModel>();

         var rebars = rebarList.Select(x => new CutRebarModel(x)).ToList();

         var group = rebars.GroupBy(x => new
         {
            RebarNumber = x.RebarNumber,
            MidPoint = x.MidPointString
         });

         foreach (var rebarGroup in group)
         {
            //Tách ra thành cây chẵn cây lẻ
            var cutRebars = new List<CutRebarModel>();

            foreach (var cutRebarModel in rebarGroup)
            {
               for (int i = 0; i < cutRebarModel.Rebar.Quantity; i++)
               {
                  var tf = cutRebarModel.Rebar.GetRebarPositionTransform(i);

                  var cutRebar = new CutRebarModel(cutRebarModel.Rebar, tf);

                  cutRebars.Add(cutRebar);

               }
            }

            cutRebars = cutRebars.OrderBy(x => x.Mid.DotProduct(Normal)).ToList();

            if (cutRebars.Count > 2)
            {
               // nhom thep chan

               var rebarChans = cutRebars.Where((x, index) => index % 2 == 0).ToList();
               var rebarChan = rebarChans.First().Clone();

               rebarChan.AllRebarPlanes =
                  rebarChans.Select(x => BPlane.CreateByNormalAndOrigin(Normal, x.Mid)).ToList();

               // nhom thep le

               var rebarLes = cutRebars.Where((x, index) => index % 2 != 0).ToList();


               var rebarLe = rebarLes.First().Clone();

               rebarLe.AllRebarPlanes =
                  rebarLes.Select(x => BPlane.CreateByNormalAndOrigin(Normal, x.Mid)).ToList();

               listCutRebarModels.Add(rebarLe);
               listCutRebarModels.Add(rebarChan);

            }
            else
            {
               var rebarLe = cutRebars.First().Clone();

               rebarLe.AllRebarPlanes =
                  cutRebars.Select(x => BPlane.CreateByNormalAndOrigin(Normal, x.Mid)).ToList();

               listCutRebarModels.Add(rebarLe);
            }
         }

         return listCutRebarModels;
      }

      #region Funtions to find location of rebars

      public static List<Rebar> FindStirrups(List<Rebar> allRebars)
      {
         var stirrups = new List<Rebar>();
         foreach (var rebar in allRebars)
         {
            if (rebar.IsStirrupOrTie())
            {
               //Ignore midbar #06A
               var shape = rebar.GetRebarShape();
               if (shape.Name != "#6A" || (rebar.TotalLength / rebar.Quantity) > DoubleUtils.MmToFoot(400))
               {
                  stirrups.Add(rebar);
               }
            }
         }
         return stirrups;
      }

      public List<Rebar> FindBottomRebars(List<Rebar> allRebars, BeamShopGeometryModel model)
      {
         var rebars = new List<Rebar>();

         var bbHeight = model.ZTop - model.ZBot;
         foreach (var rebar in allRebars)
         {
            if (rebar.IsStandardRebar())
            {
               var centerPoint = rebar.RebarCenterPoint();
               if (centerPoint.Z < model.ZBot + bbHeight / 2.5)
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

      public List<Rebar> FindTopRebars(List<Rebar> allRebars, BeamShopGeometryModel model)
      {
         var rebars = new List<Rebar>();

         var bbHeight = model.ZTop - model.ZBot;
         foreach (var rebar in allRebars)
         {
            if (rebar.IsStandardRebar())
            {
               var centerPoint = rebar.RebarCenterPoint();
               if (centerPoint.Z > model.ZTop - bbHeight / 2.5)
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
               if (num < DoubleUtils.MmToFoot(60) && !rebar.IsContainRebar(b2Rebars) && !rebar.IsContainRebar(T2Rebars) && !rebar.IsContainRebar(T1Rebars))
               {
                  rebars.Add(rebar);
               }
            }
         }
         return rebars;
      }

      public List<Rebar> FindMidRebars(List<Rebar> allRebars, BeamShopGeometryModel model)
      {
         var view3D = ViewUtils.GetA3DView();
         var rebars = new List<Rebar>();
         var bbHeight = model.ZTop - model.ZBot;
         foreach (var rebar in allRebars)
         {
            var styleParam = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_STYLE);
            var centerPoint = rebar.RebarCenterPoint();
            if (centerPoint.Z.IsEqual(model.ZTop * 0.5 + model.ZBot, 50.MmToFoot()))
            {
               if (!styleParam.AsValueString().Contains("Tie"))
               {
                  rebars.Add(rebar);
               }
               else
               {
                  if (rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH).AsDouble() < DoubleUtils.MmToFoot(400))
                  {
                     rebars.Add(rebar);
                  }
               }
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