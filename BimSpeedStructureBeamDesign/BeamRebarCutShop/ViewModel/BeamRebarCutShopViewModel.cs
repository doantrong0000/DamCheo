using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarShop;
using BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;
using BimSpeedStructureBeamDesign.RebarShop.Model;
using BimSpeedUtils;
using ViewModelBase = GalaSoft.MvvmLight.ViewModelBase;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.ViewModel
{
   public class BeamRebarCutShopViewModel : ViewModelBase
   {

      //public CouplerViewModel CouplerVM { get; set; }

      public CrackedBarViewModel CrackedBarVM { get; set; }

      public LapBarViewModel LapBarVM { get; set; }

      private int settingSplit = 1;

      public int SettingSplit
      {
         get
         {
            return settingSplit;
         }
         set
         {
            settingSplit = value;

            if (value == 0)
            {
               currentView = LapBarVM;
            }
            else if (value == 1)
            {
               currentView = CrackedBarVM;
            }

            RaisePropertyChanged();

            RaisePropertyChanged(nameof(currentView));
         }
      }


      private string path = AC.BimSpeedSettingPath + "//BeamRebarShop.json";
      public int TopCutType
      {
         get => _topCutType;
         set
         {
            _topCutType = value;
            RaisePropertyChanged();
         }
      }

      public int BotCutType
      {
         get => _botCutType;
         set
         {
            _botCutType = value;
            RaisePropertyChanged();
         }
      }

      public double TopRatio
      {
         get => _topRatio;
         set
         {
            _topRatio = value;
            RaisePropertyChanged();
         }
      }

      public double BotRatio
      {
         get => _botRatio;
         set
         {
            _botRatio = value;
            RaisePropertyChanged();
         }
      }

      private List<CutZone> cutZones = new();

      public double RebarStandardLength { get; set; } = 11.7.MeterToFoot();

      public BeamModel BeamModel { get; set; }

      public BeamRebarShopModel BeamRebarShopModel { get; set; }
      public double Lapping { get; set; } = 40;

      public List<CutRebarModel> RebarModels { get; set; } = new();

      private List<XYZ> listCutPoints = new();

      public RelayCommand OkCommand { get; set; }

      private object currentView;

      public object CurrentView
      {
         get => currentView;
         set
         {
            currentView = value;
            RaisePropertyChanged();
         }
      }

      public BeamRebarCutShopViewModel(List<FamilyInstance> beams)
      {
         BeamModel = new BeamModel(beams, new List<FamilyInstance>(), new List<FamilyInstance>());

         BeamRebarShopModel = new BeamRebarShopModel(beams, BeamModel, null);

         GetCutZones();

         OkCommand = new RelayCommand(Run);

         LoadData();
      }

      public void Run(object w)
      {
         SaveData();
         if (w is Window window)
         {
            window.Close();
         }

         using (var tg = new TransactionGroup(AC.Document))
         {
            tg.Start();

            CutRebarsByLayer(BeamRebarShopModel.TopRebars, cutZones.Where(x => x.IsTop).ToList());
            CutRebarsByLayer(BeamRebarShopModel.BotRebars, cutZones.Where(x => !x.IsTop).ToList());

            tg.Assimilate();
         }
      }

      List<CutZone> GetCutZones()
      {
         var list = new List<CutZone>();
         if (TopCutType == 1)
         {
            var zMid = 0.5 * (BeamModel.BotElevation + BeamModel.TopElevation);

            var r = (1 - TopRatio) / 2;


            foreach (var span in BeamModel.SpanModels)
            {

               var p1 = span.TopLeft.Add(r * span.Length * span.Direction);
               var p2 = span.TopRight.Add(-r * span.Length * span.Direction);

               var cutZoneTop = new CutZone(p1, p2, BeamModel.TopElevation, zMid, "");
               cutZoneTop.IsTop = true;

               list.Add(cutZoneTop);
            }
         }

         if (TopCutType == 2)
         {
            var zMid = 0.5 * (BeamModel.BotElevation + BeamModel.TopElevation);
            foreach (var span in BeamModel.SpanModels)
            {
               var pStart = span.TopLeft;
               var p1 = span.TopLeft.Add(TopRatio * span.Length * span.Direction);
               var p2 = span.TopRight.Add(-TopRatio * span.Length * span.Direction);
               var pEnd = span.TopRight;

               var cutZoneTrai = new CutZone(pStart, p1, BeamModel.TopElevation, zMid, "");
               cutZoneTrai.IsTop = true;

               var cutZonePhai = new CutZone(p2, pEnd, BeamModel.TopElevation, zMid, "");
               cutZonePhai.IsTop = true;

               list.Add(cutZoneTrai);
               list.Add(cutZonePhai);
            }
         }


         if (BotCutType == 1)
         {
            var zMid = 0.5 * (BeamModel.BotElevation + BeamModel.TopElevation);

            var r = (1 - BotRatio) / 2;

            foreach (var span in BeamModel.SpanModels)
            {
               var p1 = span.TopLeft.Add(r * span.Length * span.Direction);
               var p2 = span.TopRight.Add(-r * span.Length * span.Direction);

               var cutZoneBot = new CutZone(p1, p2, zMid, BeamModel.BotElevation, "");
               cutZoneBot.IsTop = false;

               list.Add(cutZoneBot);
            }
         }

         if (BotCutType == 2)
         {
            var zMid = 0.5 * (BeamModel.BotElevation + BeamModel.TopElevation);



            foreach (var span in BeamModel.SpanModels)
            {
               var pStart = span.TopLeft;
               var p1 = span.TopLeft.Add(BotRatio * span.Length * span.Direction);
               var p2 = span.TopRight.Add(-BotRatio * span.Length * span.Direction);
               var pEnd = span.TopRight;

               var cutZoneTrai = new CutZone(pStart, p1, zMid, BeamModel.BotElevation, "");
               cutZoneTrai.IsTop = false;

               var cutZonePhai = new CutZone(p2, pEnd, zMid, BeamModel.BotElevation, "");
               cutZonePhai.IsTop = false;

               list.Add(cutZoneTrai);
               list.Add(cutZonePhai);

            }
         }


         cutZones = list;
         return list;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="rebars">Các rebars đã được group lại theo nhóm rebar number, vị trí và vị trí chẵn lẻ , chỉ cắt thép có chiều dài lớn hơn 11.7</param>
      /// <param name="cutZones"></param>
      void CutRebarsByLayer(List<CutRebarModel> rebars, List<CutZone> cutZones)
      {


         var cutPointsUsed = new List<XYZ>();

         var cutZonesCanUse = cutZones.OrderBy(x => x.Start.DotProduct(BeamModel.Direction)).ToList();

         var i = 0;

         var rebarToCreateNews = new List<CutRebarModel>();

         while (rebars.Count > 0)
         {
            i++;

            var rebarToCut = rebars[0];


            if (rebarToCut.Length < RebarStandardLength)
            {
               rebars.Remove(rebarToCut);

               continue;
            }

            if (TryCutRebar(rebarToCut, cutZonesCanUse, cutPointsUsed, out var rb1, out var rb2, out var cutPoint))
            {
               cutPointsUsed.Add(cutPoint);

               rebarToCreateNews.Add(rb1);

               if (rb2.Length > RebarStandardLength)
               {
                  rebars.Add(rb2);
               }
               else
               {
                  rebarToCreateNews.Add(rb2);
               }
            }
            else
            {
               rebarToCreateNews.Add(rebarToCut);
            }

            rebars.Remove(rebarToCut);

            if (i > 50)
            {
               break;
            }
         }

         using (var tx = new Transaction(AC.Document, "Cut Rebar"))
         {
            tx.Start();

            foreach (var rebarToCreate in rebarToCreateNews)
            {
               var rebar = rebarToCreate.Rebar;

               var rbt = rebar.GetTypeId().ToElement() as RebarBarType;

               var startHook = rebar.GetHookTypeId(0).ToElement() as RebarHookType;

               var endHook = rebar.GetHookTypeId(1).ToElement() as RebarHookType;

               RebarHookOrientation startHookOrientation = rebar.GetHookOrientation(0);

               RebarHookOrientation endHookOrientation = rebar.GetHookOrientation(1);

               var normal = rebar.RebarNormal();

               var newRebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rbt, startHook, endHook, rebar.GetHostId().ToElement(), normal, rebarToCreate.Curves, startHookOrientation, endHookOrientation, true, true);


               AC.Document.Regenerate();
               //copy rebar

               var point = rebarToCreate.Curves.First().SP();


               rebarToCreate.AllRebarPlanes.ForEach(plane =>
               {
                  var sk = SketchPlane.Create(AC.Document, plane.ToPlane())

                     ;
                  AC.Document.Create.NewModelCurve(Line.CreateBound(plane.Origin, plane.Origin.Add(BeamModel.Direction)),
                     sk);

                  var distance = plane.SignedDistanceTo(point);

                  if (distance > 3.MmToFoot())
                  {
                     var projectedPoint = plane.ProjectPoint(point);
                     var vector = projectedPoint - point;

                     var newIds = ElementTransformUtils.CopyElement(AC.Document, newRebar.Id, -vector);
                     var newRebars = newIds.Select(x => x.ToElement()).Where(x => x is Rebar).Cast<Rebar>().ToList();

                     newRebars.SetRebarSolidUnobscured(AC.Document.ActiveView);
                  }

               });

               newRebar.SetRebarSolidUnobscured(AC.ActiveView);
            }

            AC.Document.Delete(rebarToCreateNews.Where(x => x.Rebar.IsValidObject).Select(x => x.Rebar.Id).DistinctBy2(x => x.GetElementIdValue()).ToList());

            tx.Commit();
         }
      }

      bool TryCutRebar(CutRebarModel rebarToCut, List<CutZone> cutZones, List<XYZ> alreadyCutAtThisPosition, out CutRebarModel rb1, out CutRebarModel rb2, out XYZ cutPoint)
      {
         rb1 = null;
         rb2 = null;
         cutPoint = null;

         var line = rebarToCut.MainCurve.SP().CreateLine(rebarToCut.MainCurve.EP());

         if (cutZones.Count < 1)
         {
            return false;
         }

         TryFindBestPositionToCut(rebarToCut, cutZones, alreadyCutAtThisPosition, out var point);

         var plane = BPlane.CreateByNormalAndOrigin(line.Direction, point);

         cutPoint = line.SP().ProjectOnto(plane);

         SplitCurvesByPointWithRuleLapping(rebarToCut, rebarToCut.Curves, cutPoint, out var curves1,
            out var curves2);

         rb1 = rebarToCut.Clone(curves1, curves1);
         rb2 = rebarToCut.Clone(curves2, curves2);

         return true;
      }

      List<double> chieuDaiBoiSoCuaThep = new()
      {
         11700.MmToFoot(),
         5850.MmToFoot(),
         3900.MmToFoot(),
         2925.MmToFoot(),
         2340.MmToFoot(),
         2000.MmToFoot(),
         3000.MmToFoot(),
         4000.MmToFoot(),
         5000.MmToFoot(),
         6000.MmToFoot(),
         7000.MmToFoot(),
         8000.MmToFoot(),
         9000.MmToFoot(),
         10000.MmToFoot(),
         11000.MmToFoot(),
      };

      List<double> cacChieuDaiThepChan = new()
      {
         2000.MmToFoot(),
         3000.MmToFoot(),
         4000.MmToFoot(),
         5000.MmToFoot(),
         6000.MmToFoot(),
         7000.MmToFoot(),
         8000.MmToFoot(),
         9000.MmToFoot(),
         10000.MmToFoot(),
         11000.MmToFoot(),
         10500.MmToFoot(),
         9500.MmToFoot(),
         8500.MmToFoot(),
         7500.MmToFoot(),
         6500.MmToFoot(),
         5500.MmToFoot(),
         4500.MmToFoot(),
         3500.MmToFoot(),
         2500.MmToFoot(),
      };

      private int _topCutType = 1;
      private int _botCutType = 2;
      private double _topRatio = 0.25;
      private double _botRatio = 0.25;

      bool TryFindBestPositionToCut(CutRebarModel cutRebarModel, List<CutZone> cutZones, List<XYZ> ignoreCutPosition, out XYZ cutPoint)
      {
         cutPoint = null;

         //Tìm điểm cắt để ra được chiều dài chẵn

         foreach (var length in chieuDaiBoiSoCuaThep.Concat(cacChieuDaiThepChan))
         {
            cutPoint = ComputeCutPointByLength(cutRebarModel.Curves, length);
            // check if cut point inside cut zone

            var p = cutPoint;

            //cần thêm loại bỏ các vị trí đã cắt rồi
            if (cutPoint != null && cutZones.Any(cutZone => IsPointOkInCutZone(p, cutZone, cutRebarModel.Diameter, ignoreCutPosition)))
            {
               return true;
            }
         }

         // tìm số chẵn bất bì trong cut zone

         var sortedCutZonesNearest = cutZones.OrderBy(x => x.Start.DotProduct(cutRebarModel.MainCurve.Direction()));

         foreach (var cutZone in sortedCutZonesNearest)
         {
            cutPoint = (cutZone.End + cutZone.Start) / 2;

            // lam tron so dep

            return true;
         }

         return false;
      }

      XYZ ComputeCutPointByLength(List<Curve> curves, double length)
      {
         var l = 0.0;
         foreach (var curve in curves)
         {
            if (curve.Direction().IsHorizontal() && l < length && l + curve.Length > length)
            {
               var point = curve.SP().Add((length - l) * curve.Direction());
               return point;
            }

            l += curve.Length;

         }

         return null;
      }

      bool IsPointOkInCutZone(XYZ p, CutZone cutZone, double diameter, List<XYZ> cutPointsUsed)
      {
         var direct = (cutZone.End - cutZone.Start).Normalize();

         var d1 = cutZone.Start.DotProduct(direct);
         var d2 = cutZone.End.DotProduct(direct);

         var dp = p.DotProduct(direct);

         // cần tránh các vùng đã cắt nối rồi

         foreach (var cutPointUsed in cutPointsUsed)
         {
            var dCutPointTrai = cutPointUsed.Add(-direct * diameter * Lapping).DotProduct(direct);
            var dCutPointPhai = cutPointUsed.Add(direct * diameter * Lapping).DotProduct(direct);

            if (dp.IsBetween(dCutPointTrai, dCutPointPhai))
            {
               return false;
            }
         }

         if (dp > d1 && dp < d2 && dp - d1 >= diameter * Lapping * 0.8)
         {
            return true;
         }

         return false;
      }

      public void SplitCurvesByPointWithRuleLapping(CutRebarModel cutRebarModel, List<Curve> curves, XYZ pCut, out List<Curve> curves1, out List<Curve> curves2
      )
      {
         curves1 = new List<Curve>();

         curves2 = new List<Curve>();

         var pickedCurve = curves.OrderBy(x => x.Project(pCut).Distance).FirstOrDefault();

         var index = curves.IndexOf(pickedCurve);

         for (int i = 0; i < curves.Count; i++)
         {
            if (i < index)
            {
               curves1.Add(curves[i]);
            }
            if (i > index)
            {
               curves2.Add(curves[i]);
            }
         }

         SplitLineByLapPoint(pickedCurve as Line, pCut, cutRebarModel.Diameter, out var l1, out var l2);
         //check xem có cần
         curves1.Add(l1);

         curves2.Insert(0, l2);
      }

      private void SplitLineByLapPoint(Line line, XYZ pCut, double diameter, out Line l1, out Line l2)
      {

         var distanceLap = Lapping * diameter;
         XYZ pCutOnLine = pCut.ProjectPoint2Line(line);

         listCutPoints.Add(pCutOnLine);

         var sp = line.SP();

         var ep = line.EP();

         var directEp = (ep - sp).Normalize();

         var projectedLap = pCutOnLine.Add(directEp * -distanceLap);

         l1 = sp.CreateLine(pCutOnLine);

         l2 = projectedLap.CreateLine(ep);
      }



      private void SaveData()
      {
         var json = new BeamRebarCutShopJson()
         {
            TopRatio = TopRatio,
            BotRatio = BotRatio,
            BotCutType = BotCutType,
            TopCutType = TopCutType
         };


         JsonUtils.SaveSettingToFile(json, path);


      }


      private void LoadData()
      {
         var json = JsonUtils.GetSettingFromFile<BeamRebarCutShopJson>(path);

         if (json == null)
         {
            json = new BeamRebarCutShopJson()
            {
               TopRatio = 0.5,
               BotRatio = 0.25,
               TopCutType = 1,
               BotCutType = 2
            };
         }

         TopRatio = json.TopRatio;
         BotRatio = json.BotRatio;
         TopCutType = json.TopCutType;
         BotCutType = json.BotCutType;
      }
   }
}
