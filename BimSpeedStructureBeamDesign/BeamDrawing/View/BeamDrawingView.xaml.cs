using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BimSpeedStructureBeamDesign.BeamDrawing.Model;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamDrawing.View
{
    /// <summary>
    /// Interaction logic for BeamDrawingView.xaml
    /// </summary>
    public partial class BeamDrawingView : Window
    {
        public BeamDrawingView()
        {
            InitializeComponent();
            this.SetLanguageProviderForResourceDictionary(Resources);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                var selectedItem = lb.SelectedItem as BeamDrawingSetting;
                if (selectedItem != null)
                {
                    tbSettingName.Text = selectedItem.Name;
                }
            }
        }

        private void BeamDrawingView_OnLoaded(object sender, RoutedEventArgs e)
        {
            BimSpeedStructureBeamDesign.Properties.Resources.Culture = new System.Globalization.CultureInfo("vi-VN");

            if (Constants.Lang == LangEnum.EN)
            {
                ImageBeam.Source = new BitmapImage(
                   new Uri("pack://application:,,,/BimSpeedStructureBeamDesign;component/BeamDrawing/View/beamdrawing-en.jpg"));
            }


            if (Constants.Lang == LangEnum.JP)
            {
                ImageBeam.Source = new BitmapImage(
                   new Uri("pack://application:,,,/BimSpeedStructureBeamDesign;component/BeamDrawing/View/beamdrawing-jp.png"));
            }

        }
    }
}
