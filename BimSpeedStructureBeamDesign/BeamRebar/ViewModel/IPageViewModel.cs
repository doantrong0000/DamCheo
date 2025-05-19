namespace BimSpeedStructureBeamDesign.BeamRebar.ViewModel
{
   public interface IPageViewModel
   {
      string Name { get; }
      string Image { get; }
      public bool IsSelected { get; set; }
      public bool IsShowSection { get; set; }
   }
}