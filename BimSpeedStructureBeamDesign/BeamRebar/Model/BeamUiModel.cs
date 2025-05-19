using System.Windows;
using BimSpeedStructureBeamDesign.BeamRebar.Services;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model
{
   public class BeamUiModel
   {
      public Point Origin { get; set; } = BeamRebarRevitData.Instance.OriginUiMainView;
      public List<SpanUiModel> SpanUiModels { get; set; } = new List<SpanUiModel>();
      public List<SupportUiModel> SupportUiModels { get; set; } = new List<SupportUiModel>();

      public BeamUiModel()
      {
      }

      public void SetIndex()
      {
         var i = 0;
         foreach (var spanUiModel in SpanUiModels)
         {
            spanUiModel.Index = i;
            i++;
         }

         var j = 0;
         foreach (var sp in SupportUiModels)
         {
            sp.Index = j;
            j++;
         }
      }
   }
}