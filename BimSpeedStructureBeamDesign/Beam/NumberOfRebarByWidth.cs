using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Beam
{
   public class NumberOfRebarByWidth : ViewModelBase
   {
      private int _bMin;
      private int _bMax;
      private string _numberOfRebarByWidthEnum;

      public int BMin
      {
         get => _bMin;
         set
         {
            _bMin = value;
            OnPropertyChanged();
         }
      }

      public int BMax
      {
         get => _bMax;
         set
         {
            _bMax = value;
            OnPropertyChanged();
         }
      }

      public string TypeNumberOfRebarByWidth
      {
         get => _numberOfRebarByWidthEnum;
         set
         {
            _numberOfRebarByWidthEnum = value;
            OnPropertyChanged();
         }
      }

      public string WidthInString { get; set; }

      public NumberOfRebarByWidth(int max, int min, string type)
      {
         BMin = min;
         BMax = max;
         TypeNumberOfRebarByWidth = type;
         WidthInString = BMin + "-->>" + BMax;
      }
   }
}