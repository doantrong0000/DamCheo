using System.Windows.Controls;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedRebar.RebarTools.CutRebarAtPoint.Views;

/// <summary>
/// Interaction logic for LappedBarUserControl.xaml
/// </summary>
public partial class LappedBarUserControl : UserControl
{
    public LappedBarUserControl()
    {
        InitializeComponent();
        this.SetLanguageProviderForResourceDictionary(Resources);
    }
}