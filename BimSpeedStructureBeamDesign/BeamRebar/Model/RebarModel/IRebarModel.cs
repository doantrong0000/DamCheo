using BimSpeedStructureBeamDesign.BeamRebar.Model.DrawingItemModel;
using BimSpeedStructureBeamDesign.BeamRebar.ViewModel;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public interface IRebarModel
   {
      public IPageViewModel HostViewModel { get; }
      public Guid GuidId { get; set; }
      public DimensionUiModel DimensionUiModel { get; set; }
   }
}
