using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public static class BeamRebarServices
   {
      #region StandardBars
      public static List<Rebar> CreateMainBar(MainRebar mainRebar, int bot = 1)
      {
         var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[mainRebar.Start];
         var normal = BeamRebarRevitData.Instance.BeamModel.Direction.CrossProduct(XYZ.BasisZ);
         var rbt = mainRebar.BarDiameter;
         mainRebar.Curves = GetMainBarCurves(mainRebar, true, bot);
         var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rbt, null, null, span.Beam,
             normal, mainRebar.Curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
         var list = SetRebarLayoutForRebar(mainRebar.RebarPointsInSection, rebar, span);
         mainRebar.Rebars = list;
         mainRebar.Curves = mainRebar.Curves;
         SetRebarSolidUnobscured(list, AC.ActiveView, span.Mark);

         return list;
      }

      public static void SetRebarsDirection(List<Rebar> rebars, string direct, string mark)
      {
         foreach (var rebar in rebars)
         {
            rebar.SetParameterValueByName("BS_REBAR_DIRECTION", direct);
            rebar.SetParameterValueByName("Partition", mark);
         }
      }

      public static List<Curve> GetMainBarCurves(MainRebar mainRebar, bool isDraw, int bot = 1)
      {
         var points = new List<XYZ>();
         var xTrai = mainRebar.LxTrai;
         var xPhai = mainRebar.LxPhai;
         var yTrai = mainRebar.LyTrai;
         var yPhai = mainRebar.LyPhai;

         if (isDraw)
         {
            yTrai -= mainRebar.BarDiameter.BarDiameter() / 2;
            yPhai -= mainRebar.BarDiameter.BarDiameter() / 2;
         }

         for (int i = mainRebar.Start; i < mainRebar.End; i++)
         {
            var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[i];

            var z = GetZAtLayer(span, mainRebar.IsTop, mainRebar.Layer, mainRebar.BarDiameter);

            var direct = span.BotLine.Direction;
            var pLeft = span.BotLeft.EditZ(z);
            var pRight = span.BotRight.EditZ(z);

            //Điều chỉnh điểm nhấn thép vào bên trong 50mm nên check gối tại đó >200mm
            if (span.CoTheNhanThepBottomAtLeft || span.CoTheNhanThepTopAtLeft)
            {
               pLeft = pLeft.Add(direct * -1 * span.KhoangNhanThepDiVaoSupportLeft);
            }
            if (span.CoTheNhanThepBottomAtRight || span.CoTheNhanThepTopAtRight)
            {
               pRight = pRight.Add(direct * span.KhoangNhanThepDiVaoSupportRight);
            }

            //Hiệu chỉnh neo buộc điểm start
            if (i == mainRebar.Start)
            {
               var sp = span.LeftSupportModel;
               if (sp != null)
               {
                  //neo thép vào gối và sang nhịp bên cạnh ko cần nhấn
                  if (yTrai >= 20.MmToFoot())
                  {
                     pLeft = pLeft.Add(-direct * xTrai);
                     //Check đủ chiều cao đáng kể để kéo lên trên

                     if (yTrai >= 80.MmToFoot())
                     {
                        var pUp = pLeft.Add(XYZ.BasisZ * yTrai * bot * 1);
                        points.Add(pUp);
                     }
                  }
                  else if (span.CoTheNhanThepBottomAtLeft && bot == 1)
                  {
                     var p1 = sp.BotLeft.Add(direct * span.KhoangNhanThepDiVaoSupportLeft).EditZ(z + sp.BotLeft.Z - sp.BotRight.Z);
                     var p0 = p1.Add(-direct * xTrai);
                     points.Add(p0);
                     points.Add(p1);
                  }
                  else if (span.CoTheNhanThepTopAtLeft && bot == -1)
                  {
                     var p1 = sp.TopLeft.Add(direct * span.KhoangNhanThepDiVaoSupportLeft).EditZ(z + sp.TopLeft.Z - sp.TopRight.Z);

                     var p0 = p1.Add(-direct * xTrai);
                     points.Add(p0);
                     points.Add(p1);
                  }
                  else
                  {
                     pLeft = pLeft.Add(-direct * xTrai);
                  }
               }
               else
               {
                  pLeft = pLeft.Add(-direct * xTrai);
                  if (yTrai >= 80.MmToFoot())
                  {
                     var pUp = pLeft.Add(XYZ.BasisZ * yTrai * bot * 1);
                     points.Add(pUp);
                  }
               }
            }
            points.Add(pLeft);
            points.Add(pRight);

            //Hiệu chỉnh neo buộc điểm end

            if (i == mainRebar.End - 1)
            {
               points.RemoveAt(points.Count - 1);
               var sp = span.RightSupportModel;
               if (sp != null)
               {
                  if (yPhai >= 80.MmToFoot())
                  {
                     pRight = pRight.Add(direct * xPhai);
                     //Check đủ chiều cao đáng kể để kéo lên trên

                     points.Add(pRight);
                     if (yPhai >= 80.MmToFoot())
                     {
                        var pUp = pRight.Add(XYZ.BasisZ * yPhai * bot);
                        points.Add(pUp);
                     }
                  }
                  //Neo thép sang nhịp bên cạnh và cần nhấn thép vì nD lớn hơn nhịp
                  else if (span.CoTheNhanThepBottomAtRight && bot == 1)
                  {
                     var p0 = sp.BotRight.Add(-direct * span.KhoangNhanThepDiVaoSupportRight).EditZ(z - sp.BotLeft.Z + sp.BotRight.Z);
                     var p1 = p0.Add(direct * xPhai);
                     points.Add(pRight);
                     points.Add(p0);
                     points.Add(p1);
                  }
                  else if (span.CoTheNhanThepTopAtRight && bot == -1)
                  {
                     var p1 = sp.TopRight.Add(-direct * span.KhoangNhanThepDiVaoSupportRight).EditZ(z + sp.TopRight.Z - sp.TopLeft.Z);

                     var p0 = p1.Add(direct * xPhai);
                     points.Add(pRight);
                     points.Add(p1);
                     points.Add(p0);
                  }
                  else
                  {
                     pRight = pRight.Add(direct * xPhai);
                     points.Add(pRight);
                  }
               }
               else
               {
                  pRight = pRight.Add(direct * xPhai);
                  points.Add(pRight);
                  if (yPhai >= 80.MmToFoot())
                  {
                     var pUp = pRight.Add(XYZ.BasisZ * yPhai * bot);
                     points.Add(pUp);
                  }
               }
            }
         }
         var curves = points.CurvesFromPoints();
         return curves;
      }

      public static List<Rebar> SetRebarLayoutForRebar(List<RebarPoint> rps, Rebar rebar, SpanModel spanModel)
      {
         var rebars = new List<Rebar> { rebar };
         rps = rps.Where(x => x.Checked).ToList();
         if (rps.Count >= 2)
         {
            var first = rps.First();
            var second = rps[1];
            var last = rps.Last();
            var d = first.Point.DistanceTo(last.Point);
            var spacing = d / (rps.Count - 1);
            var spacing12 = second.Point.DistanceTo(first.Point);
            //Check tất cả thép đều nhau
            if (spacing12.IsEqual(spacing, 3.MmToFoot()))
            {
               rebar.SetRebarLayoutAsFixedNumber(rps.Count, d, true, true, true);
               var mid = (first.Point + last.Point) / 2;
               var plane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, mid);
               MoveRebarToCenterPlane(rebar, plane);
            }
            else
            {
               //Tạo 2 cây đầu cuối để thành 1 cặp
               var plus = 1;
               if (rps.Count % 2 == 0)
               {
                  plus = 0;
               }
               for (int i = 0; i < rps.Count / 2 + plus; i++)
               {
                  var rp1 = rps[i];
                  var rp2 = rps[rps.Count - 1 - i];
                  if (i == 0)
                  {
                     rebar.SetRebarLayoutAsFixedNumber(2, rp1.Point.DistanceTo(rp2.Point), true, true, true);
                     var mid = (rp1.Point + rp2.Point) / 2;
                     var plane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, mid);
                     MoveRebarToCenterPlane(rebar, plane);
                  }
                  else
                  {
                     if (rp1.Index == rp2.Index)
                     {
                        var rb = ElementTransformUtils
                            .CopyElement(AC.Document, rebar.Id, XYZ.Zero).Select(x => x.ToElement()).FirstOrDefault(x => x is Rebar) as Rebar;
                        rb.SetRebarLayoutAsSingle();
                        rebars.Add(rb);
                        var mid = (rp1.Point + rp2.Point) / 2;
                        var plane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, mid);
                        MoveRebarToCenterPlane(rb, plane);
                     }
                     else
                     {
                        var rb = ElementTransformUtils
                            .CopyElement(AC.Document, rebar.Id, XYZ.Zero).Select(x => x.ToElement()).FirstOrDefault(x => x is Rebar) as Rebar;
                        rb.SetRebarLayoutAsFixedNumber(2, rp1.Point.DistanceTo(rp2.Point), true, true, true);
                        rebars.Add(rb);
                        var mid = (rp1.Point + rp2.Point) / 2;
                        var plane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, mid);
                        MoveRebarToCenterPlane(rb, plane);
                     }
                  }
               }
            }
         }

         if (rebars.Count > 1)
         {
            rebars.GroupRebar();
         }
         return rebars;
      }

      private static void SetLayoutAsFixedNumberThenMoveToCenterSpan(Rebar rebar, int number, double arrayLength, SpanModel spanModel)
      {
         if (number > 1 && rebar != null)
         {
            rebar.SetRebarLayoutAsFixedNumber(number, arrayLength, true, true, true);
            MoveRebarToCenterSpan(rebar, spanModel);
         }
      }

      public static void MoveRebarToCenterSpan(Rebar rebar, SpanModel spanModel)
      {
         var number = rebar.Quantity;

         //Move to right location
         var curves = rebar.GetCenterlineCurves(false, false, false,
             MultiplanarOption.IncludeOnlyPlanarCurves, 0);

         var origin = curves.FirstOrDefault().SP();
         if (rebar.Quantity > 1)
         {
            var first = curves.FirstOrDefault().SP();
            var tfLast = rebar.GetRebarPositionTransform(number - 1);
            var second = tfLast.OfPoint(first);
            origin = (second + first) / 2;
         }

         var plane = BPlane.CreateByThreePoints(spanModel.TopLeft, spanModel.TopRight, spanModel.BotLeft);
         var projected = plane.ProjectOnto(origin);
         var translation = projected - origin;
         ElementTransformUtils.MoveElement(AC.Document, rebar.Id, translation);
      }

      public static void MoveRebarToCenterPlane(Rebar rebar, BPlane plane)
      {
         var number = rebar.Quantity;

         //Move to right location
         var curves = rebar.GetCenterlineCurves(false, false, false,
             MultiplanarOption.IncludeOnlyPlanarCurves, 0);
         if (curves.Count < 1)
         {
            return;
         }
         var first = curves.FirstOrDefault().SP();
         if (number < 2)
         {
            var mid = first;
            var projected = plane.ProjectOnto(mid);
            var translation = projected - mid;
            ElementTransformUtils.MoveElement(AC.Document, rebar.Id, translation);
         }
         else
         {
            var tfLast = rebar.GetRebarPositionTransform(number - 1);
            var second = tfLast.OfPoint(first);
            var mid = (second + first) / 2;
            var projected = plane.ProjectOnto(mid);
            var translation = projected - mid;
            ElementTransformUtils.MoveElement(AC.Document, rebar.Id, translation);
         }
      }

      #endregion StandardBars

      #region Additional Top

      public static List<Rebar> CreateAdditionalTopBar(TopAdditionalBar bar)
      {
         var span = bar.Start.GetSpanModelByIndex();
         var normal = BeamRebarRevitData.Instance.BeamModel.Direction.CrossProduct(XYZ.BasisZ);
         if (bar.Curves == null || bar.Curves.Count == 0)
         {
            return new List<Rebar>();
         }
        var BeMoc = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.BeMocToiThieu.MmToFoot();
        bar.Curves = GetAdditionalTopBarCurves(bar, BeMoc, out var z);
         var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, bar.BarDiameter, null, null, span.Beam,
             normal, bar.Curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
         CreateRebarConKeThep(null, bar, bar.Curves);
         var rebars = SetRebarLayoutForRebar(bar.RebarPointsInSection, rebar, span);
         bar.Rebars = rebars;
         SetRebarSolidUnobscured(new List<Rebar> { rebar }, AC.ActiveView, span.Mark);
         return rebars;
      }

      public static List<Curve> GetAdditionalTopBarCurves(TopAdditionalBar bar, double BeMoc, out double z)
      {
         z = BeamRebarRevitData.Instance.BeamModel.ZTop;
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel;
         var points = new List<XYZ>();
         var diameterlayer = bar.BarDiameter.BarDiameter();
         var mainTopBar = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.TopMainBarDiameter;
         var diameterTop = mainTopBar.BarDiameter();
         var diameterBot = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.BotMainBarDiameter.BarDiameter();
         var distance2layer = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.RebarDistance2Layers;
         var coverTop= BeamRebarRevitData.Instance.BeamRebarCover + diameterTop/2;
         var DStirr = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.StirrupBarDiameter.BarDiameter();


         for (int i = bar.Start; i <= bar.End; i++)
         {
            var span = i.GetSpanModelByIndex();
            var previousSpan = (i - 1).GetSpanModelByIndex();

            z = GetZAtLayer(span, bar.IsTop, bar.Layer.Layer, bar.BarDiameter, mainTopBar);

            //Nếu Kiểu kết thúc ==2 và bar ko phải cây cuối cùng nhịp cuối thì z tính theo nhịp bên trái
            if (bar.RebarEndType == 2 && span.LeftSupportModel != null && bar.End != BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
            {
               z = z + span.LeftSupportModel.TopLeft.Z - span.LeftSupportModel.TopRight.Z;
            }

            var direct = span.BotLine.Direction;
            var pLeft = span.TopLeft.EditZ(z);
            var pRight = span.TopRight.EditZ(z);
            if (span.CoTheNhanThepTopAtRight)
            {
               pRight = pRight.Add(direct * span.KhoangNhanThepDiVaoSupportRight);
            }
            if (span.CoTheNhanThepTopAtLeft)
            {
               pLeft = pLeft.Add(-direct * span.KhoangNhanThepDiVaoSupportLeft);
            }

            var diameter = bar.BarDiameter.BarDiameter();

            var leftLength = bar.LeftLength;

                //Hiệu chỉnh neo buộc điểm start
                if (i == bar.Start)
                {
                    var sp = span.LeftSupportModel;

                    // 🟩 1. Trường hợp cây gia cường cuối cùng của dầm (ở nhịp cuối)
                    if (bar.Start == BeamRebarRevitData.Instance.BeamUiModel.SpanUiModels.Count)
                    {
                        // Đặt điểm đầu tại TopRight của span và lùi lại theo chiều dài thép bên trái
                        pLeft = span.TopRight.EditZ(z);
                        pLeft = pLeft.Add(-direct * bar.LeftLength);
                    }
                    else
                    {
                        if (sp != null) // 🟩 2. Có gối trái (LeftSupportModel tồn tại)
                        {
                            // 🟦 2.1 Cây thép gia cường ở nhịp đầu tiên và không neo theo quy định
                            if (setting.Setting.NeoThepTheoQuyDinh == false && bar.Start == 0)
                            {
                                // Kéo thép ra ngoài gối
                                pLeft = pLeft.Add(-direct * (sp.Width - BeamRebarRevitData.Instance.BeamRebarCover - diameter / 2 - distance2layer * (bar.Layer.Layer - 1)));

                                // 🟨 Nếu đủ chiều cao thì bẻ thép xuống
                                var zz = span.BotElevation + BeamRebarRevitData.Instance.BeamRebarCover + DStirr + diameterBot;
                                var pUp = pLeft.EditZ(zz);
                                points.Add(pUp);
                            }
                            else
                            {
                                // 🟦 2.2 Thép kéo sang nhịp bên trái (không cần nhấn)
                                var flag = false;
                                if (bar.Start > 0 && bar.RebarStartType != 2)
                                {
                                    if (sp.TopLeft.Z.IsGreaterEqual(sp.TopRight.Z))
                                        flag = true;
                                }

                                // 🟧 2.3 Neo thép vào gối và kéo thẳng sang nhịp bên trái (không nhấn)
                                if (bar.RebarStartType == 2 && (leftLength < sp.Width - BeamRebarRevitData.Instance.BeamRebarCover || flag))
                                {
                                    pLeft = pLeft.Add(-direct * leftLength);

                                    // 🟨 Nếu có móc neo thì bẻ móc xuống
                                    if (BeMoc > 0)
                                    {
                                        pLeft = pLeft.Add(direct * BeMoc);
                                        var down = pLeft.Add(XYZ.BasisZ * BeMoc * -1);
                                        points.Add(down);
                                    }
                                }

                                // 🟥 2.4 Kéo thép vào gối và nhấn thép (có khả năng nhấn)
                                else if (span.CoTheNhanThepTopAtLeft)
                                {
                                    var p1 = sp.TopLeft.Add(direct * span.KhoangNhanThepDiVaoSupportLeft).EditZ(z + sp.TopLeft.Z - sp.TopRight.Z);

                                    var delta = bar.LeftLength - span.KhoangNhanThepDiVaoSupportLeft - p1.DistanceTo(pLeft);

                                    // Trường hợp đặc biệt RebarStartType = 1 (kéo từ nhịp trước theo tỷ lệ)
                                    if (bar.RebarStartType == 1)
                                        delta = (previousSpan.Length * bar.LeftRatio).RoundMilimet(10, false);

                                    var p0 = p1.Add(-direct * delta);

                                    points.Add(p0);
                                    points.Add(p1);
                                }

                                // 🟪 2.5 Trường hợp thông thường (không nhấn, không kéo qua nhịp)
                                else
                                {
                                    if (bar.RebarStartType == 2)
                                    {
                                        // Kéo ra mép gối
                                        var KeoRaMep = -direct * (sp.Width - BeamRebarRevitData.Instance.BeamRebarCover - distance2layer * (bar.Layer.Layer - 1));
                                        pLeft = pLeft.Add(KeoRaMep);

                                        var delta = bar.LeftLength - (sp.Width - BeamRebarRevitData.Instance.BeamRebarCover - distance2layer * (bar.Layer.Layer - 1));

                                        // 🟨 Nếu có móc thì kéo xuống
                                        if (BeMoc > 0)
                                        {
                                            if (BeMoc < delta)
                                            {
                                                var pUp = pLeft.Add(XYZ.BasisZ * delta * -1);
                                                points.Add(pUp);
                                            }
                                            else
                                            {
                                                var zDistance = Math.Abs(BeMoc - delta);
                                                pLeft = pLeft.Add(-direct * -zDistance);
                                                var pUp = pLeft.Add(XYZ.BasisZ * BeMoc * -1);
                                                points.Add(pUp);
                                            }
                                        }
                                        else
                                        {
                                            // 🟨 Nếu không có móc thì chỉ kéo xuống nếu chiều dài đủ
                                            if (delta > distance2layer * (bar.Layer.Layer - 1))
                                            {
                                                var pUp = pLeft.Add(XYZ.BasisZ * delta * -1);
                                                points.Add(pUp);
                                            }
                                            else
                                            {
                                                pLeft = pLeft.Add(-direct * -delta);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 🟪 Nếu không RebarStartType == 2, thì kéo thẳng thêm đoạn delta
                                        var delta = bar.LeftLength;
                                        pLeft = pLeft.Add(-direct * (sp.Width + delta));
                                    }
                                }
                            }
                        }

                        else // 🟩 3. Không có gối trái (null)
                        {
                            // Lùi vào trong theo cover
                            pLeft = pLeft.Add(direct * (BeamRebarRevitData.Instance.BeamRebarCover + distance2layer * (bar.Layer.Layer - 1)));

                            // 🟨 Nếu đủ chiều cao thì bẻ xuống
                            if (leftLength > span.Height - coverTop)
                            {
                                var pBot = pLeft.EditZ(span.BotElevation + coverTop + DStirr + diameterTop / 2);
                                points.Add(pBot);
                            }
                            else
                            {
                                // Nếu không thì kéo chéo xuống
                                var pBot = pLeft.Add(-XYZ.BasisZ * leftLength);
                                points.Add(pBot);
                            }
                        }
                    }
                }

            points.Add(pLeft);
            points.Add(pRight);

            //Hiệu chỉnh neo buộc điểm end
            if (i == bar.End)
            {
                points.RemoveAt(points.Count - 1);
                var rightSupport = span.RightSupportModel;
                //Không phải cây thép gia cường ở vị trí cuối cùng của nhịp
                if (i != BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
                {
                  pRight = span.TopLeft.EditZ(z);
                }
            

               if (bar.End == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
               {
                  pRight = span.TopRight.EditZ(z);

                  rightSupport = span.RightSupportModel;
               }


               if (bar.RebarEndType == 1)
               {
                  if (span.CoTheNhanThepTopAtRight && bar.End>bar.Start)
                  {
                     var p1 = rightSupport.TopRight.Add(-direct * span.KhoangNhanThepDiVaoSupportLeft).EditZ(z);
                     var delta = bar.RightLength;

                     var p0 = p1.Add(direct * delta);
                     points.Add(p0);
                  }
                  else
                  {
                     pRight = span.TopLeft.EditZ(z);
                     pRight = pRight.Add(direct * bar.RightLength);
                     points.Add(pRight);
                  }
               }
               else if (bar.RebarEndType == 2)
               {
                  if (rightSupport != null)
                  {
                    if (setting.Setting.NeoThepTheoQuyDinh == false)
                    {
                        // Kéo thép ra ngoài gối
                        pRight= pRight.Add(direct * (rightSupport.Width - BeamRebarRevitData.Instance.BeamRebarCover - diameter / 2 - distance2layer * (bar.Layer.Layer - 1)));

                        // 🟨 Nếu đủ chiều cao thì bẻ thép xuống
                        var zz = span.BotElevation + BeamRebarRevitData.Instance.BeamRebarCover + DStirr + diameterBot;
                        var pUp = pRight.EditZ(zz);
                        points.Add(pRight);
                        points.Add(pUp);
                    }
                    else
                    {
                        //Flag để kiểm tra xem nhịp bên cạnh cao hơn nhịp bên trái hay không để kéo thẳng thép sang
                        var isRightHigherThanLeft = false;
                        if (bar.End < BeamRebarRevitData.Instance.BeamModel.SpanModels.Count - 1 && bar.RebarEndType != 1)
                        {
                            if (rightSupport.TopLeft.Z.IsSmallerEqual(rightSupport.TopRight.Z))
                            {
                                isRightHigherThanLeft = true;
                            }
                        }
                        //Nếu khoảng kéo dài nhỏ hơn bề rộng support thì kéo thẳng sang
                        if (bar.RightLength < rightSupport.Width - BeamRebarRevitData.Instance.BeamRebarCover || isRightHigherThanLeft)
                        {
                            if (BeMoc > 0)
                            {
                                pRight = pRight.Add(direct * bar.RightLength);
                                pRight = pRight.Add(direct * -BeMoc);
                                var pUp = pRight.Add(XYZ.BasisZ * BeMoc * -1);
                                points.Add(pRight);
                                points.Add(pUp);
                            }
                            else
                            {
                                pRight = pRight.Add(direct * bar.RightLength);
                                points.Add(pRight);
                            }

                        }
                        else
                        {
                            //Nếu 2 bên bằng nhau
                            if (i != BeamRebarRevitData.Instance.BeamModel.SpanModels.Count)
                            {
                                pRight = pRight.Add(direct * (-coverTop - distance2layer * (bar.Layer.Layer - 1)));
                            }
                            else
                            {
                                pRight = pRight.Add(direct * (rightSupport.Width - coverTop - distance2layer * (bar.Layer.Layer - 1)));
                            }

                            points.Add(pRight);
                            if (bar.End == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count && setting.Setting.NeoThepTheoQuyDinh == false)
                            {
                                var zz = span.BotElevation + BeamRebarRevitData.Instance.BeamRebarCover + DStirr + diameterBot;
                                if (zz < BeMoc)
                                {
                                    zz = BeMoc;
                                    var a = BeMoc - zz;
                                    int index = points.IndexOf(pRight);
                                    if (index != -1)
                                    {
                                        points.RemoveAt(index); // Xóa pRight khỏi danh sách points
                                    }
                                    pRight = pRight.Add(direct * a);
                                    points.Add(pRight);
                                }
                                var pUp = pRight.EditZ(zz);

                                points.Add(pUp);
                            }
                            else
                            {
                                //Check đủ chiều cao đáng kể để kéo xuống dưới

                                if (BeMoc > 0)
                                {
                                    var delta = bar.RightLength - (rightSupport.Width - coverTop - distance2layer * (bar.Layer.Layer - 1));
                                    int index = points.IndexOf(pRight);
                                    if (index != -1)
                                    {
                                        points.RemoveAt(index); // Xóa pRight khỏi danh sách points
                                    }

                                    if (BeMoc < delta)
                                    {
                                        var pUp = pRight.Add(XYZ.BasisZ * delta * -1);
                                        points.Add(pRight);
                                        points.Add(pUp);
                                    }
                                    else
                                    {
                                        var zDistance = Math.Abs(BeMoc - delta);
                                        pRight = pRight.Add(direct * -zDistance);
                                        var pUp = pRight.Add(XYZ.BasisZ * BeMoc * -1);
                                        points.Add(pRight);
                                        points.Add(pUp);
                                    }
                                }
                                else
                                {
                                    var delta = bar.RightLength - (rightSupport.Width - coverTop - distance2layer * (bar.Layer.Layer - 1));

                                    if (delta > distance2layer * (bar.Layer.Layer - 1))
                                    {
                                        var pUp = pRight.Add(XYZ.BasisZ * delta * -1);
                                        points.Add(pUp);
                                    }
                                    else
                                    {
                                        pRight = pRight.Add(direct * -delta);
                                        points.Add(pRight);
                                    }
                                }

                            }
                        }
                    }
                           
                  }
                  //Khi support ==null
                  else
                  {
                     pRight = pRight.Add(-direct * (coverTop + distance2layer * (bar.Layer.Layer - 1)));
                     points.Add(pRight);
                     if (bar.RightLength > span.Height - coverTop)
                     {
                        var pBot = pRight.EditZ(span.BotElevation + coverTop+ DStirr +diameterTop / 2);
                         points.Add(pBot);
                     }
                     else
                     {
                        var pBot = pRight.Add(-XYZ.BasisZ * bar.RightLength);
                        points.Add(pBot);
                     }
                  }
               }
            }
         }
         var curves = points.CurvesFromPoints();
         return curves;
      }

      #endregion Additional Top

      #region Additional Bot

      public static List<Rebar> CreateAdditionalBottomBar(BottomAdditionalBar bar)
      {
         var normal = BeamRebarRevitData.Instance.BeamModel.Direction.CrossProduct(XYZ.BasisZ);
         var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[bar.Start];

         var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, bar.BarDiameter, null, null, span.Beam,
             normal, bar.Curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
            CreateRebarConKeThep(bar, null, bar.Curves);
            var rebars = SetRebarLayoutForRebar(bar.RebarPointsInSection, rebar, span);
         bar.Rebars = rebars;

         SetRebarSolidUnobscured(rebars, AC.ActiveView, span.Mark);
         return rebars;
      }

      public static List<Curve> GetCurvesAdditionalBottomBar(BottomAdditionalBar bar, out double z)
      {
         var points = new List<XYZ>();
         z = 0;
         for (int i = bar.Start; i < bar.End; i++)
         {
            var span = BeamRebarRevitData.Instance.BeamModel.SpanModels[i];
            var mainBotBar = BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel.Setting.BotMainBarDiameter;
            var diameterBot = mainBotBar.BarDiameter();
            z = GetZAtLayer(span, bar.Layer.IsTop, bar.Layer.Layer, bar.BarDiameter,mainBotBar);
            var n = GetNBySupport(span.LeftSupportModel, false);
            var nD = n * bar.BarDiameter.BarDiameter();
            var leftSp = span.LeftSupportModel;
            var rightSp = span.RightSupportModel;
            var diameter = bar.BarDiameter.BarDiameter();
            var direct = span.BotLine.Direction;
            var pLeft = span.BotLeft.EditZ(z);
            var pRight = span.BotRight.EditZ(z);
            
                //Điều chỉnh điểm nhấn thép vào bên trong 50mm nên check gối tại đó >200mm
                if (bar.Layer.IsTop)
            {
               if (span.CoTheNhanThepBottomAtLeft && i > bar.Start)
               {
                  pLeft = pLeft.Add(direct * -1 * span.KhoangNhanThepDiVaoSupportLeft);
               }
               if (span.CoTheNhanThepBottomAtRight && i < bar.End - 1)
               {
                  pRight = pRight.Add(direct * span.KhoangNhanThepDiVaoSupportRight);
               }
            }
            else
            {
               if (span.CoTheNhanThepTopAtLeft && i > bar.Start)
               {
                  pLeft = pLeft.Add(direct * -1 * span.KhoangNhanThepDiVaoSupportLeft);
               }
               if (span.CoTheNhanThepTopAtRight && i < bar.End - 1)
               {
                  pRight = pRight.Add(direct * span.KhoangNhanThepDiVaoSupportRight);
               }
            }

            if (i == bar.Start)
            {
               #region Left

               if (bar.RebarStartType == 3)
               {
                  points.Add(pLeft);
               }
               else if (bar.RebarStartType == 1)
               {
                  pLeft = span.BotLeft.EditZ(z).Add(direct * (span.Length / 2 - bar.LeftLength));
                  points.Add(pLeft);
               }
               else if (bar.RebarStartType == 2)
               {
                  //Check thép khéo sang nhịp bên cạnh vì ko nhấn thép
                  if (leftSp == null)
                  {
                     pLeft = span.BotLeft.EditZ(z).Add(direct * BeamRebarRevitData.Instance.BeamRebarCover);
                     points.Add(pLeft);
                  }
                  else
                  {
                     var delta = nD - leftSp.Width;
                     var flag = false;
                     if (span.Index != 0)
                     {
                        flag = leftSp.BotRight.Z.IsGreaterEqual(leftSp.BotLeft.Z);
                     }
                     if (nD < leftSp.Width - BeamRebarRevitData.Instance.BeamRebarCover || flag && span.Index != 0)
                     {
                        pLeft = span.BotLeft.EditZ(z).Add(-direct * nD);
                        points.Add(pLeft);
                     }
                     else
                     {
                        if (span.Index == 0)
                        {
                           pLeft = span.BotLeft.EditZ(z).Add(-direct * (leftSp.Width - BeamRebarRevitData.Instance.BeamRebarCover - diameter / 2));
                           pLeft = pLeft.Add(direct * 25.MmToFoot());
                           if (delta > 50.MmToFoot())
                           {
                              var pUp = pLeft.Add(XYZ.BasisZ * delta);
                              points.Add(pUp);
                           }
                           points.Add(pLeft);
                        }
                        else
                        {
                           //Vươn qua nhịp bên cạnh
                           if (span.CoTheNhanThepBottomAtLeft)
                           {
                              pLeft = pLeft.Add(-direct * span.KhoangNhanThepDiVaoSupportLeft);
                              var p1 = leftSp.BotLeft.Add(direct * span.KhoangNhanThepDiVaoSupportLeft).EditZ(z + leftSp.BotLeft.Z - leftSp.BotRight.Z);
                              var delta1 = nD - span.KhoangNhanThepDiVaoSupportLeft - p1.DistanceTo(pLeft);
                              var p0 = p1.Add(-direct * delta1);
                              points.Add(p0);
                              points.Add(p1);
                              points.Add(pLeft);
                           }
                           else
                           {
                              pLeft = pLeft.Add(-direct * (leftSp.Width - BeamRebarRevitData.Instance.BeamRebarCover - diameter / 2));

                              //Check đủ chiều cao đáng kể để kéo lên trên
                              var delta1 = nD - (leftSp.Width - BeamRebarRevitData.Instance.BeamRebarCover);
                              if (delta > 50.MmToFoot())
                              {
                                 var pUp = pLeft.Add(XYZ.BasisZ * delta1);
                                 points.Add(pUp);
                                 points.Add(pLeft);
                              }
                           }
                        }
                     }
                  }
               }

               #endregion Left
            }
            else
            {
               points.Add(pLeft);
            }

            if (i == bar.End - 1)
            {
               n = GetNBySupport(span.RightSupportModel, false);
               nD = n * bar.BarDiameter.BarDiameter();

               #region Right

               if (bar.RebarEndType == 3)
               {
                  points.Add(pRight);
               }
               else if (bar.RebarEndType == 1)
               {
                  pRight = pRight.Add(-direct * (span.Length / 2 - bar.RightLength));
                  points.Add(pRight);
               }
               else if (bar.RebarEndType == 2)
               {
                  if (rightSp == null)
                  {
                     pRight = span.BotLeft.EditZ(z).Add(-direct * BeamRebarRevitData.Instance.BeamRebarCover);
                     points.Add(pRight);
                  }
                  else
                  {
                     //Check thép khéo sang nhịp bên cạnh vì ko nhấn thép
                     var flag = false;
                     if (span.Index != BeamRebarRevitData.Instance.BeamModel.SpanModels.Count - 1)
                     {
                        flag = rightSp.BotLeft.Z.IsGreaterEqual(rightSp.BotRight.Z);
                     }
                     var delta = nD - rightSp.Width;
                     if (nD < rightSp.Width || flag)
                     {
                        pRight = pRight.Add(direct * nD);
                        points.Add(pRight);
                     }
                     else
                     {
                        if (span.CoTheNhanThepBottomAtRight)
                        {
                           pRight = pRight.Add(direct * span.KhoangNhanThepDiVaoSupportRight);
                           var p0 = rightSp.BotRight.Add(-direct * span.KhoangNhanThepDiVaoSupportRight).EditZ(z + rightSp.BotRight.Z - rightSp.BotLeft.Z);
                           var delta1 = nD - span.KhoangNhanThepDiVaoSupportRight - p0.DistanceTo(pRight);
                           var p1 = p0.Add(direct * delta1);
                           points.Add(pRight);
                           points.Add(p0);
                           points.Add(p1);
                        }
                        else
                        {
                           pRight = pRight.Add(direct * (rightSp.Width - BeamRebarRevitData.Instance.BeamRebarCover - diameter / 2));
                           if (span.Index == BeamRebarRevitData.Instance.BeamModel.SpanModels.Count - 1)
                           {
                              pRight = pRight.Add(-direct * 25.MmToFoot());
                           }
                           points.Add(pRight);
                           if (delta > 50.MmToFoot())
                           {
                              var pUp = pRight.Add(XYZ.BasisZ * delta);

                              points.Add(pUp);
                           }
                        }
                     }
                  }
               }

               #endregion Right
            }
            else
            {
               points.Add(pRight);
            }
         }

         var curves = points.CurvesFromPoints();
         return curves;
      }

      #endregion Additional Bot

      #region Stirrupt

      public static List<Rebar> CreateStirrupForSpan(SpanModel spanModel)
      {
         if (spanModel.Length < 200.MmToFoot())
         {
            return null;
         }
         var setting = BeamRebarRevitData.Instance.BeamRebarSettingJson;
         var info = spanModel.StirrupForSpan;
         var rebarBarType = info.BarDiameter;
         RebarShape rebarShape = BeamRebarRevitData.Instance.StirrupShapeChuNhatKin;

         if (rebarShape == null)
         {
            "ReabarServices01_MESSAGE".NotificationError(spanModel);
         }

         var p0 = spanModel.TopLeft;
         var p100 = spanModel.TopRight;
         var direct = (p100 - p0).Normalize();
         var p05 = p0.Add(BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.KhoangCachDenThepDaiDauTien * direct);
         var p95 = p100.Add(BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.KhoangCachDenThepDaiDauTien * -direct);
         var rebars = new List<Rebar>();

         //Tạo thép đai gia cường
         //Bố trí thép đai chính theo đai gia cường
         var lines = new List<Line>();
         XYZ end = p05;
         foreach (var secondaryBeam in spanModel.SecondaryBeamModels)
         {
            //Thep Vai bo
            CreateThepVaiBoDamPhu(spanModel, secondaryBeam);
            var rbtDaiGiaCuong = spanModel.StirrupForSpan.BarDiameter;
            var pLeft = secondaryBeam.TopLeft.Add(direct * -1 * 50.MmToFoot());
            var pLeftStart = pLeft.Add(-direct * setting.KhoangCachDaiGiaCuong * (setting.SoLuongDaiGiaCuong - 1));

            var daiGiaCuong1s = CreateStirrupBy2Points(spanModel, pLeftStart, pLeft, rbtDaiGiaCuong, rebarShape, setting.KhoangCachDaiGiaCuong, out var rb1s, out var rb2s);

            rebars.AddRange(daiGiaCuong1s);
            RebarExtensibleStorage.SetEntityForRebars(daiGiaCuong1s, "", Define.DaiGiaCuongDamPhu);

            var pRight = secondaryBeam.TopRight.Add(direct * 1 * 50.MmToFoot());
            var pRightEnd = pRight.Add(direct * setting.KhoangCachDaiGiaCuong * (setting.SoLuongDaiGiaCuong - 1));
            var daiGiaCuong2s = CreateStirrupBy2Points(spanModel, pRight, pRightEnd, rbtDaiGiaCuong, rebarShape, setting.KhoangCachDaiGiaCuong, out var rb3s, out var rb4s);
            rebars.AddRange(daiGiaCuong2s);
            RebarExtensibleStorage.SetEntityForRebars(daiGiaCuong2s, "", Define.DaiGiaCuongDamPhu);
            lines.Add(Line.CreateBound(end, pLeftStart));
            end = pRightEnd;
         }

         lines.Add(Line.CreateBound(end, p95));

         if (info.KieuPhanBoThepDai == 1)
         {
            var distributePaths = GetDistributePath(p05, p95, spanModel, info.SpacingAtEnd);
            foreach (var path in distributePaths)
            {
               rebars.AddRange(CreateStirrupBy2PointsForMainStirrup(spanModel, path.SP(), path.EP(), rebarBarType, rebarShape, info.SpacingAtEnd, out var list1, out var list2));

               spanModel.StirrupForSpan.MainStirrupEnd1.AddRange(list1);
               spanModel.StirrupForSpan.SecondaryStirrupEnd1.AddRange(list2);
            }
         }
         else
         {
            var p25 = p0.Add(direct * spanModel.StirrupForSpan.End1Length);
            var p75 = p100.Add(-direct * spanModel.StirrupForSpan.End2Length);
            var distributePaths1 = GetDistributePath(p05, p25, spanModel, info.SpacingAtEnd);

            foreach (var line in distributePaths1)
            {
               var rb1 = CreateStirrupBy2PointsForMainStirrup(spanModel, line.SP(), line.EP(), rebarBarType, rebarShape, info.SpacingAtEnd, out var list1, out var list2);

               spanModel.StirrupForSpan.MainStirrupEnd1.AddRange(list1);
               spanModel.StirrupForSpan.SecondaryStirrupEnd1.AddRange(list2);
               rebars.AddRange(rb1);
            }

            var distributePaths2 = GetDistributePath(p25.Add(direct * info.SpacingAtEnd), p75.Add(-direct * info.SpacingAtEnd), spanModel, info.SpacingAtEnd);

            foreach (var line in distributePaths2)
            {
               var rb1 = CreateStirrupBy2PointsForMainStirrup(spanModel, line.SP(), line.EP(), rebarBarType, rebarShape, info.SpacingAtMid, out var list1, out var list2);


               spanModel.StirrupForSpan.MainStirrupMid.AddRange(list1);
               spanModel.StirrupForSpan.SecondaryStirrupMid.AddRange(list2);
               rebars.AddRange(rb1);
            }


            var distributePaths3 = GetDistributePath(p75, p95, spanModel, info.SpacingAtEnd);
            foreach (var line in distributePaths3)
            {
               var rb1 = CreateStirrupBy2PointsForMainStirrup(spanModel, line.SP(), line.EP(), rebarBarType, rebarShape, info.SpacingAtEnd, out var list1, out var list2);

               spanModel.StirrupForSpan.MainStirrupEnd2.AddRange(list1);
               spanModel.StirrupForSpan.SecondaryStirrupEnd2.AddRange(list2);
               rebars.AddRange(rb1);
            }
         }


         SetRebarSolidUnobscured(rebars, AC.ActiveView, spanModel.Mark);
         return rebars;
      }

      private static List<Rebar> CreateThepVaiBoDamPhu(SpanModel spanModel, SecondaryBeamModel secondaryBeam)
      {
         if (BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.TaoThepVaiBoDamPhu == false)
         {
            return null;
         }
         var extendtionLength = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.DoanKeoDai2Ben;
         var rebars = new List<Rebar>();
         var curves = new List<Curve>();

         var zTop = GetZAtLayer(spanModel, true, 1, 25.GetRebarBarTypeByNumber());
         var zBot = GetZAtLayer(spanModel, false, 1, 25.GetRebarBarTypeByNumber());


         var vectorLeftToRight = (secondaryBeam.BotRight - secondaryBeam.BotLeft).Normalize();
         var pBotLeft = secondaryBeam.BotLeft.EditZ(zBot).Add(vectorLeftToRight * -50.MmToFoot());
         var pBotRight = secondaryBeam.BotRight.EditZ(zBot).Add(vectorLeftToRight * 50.MmToFoot());
         var d = (zTop - zBot) * Math.Tan(Math.PI / 4);
         var pTopLeftEnd = pBotLeft.Add(vectorLeftToRight * -d).EditZ(zTop);
         var pTopLeftStart = pTopLeftEnd.Add(vectorLeftToRight * -extendtionLength);
         var pTopRightStart = pBotRight.Add(vectorLeftToRight * d).EditZ(zTop);
         var pTopRightEnd = pTopRightStart.Add(vectorLeftToRight * extendtionLength);
         curves.Add(Line.CreateBound(pTopLeftStart, pTopLeftEnd));
         curves.Add(Line.CreateBound(pTopLeftEnd, pBotLeft));
         curves.Add(Line.CreateBound(pBotLeft, pBotRight));
         curves.Add(Line.CreateBound(pBotRight, pTopRightStart));
         curves.Add(Line.CreateBound(pTopRightStart, pTopRightEnd));
         var rbt = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.DuongKhiThepVaiBo;
         var number = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.SoLuongThepVaiBo;
         var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rbt, null, null, spanModel.Beam,
             spanModel.XVecForStirrupBox, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
         rebars.Add(rebar);
         SetLayoutAsFixedNumberThenMoveToCenterSpan(rebar, number, spanModel.Width * 0.6, spanModel);
         SetRebarSolidUnobscured(rebars, AC.ActiveView);
         return rebars;
      }

      private static List<Rebar> CreateStirrupBy2Points(SpanModel spanModel, XYZ start, XYZ end, RebarBarType rebarBarType, RebarShape rebarShape, double spacing, out List<Rebar> rebars1, out List<Rebar> rebars2, bool includeFirstLast = true)
      {
         if (rebarShape == null)
         {
            rebars1 = new List<Rebar>();
            rebars2 = new List<Rebar>();
            return new List<Rebar>();
         }

         rebars1 = new List<Rebar>();
         rebars2 = new List<Rebar>();
         var host = spanModel.Beam;
         var origin = spanModel.OriginForStirrupBoxBotCorner;
         var xVec = spanModel.XVecForStirrupBox;
         var yVec = spanModel.YVecForStirrupBox;

         var rebar = Rebar.CreateFromRebarShape(AC.Document, rebarShape, rebarBarType, host, origin, xVec, yVec);

         rebars1.Add(rebar);

         var plane = BPlane.CreateByNormalAndOrigin(start - end, start);
         origin = origin.ProjectOnto(plane);
         rebar.RebarScaleToBox(origin, xVec, yVec);
         var arrayLength = (start - end).GetLength();
         rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, true, includeFirstLast, includeFirstLast);
         if (rebar.RebarNormal().DotProduct(end - start) < 0)
         {
            rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, false, includeFirstLast, includeFirstLast);
         }

         foreach (var stirrupModel in spanModel.StirrupForSpan.StirrupModels)
         {
            var daiMocDiameter = spanModel.StirrupForSpan.BarDiameterDaiMoc;

            if (stirrupModel.IsDaiMoc)
            {
               if (ElementTransformUtils.CopyElement(AC.Document, rebar.Id, XYZ.Zero).FirstOrDefault().ToElement() is Rebar rb)
               {
                  rb.ChangeTypeId(daiMocDiameter.Id);
                  //Change shape
                  var newOrigin1 = stirrupModel.Start.EditZ(origin.Z).ProjectOnto(plane);
                  rb.get_Parameter(BuiltInParameter.REBAR_SHAPE).Set(BeamRebarRevitData.Instance.StirrupDaiMoc135x135.Id);
                  rb.RebarScaleToBox(newOrigin1, yVec, -xVec);

                  var curves =
                      rb.GetCenterlineCurves(false, false, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
                  var points = new List<XYZ>();
                  foreach (var curve in curves)
                  {
                     points.Add(curve.SP());
                     points.Add(curve.EP());
                  }
                  var f = spanModel.XVecForStirrupBox.FirstPointByDirection(points);
                  var l = (-spanModel.XVecForStirrupBox).FirstPointByDirection(points);
                  var fPlane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, f);
                  var lPlane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, l);
                  var p1 = newOrigin1.ProjectOnto(fPlane);
                  var p2 = newOrigin1.ProjectOnto(lPlane);
                  var mid = (p1 + p2) / 2;
                  if (p1.DistanceTo(p2) > 5.MmToFoot())
                  {
                     var moveVector = mid - newOrigin1;
                     ElementTransformUtils.MoveElement(AC.Document, rb.Id, -moveVector);
                  }
                  rebars2.Add(rb);
               }
            }
            else
            {
               //Set box
               var newOrigin1 = stirrupModel.Start.EditZ(origin.Z).ProjectOnto(plane);
               var newOrigin2 = stirrupModel.End.EditZ(origin.Z).ProjectOnto(plane);
               var vector = (newOrigin2 - newOrigin1).Normalize();
               newOrigin1 = newOrigin1.Add(-vector * 15.MmToFoot());
               newOrigin2 = newOrigin2.Add(vector * 15.MmToFoot());
               var xVec1 = newOrigin2 - newOrigin1;
               if (ElementTransformUtils.CopyElement(AC.Document, rebar.Id, XYZ.Zero).FirstOrDefault().ToElement() is Rebar rb)
               {
                  rb.ChangeTypeId(daiMocDiameter.Id);
                  rb.RebarScaleToBox(newOrigin1, xVec1, yVec);
                  rebars2.Add(rb);
               }
            }
         }


         if (!BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.DrawMainStirrup)
         {
            AC.Document.Delete(rebars1.Where(x => x.IsValidObject).Select(x => x.Id).ToList());
            rebars1.Clear();
         }

         var a = new List<Rebar>();
         a.AddRange(rebars1);
         a.AddRange(rebars2);
         return a;
      }

      private static List<Rebar> CreateStirrupBy2PointsForMainStirrup(SpanModel spanModel, XYZ start, XYZ end, RebarBarType rebarBarType, RebarShape rebarShape, double spacing, out List<Rebar> rebars1, out List<Rebar> rebars2, bool includeFirstLast = true)
      {
         if (rebarShape == null)
         {
            rebars1 = new List<Rebar>();
            rebars2 = new List<Rebar>();
            return new List<Rebar>();
         }

         rebars1 = new List<Rebar>();
         rebars2 = new List<Rebar>();
         var host = spanModel.Beam;
         var origin = spanModel.OriginForStirrupBoxBotCorner;
         var xVec = spanModel.XVecForStirrupBox;
         var yVec = spanModel.YVecForStirrupBox;


         var rebar = Rebar.CreateFromRebarShape(AC.Document, rebarShape, rebarBarType, host, origin, xVec, yVec);

         var plane = BPlane.CreateByNormalAndOrigin(start - end, start);

         if (spanModel.StirrupForSpan.ShapeDaiChinh == 1)
         {
            rebars1.Add(rebar);

            origin = origin.ProjectOnto(plane);
            rebar.RebarScaleToBox(origin, xVec, yVec);
            var arrayLength = (start - end).GetLength();

            rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, true, includeFirstLast, includeFirstLast);
            if (rebar.RebarNormal().DotProduct(end - start) < 0)
            {
               rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, false, includeFirstLast, includeFirstLast);
            }
         }
         else if (spanModel.StirrupForSpan.ShapeDaiChinh == 2)
         {
            rebars1.Add(rebar);
            rebar.get_Parameter(BuiltInParameter.REBAR_SHAPE).Set(BeamRebarRevitData.Instance.StirrupUShape.Id);
            origin = origin.ProjectOnto(plane);
            rebar.RebarScaleToBox(origin, xVec, yVec);
            var arrayLength = (start - end).GetLength();

            rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, true, includeFirstLast, includeFirstLast);

            if (rebar.RebarNormal().DotProduct(end - start) < 0)
            {
               rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, false, includeFirstLast, includeFirstLast);
            }

            //Thep Links top
            if (ElementTransformUtils.CopyElement(AC.Document, rebar.Id, XYZ.Zero).FirstOrDefault().ToElement() is Rebar rb)
            {
               rb.get_Parameter(BuiltInParameter.REBAR_SHAPE).Set(BeamRebarRevitData.Instance.StirrupDaiMoc135x135.Id);

               rebars1.Add(rb);
               var origin2 = spanModel.OriginForStirrupBoxTopCorner.ProjectOnto(plane);
               rb.RebarScaleToBox(origin2, xVec, -yVec);

               rb.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, true, includeFirstLast, includeFirstLast);

               if (rb.RebarNormal().DotProduct(end - start) < 0)
               {
                  rb.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, false, includeFirstLast, includeFirstLast);
               }
            }
         }

         foreach (var stirrupModel in spanModel.StirrupForSpan.StirrupModels)
         {
            var daiMocDiameter = spanModel.StirrupForSpan.BarDiameterDaiMoc;

            if (stirrupModel.IsDaiMoc)
            {
               if (ElementTransformUtils.CopyElement(AC.Document, rebar.Id, XYZ.Zero).FirstOrDefault().ToElement() is Rebar rb)
               {
                  var shapeDaiMoc = spanModel.StirrupForSpan.ShapeDaiPhuChuC == 1 ? BeamRebarRevitData.Instance.StirrupDaiMoc135x135 : BeamRebarRevitData.Instance.StirrupDaiMoc135x90;

                  rb.ChangeTypeId(daiMocDiameter.Id);
                  //Change shape
                  var newOrigin1 = stirrupModel.Start.EditZ(origin.Z).ProjectOnto(plane);

                  rb.get_Parameter(BuiltInParameter.REBAR_SHAPE).Set(shapeDaiMoc.Id);


                  rb.RebarScaleToBox(newOrigin1, yVec, -xVec);

                  var curves =
                      rb.GetCenterlineCurves(false, false, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);

                  var points = new List<XYZ>();

                  foreach (var curve in curves)
                  {
                     points.Add(curve.SP());
                     points.Add(curve.EP());
                  }

                  var f = spanModel.XVecForStirrupBox.FirstPointByDirection(points);
                  var l = (-spanModel.XVecForStirrupBox).FirstPointByDirection(points);

                  var fPlane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, f);
                  var lPlane = BPlane.CreateByNormalAndOrigin(spanModel.XVecForStirrupBox, l);

                  var p1 = newOrigin1.ProjectOnto(fPlane);
                  var p2 = newOrigin1.ProjectOnto(lPlane);
                  var mid = (p1 + p2) / 2;

                  if (p1.DistanceTo(p2) > 5.MmToFoot())
                  {
                     var moveVector = mid - newOrigin1;
                     ElementTransformUtils.MoveElement(AC.Document, rb.Id, -moveVector);
                  }

                  rebars2.Add(rb);
               }
            }
            else
            {
               //Set box
               var newOrigin1 = stirrupModel.Start.EditZ(origin.Z).ProjectOnto(plane);
               var newOrigin2 = stirrupModel.End.EditZ(origin.Z).ProjectOnto(plane);

               var shape = spanModel.StirrupForSpan.ShapeDaiPhuChuNhat == 1
                  ? BeamRebarRevitData.Instance.StirrupShapeChuNhatKin
                  : BeamRebarRevitData.Instance.StirrupUShape;

               var vector = (newOrigin2 - newOrigin1).Normalize();
               newOrigin1 = newOrigin1.Add(-vector * 15.MmToFoot());
               newOrigin2 = newOrigin2.Add(vector * 15.MmToFoot());
               var xVec1 = newOrigin2 - newOrigin1;
               if (ElementTransformUtils.CopyElement(AC.Document, rebar.Id, XYZ.Zero).FirstOrDefault().ToElement() is Rebar rb)
               {
                  rb.get_Parameter(BuiltInParameter.REBAR_SHAPE).Set(shape.Id);
                  rb.ChangeTypeId(daiMocDiameter.Id);
                  rb.RebarScaleToBox(newOrigin1, xVec1, yVec);
                  rebars2.Add(rb);
               }
            }
         }

         if (!BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.DrawMainStirrup)
         {
            AC.Document.Delete(rebars1.Where(x => x.IsValidObject).Select(x => x.Id).ToList());
            rebars1.Clear();
         }

         var a = new List<Rebar>();
         a.AddRange(rebars1);
         a.AddRange(rebars2);
         return a;
      }

      public static List<string> GetTypeOfStirrupsByWidth(double width)
      {
         var rebarByWidths = BeamRebarRevitData.Instance.NumberOfRebarByWidths;
         foreach (var rebarByWidth in rebarByWidths)
         {
            var w = width.FootToMm();
            if (rebarByWidth.BMin < w && w < rebarByWidth.BMax)
            {
               if (rebarByWidth.TypeNumberOfRebarByWidth == Define.ThreeBars || rebarByWidth.TypeNumberOfRebarByWidth == Define.FourBars1)
               {
                  return new List<string>() { Define.DaiDon };
               }

               if (rebarByWidth.TypeNumberOfRebarByWidth == Define.FourBars2 ||
                   rebarByWidth.TypeNumberOfRebarByWidth == Define.FiveBars1 ||
                   rebarByWidth.TypeNumberOfRebarByWidth == Define.SixBars ||
                   rebarByWidth.TypeNumberOfRebarByWidth == Define.SeventBars ||
                   rebarByWidth.TypeNumberOfRebarByWidth == Define.NineBars)
               {
                  return new List<string>() { Define.DaiDon, Define.DaiKep, Define.DaiLongChuU, Define.DaiLongKin, Define.DaiMoc };
               }

               if (rebarByWidth.TypeNumberOfRebarByWidth == Define.FiveBars2)
               {
                  return new List<string>() { Define.DaiDon, Define.DaiMoc };
               }
            }
         }
         return new List<string>() { Define.DaiDon };
      }

      private static List<Line> GetDistributePath(XYZ start, XYZ end, SpanModel spanModel, double spacing)
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarSettingJson;

         var solids = new List<Solid>();
         var normal = spanModel.Direction.CrossProduct(XYZ.BasisZ);
         foreach (var secondaryBeam in spanModel.SecondaryBeamModels)
         {
            var p1 = secondaryBeam.TopLeft.Add(spanModel.Direction * -(spacing + 50.MmToFoot() + setting.KhoangCachDaiGiaCuong * (setting.SoLuongDaiGiaCuong - 1))).EditZ(spanModel.BotElevation);
            var p2 = secondaryBeam.TopRight.Add(spanModel.Direction * (50.MmToFoot() + spacing + setting.KhoangCachDaiGiaCuong * (setting.SoLuongDaiGiaCuong - 1))).EditZ(spanModel.BotElevation);
            var a = p1.Add(normal * spanModel.Width);
            var b = p2.Add(normal * spanModel.Width);
            var c = p2.Add(-normal * spanModel.Width);
            var d = p1.Add(-normal * spanModel.Width);
            var curveLoop = new CurveLoop();
            curveLoop.Append(Line.CreateBound(a, b));
            curveLoop.Append(Line.CreateBound(b, c));
            curveLoop.Append(Line.CreateBound(c, d));
            curveLoop.Append(Line.CreateBound(d, a));
            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop },
                XYZ.BasisZ, spanModel.Height + 1);
            solids.Add(solid);
         }
         var line = Line.CreateBound(start, end);
         var lines = BeamRebarCommonService.TrimLinesBySolids(line, solids);
         return lines;
      }

      #endregion Stirrupt

      #region Con Ke Thep

      public static List<Rebar> CreateRebarConKeThep(BottomAdditionalBar barBot, TopAdditionalBar barTop, List<Curve> curves)
      {
         var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.ConKeThep;
         if (setting.NeedConKeThep == false)
         {
            return null;
         }
         var rebars = new List<Rebar>();
         var diameterdistence =BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.RebarDistance2Layers;
         var diameter = 25.MmToFoot();
         var span = BeamRebarRevitData.Instance.BeamModel.SpanModels.FirstOrDefault();
         if (span == null)
         {
            return null;
         }
         var layer = 1;
         if (barBot != null)
         {
            diameter = barBot.BarDiameter.BarDiameter();
            span = barBot.Start.GetSpanModelByIndex();
            layer = barBot.Layer.Layer;
         }
         if (barTop != null)
         {
            diameter = barTop.BarDiameter.BarDiameter();
            span = barTop.Start.GetSpanModelByIndex();
            layer = barTop.Layer.Layer;
         }
         if (layer != 1)
         {
            foreach (var curve in curves)
            {
                var dir = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

                // Bỏ qua nếu curve gần như song song trục Z
                if (Math.Abs(dir.DotProduct(XYZ.BasisZ)) > 0.98)
                    continue;
                if (curve.Length > 400.MmToFoot())
                {
                  var dia = setting
                      .ConKeThepInfo.Diameter;

                  var spacing = setting
                      .ConKeThepInfo.Spacing;
                  if (setting.IsConKeBangCotThep == false)
                  {
                     dia = setting
                        .ConKeDaiMocInfo.Diameter;
                     spacing = setting
                        .ConKeDaiMocInfo.Spacing;
                  }

                  var rebarBarType = dia;
                  var z = curve.SP().Z - diameter / 2 - dia.BarDiameter() / 2;
                  if (barTop != null)
                  {
                     z = curve.SP().Z + diameter / 2 + dia.BarDiameter() / 2;
                  }

                  if (setting.IsConKeBangCotThep == false)
                  {
                     z = curve.SP().Z + diameter / 2 + dia.BarDiameter();
                     if (barTop != null)
                     {
                        z = curve.SP().Z - diameter / 2 - dia.BarDiameter();
                     }
                  }
                  if (span != null)
                  {
                     var direct = span.TopLine.Direction;
                     var sp = span.OriginForStirrupBoxBotCorner.EditZ(z);
                     var ep = sp.Add(span.XVecForStirrupBox);
                     var n = curve.Length / spacing;
                     var arrayLength = 0.0;
                     if (n < 1)
                     {
                        arrayLength = curve.Length - 100.MmToFoot();
                        spacing = arrayLength;
                        var plane = BPlane.CreateByNormalAndOrigin(direct, curve.SP().Add(direct * 50.MmToFoot()));
                        sp = sp.ProjectOnto(plane);
                        ep = ep.ProjectOnto(plane);
                     }
                     else
                     {
                        var round = Math.Floor(n);
                        arrayLength = round * spacing;
                        var vector = direct * (n - round) * spacing / 2;
                        var origin = curve.SP().Add(vector);
                        var plane = BPlane.CreateByNormalAndOrigin(direct, origin);
                        sp = sp.ProjectOnto(plane);
                        ep = ep.ProjectOnto(plane);
                     }

                     var lines = new List<Curve>() { Line.CreateBound(sp, ep) };
                     Rebar rebar = null;
                     if (setting.IsConKeBangCotThep)
                     {
                        rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rebarBarType, null, null, span.Beam,
                            span.TopLine.Direction, lines, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                     }
                     else
                     {
                        rebar = Rebar.CreateFromRebarShape(AC.Document, BeamRebarRevitData.Instance.StirrupDaiMoc135x135, rebarBarType, span.Beam, sp,
                            ep - sp, XYZ.BasisZ);
                        rebar.RebarScaleToBox(sp, ep - sp, -XYZ.BasisZ);
                        var bb = rebar.get_BoundingBox(AC.ActiveView);
                        if (bb != null)
                        {
                           if (barTop != null && bb.Max.Z < sp.Z + 10.MmToFoot())
                           {
                              rebar.RebarScaleToBox(sp, ep - sp, XYZ.BasisZ);
                           }
                           else if (barBot != null && bb.Min.Z > sp.Z - 10.MmToFoot())
                           {
                              rebar.RebarScaleToBox(sp, ep - sp, XYZ.BasisZ);
                           }
                        }
                     }

                     if (rebar != null)
                     {
                        if (arrayLength > 50.MmToFoot())
                        {
                           rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, true, true, true);

                           if (rebar.RebarNormal().DotProduct(span.Direction) < 0)
                           {
                              rebar.SetRebarLayoutAsMaximumSpacing(spacing, arrayLength, false, true, true);
                           }
                        }
                        //Set layout
                        SetRebarSolidUnobscured(new List<Rebar>() { rebar }, AC.ActiveView, span.Mark);
                     }
                  }
               }
            }
         }
         span.ConKe.AddRange(rebars);
         return rebars;
      }

      #endregion Con Ke Thep

      #region Thep Cau Tao

      public static void CreateThepGiaCuongBung(List<SpanModel> spanModels)
      {
         foreach (var span in spanModels)
         {
            var setting = BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting;

            //Đặt Thép Thẳng và đai móc
            var n = BeamRebarRevitData.Instance.BeamRebarViewModel.ThepChongPhinhViewModel.SoLuongLopThepChongPhinh;
            var dis = Math.Round((span.Height / (n + 1)).FootToMm() / 10) * 10.MmToFoot();
            var first = (span.Height - (n - 1) * dis) / 2;
            var sp = span.OriginForStirrupBoxBotCorner;

            var ep = sp.Add(span.TopLine.Direction * (span.TopLeft - span.TopRight).GetLength());
            var direct = span.TopLine.Direction;
            if (span.LeftSupportModel != null)
            {
               if (span.LeftSupportModel.Width - BeamRebarRevitData.Instance.BeamRebarCover < setting.ThepCauTaoGiuaDamModel.LengthGoInColumn)
               {
                  sp = sp.Add(-direct * (span.LeftSupportModel.Width - BeamRebarRevitData.Instance.BeamRebarCover));
               }
               else
               {
                  sp = sp.Add(-direct * setting.ThepCauTaoGiuaDamModel.LengthGoInColumn);
               }
            }
            else
            {
               sp = sp.Add(direct * 50.MmToFoot());
            }

            if (span.RightSupportModel != null)
            {
               if (span.RightSupportModel.Width - BeamRebarRevitData.Instance.BeamRebarCover < setting.ThepCauTaoGiuaDamModel.LengthGoInColumn)
               {
                  ep = ep.Add(direct * (span.RightSupportModel.Width - BeamRebarRevitData.Instance.BeamRebarCover));
               }
               else
               {
                  ep = ep.Add(direct * setting.ThepCauTaoGiuaDamModel.LengthGoInColumn);
               }
            }
            else
            {
               ep = ep.Add(-direct * 50.MmToFoot());
            }

            var normal = BeamRebarRevitData.Instance.BeamModel.Direction.CrossProduct(XYZ.BasisZ);
            var rbt = setting.ThepCauTaoGiuaDamModel.BarDiameter;
            var rbtMoc = setting.ThepCauTaoGiuaDamModel.BarDiameterForBarGoInColumn;

            for (int i = 0; i < n; i++)
            {
               if (i == 0)
               {
                  sp = sp.Add(XYZ.BasisZ * first);
                  ep = ep.Add(XYZ.BasisZ * first);
               }
               else
               {
                  sp = sp.Add(XYZ.BasisZ * dis);
                  ep = ep.Add(XYZ.BasisZ * dis);
               }
               var curves = new List<Curve>() { Line.CreateBound(sp, ep) };
               var rebar = Rebar.CreateFromCurves(AC.Document, RebarStyle.Standard, rbt, null, null, span.Beam,
                   normal, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
               span.ThepGiaCuong.Add(rebar);
               var arrayLength = span.XVecForStirrupBox.GetLength() - 8.MmToFoot() * 2 - rbt.BarDiameter();
               SetLayoutAsFixedNumberThenMoveToCenterSpan(rebar, 2, arrayLength, span);
               SetRebarSolidUnobscured(new List<Rebar>() { rebar }, AC.ActiveView);


               //Create Dai moc
               var number = (int)Math.Floor((ep - sp).GetLength() / setting.ThepCauTaoGiuaDamModel.DistanceForBarGoInColumn);
               var dis1 = ((ep - sp).GetLength() - number * setting.ThepCauTaoGiuaDamModel.DistanceForBarGoInColumn) /
                          2;
               //var arrayLength1 = (ep - sp).GetLength() - 2 * dis1;
               var p = span.OriginForStirrupBoxBotCorner.EditZ(sp.Z + rbt.BarDiameter() / 2 + rbtMoc.BarDiameter() / 2).Add(direct * 50.MmToFoot());
               var rebar1 = Rebar.CreateFromRebarShape(AC.Document, BeamRebarRevitData.Instance.StirrupDaiMoc135x135, rbtMoc, span.Beam, p,
                    span.XVecForStirrupBox, XYZ.BasisZ);
               AC.Document.Regenerate();
               span.DaiGiaCuongs.Add(rebar1);
               var plane = BPlane.CreateByNormalAndOrigin(span.TopLine.Direction, span.TopLeft.Add(direct * 50.MmToFoot()));
               p = p.ProjectOnto(plane);
               rebar1.RebarScaleToBox(p, span.XVecForStirrupBox, XYZ.BasisZ);
               AC.Document.Regenerate();
               rebar1.SetRebarLayoutAsMaximumSpacing(setting.ThepCauTaoGiuaDamModel.Distance, span.TopLine.Length - 100.MmToFoot(), true, true, true);
               var bb = rebar1.get_BoundingBox(AC.ActiveView);
               if (bb != null)
               {
                  var minZ = bb.Min.Z;
                  if (minZ > p.Z - 10.MmToFoot())
                  {
                     //Mirror
                     var mirrorPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, p.Add(XYZ.BasisZ * (rbtMoc.BarDiameter() / 2)));
                     ElementTransformUtils.MirrorElements(AC.Document, new List<ElementId>() { rebar1.Id }, mirrorPlane, false);
                  }
               }
               SetRebarSolidUnobscured(new List<Rebar>() { rebar1 }, AC.ActiveView);
            }
         }

      }

      #endregion Thep Cau Tao

      #region Others

      public static void SetRebarSolidUnobscured(this List<Rebar> rebars, Autodesk.Revit.DB.View view, string partition = null)
      {
         var view3d = view as View3D;
         foreach (var rebar in rebars)
         {
            if (rebar == null)
            {
               continue;
            }
            if (view3d != null)
            {
               RebarUtils.SetSolidRebarIn3DView(view3d, new List<Rebar>() { rebar });
            }
            rebar.SetUnobscuredInView(view, true);
            if (partition != null)
            {
               rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set(partition);
            }
         }
      }

      public static void SetRebarSolidUnobscured(this Rebar rebar, Autodesk.Revit.DB.View view, string partition = null)
      {
         var view3d = view as View3D;
         if (rebar == null)
         {
            return;
         }
         if (view3d != null)
         {
            RebarUtils.SetSolidRebarIn3DView(view3d, new List<Rebar>() { rebar });
         }

         rebar.SetUnobscuredInView(view, true);
         if (partition != null)
         {
            rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set(partition);
         }
      }

      /// <summary>
      /// Neesu khoong cos support sẽ tự động trả về 30
      /// </summary>
      /// <param name="supportModel"></param>
      /// <param name="isTop"></param>
      /// <returns></returns>
      public static double GetNBySupport(SupportModel supportModel, bool isTop = true)
      {
         if (supportModel != null)
         {
            var cat = supportModel.Element.Category.ToBuiltinCategory();
            if (cat == BuiltInCategory.OST_Walls)
            {
               if (isTop)
               {
                  return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForWall
                      .Top;
               }
               return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForWall
                   .Bot;
            }

            if (cat == BuiltInCategory.OST_StructuralFraming)
            {
               if (isTop)
               {
                  return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForBeam
                      .Top;
               }

               return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForBeam
                   .Bot;
            }

            if (cat == BuiltInCategory.OST_StructuralFoundation)
            {
               if (isTop)
               {
                  return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForFoundation
                      .Top;
               }

               return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForFoundation
                   .Bot;
            }

            if (cat == BuiltInCategory.OST_StructuralColumns)
            {
               if (isTop)
               {
                  return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForColumn
                      .Top;
               }
               return BeamRebarRevitData.Instance.BeamRebarViewModel.BeamRebarSettingViewModel.Setting.AnchorRebarSettingForColumn
                   .Bot;
            }
         }

         return 30;
      }

      #endregion Others

      public static double GetZAtLayer(SpanModel spanModel, bool isTop, int layer, RebarBarType diameterType, RebarBarType mainRebar =null)
      {
         var addBarDiameter = diameterType.BarDiameter();
         var stirDiameter = spanModel.StirrupForSpan.BarDiameter.BarDiameter();
         var Cover = BeamRebarRevitData.Instance.BeamRebarCover;
         var z = spanModel.BotElevation + Cover + stirDiameter + addBarDiameter/ 2 ;
         if (isTop)
         {
            z = spanModel.TopElevation - Cover - stirDiameter - addBarDiameter / 2 ;
         }
         double DiameterMain = 0;
         if (mainRebar != null)
         {
            DiameterMain = mainRebar.BarDiameter();
            z = spanModel.BotElevation + Cover + stirDiameter + DiameterMain / 2;
            if (isTop)
            {
                z = spanModel.TopElevation - Cover - stirDiameter - DiameterMain / 2;
            }
         }
            var dis = BeamRebarRevitData.Instance.BeamRebarSettingViewModel.Setting.RebarDistance2Layers ;

         if (isTop)
         {
            if (layer == 2)
            {
               z -= 1 * dis;
            }
            if (layer == 3)
            {
               z -= 2 * dis;
            }
            if (layer == 4)
            {
               z -= 3 * dis;
            }
            if (layer == 5)
            {
               z -= 4 * dis;
            }
         }
         else
         {
            if (layer == 2)
            {
               z += 1 * dis;
            }
            if (layer == 3)
            {
               z += 2 * dis;
            }
            if (layer == 4)
            {
               z += 3 * dis;
            }
            if (layer == 5)
            {
               z += 4 * dis;
            }
         }
         return z;
      }

      public static double SetZeroIfLess(this double number, double n = 0.16404199475)
      {
         if (number.IsEqual(n, 5.MmToFoot()))
         {
            return number;
         }

         return number;
      }

      public static double SetLengthLessThanByCover(this double number, double supportLength, double cover)
      {
         if (number < supportLength - cover)
         {
            return number;
         }

         return (number - cover).RoundMilimet(10, false);
      }
   }
}
