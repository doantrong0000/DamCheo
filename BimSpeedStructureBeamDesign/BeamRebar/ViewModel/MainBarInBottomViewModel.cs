using System.Windows.Controls;
using Autodesk.Revit.DB.Structure;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public class MainBarInBottomViewModel : ViewModelBase, IPageViewModel
   {
      private bool _showSection = false;
      public bool IsShowSection
      {
         get => _showSection;
         set
         {
            _showSection = value;
            OnPropertyChanged();
         }
      }
      private MainRebar selectedMainBar;
      private bool _isSelected = false;
      public bool IsSelected
      {
         get => _isSelected;
         set
         {
            _isSelected = value;
            OnPropertyChanged();
         }
      }
      public List<MainRebar> MainRebars { get; set; } = new List<MainRebar>();

      public MainRebar SelectedMainBar
      {
         get => selectedMainBar;
         set
         {
            if (selectedMainBar != null)
            {
               selectedMainBar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
            }
            foreach (var mainRebar in MainRebars)
            {
               mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
               mainRebar.DimensionUiModel.ShowHideDim(false);
            }
            selectedMainBar = value;

            if (selectedMainBar != null)
            {
               selectedMainBar.DimensionUiModel.ShowSelectedBarInPink();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(MainRebars));
         }
      }

      public BeamRebarRevitData BeamRebarRevitData { get; set; } = BeamRebarRevitData.Instance;
      public RelayCommand AddRebarCommand { get; set; }
      public RelayCommand NextRebarCommand { get; set; }
      public RelayCommand DeleteRebarCommand { get; set; }
      public RelayCommand DeleteAllCommand { get; set; }
      public BeamModel BeamModel { get; set; }

      public MainBarInBottomViewModel()
      {
         GetData();
      }

      private void GetData()
      {
         AddRebarCommand = new RelayCommand(x => AddRebar());
         NextRebarCommand = new RelayCommand(x => DrawNextRebar());
         DeleteRebarCommand = new RelayCommand(DeleteRebar);
         DeleteAllCommand = new RelayCommand(x => DeleteAllRebar());
      }

      private void AddRebar()
      {
         if (SelectedMainBar != null)
         {
            if (MainRebars.Contains(SelectedMainBar) == false)
            {
               MainRebars.Add(SelectedMainBar);
               SelectedMainBar = null;
            }
         }
         else
         {
            SelectedMainBar = GetNextPreviewRebar();
            if (SelectedMainBar != null)
            {
               if (MainRebars.Contains(SelectedMainBar) == false)
               {
                  MainRebars.Add(SelectedMainBar);
                  SelectedMainBar = null;
               }
            }
         }
         MainRebars = new List<MainRebar>(MainRebars.OrderBy(x => x.Start));
         OnPropertyChanged(nameof(MainRebars));
      }

      public void DrawNextRebar()
      {
         SelectedMainBar = GetNextPreviewRebar();
      }

      private void DeleteRebar(object obj)
      {
         if (obj is ListView lv)
         {
            var selecteds = lv.SelectedItems.Cast<MainRebar>();
            foreach (var mainRebar in selecteds)
            {
               mainRebar.DimensionUiModel.RemoveFromUi();
               MainRebars.Remove(mainRebar);
               selectedMainBar = null;
            }
            MainRebars = new List<MainRebar>(MainRebars.OrderBy(x => x.Start));
            BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
            OnPropertyChanged(nameof(MainRebars));
            OnPropertyChanged(nameof(SelectedMainBar));
         }
      }

      private void DeleteAllRebar()
      {
         foreach (var mainRebar in MainRebars)
         {
            mainRebar.DimensionUiModel.RemoveFromUi();
         }

         if (selectedMainBar != null)
         {
            selectedMainBar.DimensionUiModel.RemoveFromUi();
         }

         selectedMainBar = null;
         MainRebars = new List<MainRebar>();
         BeamRebarRevitData.Instance.BeamRebarViewModel.SelectedSpanModel.DrawSection();
         OnPropertyChanged(nameof(MainRebars));
         OnPropertyChanged(nameof(SelectedMainBar));
      }

      public MainRebar GetNextPreviewRebar(int layer = -1)
      {
         for (int l = 1; l < 6; l++)
         {
            if (layer != -1 && layer != l)
            {
               continue;
            }

            for (int j = 0; j < BeamModel.SpanModels.Count; j++)
            {
               if (IsSpanNeedRebar(BeamModel.SpanModels[j], MainRebars.ToList(), l))
               {
                  var number = j + 1;
                  for (int i = j; i < BeamModel.SpanModels.Count - 1; i++)
                  {
                     var currentSpan = BeamModel.SpanModels[i];
                     var nextSpan = BeamModel.SpanModels[i + 1];
                     if (BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan) && IsSpanNeedRebar(nextSpan, MainRebars.ToList(), l))
                     {
                        number++;
                     }
                     else
                     {
                        break;
                     }
                  }
                  var mb = new MainRebar(j, number, l);
                  return mb;
               }
            }
         }
         return null;
      }

      public bool IsSpanNeedRebar(SpanModel spanModel, List<MainRebar> mainRebars, int layer)
      {
         if (mainRebars.Count == 0)
         {
            return true;
         }
         foreach (var mainRebar in mainRebars.Where(x => x.Layer == layer))
         {
            if (spanModel.Index >= mainRebar.Start && spanModel.Index < mainRebar.End)
            {
               return false;
            }
         }
         return true;
      }

      public bool IsEnoughBar()
      {
         var bar = GetNextPreviewRebar(1);
         if (bar == null)
         {
            return true;
         }

         bar.DimensionUiModel.RemoveFromUi();
         return false;
      }

      public void QuickGetRebar(QuickBeamRebarSettingViewModel vm)
      {
         DeleteAllRebar();
         var setting = vm.Setting;

         if (vm.IsUseBeamDataInput)
         {
            var spans = BeamModel.SpanModels.OrderBy(x => x.Index).ToList();
            var groupMainTopBar = new List<List<RebarAtPositionOfSpan>>();

            var listMainBotBar = new List<RebarAtPositionOfSpan>();


            foreach (var spanModel in spans)
            {

               //main bot bar  
               if (!listMainBotBar.Any())
               {
                  listMainBotBar.Add(spanModel.RebarAtPositionOfSpanStart);
                  listMainBotBar.Add(spanModel.RebarAtPositionOfSpanEnd);
                  continue;
               }

               if (spanModel.RebarAtPositionOfSpanStart.MainBot1.IsSame(listMainBotBar.Last().MainBot1))
               {
                  listMainBotBar.Add(spanModel.RebarAtPositionOfSpanStart);
                  listMainBotBar.Add(spanModel.RebarAtPositionOfSpanEnd);
               }
               else
               {
                  groupMainTopBar.Add(new List<RebarAtPositionOfSpan>(listMainBotBar));
                  listMainBotBar = new List<RebarAtPositionOfSpan>
                  {
                     spanModel.RebarAtPositionOfSpanStart,
                     spanModel.RebarAtPositionOfSpanEnd
                  };
               }


            }

            //last rebar data
            if (listMainBotBar.Any())
            {
               groupMainTopBar.Add(new List<RebarAtPositionOfSpan>(listMainBotBar));
            }


            foreach (var list in groupMainTopBar)
            {
               var first = list.First();
               var last = list.Last();

               var mains = GetRebarByInputParam(first.MainTop1.Diameter, first.Index, last.Index, 1);

               MainRebars.AddRange(mains);
            }

         }
         else
         {
            if (setting.HasMainTopBar)
            {
               for (int i = 0; i < 20; i++)
               {
                  var bar = GetNextPreviewRebar(1);
                  if (bar != null)
                  {
                     bar.BarDiameter = setting.BotMainBarDiameter;
                     MainRebars.Add(bar);
                  }
                  else
                  {
                     break;
                  }
               }
            }
         }


         foreach (var mainRebar in MainRebars)
         {
            mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
         }
      }


      private List<MainRebar> GetRebarByInputParam(RebarBarType diameterType, int start, int end, int layer = 1)
      {
         var rebars = new List<MainRebar>();

         var j = start;
         while (j < end)
         {
            var number = j + 1;
            for (int i = j; i < BeamModel.SpanModels.Count - 1; i++)
            {
               var currentSpan = BeamModel.SpanModels[i];
               var nextSpan = BeamModel.SpanModels[i + 1];
               if (number < end && BeamRebarCommonService.CheckRebarCanGoThrough2Spans(currentSpan, nextSpan, false) && IsSpanNeedRebar(nextSpan, MainRebars.ToList(), layer))
               {
                  number++;
               }
               else
               {
                  break;
               }
            }

            var r = new MainRebar(j, number, layer, false)
            {
               BarDiameter = diameterType
            };

            rebars.Add(r);

            j = number;
         }

         return rebars;
      }

      public string Name => "BEAM_REBAR_SIDE_BAR_MAINBOTBAR".GetValueInResources(this);
      public string Image => "Images/Tabs/TAB-MAIN-BOT.png";

   }
}