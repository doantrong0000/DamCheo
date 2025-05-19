using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using BimSpeedUtils.LanguageUtils;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
   /// <summary>
   /// Interaction logic for ProgressBarView.xaml
   /// </summary>
   public partial class ProgressBarWithStatusView : Window
   {
      private delegate void ProgressBarDelegate();

      public bool Flag = true;

      public ProgressBarWithStatusView()
      {
         InitializeComponent();
            this.SetLanguageProviderForResourceDictionary(Resources);
        }

      private string oldMainTask = "";
      private string oldSubTask = "";

      public bool Create(int max, string mainTask, string subTask, bool isNewProcess = false)
      {
         TbStatus.TextWrapping = TextWrapping.Wrap;
         if (oldMainTask == mainTask)
         {
            if (oldSubTask != subTask)
            {
               TbStatus.Inlines.Add("      -" + subTask);
               TbStatus.Inlines.Add(new LineBreak());
            }
         }
         else
         {
            TbStatus.Inlines.Add(new Run("+" + mainTask) { FontWeight = FontWeights.Bold });
            TbStatus.Inlines.Add(new LineBreak());
            TbStatus.Inlines.Add("      -" + subTask);
            TbStatus.Inlines.Add(new LineBreak());
         }
         oldMainTask = mainTask;
         oldSubTask = subTask;
         pb.Dispatcher?.Invoke(new ProgressBarDelegate(UpdateProgress), DispatcherPriority.Background);
         return Flag;
      }

      private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
      {
         Flag = false;
      }

      private void UpdateProgress()
      {
         pb.Value++;
      }

      private void BtClose_OnClick(object sender, RoutedEventArgs e)
      {
         Flag = false;
      }
   }
}