//using BeamRebar.Commands;
//using Nice3point.Revit.Toolkit.External;

//namespace BeamRebar
//{
//    /// <summary>
//    ///     Application entry point
//    /// </summary>
//    [UsedImplicitly]
//    public class Application : ExternalApplication
//    {
//        public override void OnStartup()
//        {
//            CreateRibbon();
//        }

//        private void CreateRibbon()
//        {
//            var panel = Application.CreatePanel("Commands", "BeamRebar");

//            panel.AddPushButton<StartupCommand>("Execute")
//                .SetImage("/BeamRebar;component/Resources/Icons/RibbonIcon16.png")
//                .SetLargeImage("/BeamRebar;component/Resources/Icons/RibbonIcon32.png");
//        }
//    }
//}