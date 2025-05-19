using System.Windows;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamSectionGenerator.View
{
    /// <summary>
    /// Interaction logic for BeamSectionGeneratorView.xaml
    /// </summary>
    public partial class BeamSectionGeneratorView : Window
    {
        public BeamSectionGeneratorView()
        {
            InitializeComponent();
            this.SetLanguageProviderForResourceDictionary(Resources);
        }

        //private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        //{
        //    //if (Constants.Lang == 1)
        //    //{
        //    //    Constants.Lang = 2;
        //    //}
        //    //else
        //    //{
        //    //    Constants.Lang = 1;
        //    //}
        //    //this.SetLanguageProviderForResourceDictionary(Resources);
        //}

    }
}