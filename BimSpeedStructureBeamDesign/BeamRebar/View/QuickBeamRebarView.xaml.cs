using System.Windows;
using BimSpeedUtils;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
   /// <summary>
   /// Interaction logic for Window1.xaml
   /// </summary>
   public partial class QuickBeamRebarView : Window
   {
      public QuickBeamRebarView()
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
