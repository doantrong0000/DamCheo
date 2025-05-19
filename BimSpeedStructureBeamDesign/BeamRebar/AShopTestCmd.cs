using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.RebarShape2D.Model;
using BimSpeedStructureBeamDesign.RebarShop.Model;
using BimSpeedUtils;
using BeamSelectionFilter = BimSpeedStructureBeamDesign.BeamDrawing.Others.BeamSelectionFilter;

namespace BimSpeedStructureBeamDesign.BeamRebar
{
   [Transaction(TransactionMode.Manual)]
   [Regeneration(RegenerationOption.Manual)]
   public class AShopTestCmd : IExternalCommand
   {
      public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
      {
         AC.GetInformation(commandData, GetType().Name);
         List<RebarShopModel> list;

         List<FamilyInstance> beams;

         try
         {
            beams = AC.Selection.PickObjects(ObjectType.Element, new BeamSelectionFilter(), "Beam...")
                .Select(x => x.ToElement()).Cast<FamilyInstance>().ToList();
         }
         catch (Exception)
         {
            return Result.Failed;
         }

         var bsg = new BeamShopGeometryModel(beams.First());

         using (var tg = new TransactionGroup(AC.Document))
         {
            tg.Start("Beam Shop");

            using var tx = new Transaction(AC.Document, "Create Rebar Shop");
            {
               tx.Start();
               ViewUtils.SetSketchPlane();

               var eles = AC.Selection.PickObjects(ObjectType.Element, new RebarShopSelectFilter(), "Select Rebar").Select(x => x.ToElement()).ToList();

               var rebars = eles.Where(x => x is Rebar).Cast<Rebar>().ToList();
               var cutZones = GetCutZones(eles.Where(x => x is FilledRegion).Cast<FilledRegion>().ToList());

               var rebarToCuts = new List<RebarShopModel>();
               var listRebars = rebars.Select(x => new RebarShopModel(x)).ToList();
               foreach (var rebarShopModel in listRebars)
               {
                  if (rebarShopModel.OriginalQuantity > 2 && rebarShopModel.Length > 11.7.MeterToFoot())
                  {
                     rebarShopModel.Split(out var r1, out var r2);
                     rebarToCuts.Add(r1);
                     rebarToCuts.Add(r2);
                  }
                  else
                  {
                     rebarToCuts.Add(rebarShopModel);
                  }
               }

               list = RebarShopService.CutRebarShop(rebarToCuts, cutZones);
               tx.Commit();
            }

            CreateDetailShop(list, bsg);
            CreateDetailShop(list, bsg, false);

            tg.Assimilate();
         }

         return Result.Succeeded;
      }

      private List<CutZone> GetCutZones(List<FilledRegion> hatchs)
      {
         var list = new List<CutZone>();

         foreach (var filledRegion in hatchs)
         {
            var bb = filledRegion.get_BoundingBox(AC.ActiveView);
            var min = bb.Min;
            var max = bb.Max;

            var cutZone = new CutZone(min, max, max.Z, min.Z);
            cutZone.Direction = AC.ActiveView.RightDirection;
            list.Add(cutZone);
         }

         return list;
      }

      private void CreateDetailShop(List<RebarShopModel> rebarShopModels, BeamShopGeometryModel beamShopGeometry, bool isTop = true)
      {
         IOrderedEnumerable<IGrouping<string, RebarShopModel>> gBars;

         var z = beamShopGeometry.ZTop + 700.MmToFoot();
         List<RebarShopModel> list;

         if (isTop)
         {
            list = rebarShopModels.Where(x => x.Elevation > beamShopGeometry.ZMid).ToList();
         }
         else
         {
            list = rebarShopModels.Where(x => x.Elevation < beamShopGeometry.ZMid).ToList();

            z = beamShopGeometry.ZBot - 700.MmToFoot();
         }

         var list1 = list.Where(x => x.IsCut == false).OrderBy(x => x.Elevation).ThenBy(x =>
             x.CurvesPair.MaxCurve.Midpoint().DotProduct(AC.ActiveView.RightDirection)).ToList();

         gBars = list.Where(x => x.IsCut).GroupBy(x => x.IdOriginalRebar).OrderBy(x => x.First().Elevation);

         for (var index = 0; index < list1.Count; index++)
         {
            var rebarShopModel = list1[index];
            var service = new RebarDetailService(rebarShopModel.NewRebar, AC.ActiveView);
            service.CreateDetail2D(rebarShopModel.CurvesPair.CenterCurves.First().SP().EditZ(z));

            //Check giao voi thep truoc
            if (index > 0)
            {
               var pre = list1[index - 1];
               if (!Check2RebarNotIntersectCurve(pre.CurvesPair.MaxCurve, rebarShopModel.CurvesPair.MaxCurve, AC.ActiveView.RightDirection))
               {
                  if (isTop)
                  {
                     z += 500.MmToFoot();
                  }
                  else
                  {
                     z -= 500.MmToFoot();
                  }
               }
            }
         }

         foreach (var gBar in gBars)
         {
            if (isTop)
            {
               z += 500.MmToFoot();
            }
            else
            {
               z -= 500.MmToFoot();
            }

            var z2 = z + 50.MmToFoot();
            for (var index = 0; index < gBar.ToList().Count; index++)
            {
               var rebarShopModel = gBar.ToList()[index];
               var service = new RebarDetailService(rebarShopModel.NewRebar, AC.ActiveView);
               service.CreateDetail2D(rebarShopModel.CurvesPair.CenterCurves.First().SP().EditZ(index % 2 == 0 ? z : z2));
            }
         }
      }

      public bool Check2RebarNotIntersectCurve(Curve c1, Curve c2, XYZ vector)
      {
         var d1 = c1.SP().DotProduct(vector);
         var d2 = c1.EP().DotProduct(vector);

         var d3 = c2.SP().DotProduct(vector);
         var d4 = c2.EP().DotProduct(vector);

         if (!d1.IsBetweenEqual(d3, d4, 5.MmToFoot()) && !d2.IsBetweenEqual(d3, d4, 5.MmToFoot()))
         {
            return true;
         }

         return false;
      }
   }
}