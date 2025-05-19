using Autodesk.Revit.DB;

namespace BimSpeedStructureBeamDesign.BeamRebar.Services
{
   public class WarningDiscard : IFailuresPreprocessor
   {
      FailureProcessingResult
          IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
      {
         String transactionName = failuresAccessor.GetTransactionName();

         IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();

         if (fmas.Count == 0)
         {
            return FailureProcessingResult.Continue;
         }
         failuresAccessor.DeleteAllWarnings();
         return FailureProcessingResult.Continue;
      }
   }
}