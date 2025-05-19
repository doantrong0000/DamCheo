using System.Globalization ;
using System.Windows.Controls ;
using System.Windows.Shapes ;
using Autodesk.Revit.DB ;
using BimSpeedStructureBeamDesign.BeamRebar.Model ;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel ;
using BimSpeedStructureBeamDesign.BeamRebar.Services ;
using BimSpeedUtils ;
using BimSpeedUtils.LanguageUtils ;
using Visibility = System.Windows.Visibility ;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
  public class StirrupTabViewModel : ViewModelBase, IPageViewModel
  {
    private bool _showSection = false ;

    public bool IsShowSection
    {
      get => _showSection ;
      set
      {
        _showSection = value ;
        OnPropertyChanged() ;
      }
    }

    private SpanModel selectedSpanModel ;
    private bool _isSelected = false ;

    public bool IsSelected
    {
      get => _isSelected ;
      set
      {
        _isSelected = value ;
        OnPropertyChanged() ;
      }
    }

    private bool drawMainStirrup = true ;

    public List<SpanModel> SpanModels { get ; set ; }

    public string Name => "BEAM_REBAR_SIDE_BAR_STIRRUP".GetValueInResources( this ) ;
    public string Image => "Images/Tabs/STIRRUP.png" ;

    public SpanModel SelectedSpanModel
    {
      get => selectedSpanModel ;
      set
      {
        try {
          selectedSpanModel = value ;
          if ( selectedSpanModel != null ) {
            BeamRebarUiServices.SelectSpanByIndex( selectedSpanModel.Index ) ;
          }
          else {
            BeamRebarUiServices.SelectSpanByIndex( -1 ) ;
          }

          OnPropertyChanged() ;
        }
        catch ( Exception ) {
          //
        }
      }
    }

    public BeamRebarRevitData BeamRebarRevitData { get ; set ; }

    public RelayCommand SetAllSpansCommand { get ; set ; }
    public RelayCommand SetRemainingSpansCommand { get ; set ; }
    public RelayCommand ToggleDaiChinhCommand { get ; set ; }
    public Path Path { get ; set ; } = new() ;
    public List<Path> DimensionPaths { get ; set ; } = new() ;
    public List<Label> Labels { get ; set ; } = new() ;

    public bool DrawMainStirrup
    {
      get => drawMainStirrup ;
      set
      {
        drawMainStirrup = value ;
        OnPropertyChanged() ;
      }
    }

    public StirrupTabViewModel( BeamModel beamModel )
    {
      SpanModels = beamModel?.SpanModels ;
      SelectedSpanModel = SpanModels?.FirstOrDefault() ;
      BeamRebarRevitData = BeamRebarRevitData.Instance ;
      SetAllSpansCommand = new RelayCommand( x => SetForAllSpans() ) ;
      SetRemainingSpansCommand = new RelayCommand( x => SetForRemaining() ) ;
      ToggleDaiChinhCommand = new RelayCommand( x => ToggleDaiChinh() ) ;
    }

    private void ToggleDaiChinh()
    {
      drawMainStirrup = ! drawMainStirrup ;
      foreach ( var spanModel in BeamRebarRevitData.Instance.BeamRebarViewModel.SpanModels ) {
        spanModel.SectionUiModel1.ToggleMainStirrup( drawMainStirrup ) ;
        spanModel.SectionUiModel2.ToggleMainStirrup( drawMainStirrup ) ;
        spanModel.SectionUiModel3.ToggleMainStirrup( drawMainStirrup ) ;
      }
    }

    public void DimensionStirrups()
    {
      DimensionPaths.ForEach( x => BeamRebarRevitData.Instance.Grid.Children.Remove( x ) ) ;
      foreach ( var label in Labels ) {
        BeamRebarRevitData.Instance.Grid.Children.Remove( label ) ;
      }

      var yLDimLine = new XYZ( 0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 300.MmToFoot() )
        .ConvertToMainViewPoint().Y ;
      var yEnd = new XYZ( 0, 0, BeamRebarRevitData.Instance.BeamModel.ZTop + 50.MmToFoot() )
        .ConvertToMainViewPoint().Y ;

      DimensionPaths.Clear() ;
      Labels.Clear() ;

      foreach ( var spanModel in SpanModels ) {
        var xs = new List<double>() ;
        var listSpacing = new List<string>() ;
        var listNumber = new List<string>() ;
        var stirrupForSpan = spanModel.StirrupForSpan ;
        var vector = spanModel.Direction.Normalize() ;
        var left = spanModel.TopLine.SP().ConvertToMainViewPoint() ;
        var right = spanModel.TopLine.EP().ConvertToMainViewPoint() ;
        xs.Add( left.X ) ;
        var midSpacing = Math.Round( stirrupForSpan.SpacingAtMid.FootToMm(), 0 ) ;
        var endSpacing = Math.Round( stirrupForSpan.SpacingAtEnd.FootToMm(), 0 ) ;

        if ( stirrupForSpan.KieuPhanBoThepDai != 1 ) {
          var end1 = spanModel.TopLine.SP().Add( vector * stirrupForSpan.End1Length )
            .ConvertToMainViewPoint() ;
          var end2 = spanModel.TopLine.EP().Add( -vector * stirrupForSpan.End2Length )
            .ConvertToMainViewPoint() ;
          xs.Add( end1.X ) ;
          xs.Add( end2.X ) ;

          listSpacing.Add( $"{stirrupForSpan.BarDiameter.Name}@{endSpacing}" ) ;
          listSpacing.Add( $"{stirrupForSpan.BarDiameter.Name}@{midSpacing}" ) ;
          listSpacing.Add( $"{stirrupForSpan.BarDiameter.Name}@{endSpacing}" ) ;

          listNumber.Add( Math.Round( stirrupForSpan.End1Length.FootToMm() )
            .ToString( CultureInfo.InvariantCulture ) ) ;
          var mid = spanModel.Length - stirrupForSpan.End1Length - stirrupForSpan.End2Length ;
          listNumber.Add( Math.Round( mid.FootToMm() ).ToString( CultureInfo.InvariantCulture ) ) ;
          listNumber.Add( Math.Round( stirrupForSpan.End2Length.FootToMm() )
            .ToString( CultureInfo.InvariantCulture ) ) ;
        }
        else {
          listSpacing.Add( $"{stirrupForSpan.BarDiameter.Name}@{endSpacing}" ) ;
          listNumber.Add( Math.Round( spanModel.Length.FootToMm() )
            .ToString( CultureInfo.InvariantCulture ) ) ;
        }

        xs.Add( right.X ) ;
        DimensionPaths.Add( BeamRebarUiServices.DrawHorizontalDimension( xs, yEnd, yLDimLine,
          out var labels, listSpacing, isDimOverall: false ) ) ;


        Labels.AddRange( labels ) ;
      }

      DimensionPaths.ForEach( x => BeamRebarRevitData.Instance.Grid.Children.Add( x ) ) ;
      Labels.ForEach( x => BeamRebarRevitData.Instance.Grid.Children.Add( x ) ) ;
    }

    public void ShowHide( bool isShow = true )
    {
      var visibility = isShow ? Visibility.Visible : Visibility.Hidden ;

      DimensionPaths.ForEach( x => x.Visibility = visibility ) ;
      Labels.ForEach( x => x.Visibility = visibility ) ;
    }

    private void SetForAllSpans()
    {
      foreach ( var spanModel in SpanModels ) {
        if ( spanModel.Index == selectedSpanModel.Index ) {
          continue ;
        }

        spanModel.StirrupForSpan.SpacingAtEnd = selectedSpanModel.StirrupForSpan.SpacingAtEnd ;
        spanModel.StirrupForSpan.SpacingAtMid = selectedSpanModel.StirrupForSpan.SpacingAtMid ;
        spanModel.StirrupForSpan.BarDiameter = selectedSpanModel.StirrupForSpan.BarDiameter ;
        if ( spanModel.StirrupForSpan.Max == selectedSpanModel.StirrupForSpan.Max ) {
          spanModel.StirrupForSpan.StirrupModels.Clear() ;
          foreach ( var stirrupModel in selectedSpanModel.StirrupForSpan.StirrupModels ) {
            if ( stirrupModel.IsDaiMoc ) {
              spanModel.StirrupForSpan.StirrupModels.Add(
                new StirrupModel( stirrupModel.StartIndex ) { Start = stirrupModel.Start } ) ;
            }
            else {
              spanModel.StirrupForSpan.StirrupModels.Add(
                new StirrupModel( stirrupModel.StartIndex, stirrupModel.EndIndex )
                {
                  Start = stirrupModel.Start, End = stirrupModel.End
                } ) ;
            }
          }
        }
      }
    }

    private void SetForRemaining()
    {
      if ( SelectedSpanModel != null ) {
        foreach ( var spanModel in SpanModels ) {
          if ( spanModel.Index > SelectedSpanModel.Index ) {
            var e1 = spanModel.StirrupForSpan.End1Length ;
            var e2 = spanModel.StirrupForSpan.End2Length ;
            spanModel.StirrupForSpan = SelectedSpanModel.StirrupForSpan.Clone() as StirrupForSpan ;
            if ( spanModel.StirrupForSpan != null ) {
              spanModel.StirrupForSpan.End1Length = e1 ;
              spanModel.StirrupForSpan.End2Length = e2 ;
            }
          }
        }
      }
    }

    public void QuickGetStirrup( QuickBeamRebarSettingViewModel vm )
    {
      var setting = vm.Setting ;
      if ( setting.HasStirrup ) {
        #region Code cu

        // if (vm.IsUseBeamDataInput && false)
        // {
        //    //foreach (var span in RevitData.BeamModel.SpanModels)
        //    //{
        //    //    span.StirrupForSpan.BarDiameter = span.StirrupMid.Diameter;
        //    //    span.StirrupForSpan.KieuPhanBoThepDai = setting.KieuBoTriThepDai;
        //    //    span.StirrupForSpan.SpacingAtEnd = span.StirrupEnd.Spacing.MmToFoot();
        //    //    span.StirrupForSpan.SpacingAtMid = span.StirrupEnd.Spacing.MmToFoot();
        //    //    if (span.Length < setting.L || span.LeftSupportModel == null || span.RightSupportModel == null)
        //    //    {
        //    //        span.StirrupForSpan.KieuPhanBoThepDai = Define.BoTriDeu;
        //    //    }
        //    //}
        // }
        // else
        // {
        //    foreach (var span in BeamRebarRevitData.BeamModel.SpanModels)
        //    {
        //       span.StirrupForSpan.BarDiameter = setting.StirrupBarDiameter;
        //       span.StirrupForSpan.KieuPhanBoThepDai = setting.KieuBoTriThepDai;
        //       span.StirrupForSpan.SpacingAtEnd = setting.A1.MmToFoot();
        //       span.StirrupForSpan.SpacingAtMid = setting.A2.MmToFoot();
        //       if (span.Length < setting.L || span.LeftSupportModel == null || span.RightSupportModel == null)
        //       {
        //          span.StirrupForSpan.KieuPhanBoThepDai = 1;
        //       }
        //    }
        // }

        #endregion

        foreach ( var span in BeamRebarRevitData.BeamModel.SpanModels ) {
          span.StirrupForSpan.BarDiameter = setting.StirrupBarDiameter ;
          span.StirrupForSpan.KieuPhanBoThepDai = setting.KieuBoTriThepDai ;
          span.StirrupForSpan.SpacingAtEnd = setting.A1.MmToFoot() ;
          span.StirrupForSpan.SpacingAtMid = setting.A2.MmToFoot() ;
          if ( span.Length < setting.L || span.LeftSupportModel == null ||
               span.RightSupportModel == null ) {
            span.StirrupForSpan.KieuPhanBoThepDai = 1 ;
          }
        }
      }
    }

    public void CreateStirrupForLintel()
    {
      ToggleDaiChinh() ;

      foreach ( var spanModel in SpanModels ) {
        spanModel.StirrupForSpan.AddStirrup( 1 ) ;
      }
    }
  }
}