using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
    /// <summary>
    /// Interaction logic for BeamRebarView2.xaml
    /// </summary>
    public partial class BeamRebarViewSetting : Window
    {
        public BeamRebarViewSetting()
        {
            InitializeComponent();
            this.SetLanguageProviderForResourceDictionary(Resources);
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}