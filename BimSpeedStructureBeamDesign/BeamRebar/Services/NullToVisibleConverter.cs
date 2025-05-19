using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   [ValueConversion(typeof(bool), typeof(bool))]
   public class NullToVisibleConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (value != null)
         {
            return Visibility.Visible;
         }
         return Visibility.Collapsed;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return DependencyProperty.UnsetValue;
      }
   }
}