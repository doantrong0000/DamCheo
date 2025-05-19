using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.Model
{
   public class BeamRebarCutShopJson
   {
      public int TopCutType { get; set; } = 1;
      public int BotCutType { get; set; } = 1;

      public double TopRatio { get; set; } = 0.5;
      public double BotRatio { get; set; } = 0.25;
   }
}
