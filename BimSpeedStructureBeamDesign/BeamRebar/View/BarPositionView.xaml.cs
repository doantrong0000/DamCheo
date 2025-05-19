using System.Windows;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
   /// <summary>
   /// Interaction logic for BarPositionView.xaml
   /// </summary>
   public partial class BarPositionView : Window
   {
      public BarPositionView()
      {
         InitializeComponent();
            this.SetLanguageProviderForResourceDictionary(Resources);
        }

      private void Close_OnClick(object sender, RoutedEventArgs e)
      {
         Close();
      }
   }
}