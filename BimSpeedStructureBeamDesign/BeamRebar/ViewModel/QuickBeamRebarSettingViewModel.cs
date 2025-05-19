using System.Collections.ObjectModel ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Data ;
using System.Windows.Media ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI.Selection ;
using BimSpeedStructureBeamDesign.Beam ;
using BimSpeedStructureBeamDesign.BeamRebar.Filter ;
using BimSpeedStructureBeamDesign.BeamRebar.Model ;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel ;
using BimSpeedStructureBeamDesign.BeamRebar.Services ;
using BimSpeedStructureBeamDesign.BeamRebar.View ;
using BimSpeedStructureBeamDesign.Utils.View ;
using BimSpeedUtils ;
using BimSpeedUtils.LanguageUtils ;
using FamilyUtils = BimSpeedStructureBeamDesign.Utils.FamilyUtils ;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
  public class QuickBeamRebarSettingViewModel : ViewModelBase
  {
    private readonly string _path = AC.BimSpeedSettingPath + "\\BeamQuickSettingJson.json" ;
    private List<FamilyInstance> _beamSupports = new() ;
    private List<FamilyInstance> _secondaryBeams = new() ;
    public List<int> Numbers { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    public List<FamilyInstance> Beams { get ; set ; } = new() ;

    public BeamQuickSetting Setting
    {
      get => _setting ;
      set
      {
        if ( Equals( value, _setting ) ) return ;
        _setting = value ;
        OnPropertyChanged() ;
      }
    }

    public BeamRebarRevitData BeamRebarRevitData { get ; set ; }
    public RelayCommand SettingCommand { get ; set ; }
    public RelayCommand BtnSearchSheet { get ; set ; }
    public RelayCommand CreateNowCommand { get ; set ; }
    public RelayCommand CloseCommand { get ; set ; }
    public RelayCommand CmAddToCurrentSheet { get ; set ; }
    public RelayCommand NextCommand { get ; set ; }
    public RelayCommand SelectBeamAsSupportCommand { get ; set ; }
    public RelayCommand SelectSecondaryBeamCommand { get ; set ; }
    public RelayCommand MultiBeamsCommand { get ; set ; }
    public List<FamilyInstance> SelectedBeams { get ; set ; }
    public List<string> SheetNumbers { get ; set ; } = new() ;
    public List<string> SheetNames { get ; set ; } = new() ;
    public FamilySymbol TitleBlock { get ; set ; }
    public List<FamilySymbol> TitleBlocks { get ; set ; } = new() ;
    public SheetFinderView SheetFinderView { get ; set ; }

    public ObservableCollection<ViewSheet> ViewSheets { get ; set ; }

    public ViewSheet SheetSelected { get ; set ; }

    private string _filterString = string.Empty ;

    public string FilterString
    {
      get => _filterString ;
      set
      {
        _filterString = value ;
        CollectionView collectionView =
          (CollectionView) CollectionViewSource.GetDefaultView( ViewSheets ) ;
        collectionView.Refresh() ;
      }
    }

    public bool IsUseBeamDataInput { get ; set ; } = false ;

    public Dictionary<string, ViewSheet> dicSheets = new() ;
    private BeamQuickSetting _setting ;

    public QuickBeamRebarSettingViewModel( List<FamilyInstance> beams )
    {
      BeamRebarRevitData = BeamRebarRevitData.Instance ;
      BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel = this ;
      SelectedBeams = beams ;
      MultiBeamsCommand = new RelayCommand( MultiBeams ) ;
      GetData() ;
    }

    private void ShowFormSelectSheet()
    {
      SheetFinderView = new SheetFinderView() { DataContext = this } ;
      SheetFinderView.ShowDialog() ;
    }

    private void AddSheetNumberName()
    {
      Setting.SheetNumber = SheetSelected.SheetNumber ;
      Setting.SheetName = SheetSelected.Name ;

      OnPropertyChanged( nameof( Setting.SheetNumber ) ) ;
      OnPropertyChanged( nameof( Setting.SheetName ) ) ;
      SheetFinderView.Close() ;
    }

    private void CreateNow( object obj )
    {
      if ( obj is Window w ) {
        w.Close() ;
      }

      var beamModel = new BeamModel( SelectedBeams, _beamSupports, _secondaryBeams ) ;
      var maxHeight = beamModel.SpanModels.Max( x => x.TopElevation ) -
                      beamModel.SpanModels.Min( x => x.BotElevation ) ;
      var heightMin = beamModel.SpanModels.Min( x => x.Height ) ;

      var viewModel = new BeamRebarViewModel( beamModel ) ;
      BeamRebarRevitData.Instance.BeamRebarViewModel = viewModel ;
      var view = new BeamRebarView2 { DataContext = viewModel } ;
      BeamRebarRevitData.Instance.BeamRebarView2 = view ;
      BeamRebarRevitData.Grid = view.Grid ;
      Service.GetScale( ( beamModel.Origin - beamModel.Last ).GetLength(), maxHeight, heightMin ) ;
      var beamUiModel = beamModel.ConvertToBeamUiModel() ;
      BeamRebarRevitData.BeamModel = beamModel ;
      BeamRebarRevitData.BeamUiModel = beamUiModel ;
      foreach ( var spanUiModel in beamUiModel.SpanUiModels ) {
        spanUiModel.DrawLine() ;
      }

      foreach ( var supportUiModel in beamUiModel.SupportUiModels ) {
        supportUiModel.DrawLine() ;
      }

      viewModel.QuickGetRebar( this ) ;
      CreateRebarAndSheet( viewModel ) ;
      if ( Beams.Count > 0 ) {
        RunForOthersBeam() ;
      }
    }

    private void SelectBeamAsSupport( object obj )
    {
      if ( obj is Window w ) {
        w.Hide() ;
        var supports = BeamRebarCommonService.GetBeamAsSupports( SelectedBeams ) ;
        var selectedRfs = supports.Select( y => new Reference( y ) ).ToList() ;
        try {
          AC.UiDoc.RefreshActiveView() ;
          var rfs = AC.Selection.PickObjects( ObjectType.Element, new BeamSelectionFilter(),
            "Select Beam As Support...", selectedRfs ) ;
          _beamSupports = rfs.Select( y => y.ToElement() ).Cast<FamilyInstance>()
            .Where( BeamRebarCommonService.CheckBeamStraightAndHorizontal ).ToList() ;
          AC.UiDoc.RefreshActiveView() ;
        }
        catch {
          //
        }

        w.ShowDialog() ;
      }
    }

    private void SelectSecondaryBeam( object obj )
    {
      if ( obj is Window w ) {
        w.Hide() ;
        var supports = BeamRebarCommonService.GetBeamAsSupports( SelectedBeams ) ;
        var selectedRfs = supports.Select( y => new Reference( y ) ).ToList() ;
        try {
          AC.UiDoc.RefreshActiveView() ;
          var rfs = AC.Selection.PickObjects( ObjectType.Element, new BeamColumnSelectionFilter(),
            "Select Beam/Column As Secondary beams...", selectedRfs ) ;
          _secondaryBeams = rfs.Select( y => y.ToElement() ).Cast<FamilyInstance>().Where( x =>
            BeamRebarCommonService.CheckBeamStraightAndHorizontal( x ) ||
            x.Category.ToBuiltinCategory() == BuiltInCategory.OST_StructuralColumns ).ToList() ;
          AC.UiDoc.RefreshActiveView() ;
        }
        catch {
          //
        }

        w.ShowDialog() ;
      }
    }

    private void Next( object obj )
    {
      if ( obj is Window w ) {
        w.Hide() ;
        var beamModel = new BeamModel( SelectedBeams, _beamSupports, _secondaryBeams ) ;
        var maxHeight = ( beamModel.SpanModels.Max( x => x.TopElevation ) -
                          beamModel.SpanModels.Min( x => x.BotElevation ) ) ;

        var heightMin = beamModel.SpanModels.Min( x => x.Height ) ;

        var viewModel = new BeamRebarViewModel( beamModel ) ;
        BeamRebarRevitData.Instance.QuickBeamRebarSettingViewModel = this ;
        BeamRebarRevitData.Instance.BeamRebarViewModel = viewModel ;
        var view = new BeamRebarView2 { DataContext = viewModel } ;
        BeamRebarRevitData.Instance.BeamRebarView2 = view ;
        BeamRebarRevitData.Grid = view.Grid ;
        Service.GetScale( ( beamModel.Origin - beamModel.Last ).GetLength(), maxHeight,
          heightMin ) ;
        Service.GetBreakLineY( beamModel, BeamRebarRevitData.Grid ) ;
        var beamUiModel = beamModel.ConvertToBeamUiModel() ;
        BeamRebarRevitData.BeamModel = beamModel ;
        BeamRebarRevitData.BeamUiModel = beamUiModel ;
        foreach ( var spanUiModel in beamUiModel.SpanUiModels ) {
          spanUiModel.DrawLine() ;
          foreach ( var secondaryBeamUiModel in spanUiModel.SecondaryBeamUiModels ) {
            secondaryBeamUiModel.DrawLine() ;
          }
        }

        foreach ( var supportUiModel in beamUiModel.SupportUiModels ) {
          supportUiModel.DrawLine() ;
        }

        //DrawRectangleRegion dimension
        BeamRebarUiServices.DrawDimension() ;
        DrawTenNhip() ;
        viewModel.QuickGetRebar( this ) ;


        BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection() ;
        viewModel.CurrentPageViewModel = viewModel.PageViewModels[ 0 ] ;
        if ( view.ShowDialog() == true ) {
          CreateRebarAndSheet( viewModel ) ;
          w.Close() ;
        }
        else {
          if ( BeamRebarRevitData.IsBack ) {
            BeamRebarRevitData.IsBack = false ;
            w.ShowDialog() ;
          }
        }
      }
    }

    private void SettingRebar()
    {
      BeamRebarRevitData.BeamRebarSettingViewModel.BeamRebarRevitData = BeamRebarRevitData ;
      var view =
        new BeamRebarViewSetting() { DataContext = BeamRebarRevitData.BeamRebarSettingViewModel } ;
      view.ShowDialog() ;
    }

    private void GetData()
    {
      var settingJson = JsonUtils.GetSettingFromFile<BeamQuickSettingJson>( _path ) ??
                        new BeamQuickSettingJson
                        {
                          TopMainBarDiameter = 20,
                          HasMainTopBar = true,
                          HasMainBotBar = true,
                          BotMainBarDiameter = 20,
                          HasBot1 = true,
                          BotAdditionBarDiameter1 = 18,
                          HasBot2 = true,
                          BotAdditionBarDiameter2 = 18,
                          HasTop1 = true,
                          TopAdditionalBarDiameter1 = 18,
                          HasTop2 = true,
                          TopAdditionalBarDiameter2 = 18,
                          HasStirrup = true,
                          A1 = 100,
                          A2 = 200,
                          KieuBoTriThepDai = 2,
                          StirrupBarDiameter = 8,
                          L = 1000.MmToFoot()
                        } ;

      Setting = new BeamQuickSetting( this )
      {
        AddTop1 = settingJson.AddTop1,
        IsCreateSheet = settingJson.IsCreateSheet,
        MainTop1 = settingJson.MainTop1,
        AddTop2 = settingJson.AddTop2,
        AddBot2 = settingJson.AddBot2,
        MainBot1 = settingJson.MainBot1,
        AddBot1 = settingJson.AddBot1,
        TopMainBarDiameter =
          settingJson.TopMainBarDiameter.GetRebarBarTypeByNumber( findBestMatchIfNull: true ),
        HasMainTopBar = settingJson.HasMainTopBar,
        HasMainBotBar = settingJson.HasMainBotBar,
        BotMainBarDiameter =
          settingJson.BotMainBarDiameter.GetRebarBarTypeByNumber( findBestMatchIfNull: true ),
        HasBot1 = settingJson.HasBot1,
        BotAdditionBarDiameter1 =
          settingJson.BotAdditionBarDiameter1
            .GetRebarBarTypeByNumber( findBestMatchIfNull: true ),
        HasBot2 = settingJson.HasBot2,
        BotAdditionBarDiameter2 =
          settingJson.BotAdditionBarDiameter2
            .GetRebarBarTypeByNumber( findBestMatchIfNull: true ),
        HasTop1 = settingJson.HasTop1,
        TopAdditionalBarDiameter1 =
          settingJson.TopAdditionalBarDiameter1.GetRebarBarTypeByNumber(
            findBestMatchIfNull: true ),
        HasTop2 = settingJson.HasTop2,
        TopAdditionalBarDiameter2 =
          settingJson.TopAdditionalBarDiameter2.GetRebarBarTypeByNumber(
            findBestMatchIfNull: true ),
        HasStirrup = settingJson.HasStirrup,
        A1 = settingJson.A1,
        A2 = settingJson.A2,
        KieuBoTriThepDai = settingJson.KieuBoTriThepDai,
        StirrupBarDiameter =
          settingJson.StirrupBarDiameter.GetRebarBarTypeByNumber( findBestMatchIfNull: true ),
        L = settingJson.L,
        SheetName = settingJson.SheetName,
        SheetNumber = settingJson.SheetNumber
      } ;

      CreateNowCommand = new RelayCommand( CreateNow ) ;
      NextCommand = new RelayCommand( Next ) ;
      SettingCommand = new RelayCommand( _ => SettingRebar() ) ;
      SelectBeamAsSupportCommand = new RelayCommand( SelectBeamAsSupport ) ;
      SelectSecondaryBeamCommand = new RelayCommand( SelectSecondaryBeam ) ;
      BtnSearchSheet = new RelayCommand( _ => ShowFormSelectSheet() ) ;
      CmAddToCurrentSheet = new RelayCommand( _ => AddSheetNumberName() ) ;
      CloseCommand = new RelayCommand( x =>
      {
        if(x is QuickBeamRebarView view) view.Close();
      } ) ;

      ViewSheets = new ObservableCollection<ViewSheet>( new FilteredElementCollector( AC.Document )
        .OfClass( typeof( ViewSheet ) ).Cast<ViewSheet>().OrderBy( x => x.SheetNumber ).ToList() ) ;

      CollectionView collectionView =
        (CollectionView) CollectionViewSource.GetDefaultView( ViewSheets ) ;

      collectionView.Filter = UserFilter ;

      foreach ( var viewSheet in ViewSheets ) {
        if ( dicSheets.ContainsKey( viewSheet.SheetNumber ) == false ) {
          dicSheets.Add( viewSheet.SheetNumber, viewSheet ) ;
          SheetNumbers.Add( viewSheet.SheetNumber ) ;
          SheetNames.Add( viewSheet.Name ) ;
        }
      }

      SheetNames = SheetNames.Distinct().OrderBy( x => x ).ToList() ;

      TitleBlocks = new FilteredElementCollector( AC.Document ).OfClass( typeof( FamilySymbol ) )
        .OfCategory( BuiltInCategory.OST_TitleBlocks ).Cast<FamilySymbol>().OrderBy( x => x.Name )
        .ToList() ;

      if ( TitleBlocks.Any() ) {
        FamilyUtils.ActiveSymbol( TitleBlocks ) ;

        TitleBlock = TitleBlocks.FirstOrDefault() ;
      }
    }

    private bool UserFilter( object item )
    {
      bool flag = false ;

      ViewSheet vs = item as ViewSheet ;

      if ( string.IsNullOrEmpty( _filterString ) ) {
        flag = true ;
      }
      else {
        flag = vs.Name.IsContainFilter( _filterString ) ||
               vs.SheetNumber.IsContainFilter( _filterString ) ;
      }

      return flag ;
    }

    private void CreateRebarAndSheet( BeamRebarViewModel viewModel )
    {
      ViewSheet vs = null ;

      using ( var tx = new Transaction( AC.Document, "Beam Rebar" ) ) {
        tx.Start() ;
        var options = tx.GetFailureHandlingOptions() ;
        options.SetFailuresPreprocessor( new WarningDiscard() ) ;
        tx.SetFailureHandlingOptions( options ) ;
        var direct = "UnDefine" ;

        if ( viewModel.BeamModel.Direction.IsParallel( XYZ.BasisY ) ) {
          direct = "Y" ;
        }

        if ( viewModel.BeamModel.Direction.IsParallel( XYZ.BasisX ) ) {
          direct = "X" ;
        }

        foreach ( var mainRebar in viewModel.MainBarInBottomViewModel.MainRebars ) {
          var rebars = BeamRebarServices.CreateMainBar( mainRebar ) ;

          RebarExtensibleStorage.SetSchemaForMainBar( rebars, mainRebar ) ;
        }

        foreach ( var mainRebar in viewModel.MainBarInTopViewModel.MainRebars ) {
          var rebars = BeamRebarServices.CreateMainBar( mainRebar, -1 ) ;
          RebarExtensibleStorage.SetSchemaForMainBar( rebars, mainRebar ) ;
        }

        foreach ( var bar in viewModel.AdditionalBottomBarViewModel.AllBars ) {
          try {
            var rebars = BeamRebarServices.CreateAdditionalBottomBar( bar ) ;
            RebarExtensibleStorage.SetSchemaForAdditionalBotBar( rebars, bar ) ;
          }
          catch ( Exception e ) {
            AC.Log( e.Message ) ;
          }
        }

        foreach ( var bar in viewModel.AdditionalTopBarViewModel.AllBars ) {
          var rebars = BeamRebarServices.CreateAdditionalTopBar( bar ) ;
          RebarExtensibleStorage.SetSchemaForAdditionalTopBar( rebars, bar ) ;
        }


        foreach ( var spanModel in viewModel.BeamModel.SpanModels ) {
          BeamRebarServices.CreateStirrupForSpan( spanModel ) ;
        }

        BeamRebarServices.CreateThepGiaCuongBung( viewModel.BeamModel.SpanModels ) ;

        AC.Document.Regenerate() ;

        var mark = BeamRebarRevitData.Instance.BeamModel.SpanModels.Select( x => x.Mark )
          .FirstOrDefault( x => string.IsNullOrEmpty( x ) == false ) ;
        foreach ( var familyInstance in SelectedBeams ) {
          var rebars = familyInstance.GetRebarFromHost( AC.Document ) ;
          BeamRebarServices.SetRebarsDirection( rebars, direct, mark ) ;
        }

        //RebarServices.CreateThepGiaCuongTop();
        AC.Document.Regenerate() ;
        tx.Commit() ;
      }

      using ( var tx = new Transaction( AC.Document, "Create Sheet" ) ) {
        tx.Start() ;
        if ( Setting.IsCreateSheet ) {
          var horizontalSection =
            BeamDrawingService.DetailingHorizontalSection( BeamRebarRevitData.BeamModel ) ;
          var crossSections =
            BeamDrawingService.DetailingCrossSection( BeamRebarRevitData.BeamModel,
              horizontalSection.ViewSection ) ;

          //CreateCutZones(horizontalSection.ViewSection);
          vs = BeamDrawingService.AddToSheet( dicSheets, Setting.SheetNumber, Setting.SheetName,
            horizontalSection.ViewSection, crossSections,
            BeamRebarRevitData.BeamRebarSettingViewModel.BeamDrawingSettingViewModel
              .BeamDrawingSetting.BeamSheetSetting.TitleBlock ) ;
        }

        tx.Commit() ;
      }

      if ( Setting.IsCreateSheet ) {
        try {
          var rs = "QuickBeamRebarSettingViewModel01_MESSAGE".NotificatonQuestion( this ) ;
          if ( rs == MessageBoxResult.Yes ) {
            AC.UiDoc.ActiveView = vs ;
          }
        }
        catch ( Exception e ) {
          MessageBox.Show( e.Message ) ;
        }
      }

      var setting = new BeamQuickSettingJson()
      {
        TopMainBarDiameter = (int) Setting.TopMainBarDiameter.BarDiameter().FootToMm(),
        HasMainTopBar = Setting.HasMainTopBar,
        HasMainBotBar = Setting.HasMainBotBar,
        AddTop1 = Setting.AddTop1,
        MainTop1 = Setting.MainTop1,
        AddTop2 = Setting.AddTop2,
        MainBot1 = Setting.MainBot1,
        AddBot1 = Setting.AddBot1,
        AddBot2 = Setting.AddBot2,
        BotMainBarDiameter = (int) Setting.BotMainBarDiameter.BarDiameter().FootToMm(),
        HasBot1 = Setting.HasBot1,
        BotAdditionBarDiameter1 = (int) Setting.BotAdditionBarDiameter1.BarDiameter().FootToMm(),
        HasBot2 = Setting.HasBot2,
        BotAdditionBarDiameter2 = (int) Setting.BotAdditionBarDiameter2.BarDiameter().FootToMm(),
        HasTop1 = Setting.HasTop1,
        TopAdditionalBarDiameter1 =
          (int) Setting.TopAdditionalBarDiameter1.BarDiameter().FootToMm(),
        HasTop2 = Setting.HasTop2,
        TopAdditionalBarDiameter2 =
          (int) Setting.TopAdditionalBarDiameter2.BarDiameter().FootToMm(),
        HasStirrup = Setting.HasStirrup,
        A1 = Setting.A1,
        A2 = Setting.A2,
        KieuBoTriThepDai = Setting.KieuBoTriThepDai,
        StirrupBarDiameter = (int) Setting.StirrupBarDiameter.BarDiameter().FootToMm(),
        L = Setting.L,
        IsCreateSheet = Setting.IsCreateSheet,
        SheetName = Setting.SheetName,
        SheetNumber = Setting.SheetNumber
      } ;

      JsonUtils.SaveSettingToFile( setting, _path ) ;
    }

    private void CreateRebarShopAndSheet( BeamRebarViewModel viewModel )
    {
      ViewSheet vs = null ;
      using ( var tx = new Transaction( AC.Document, "Beam Rebar" ) ) {
        tx.Start() ;
        var options = tx.GetFailureHandlingOptions() ;
        options.SetFailuresPreprocessor( new WarningDiscard() ) ;
        tx.SetFailureHandlingOptions( options ) ;
        var direct = "UnDefine" ;
        if ( viewModel.BeamModel.Direction.IsParallel( XYZ.BasisY ) ) {
          direct = "Y" ;
        }

        if ( viewModel.BeamModel.Direction.IsParallel( XYZ.BasisX ) ) {
          direct = "X" ;
        }

        foreach ( var mainRebar in viewModel.MainBarInBottomViewModel.MainRebars ) {
          var rebars = BeamRebarServices.CreateMainBar( mainRebar ) ;

          RebarExtensibleStorage.SetSchemaForMainBar( rebars, mainRebar ) ;
        }

        foreach ( var mainRebar in viewModel.MainBarInTopViewModel.MainRebars ) {
          var rebars = BeamRebarServices.CreateMainBar( mainRebar, -1 ) ;
          RebarExtensibleStorage.SetSchemaForMainBar( rebars, mainRebar ) ;
        }

        foreach ( var bar in viewModel.AdditionalBottomBarViewModel.AllBars ) {
          try {
            var rebars = BeamRebarServices.CreateAdditionalBottomBar( bar ) ;
            RebarExtensibleStorage.SetSchemaForAdditionalBotBar( rebars, bar ) ;
          }
          catch ( Exception e ) {
            AC.Log( e.Message ) ;
          }
        }

        foreach ( var bar in viewModel.AdditionalTopBarViewModel.AllBars ) {
          var rebars = BeamRebarServices.CreateAdditionalTopBar( bar ) ;
          RebarExtensibleStorage.SetSchemaForAdditionalTopBar( rebars, bar ) ;
        }

        foreach ( var spanModel in viewModel.BeamModel.SpanModels ) {
          BeamRebarServices.CreateStirrupForSpan( spanModel ) ;
        }

        BeamRebarServices.CreateThepGiaCuongBung( viewModel.BeamModel.SpanModels ) ;

        AC.Document.Regenerate() ;

        var mark = BeamRebarRevitData.Instance.BeamModel.SpanModels.Select( x => x.Mark )
          .FirstOrDefault( x => string.IsNullOrEmpty( x ) == false ) ;
        foreach ( var familyInstance in SelectedBeams ) {
          var rebars = familyInstance.GetRebarFromHost( AC.Document ) ;
          BeamRebarServices.SetRebarsDirection( rebars, direct, mark ) ;
        }

        //RebarServices.CreateThepGiaCuongTop();
        AC.Document.Regenerate() ;
        tx.Commit() ;
      }

      using ( var tx = new Transaction( AC.Document, "Create Sheet" ) ) {
        tx.Start() ;
        if ( Setting.IsCreateSheet ) {
          var horizontalSection =
            BeamDrawingService.DetailingHorizontalSection( BeamRebarRevitData.BeamModel ) ;
          var crossSections =
            BeamDrawingService.DetailingCrossSection( BeamRebarRevitData.BeamModel,
              horizontalSection.ViewSection ) ;
          CreateCutZones( horizontalSection.ViewSection ) ;

          vs = BeamDrawingService.AddToSheet( dicSheets, Setting.SheetNumber, Setting.SheetName,
            horizontalSection.ViewSection, crossSections,
            BeamRebarRevitData.BeamRebarSettingViewModel.BeamDrawingSettingViewModel
              .BeamDrawingSetting.BeamSheetSetting.TitleBlock ) ;
        }

        tx.Commit() ;
      }

      if ( Setting.IsCreateSheet ) {
        try {
          var rs = "QuickBeamRebarSettingViewModel01_MESSAGE".NotificationSuccess( this ) ;
          if ( rs == MessageBoxResult.Yes ) {
            AC.UiDoc.ActiveView = vs ;
          }
        }
        catch ( Exception e ) {
          MessageBox.Show( e.Message ) ;
          //
        }
      }

      JsonUtils.SaveSettingToFile( Setting, _path ) ;
    }

    private void CreateCutZones( Autodesk.Revit.DB.View view )
    {
      var hatchTypes = new FilteredElementCollector( AC.Document )
        .OfClass( typeof( FilledRegionType ) ).Cast<FilledRegionType>().ToList() ;
      var hatchType = hatchTypes.FirstOrDefault( x => x.Name == "Hatch shop thép dầm" ) ;
      if ( hatchType == null ) {
        hatchType = hatchTypes.First() ;
      }

      foreach ( var spanModel in BeamRebarRevitData.BeamModel.SpanModels ) {
        //Create Top Cut zones
        var topEle = spanModel.TopElevation ;
        var midEle = ( spanModel.TopElevation + spanModel.BotElevation ) * 0.5 + 10.MmToFoot() ;
        var pA = spanModel.TopLine
          .Evaluate( BeamRebarRevitData.BeamShopSetting.TopShopSpanFactor, true ).EditZ( topEle ) ;
        var pB = spanModel.TopLine
          .Evaluate( 1 - BeamRebarRevitData.BeamShopSetting.TopShopSpanFactor, true )
          .EditZ( topEle ) ;
        var pC = pB.EditZ( midEle ) ;
        var pD = pA.EditZ( midEle ) ;

        var cl = CurveLoopByPoints( new List<XYZ>() { pA, pB, pC, pD } ) ;

        FilledRegion.Create( AC.Document, hatchType.Id, view.Id, new List<CurveLoop>() { cl } ) ;
      }

      for ( var index = 0 ; index < BeamRebarRevitData.BeamModel.SpanModels.Count ; index++ ) {
        var spanModelCurrent = BeamRebarRevitData.BeamModel.SpanModels[ index ] ;

        var botEle = spanModelCurrent.BotElevation ;
        var midEle = ( spanModelCurrent.TopElevation + spanModelCurrent.BotElevation ) * 0.5 -
                     10.MmToFoot() ;

        if ( index == 0 ) {
          var pA = spanModelCurrent.TopLine.SP().EditZ( midEle ) ;
          if ( spanModelCurrent.LeftSupportModel != null ) {
            pA = pA.Add( spanModelCurrent.Direction * -spanModelCurrent.LeftSupportModel.Width /
                         2 ) ;
          }

          var pB = spanModelCurrent.TopLine
            .Evaluate( BeamRebarRevitData.BeamShopSetting.BotShopSpanFactor, true )
            .EditZ( midEle ) ;
          var pC = pB.EditZ( botEle ) ;
          var pD = pA.EditZ( botEle ) ;
          var cl = CurveLoopByPoints( new List<XYZ>() { pA, pB, pC, pD } ) ;

          FilledRegion.Create( AC.Document, hatchType.Id, view.Id, new List<CurveLoop>() { cl } ) ;
        }

        if ( index == BeamRebarRevitData.BeamModel.SpanModels.Count - 1 ) {
          var pA = spanModelCurrent.TopLine.EP().EditZ( midEle ) ;
          if ( spanModelCurrent.RightSupportModel != null ) {
            pA = pA.Add( spanModelCurrent.Direction * spanModelCurrent.RightSupportModel.Width /
                         2 ) ;
          }

          var pB = spanModelCurrent.TopLine
            .Evaluate( 1 - BeamRebarRevitData.BeamShopSetting.BotShopSpanFactor, true )
            .EditZ( midEle ) ;
          var pC = pB.EditZ( botEle ) ;
          var pD = pA.EditZ( botEle ) ;

          var cl = CurveLoopByPoints( new List<XYZ>() { pA, pB, pC, pD } ) ;

          FilledRegion.Create( AC.Document, hatchType.Id, view.Id, new List<CurveLoop>() { cl } ) ;
        }
        else {
          var spanModelNext = BeamRebarRevitData.BeamModel.SpanModels[ index + 1 ] ;

          var pA = spanModelCurrent.TopLine
            .Evaluate( 1 - BeamRebarRevitData.BeamShopSetting.BotShopSpanFactor, true )
            .EditZ( midEle ) ;
          if ( spanModelCurrent.RightSupportModel != null ) {
            pA = pA.Add( spanModelCurrent.Direction * spanModelCurrent.RightSupportModel.Width /
                         2 ) ;
          }

          var pB = spanModelNext.TopLine
            .Evaluate( BeamRebarRevitData.BeamShopSetting.BotShopSpanFactor, true )
            .EditZ( midEle ) ;
          var pC = pB.EditZ( botEle ) ;
          var pD = pA.EditZ( botEle ) ;

          var cl = CurveLoopByPoints( new List<XYZ>() { pA, pB, pC, pD } ) ;

          FilledRegion.Create( AC.Document, hatchType.Id, view.Id, new List<CurveLoop>() { cl } ) ;
        }

        //Create Bot Cut zones
      }
    }

    private CurveLoop CurveLoopByPoints( List<XYZ> points )
    {
      var cl = new CurveLoop() ;
      for ( int i = 0 ; i < points.Count - 1 ; i++ ) {
        var s = points[ i ] ;
        var e = points[ i + 1 ] ;
        cl.Append( s.CreateLine( e ) ) ;
      }

      cl.Append( points.Last().CreateLine( points.First() ) ) ;

      return cl ;
    }

    private void DrawTenNhip()
    {
      var z = BeamRebarRevitData.Instance.BeamModel.SpanModels.Min( x => x.BotElevation ) ;
      foreach ( var beamModelSpanModel in BeamRebarRevitData.Instance.BeamModel.SpanModels ) {
        var p = beamModelSpanModel.BotLine.Midpoint().EditZ( z ).ConvertToMainViewPoint() ;
        p = new System.Windows.Point( p.X, p.Y + 80 ) ;
        BeamRebarRevitData.Instance.Grid.Children.Add( DrawText( p,
          $"Span {beamModelSpanModel.Index}", 16 ) ) ;
      }
    }

    private Label DrawText( System.Windows.Point p, string s, int size = 12 )
    {
      //Convert to window point
      var tbMid = new Label() { Content = s, FontSize = size, Foreground = Brushes.IndianRed } ;
      tbMid.SetValue( CenterOnPoint.CenterPointProperty, p ) ;
      return tbMid ;
    }

    private void MultiBeams( object obj )
    {
      if ( obj is Window w ) {
        w.Hide() ;
        try {
          var rfs = AC.Selection.PickObjects( ObjectType.Element, new BeamSelectionFilter(),
            "Vẽ thép cho nhiều dầm, lanh tô khác nhau..." ) ;
          var selectedIds =
            SelectedBeams.Select( x => int.Parse( x.Id.GetElementIdValue().ToString() ) ).ToList() ;
          Beams = rfs.Select( x => x.ToElement() )
            .Where( x => selectedIds.Contains( int.Parse( x.Id.GetElementIdValue().ToString() ) ) == false )
            .Cast<FamilyInstance>().ToList() ;
        }
        catch {
          //
        }

        w.ShowDialog() ;
      }
    }

    private void RunForOthersBeam()
    {
      foreach ( var beam in Beams ) {
        SelectedBeams = new List<FamilyInstance>() { beam } ;
        _secondaryBeams = new List<FamilyInstance>() ;
        var beamModel = new BeamModel( SelectedBeams, _beamSupports, _secondaryBeams ) ;
        var maxHeight = ( beamModel.SpanModels.Max( x => x.TopElevation ) -
                          beamModel.SpanModels.Min( x => x.BotElevation ) ) ;
        var heightMin = beamModel.SpanModels.Min( x => x.Height ) ;

        var viewModel = new BeamRebarViewModel( beamModel ) ;
        BeamRebarRevitData.Instance.BeamRebarViewModel = viewModel ;
        var view = new BeamRebarView2 { DataContext = viewModel } ;
        BeamRebarRevitData.Instance.BeamRebarView2 = view ;
        BeamRebarRevitData.Grid = view.Grid ;
        Service.GetScale( ( beamModel.Origin - beamModel.Last ).GetLength(), maxHeight,
          heightMin ) ;
        var beamUiModel = beamModel.ConvertToBeamUiModel() ;
        BeamRebarRevitData.BeamModel = beamModel ;
        BeamRebarRevitData.BeamUiModel = beamUiModel ;
        foreach ( var spanUiModel in beamUiModel.SpanUiModels ) {
          spanUiModel.DrawLine() ;
        }

        foreach ( var supportUiModel in beamUiModel.SupportUiModels ) {
          supportUiModel.DrawLine() ;
        }

        viewModel.QuickGetRebar( this ) ;

        CreateRebarAndSheet( viewModel ) ;
      }
    }
  }


  public class BeamQuickSetting : ViewModelBase
  {
    public bool IsCreateSheet { get ; set ; }
    public RebarBarType TopMainBarDiameter { get ; set ; }
    public RebarBarType BotMainBarDiameter { get ; set ; }
    public RebarBarType TopAdditionalBarDiameter1 { get ; set ; }
    public RebarBarType BotAdditionBarDiameter1 { get ; set ; }
    public RebarBarType TopAdditionalBarDiameter2 { get ; set ; }
    public RebarBarType BotAdditionBarDiameter2 { get ; set ; }
    public double LengthNeedAdditionalBotBar { get ; set ; } = 2000.MmToFoot() ;
    public bool HasMainTopBar { get ; set ; }
    public bool HasMainBotBar { get ; set ; }

    public bool HasTop1 { get ; set ; }

    public bool HasBot1 { get ; set ; }

    public bool HasTop2 { get ; set ; }

    public bool HasBot2 { get ; set ; }
    public int MainTop1 { get ; set ; } = 2 ;
    public int AddTop1 { get ; set ; } = 2 ;
    public int AddTop2 { get ; set ; } = 2 ;

    public int MainBot1 { get ; set ; } = 2 ;
    public int AddBot1 { get ; set ; } = 2 ;
    public int AddBot2 { get ; set ; } = 2 ;

    public bool HasStirrup { get ; set ; }

    public int KieuBoTriThepDai
    {
      get => _kieuBoTriThepDai ;
      set
      {
        if ( value == _kieuBoTriThepDai ) return ;
        _kieuBoTriThepDai = value ;
        OnPropertyChanged() ;
      }
    }

    public RebarBarType StirrupBarDiameter { get ; set ; }
    public int A1 { get ; set ; }
    public int A2 { get ; set ; }
    public double L { get ; set ; }

    private string sheetNumber = "" ;
    private string sheetName ;

    public string SheetName
    {
      get => sheetName ;
      set
      {
        sheetName = value ;
        OnPropertyChanged() ;
      }
    }

    private bool isEnableSheetName ;

    public bool IsEnableSheetName
    {
      get => isEnableSheetName ;
      set
      {
        isEnableSheetName = value ;
        OnPropertyChanged() ;
      }
    }

    public string SheetNumber
    {
      get => sheetNumber ;
      set
      {
        sheetNumber = value ;


        if ( ! string.IsNullOrEmpty( sheetNumber ) &&
             dicSheets.TryGetValue( sheetNumber, out var sheet ) ) {
          sheetName = sheet.Name ;
          isEnableSheetName = false ;
        }
        else {
          isEnableSheetName = true ;
        }


        OnPropertyChanged() ;
        OnPropertyChanged( nameof( SheetName ) ) ;
        OnPropertyChanged( nameof( IsEnableSheetName ) ) ;
      }
    }

    public Dictionary<string, ViewSheet> dicSheets = new() ;
    private int _kieuBoTriThepDai ;

    public BeamQuickSetting( QuickBeamRebarSettingViewModel viewModel )
    {
      dicSheets = viewModel.dicSheets ;
    }
  }
}