using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;

public class LengthOrDiameterCracked : ViewModelBase
{
   private int _isByDiameterCracked = 1;

   public int IsByDiameterCracked
   {
      get => _isByDiameterCracked;
      set
      {
         _isByDiameterCracked = value;
         OnPropertyChanged();
      }
   }

   private double _lengthByDiameterCracked;

   public double LengthByDiameterCracked
   {
      get => _lengthByDiameterCracked;
      set
      {
         _lengthByDiameterCracked = value;
         OnPropertyChanged();
      }
   }

   private int _nCracked = 6;

   public int NCracked
   {
      get => _nCracked;
      set
      {
         _nCracked = value;

         LengthByDiameterCracked = (_nCracked * Diameter).RoundMilimet(10);

         OnPropertyChanged();

         OnPropertyChanged(nameof(LengthByDiameterCracked));
      }
   }

   private double _lengthCracked = 1000.MmToFoot();

   public double LengthCracked
   {
      get => _lengthCracked;
      set
      {
         _lengthCracked = value;
         OnPropertyChanged();
      }
   }

   public double Diameter { get; set; }

   public LengthOrDiameterCracked(int isByDiameterCracked, double diameter, int nCracked, double lengthCracked)
   {
      IsByDiameterCracked = isByDiameterCracked;

      this.Diameter = diameter;

      this.NCracked = nCracked;

      this.LengthCracked = lengthCracked;

      LengthByDiameterCracked = (diameter * nCracked).RoundMilimet(10);
   }
}