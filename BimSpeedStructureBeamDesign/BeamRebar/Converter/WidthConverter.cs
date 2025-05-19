using System.Globalization;
using System.Windows.Data;

namespace BimSpeedStructureBeamDesign.BeamRebar.Converter
{
   public class WidthConverter : IValueConverter
   {
      public bool Inverse { get; set; }

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         bool boolValue = (bool)value;

         return boolValue ? 0 : "*";
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return value;
      }
   }
}