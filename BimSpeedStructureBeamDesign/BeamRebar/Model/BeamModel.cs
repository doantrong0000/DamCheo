using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class BeamModel
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
      public List<BeamGeometry> BeamGeometries { get; private set; } = new List<BeamGeometry>();
      public BeamUiModel BeamUiModel { get; set; }
      public List<FamilyInstance> BeamAsSupports { get; set; }
      public List<FamilyInstance> SecondaryBeams { get; set; }
      public double TopElevation { get; set; }
      public double BotElevation { get; set; }

      public BeamModel(List<FamilyInstance> beams, List<FamilyInstance> beamAsSupports, List<FamilyInstance> secondaryBeams)
      {
         BeamAsSupports = beamAsSupports;
         var ids = beamAsSupports.Select(x => x.Id.GetElementIdValue()).ToList();
         SecondaryBeams = secondaryBeams.Where(x => ids.Contains(x.Id.GetElementIdValue()) == false).ToList();

         GetData(beams);
      }

      private void GetData(List<FamilyInstance> beams)
      {
         foreach (var b in beams)
         {
            var beamGeometry = new BeamGeometry(b);
            BeamGeometries.Add(beamGeometry);
         }

         Direction = BeamRebarCommonService.EditBeamDirection(BeamGeometries[0].BeamLine.Direction);

         BeamGeometries.ForEach(x => x.BeamLine = BeamRebarCommonService.EditLineByDirection(x.BeamLine, Direction));

         if (BeamGeometries.Count > 1)
         {
            BeamGeometries = BeamGeometries.OrderBy(x => x.MidPoint.DotProduct(Direction)).ToList();
         }

         var walls = new FilteredElementCollector(AC.Document, AC.ActiveView.Id)
             .OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Wall)).Cast<Wall>().Where(x =>
                 x.WallType?.Kind == WallKind.Basic &&
                 x.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsInteger() == 1).ToList();


         var columns = new FilteredElementCollector(AC.Document, AC.ActiveView.Id).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(x => x.StructuralMaterialType == StructuralMaterialType.Concrete).ToList();

         var foundations = new FilteredElementCollector(AC.Document, AC.ActiveView.Id)
             .OfCategory(BuiltInCategory.OST_StructuralFoundation).WhereElementIsNotElementType().ToList();

         SupportModel tempSupport = null;
         var first = true;
         foreach (var beamGeometry in BeamGeometries)
         {
            var line = beamGeometry.BeamLine;
            var filter = new ElementIntersectsSolidFilter(beamGeometry.OriginalSolidTransformed);
            var wallSupports = walls.Where(x => filter.PassesFilter(x)).Select(x => new ElementGeometry(x)).ToList();
            var columnSupports = columns.Where(x => filter.PassesFilter(x)).Select(x => new ElementGeometry(x)).ToList();
            var foundationsSupport = foundations.Where(x => filter.PassesFilter(x)).Select(x => new ElementGeometry(x)).ToList();
            var beamAsSupportGeometries = BeamAsSupports.Select(x => new BeamGeometry(x));
            var beamSupports = new List<ElementGeometry>();
            foreach (var beamAsSupportGeometry in beamAsSupportGeometries)
            {
               var eleGeo = new ElementGeometry(beamAsSupportGeometry.Beam)
               {
                  Solid = beamAsSupportGeometry.GetOriginalSolidTransformed(2.MeterToFoot())
               };
               beamSupports.Add(eleGeo);
            }
            var solids = wallSupports.Concat(columnSupports).Concat(beamSupports).Concat(foundationsSupport).Select(x => x.Solid).Where(x => x != null && x.Volume > 0.001).ToList();
            var lines = BeamRebarCommonService.TrimLinesBySolids(line, solids);
            lines = BeamRebarCommonService.EditLinesByDirectionAndOrdering(lines, Direction);

            var supports = columnSupports.Concat(wallSupports).Concat(beamSupports).Concat(foundationsSupport)
                .Where(x => x.Solid != null && x.Solid.Volume > 0.001).ToList();

            foreach (var line1 in lines)
            {
               var left = line1.SP();
               var right = line1.EP();
               var span = new SpanModel(line1, beamGeometry);
               //Tim supports
               foreach (var elementGeometry in supports)
               {
                  if (first && BeamRebarCommonService.IsPointNearSolid(elementGeometry.Solid, left, Direction, out var lineInsideSupport))
                  {
                     var support = new SupportModel(elementGeometry, lineInsideSupport);
                     span.LeftSupportModel = support;
                     SupportModels.Add(support);
                  }
                  if (first == false)
                  {
                     span.LeftSupportModel = tempSupport;
                  }
                  if (BeamRebarCommonService.IsPointNearSolid(elementGeometry.Solid, right, Direction, out var lineInsideSupport1))
                  {
                     var support = new SupportModel(elementGeometry, lineInsideSupport1);
                     span.RightSupportModel = support;
                     SupportModels.Add(support);
                     tempSupport = support;
                     if (first == false)
                     {
                        break;
                     }
                  }
               }
               first = false;
               SpanModels.Add(span);
            }
            //Get Cover
         }

         ZTop = SpanModels[0].TopElevation;
         Origin = SpanModels[0].LeftSupportModel != null ? SpanModels[0].LeftSupportModel.Line.SP() : SpanModels[0].TopLine.SP();
         var lastSpan = SpanModels.LastOrDefault();

         if (lastSpan?.RightSupportModel != null)
         {
            Last = lastSpan.RightSupportModel.Line.EP();
         }
         else
         {
            if (lastSpan != null) Last = lastSpan.TopLine.EP();
         }

         Origin = Origin.EditZ(ZTop);
         Last = Last.EditZ(ZTop);

         var supportIndex = 0;
         for (int i = 0; i < SpanModels.Count; i++)
         {
            SpanModels[i].Index = i;
            if (SpanModels[i].LeftSupportModel != null)
            {
               SpanModels[i].LeftSupportModel.Index = supportIndex;
               supportIndex++;
            }

            if (i == SpanModels.Count - 1)
            {
               if (SpanModels[i].RightSupportModel != null)
               {
                  SpanModels[i].RightSupportModel.Index = supportIndex;
                  supportIndex++;
               }
            }
         }

         GetSupportPoints();
         TimCacViTriCoTheNhanThep();
         GetSecondaryBeamForSpan();
      }

      private void GetSecondaryBeamForSpan()
      {
         var secondaryBeamGeometry = new List<BeamGeometry>();
         foreach (var beam in SecondaryBeams)
         {
            if (beam.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralFraming)
            {
               var beamGeometry = new BeamGeometry(beam);
               //neu la beam extend 2 dau
               beamGeometry.OriginalSolidTransformed = beamGeometry.GetOriginalSolidTransformed(2000.MmToFoot());
               //Neu la cot extent theo phuong dung

               secondaryBeamGeometry.Add(beamGeometry);
            }
            else if (beam.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralColumns)
            {
               var beamGeometry = new BeamGeometry() { Beam = beam, IsColumn = true, Transform = beam.GetTransform() };
               beamGeometry.OriginalSolidTransformed = beam.GetAllSolids(true).FirstOrDefault();
               secondaryBeamGeometry.Add(beamGeometry);
            }

         }

         var ignores = new List<int>();
         foreach (var spanModel in SpanModels)
         {
            foreach (var beamGeometry in secondaryBeamGeometry)
            {
               if (ignores.Contains(beamGeometry.Beam.Id.GetElementIdValue()))
               {
                  continue;
               }
               var solid = beamGeometry.OriginalSolidTransformed;
               var z = solid.ComputeCentroid().Z;
               if (!beamGeometry.IsColumn)
               {
                  if (z.IsBetweenEqual(spanModel.BotElevation, spanModel.TopElevation, 300.MmToFoot()))
                  {
                     var l = Line.CreateBound(spanModel.TopLeft.EditZ(z), spanModel.TopRight.EditZ(z));
                     var insideLine = l.GetInsideLinesIntersectSolids(new List<Solid>() { solid }).FirstOrDefault();
                     if (insideLine != null)
                     {
                        insideLine = BeamRebarCommonService.EditLineByDirection(insideLine, Direction);
                        //Check Secondary beam is inside span
                        if (insideLine.SP().DotProduct(Direction) > spanModel.TopLeft.DotProduct(Direction) && insideLine.EP().DotProduct(Direction) < spanModel.TopRight.DotProduct(Direction))
                        {
                           var secondaryBeam = new SecondaryBeamModel(beamGeometry, insideLine);
                           spanModel.SecondaryBeamModels.Add(secondaryBeam);
                           ignores.Add(beamGeometry.Beam.Id.GetElementIdValue());
                        }
                     }
                  }
               }
               else
               {
                  var l = Line.CreateBound(spanModel.TopLeft.EditZ(z), spanModel.TopRight.EditZ(z));
                  var insideLine = l.GetInsideLinesIntersectSolids(new List<Solid>() { solid }).FirstOrDefault();
                  if (insideLine != null)
                  {
                     insideLine = BeamRebarCommonService.EditLineByDirection(insideLine, Direction);
                     //Check Secondary beam is inside span
                     if (insideLine.SP().DotProduct(Direction) > spanModel.TopLeft.DotProduct(Direction) && insideLine.EP().DotProduct(Direction) < spanModel.TopRight.DotProduct(Direction))
                     {
                        beamGeometry.TopElevation = spanModel.TopElevation;
                        beamGeometry.BotElevation = spanModel.BotElevation;
                        beamGeometry.Height = beamGeometry.TopElevation - beamGeometry.BotElevation;
                        var secondaryBeam = new SecondaryBeamModel(beamGeometry, insideLine);
                        spanModel.SecondaryBeamModels.Add(secondaryBeam);
                        ignores.Add(beamGeometry.Beam.Id.GetElementIdValue());
                     }
                  }
               }

            }
         }

      }

      private void TimCacViTriCoTheNhanThep()
      {
         if (SpanModels.Count > 2)
         {
            for (int i = 1; i < SpanModels.Count - 1; i++)
            {
               var previousSpan = SpanModels[i - 1];
               var currentSpan = SpanModels[i];
               var nextSpan = SpanModels[i + 1];
               if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, true) && currentSpan.BotElevation.IsEqual(previousSpan.BotElevation, 0.1) == false)
               {
                  currentSpan.CoTheNhanThepBottomAtRight = true;
               }
               if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, false) && currentSpan.TopElevation.IsEqual(previousSpan.TopElevation, 0.1) == false)
               {
                  currentSpan.CoTheNhanThepTopAtRight = true;
               }

               if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, previousSpan, true) && currentSpan.BotElevation.IsEqual(previousSpan.BotElevation, 0.1) == false)
               {
                  currentSpan.CoTheNhanThepBottomAtLeft = true;
               }
               if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, previousSpan, false) && currentSpan.TopElevation.IsEqual(previousSpan.TopElevation, 0.1) == false)
               {
                  currentSpan.CoTheNhanThepTopAtLeft = true;
               }

               if (currentSpan.LeftSupportModel?.Width > 180.MmToFoot())
               {
                  currentSpan.KhoangNhanThepDiVaoSupportLeft = 50.MmToFoot();
               }

               if (currentSpan.RightSupportModel?.Width > 180.MmToFoot())
               {
                  currentSpan.KhoangNhanThepDiVaoSupportRight = 50.MmToFoot();
               }

               if (i == 1)
               {
                  previousSpan.KhoangNhanThepDiVaoSupportRight = currentSpan.KhoangNhanThepDiVaoSupportLeft;
                  previousSpan.CoTheNhanThepBottomAtRight = currentSpan.CoTheNhanThepBottomAtLeft;
                  previousSpan.CoTheNhanThepTopAtRight = currentSpan.CoTheNhanThepTopAtLeft;
               }
               if (i == SpanModels.Count - 2)
               {
                  nextSpan.KhoangNhanThepDiVaoSupportLeft = currentSpan.KhoangNhanThepDiVaoSupportRight;
                  nextSpan.CoTheNhanThepBottomAtLeft = currentSpan.CoTheNhanThepBottomAtRight;
                  nextSpan.CoTheNhanThepTopAtLeft = currentSpan.CoTheNhanThepTopAtRight;
               }
            }
         }
      }

      private void GetSupportPoints()
      {
         foreach (var spanModel in SpanModels)
         {
            var leftSp = spanModel.LeftSupportModel;
            if (leftSp != null)
            {
               leftSp.TopRight = spanModel.TopLeft;
               leftSp.BotRight = spanModel.BotLeft;
               if (spanModel.Index == 0)
               {
                  leftSp.TopLeft = leftSp.TopLeft.EditZ(spanModel.TopElevation);
                  leftSp.TopRight = leftSp.TopRight.EditZ(spanModel.TopElevation);
               }
            }

            var rightSp = spanModel.RightSupportModel;
            if (rightSp != null)
            {
               rightSp.TopLeft = spanModel.TopRight;
               rightSp.BotLeft = spanModel.BotRight;
            }
         }
      }

      public BeamUiModel ConvertToBeamUiModel()
      {
         var beamUiModel = new BeamUiModel();
         SupportUiModel temp = null;
         foreach (var spanModel in SpanModels)
         {
            var spanUiModel = new SpanUiModel(spanModel, this, beamUiModel, temp);
            beamUiModel.SpanUiModels.Add(spanUiModel);
            temp = spanUiModel.RightSupport;
            if (spanUiModel.LeftSupport != null)
            {
               beamUiModel.SupportUiModels.Add(spanUiModel.LeftSupport);
            }
         }

         if (temp != null)
         {
            beamUiModel.SupportUiModels.Add(temp);
         }
         beamUiModel.SetIndex();
         BeamUiModel = beamUiModel;
         return beamUiModel;
      }
   }
}