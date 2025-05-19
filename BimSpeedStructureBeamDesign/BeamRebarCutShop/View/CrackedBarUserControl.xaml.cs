using System.Windows.Controls;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedRebar.RebarTools.CutRebarAtPoint.Views;

/// <summary>
/// Interaction logic for CrackedBarUserControl.xaml
/// </summary>
public partial class CrackedBarUserControl : UserControl
{
    public CrackedBarUserControl()
    {
        InitializeComponent();
        this.SetLanguageProviderForResourceDictionary(Resources);
    }
}