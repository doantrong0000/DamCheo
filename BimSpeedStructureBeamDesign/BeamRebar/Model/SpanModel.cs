using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using Line = Autodesk.Revit.DB.Line;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
    public class SpanModel : ViewModelBase
    {
        public List<SecondaryBeamModel> SecondaryBeamModels { get; set; } = new();

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                RebarAtPositionOfSpanStart.Index = value;
                RebarAtPositionOfSpanMid.Index = value;
                RebarAtPositionOfSpanEnd.Index = value + 1;
            }
        }

        public XYZ TopLeft { get; set; }
        public XYZ TopRight { get; set; }
        public XYZ BotLeft { get; set; }
        public XYZ BotRight { get; set; }
        public Line TopLine { get; set; }
        public Line BotLine { get; set; }
        public FamilyInstance Beam { get; set; }
        public double TopElevation { get; set; }
        public double BotElevation { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public SupportModel LeftSupportModel { get; set; }
        public SupportModel RightSupportModel { get; set; }
        public BeamGeometry BeamGeometry { get; set; }
        public XYZ OriginForStirrupBoxBotCorner { get; set; }
        public XYZ OriginForStirrupBoxTopCorner { get; set; }
        public XYZ XVecForStirrupBox { get; set; }
        public XYZ YVecForStirrupBox { get; set; }
        public StirrupForSpan StirrupForSpan { get; set; }
        public XYZ TopLeftCorner { get; set; }
        public XYZ TopRightCorner { get; set; }
        public XYZ BotLeftCorner { get; set; }
        public XYZ BotRightCorner { get; set; }

        public string TypeNumberOfRebarByWidth
        {
            get => typeNumberOfRebarByWidth;
            set
            {
                if (typeNumberOfRebarByWidth != value)
                {
                    typeNumberOfRebarByWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CoTheNhanThepBottomAtLeft { get; set; } = false;
        public bool CoTheNhanThepBottomAtRight { get; set; } = false;
        public bool CoTheNhanThepTopAtLeft { get; set; } = false;
        public bool CoTheNhanThepTopAtRight { get; set; } = false;
        public double KhoangNhanThepDiVaoSupportLeft { get; set; } = 0.0;
        public double KhoangNhanThepDiVaoSupportRight { get; set; } = 0.0;
        public string Mark { get; set; }
        public XYZ Direction { get; set; }
        public XYZ Normal { get; set; }
        public List<Rebar> DaiGiaCuongs { get; set; } = new();
        public List<Rebar> ThepGiaCuong { get; set; } = new();
        public List<Rebar> ConKe { get; set; } = new();

        public SectionUiModel SectionUiModel1
        {
            get => sectionUiModel1;
            set
            {
                sectionUiModel1 = value;
                OnPropertyChanged();
            }
        }

        public SectionUiModel SectionUiModel2
        {
            get => sectionUiModel2;
            set
            {
                sectionUiModel2 = value;
                OnPropertyChanged();
            }
        }

        public SectionUiModel SectionUiModel3
        {
            get => sectionUiModel3;
            set
            {
                sectionUiModel3 = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Shapes.Line sectionPath1;
        private System.Windows.Shapes.Line sectionPath2;
        private System.Windows.Shapes.Line sectionPath3;


        public RebarAtPositionOfSpan RebarAtPositionOfSpanStart { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanMid { get; set; }
        public RebarAtPositionOfSpan RebarAtPositionOfSpanEnd { get; set; }
        public RebarQuantityDiameter StirrupEnd { get; set; }
        public RebarQuantityDiameter StirrupMid { get; set; }
        public SpanModel(Line line, BeamGeometry beamGeometry)
        {
            BeamGeometry = beamGeometry;
            TopElevation = beamGeometry.TopElevation;
            BotElevation = beamGeometry.BotElevation;

            Beam = beamGeometry.Beam;
            TopLeft = line.SP().EditZ(TopElevation);
            TopRight = line.EP().EditZ(TopElevation);
            TopLine = Line.CreateBound(TopLeft, TopRight);
            BotLeft = TopLeft.EditZ(BotElevation);
            BotRight = TopRight.EditZ(BotElevation);
            BotLine = Line.CreateBound(BotLeft, BotRight);
            Length = line.Length;
            Height = beamGeometry.Height;
            Width = beamGeometry.Width;
            YVecForStirrupBox = XYZ.BasisZ * (Height - 2 * BeamRebarRevitData.Instance.BeamRebarCover);
            var xVec = XYZ.BasisZ.CrossProduct(line.Direction).Normalize();
            XVecForStirrupBox = xVec * (Width - 2 * BeamRebarRevitData.Instance.BeamRebarCover);


            OriginForStirrupBoxBotCorner = TopLeft.Add(XYZ.BasisZ * (BeamRebarRevitData.Instance.BeamRebarCover - Height))
                .Add(-xVec * (Width / 2 - BeamRebarRevitData.Instance.BeamRebarCover));


            OriginForStirrupBoxTopCorner = TopLeft
               .Add(XYZ.BasisZ * -(BeamRebarRevitData.Instance.BeamRebarCover))
               .Add(-xVec * (Width / 2 - BeamRebarRevitData.Instance.BeamRebarCover));


            StirrupForSpan = new StirrupForSpan { TypeOfStirrups = BeamRebarServices.GetTypeOfStirrupsByWidth(Width), SpanModel = this };

            TopLeftCorner = TopLeft.Add(-xVec * Width / 2);
            TopRightCorner = TopLeft.Add(-xVec * Width / 2);

            BotLeftCorner = BotLeft.Add(-xVec * Width / 2);
            BotRightCorner = BotLeft.Add(xVec * Width / 2);
            Mark = Beam.GetParameterValueAsString(BuiltInParameter.DOOR_NUMBER);
            Direction = TopLine.Direction;
            Normal = XYZ.BasisZ.CrossProduct(Direction).Normalize();
            SectionUiModel1 = new SectionUiModel(1, this);
            SectionUiModel2 = new SectionUiModel(2, this);
            SectionUiModel3 = new SectionUiModel(3, this);


            RebarAtPositionOfSpanStart = beamGeometry.RebarAtPositionOfSpanStart.Clone();
            RebarAtPositionOfSpanMid = beamGeometry.RebarAtPositionOfSpanMid.Clone();
            RebarAtPositionOfSpanEnd = beamGeometry.RebarAtPositionOfSpanEnd.Clone();



            StirrupMid = beamGeometry.StirrupMid;
            StirrupEnd = beamGeometry.StirrupEnd;
        }

        private SectionUiModel sectionUiModel1;
        private SectionUiModel sectionUiModel2;
        private SectionUiModel sectionUiModel3;
        public string typeNumberOfRebarByWidth;
        private int _index;

        public void DrawSection()
        {
            if (BeamRebarRevitData.Instance.BeamRebarView2 != null)
            {
                SectionUiModel1.Draw();
                SectionUiModel2.Draw();
                SectionUiModel3.Draw();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="windowOrigin">Điểm này tương đương điểm Top Left ở trong revit</param>
        /// <returns></returns>
        public System.Windows.Point ConvertToWindowPointForSection(XYZ p, System.Windows.Point windowOrigin, double s)
        {
            var dx = (p - TopLeftCorner).DotProduct(XVecForStirrupBox.Normalize()) * s;
            var dy = (p - TopLeftCorner).DotProduct(YVecForStirrupBox.Normalize()) * s;
            return new System.Windows.Point(windowOrigin.X + dx, windowOrigin.Y - dy);
        }

        public void DrawSectionSymbol()
        {
            if (BeamRebarRevitData.Instance.BeamModel != null)
            {
                BeamRebarRevitData.Instance.Grid.Children.Remove(sectionPath1);
                BeamRebarRevitData.Instance.Grid.Children.Remove(sectionPath2);
                BeamRebarRevitData.Instance.Grid.Children.Remove(sectionPath3);
                var line = BotLine;

                var p25 = line.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri1, true).EditZ(TopElevation);
                var p50 = line.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri2, true).EditZ(TopElevation);
                var p75 = line.Evaluate(BeamRebarRevitData.Instance.BeamRebarSettingViewModel.BeamDrawingSettingViewModel.BeamDrawingSetting.BeamSectionSetting.ViTri3, true).EditZ(TopElevation);

                var p25Top = p25.Add(XYZ.BasisZ * 80.MmToFoot()).ConvertToMainViewPoint();

                var p25Bot = p25.EditZ(BotElevation).Add(XYZ.BasisZ * -80.MmToFoot()).ConvertToMainViewPoint();

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p25Top.EditX(p25Top.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 1).ToString()));

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p25Bot.EditX(p25Bot.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 1).ToString()));

                sectionPath1 = BeamRebarUiServices.CreateLineDash(p25Top, p25Bot);
                BeamRebarRevitData.Instance.Grid.Children.Add(sectionPath1);

                var p50Top = p50.Add(XYZ.BasisZ * 80.MmToFoot()).ConvertToMainViewPoint();

                var p50Bot = p50.EditZ(BotElevation).Add(XYZ.BasisZ * -80.MmToFoot()).ConvertToMainViewPoint();

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p50Top.EditX(p50Top.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 2).ToString()));

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p50Bot.EditX(p50Bot.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 2).ToString()));

                sectionPath2 = BeamRebarUiServices.CreateLineDash(p50Top, p50Bot);
                BeamRebarRevitData.Instance.Grid.Children.Add(sectionPath2);

                var p75Top = p75.Add(XYZ.BasisZ * 80.MmToFoot()).ConvertToMainViewPoint();

                var p75Bot = p75.EditZ(BotElevation).Add(XYZ.BasisZ * -80.MmToFoot()).ConvertToMainViewPoint();

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p75Top.EditX(p75Top.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 3).ToString()));

                BeamRebarRevitData.Instance.Grid.Children.Add(DrawText(p75Bot.EditX(p75Bot.X - 40.MmToFoot() * BeamRebarRevitData.XScale), (Index * 3 + 3).ToString()));

                sectionPath3 = BeamRebarUiServices.CreateLineDash(p75Top, p75Bot);
                BeamRebarRevitData.Instance.Grid.Children.Add(sectionPath3);

                SectionUiModel1.SectionName = (Index * 3 + 1) + "-" + (Index * 3 + 1);
                SectionUiModel2.SectionName = (Index * 3 + 2) + "-" + (Index * 3 + 2);
                SectionUiModel3.SectionName = (Index * 3 + 3) + "-" + (Index * 3 + 3);
            }
        }

        private Label DrawText(System.Windows.Point p, string s)
        {
            //Convert to window point
            var tbMid = new Label { Content = s, FontSize = 12, Foreground = Brushes.IndianRed };
            tbMid.SetValue(CenterOnPoint.CenterPointProperty, p);
            return tbMid;
        }

        public RebarQuantityByWidth GetRebarQuantityByWidth()
        {
            return BeamRebarRevitData.Instance.BeamRebarViewModel.RebarQuantityManager?.RebarQuantityByWidths.FirstOrDefault(x =>
                x.Width.IsEqual(Width, 5.MmToFoot()));
        }
    }

    public class RebarAtPositionOfSpan
    {
        public int Index { get; set; }
        /// <summary>
        /// main rebar at top
        /// </summary>
        public RebarQuantityDiameter MainTop1 { get; set; }
        /// <summary>
        /// addtional bar at layer 2
        /// </summary>
        public RebarQuantityDiameter AddTop2 { get; set; }
        public RebarQuantityDiameter AddTop3 { get; set; }
        /// <summary>
        /// additional bar at top layer
        /// </summary>
        public RebarQuantityDiameter AddTop1 { get; set; }

        public RebarQuantityDiameter MainBot1 { get; set; }
        public RebarQuantityDiameter AddBot2 { get; set; }
        public RebarQuantityDiameter AddBot3 { get; set; }
        public RebarQuantityDiameter AddBot1 { get; set; }
        public RebarPositionTypeInSpan PositionType { get; set; } = RebarPositionTypeInSpan.Start;

        public RebarAtPositionOfSpan()
        {

        }
        public RebarAtPositionOfSpan(string first)
        {
            var numbers = GetNumbers(first);
            if (numbers.Count == 16)
            {
                MainTop1 = new RebarQuantityDiameter
                {
                    Quantity = numbers[0],
                    Diameter = numbers[1].GetRebarBarTypeByNumber(),
                };

                AddTop1 = new RebarQuantityDiameter
                {
                    Quantity = numbers[2],
                    Diameter = numbers[3].GetRebarBarTypeByNumber(),
                };

                AddTop2 = new RebarQuantityDiameter
                {
                    Quantity = numbers[4],
                    Diameter = numbers[5].GetRebarBarTypeByNumber(),
                };

                AddTop3 = new RebarQuantityDiameter
                {
                    Quantity = numbers[6],
                    Diameter = numbers[7].GetRebarBarTypeByNumber(),
                };


                MainBot1 = new RebarQuantityDiameter
                {
                    Quantity = numbers[8],
                    Diameter = numbers[9].GetRebarBarTypeByNumber(),
                };

                AddBot1 = new RebarQuantityDiameter
                {
                    Quantity = numbers[10],
                    Diameter = numbers[11].GetRebarBarTypeByNumber(),
                };

                AddBot2 = new RebarQuantityDiameter
                {
                    Quantity = numbers[12],
                    Diameter = numbers[13].GetRebarBarTypeByNumber(),
                };

                AddBot3 = new RebarQuantityDiameter
                {
                    Quantity = numbers[14],
                    Diameter = numbers[15].GetRebarBarTypeByNumber(),
                };
            }
        }

        private List<int> GetNumbers(string s)
        {
            var list = new List<int>();
            var numberStrings = s.Split('#', '$');
            foreach (var numberString in numberStrings)
            {
                int.TryParse(numberString, out var n);
                list.Add(n);
            }

            return list;
        }

        public RebarAtPositionOfSpan Clone()
        {
            return new()
            {
                MainTop1 = MainTop1,
                AddTop1 = AddTop1,
                AddTop2 = AddTop2,
                AddTop3 = AddTop3,


                MainBot1 = MainBot1,
                AddBot1 = AddBot1,
                AddBot2 = AddBot2,
                AddBot3 = AddBot3,
                PositionType = PositionType
            };
        }
    }

    public class RebarQuantityDiameter
    {
        public int Quantity { get; set; }
        public int Spacing { get; set; }
        public RebarBarType Diameter { get; set; }

        public RebarQuantityDiameter()
        {

        }

        public RebarQuantityDiameter Clone()
        {
            return new RebarQuantityDiameter
            {
                Quantity = this.Quantity,
                Spacing = Spacing,
                Diameter = Diameter
            };
        }

        public bool IsSame(RebarQuantityDiameter other)
        {
            return Quantity == other.Quantity && Diameter == other.Diameter && Spacing == other.Spacing;
        }
    }

    public enum RebarPositionTypeInSpan
    {
        Start = 1,
        Mid = 2,
        End = 3

    }
}