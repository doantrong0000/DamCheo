using Autodesk.Revit.Attributes;
using BeamRebar.ViewModels;
using BeamRebar.Views;
using Nice3point.Revit.Toolkit.External;

namespace BeamRebar.Commands
{
    /// <summary>
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new BeamRebarViewModel();
            var view = new BeamRebarView(viewModel);
            view.ShowDialog();
        }
    }
}