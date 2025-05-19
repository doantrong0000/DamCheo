using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.Model;

public class LengthOrDiameterLap : ViewModelBase
{
   public LengthOrDiameterLap()
   {
      IsByDiameter = 1;

      N_Lap = 40;

      Diameter = 20;

      LengthByDiameter = RoundMilimet((Diameter * n_Lap), 10);

      Length = 200;
   }

   public double RoundMilimet(double feet, double roundMm, bool isRoundUp = true)
   {
      var mm = feet.FootToMm();
      if (isRoundUp)
      {
         mm = Math.Ceiling(mm / roundMm) * roundMm;
      }
      else
      {
         mm = Math.Floor(mm / roundMm) * roundMm;
      }
      return mm.MmToFoot();
   }

   private double length = 1000.MmToFoot();

   public double Length
   {
      get => length;
      set
      {
         length = value;
         OnPropertyChanged();
      }
   }

   private int isByDiameter = 1;

   public int IsByDiameter
   {
      get => isByDiameter;
      set
      {
         isByDiameter = value;
         OnPropertyChanged();
      }
   }

   private double lengthByDiameter;

   public double LengthByDiameter
   {
      get => lengthByDiameter;
      set
      {
         lengthByDiameter = value;
         OnPropertyChanged();
      }
   }

   public double Diameter { get; set; }

   private int n_Lap;

   public int N_Lap
   {
      get => n_Lap;
      set
      {
         n_Lap = value;

         lengthByDiameter = (n_Lap * Diameter).RoundMilimet(10);

         OnPropertyChanged();

         OnPropertyChanged(nameof(LengthByDiameter));
      }
   }
}