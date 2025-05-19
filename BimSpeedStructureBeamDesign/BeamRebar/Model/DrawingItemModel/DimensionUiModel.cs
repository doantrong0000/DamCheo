using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using BimSpeedStructureBeamDesign.BeamRebar.Services;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.DrawingItemModel
{
   public class DimensionUiModel
   {
      private List<Path> _dimensionPaths = new List<Path> { };
      private List<Label> _labels = new List<Label> { };

      public List<Path> DimensionPaths
      {
         get => _dimensionPaths;
         set
         {
            _dimensionPaths = value;

         }
      }


      public List<Label> Labels
      {

         get => _labels;
         set
         {
            _labels = value;

         }
      }

      public object RebarModel { get; set; }

      public Path RebarPath { get; set; } = new Path();
      public void RemoveFromUi()
      {
         foreach (var path in DimensionPaths)
         {
            BeamRebarRevitData.Instance.Grid.Children.Remove(path);
         }

         foreach (var label in Labels)
         {
            BeamRebarRevitData.Instance.Grid.Children.Remove(label);
         }

         BeamRebarRevitData.Instance.Grid.Children.Remove(RebarPath);

         isAddDim = false;
         isAddRebar = false;

         DimensionPaths.Clear();
         Labels.Clear();
      }

      private bool isAddDim = false;
      private bool isAddRebar = false;

      public void AddToUiGrid()
      {
         SetTag();
         if (!isAddDim)
         {
            foreach (var path in DimensionPaths)
            {

               BeamRebarRevitData.Instance.Grid.Children.Add(path);
            }

            foreach (var label in Labels)
            {

               BeamRebarRevitData.Instance.Grid.Children.Add(label);
            }

            isAddDim = true;

         }


         if (!isAddRebar)
         {
            BeamRebarRevitData.Instance.Grid.Children.Add(RebarPath);

            isAddRebar = true;
         }


      }
      public void ShowHideDim(bool isShow = true)
      {
         if (isShow)
         {
            Labels.ForEach(x => x.Visibility = Visibility.Visible);
            DimensionPaths.ForEach(x => x.Visibility = Visibility.Visible);
         }
         else
         {
            Labels.ForEach(x => x.Visibility = Visibility.Hidden);
            DimensionPaths.ForEach(x => x.Visibility = Visibility.Hidden);
         }
      }

      public void ShowHideRebar(bool isShow = true)
      {
         if (isShow)
         {
            RebarPath.Visibility = Visibility.Visible;

         }
         else
         {
            RebarPath.Visibility = Visibility.Hidden;
         }
      }

      public void SetTag()
      {
         DimensionPaths.ForEach(x => x.Tag = RebarModel);
         Labels.ForEach(x => x.Tag = RebarModel);
         RebarPath.Tag = RebarModel;
      }

      public void ShowSelectedBarInPink()
      {
         RebarPath.Stroke = Define.RebarPreviewColor;
         ShowHideDim(true);
      }
   }
}
