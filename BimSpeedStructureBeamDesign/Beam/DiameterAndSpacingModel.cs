using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB.Structure;
using BimSpeedUtils;
using Newtonsoft.Json;

namespace BimSpeedStructureBeamDesign.Beam
{
   public class DiameterAndSpacingModel :INotifyPropertyChanged
   {
      [JsonIgnore]
      private RebarBarType _diameter;

      [JsonIgnore]
      public RebarBarType Diameter
      {
         get => _diameter;
         set
         {
            _diameter = value;
            OnPropertyChanged();
            DiameterInt = _diameter.DiameterInMm();
         }
      }

      public double DiameterInt { get; set; }


      /// <summary>
      /// Spacing in Feet
      /// </summary>
      public double Spacing { get; set; }

      public DiameterAndSpacingModel()
      {

      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, value)) return false;
         field = value;
         OnPropertyChanged(propertyName);
         return true;
      }
   }
}