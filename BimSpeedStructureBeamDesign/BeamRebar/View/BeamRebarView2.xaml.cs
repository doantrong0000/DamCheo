using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BimSpeedStructureBeamDesign.BeamRebar.Model;
using BimSpeedStructureBeamDesign.BeamRebar.Services;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
   /// <summary>
   /// Interaction logic for BeamRebarView2.xaml
   /// </summary>
   public partial class BeamRebarView2 : Window
   {
      public BeamRebarView2()
      {
         InitializeComponent();

         this.SetLanguageProviderForResourceDictionary(Resources);
      }


      private int pickedSpanIndex = 0;
      private int rebarLayer = 1;
      private bool isTop = true;

      private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
      {
         BeamRebarViewModel viewModel = BeamRebarRevitData.Instance.BeamRebarViewModel;
         var x = Mouse.GetPosition(Grid).X;
         if (viewModel.CurrentPageViewModel is StirrupTabViewModel || viewModel.CurrentPageViewModel is GeometryViewModel)
         {
            var spanIndex = BeamRebarUiServices.SelectSpanByX(x);
            viewModel.StirrupTabViewModel.SelectedSpanModel =
                viewModel.StirrupTabViewModel.SpanModels.FirstOrDefault(y => y.Index == spanIndex);

         }
         else if (viewModel.CurrentPageViewModel is MainBarInBottomViewModel)
         {
            //Highlight thep
            var spanIndex = BeamRebarUiServices.SelectSpanByX(x, false);
            var vm = viewModel.MainBarInBottomViewModel;
            if (vm.MainRebars.Contains(vm.SelectedMainBar) == false && vm.SelectedMainBar != null)
            {
               vm.SelectedMainBar.DimensionUiModel.RemoveFromUi();
            }
            vm.SelectedMainBar = null;

            foreach (var mainRebar in vm.MainRebars)
            {
               if (spanIndex >= mainRebar.Start && spanIndex < mainRebar.End)
               {
                  mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarPreviewColor;
                  vm.SelectedMainBar = mainRebar;
               }
               else
               {
                  mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
               }
            }

         }
         else if (viewModel.CurrentPageViewModel is MainBarInTopViewModel)
         {
            //Highlight thep
            var spanIndex = BeamRebarUiServices.SelectSpanByX(x, false);

            var vm = viewModel.MainBarInTopViewModel;
            if (vm.MainRebars.Contains(vm.SelectedMainBar) == false && vm.SelectedMainBar != null)
            {
               vm.SelectedMainBar.DimensionUiModel.RemoveFromUi();
            }
            vm.SelectedMainBar = null;
            foreach (var mainRebar in vm.MainRebars)
            {
               if (spanIndex >= mainRebar.Start && spanIndex < mainRebar.End)
               {
                  mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarPreviewColor;
                  vm.SelectedMainBar = mainRebar;
               }
               else
               {
                  mainRebar.DimensionUiModel.RebarPath.Stroke = Define.RebarColor;
               }
            }
         }
         else if (viewModel.CurrentPageViewModel is AdditionalBottomBarViewModel)
         {
            //Highlight thep
            var spanIndex = BeamRebarUiServices.SelectSpanByX(x, false);
            var vm = viewModel.AdditionalBottomBarViewModel;

            if (vm.AllBars.Contains(vm.SelectedBar) == false && vm.SelectedBar != null)
            {
               vm.SelectedBar.DimensionUiModel.RemoveFromUi();
               vm.SelectedBar = null;
            }

            vm.AllBars.ForEach(a => a.DimensionUiModel.RebarPath.Stroke = Define.RebarColor);

            //Hignlight rebar
            if (pickedSpanIndex == spanIndex)
            {
               rebarLayer = IncreaseRebarLayer(rebarLayer);
            }
            else
            {
               pickedSpanIndex = spanIndex;
               rebarLayer = 1;
            }
            var barsInSelectedSpan = vm.AllBars.Where(a => a.Start == spanIndex).OrderBy(a => a.Layer.Layer).ToList();
            if (barsInSelectedSpan.Any(o => o.Layer.Layer == rebarLayer && o.Layer.IsTop == isTop))
            {
               //HighLight
               vm.SelectedBar = barsInSelectedSpan.FirstOrDefault(o => o.Layer.Layer == rebarLayer && o.Layer.IsTop == isTop);
            }
            else
            {
               //Preview
               vm.SelectedBar = vm.GetNextPreviewRebar(spanIndex, rebarLayer, isTop);
               BeamRebarCommonService.ArrangeRebar();
            }
            if (vm.SelectedBar != null) vm.SelectedBar.DimensionUiModel.RebarPath.Stroke = Define.RebarPreviewColor;
         }
         else if (viewModel.CurrentPageViewModel is AdditionalTopBarViewModel)
         {
            //Highlight thep
            var spanIndex = BeamRebarUiServices.SelectSpanByX(x, false);
            var vm = viewModel.AdditionalTopBarViewModel;
            if (vm.AllBars.Contains(vm.SelectedBar) == false && vm.SelectedBar != null)
            {
               vm.SelectedBar.DimensionUiModel.RemoveFromUi();
               vm.SelectedBar = null;
            }
            vm.AllBars.ForEach(a => a.SetPathsColor(Define.RebarColor));

            //Hignlight rebarvm.SelectedBar.DimensionUiModel
            if (pickedSpanIndex == spanIndex)
            {
               rebarLayer = IncreaseRebarLayer(rebarLayer);
            }
            else
            {
               pickedSpanIndex = spanIndex;
               rebarLayer = 1;
            }
            var barsInSelectedSpan = vm.AllBars.Where(a => x.IsBetweenEqual(a.PathStartX, a.PathEndX, 0.01)).OrderBy(a => a.Layer.Name).ThenBy(a => a.Layer.Layer).ToList();
            if (barsInSelectedSpan.Any(o => o.Layer.Layer == rebarLayer))
            {
               //HighLight
               vm.SelectedBar = barsInSelectedSpan.FirstOrDefault(o => o.Layer.Layer == rebarLayer);
            }
            else
            {
               //Preview
               vm.SelectedBar = vm.GetNextPreviewRebar(spanIndex, rebarLayer);
            }
            if (vm.SelectedBar != null) vm.SelectedBar.SetPathsColor(Define.RebarPreviewColor);

         }
      }

      private int IncreaseRebarLayer(int layer)
      {
         layer++;
         if (layer > 3)
         {
            layer = 1;
            isTop = !isTop;
         }
         return layer;
      }

      private void BtBack_OnClick(object sender, RoutedEventArgs e)
      {
         BeamRebarRevitData.Instance.IsBack = true;
         DialogResult = false;
         Close();
      }

      private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         Regex regex = new Regex("[^0-9]+-");
         e.Handled = regex.IsMatch(e.Text);
      }

      private void Canvas1_OnMouseDown(object sender, MouseButtonEventArgs e)
      {
         BeamRebarViewModel viewModel = BeamRebarRevitData.Instance.BeamRebarViewModel;
         if (!(viewModel.CurrentPageViewModel is StirrupTabViewModel))
         {
            return;
         }

         if (e.ButtonState == MouseButtonState.Pressed)
         {
            if (currentPointExtension != null)
            {
               if (currentPointExtension.Type == 1)
               {
                  if (BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 1)
                  {
                     isFirstClick = true;
                  }
                  else
                  {
                     isFirstClick = !isFirstClick;
                  }
                  var start = int.Parse(selectedIndexString1);
                  var end = int.Parse(selectedIndexString2);
                  if (start >= 0)
                  {
                     //Dai moc
                     if (BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 1)
                     {
                        BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.AddStirrup(start, end, true);
                     }
                     //Dai long kin
                     else
                     {
                        if (isFirstClick)
                        {
                           BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.AddStirrup(start, end, true);
                        }
                     }
                  }
               }
               else
               {
                  if (BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 3)
                  {
                     isFirstClick = true;
                  }
                  else
                  {
                     isFirstClick = !isFirstClick;
                  }

                  var start = int.Parse(selectedIndexString1);

                  //Dai moc
                  if (BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 3)
                  {
                     BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.AddStirrupHorizontal(currentPointExtension, true);
                  }

                  if (BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 1)
                  {
                     if (start>0)
                     {
                        BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.AddStirrup(start, -1, true);
                        isFirstClick = true;
                     }
               
                  }

               }
            }


         }
      }

      private bool isFirstClick = true;
      private string selectedIndexString1 = "";
      private string selectedIndexString2 = "";


      private LabelPointExtension currentPointExtension = null;
      private void Canvas1_OnMouseMove(object sender, MouseEventArgs e)
      {
         if (isFirstClick)
         {
            selectedIndexString1 = "-1";
            selectedIndexString2 = "-1";
         }
         else
         {
            selectedIndexString2 = "-1";
         }

         var canvas = sender as Canvas;
         if (canvas == null)
         {
            return;
         }
         var currentPoint = e.GetPosition(canvas);
         foreach (UIElement canvasChild in canvas.Children)
         {
            if (canvasChild is Label label)
            {
               currentPointExtension = label.Tag as LabelPointExtension;
               //horizon
               if (label.Tag is LabelPointExtension p && p.Type == 1 && BeamRebarRevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio != 3)
               {
                  if (p.Point.X.IsEqual(currentPoint.X, 5))
                  {
                     if (isFirstClick)
                     {
                        label.FontSize = 40;
                        selectedIndexString1 = label.Content as string;
                     }
                     else
                     {
                        label.FontSize = 40;
                        selectedIndexString2 = label.Content as string;
                     }
                  }
                  else
                  {
                     label.FontSize = 12;
                  }
               }

               ////vertical
               //if (label.Tag is LabelPointExtension p2 && p2.Type == 2 && RevitData.Instance.BeamRebarViewModel.StirrupTabViewModel.SelectedSpanModel.StirrupForSpan.StirrupRadio == 3)
               //{
               //   if (p2.Point.Y.IsEqual(currentPoint.Y, 5))
               //   {
               //      if (isFirstClick)
               //      {
               //         label.FontSize = 30;
               //         selectedIndexString1 = label.Content as string;
               //      }
               //      else
               //      {
               //         label.FontSize = 30;
               //         selectedIndexString2 = label.Content as string;
               //      }
               //   }
               //   else
               //   {
               //      label.FontSize = 12;
               //   }
               //}
            }
         }
      }

      public IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
      {
         if (parent == null)
            throw new ArgumentNullException(nameof(parent));

         var queue = new Queue<DependencyObject>(new[] { parent });

         while (queue.Any())
         {
            var reference = queue.Dequeue();
            var count = VisualTreeHelper.GetChildrenCount(reference);

            for (var i = 0; i < count; i++)
            {
               var child = VisualTreeHelper.GetChild(reference, i);
               if (child is T children)
                  yield return children;

               queue.Enqueue(child);
            }
         }
      }

   }
}