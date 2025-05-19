using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View.SubViews
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class MainBotBarView : UserControl
    {
        public MainBotBarView()
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