using System.Windows;
using System.Windows.Controls;

namespace BimSpeedStructureBeamDesign.Utils.ResourceDicrectory;

public class ColorZone:ContentControl
{
    static ColorZone()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorZone), new FrameworkPropertyMetadata(typeof(ColorZone)));
    }
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(ColorZoneMode), typeof(ColorZone), new PropertyMetadata(default(ColorZoneMode)));

    public ColorZoneMode Mode
    {
        get => (ColorZoneMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(ColorZone), new PropertyMetadata(default(CornerRadius)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}

public enum ColorZoneMode
{
    Standard,
    Inverted,
    PrimaryLight,
    PrimaryMid,
    PrimaryDark,
    SecondaryLight,
    SecondaryMid,
    SecondaryDark,
    Light,
    Dark,
    Custom
}