using System.Windows;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebarCutShop.View
{
   /// <summary>
   /// Interaction logic for Window1.xaml
   /// </summary>
   public partial class BeamRebarCutShopView : Window
   {
      public BeamRebarCutShopView()
      {
         InitializeComponent();
         this.SetLanguageProviderForResourceDictionary(Resources);
         if (Constants.Lang == LangEnum.EN)
         {
            Title = "Quick Setting";
         }
      }
   }
}
