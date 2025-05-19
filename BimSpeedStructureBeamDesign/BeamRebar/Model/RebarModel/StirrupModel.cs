using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace BimSpeedStructureBeamDesign.BeamRebar.Model.RebarModel
{
   public class StirrupModel
   {
      public Rebar Rebar { get; set; }
      public List<Rebar> Rebars { get; set; } = new List<Rebar>();
      public bool IsHorizontal { get; set; }
      public bool IsDaiMoc { get; set; }
      public int StartIndex { get; set; }
      public XYZ Start { get; set; }
      public XYZ End { get; set; }
      public int EndIndex { get; set; }
      public List<Path> Paths { get; set; } = new List<Path>();
      public string Location { get; set; } = "BOT";

      public StirrupModel(int start)
      {
         IsDaiMoc = true;
         StartIndex = start;
      }

      public StirrupModel(int start, bool isHorizontal = true, string location = "BOT")
      {
         IsDaiMoc = true;
         StartIndex = start;
         IsHorizontal = isHorizontal;
         Location = location;
      }

      public StirrupModel(int start, int end)
      {
         IsDaiMoc = false;
         StartIndex = start;
         EndIndex = end;
         if (start > end)
         {
            StartIndex = end;
            EndIndex = start;
         }
      }

      public override string ToString()
      {
         var s = "Đại Móc ";
         if (IsDaiMoc == false)
         {
            s = "Đai Lồng Kín ";
         }
         s += "[ " + "Start :" + StartIndex + " - " + "End :" + EndIndex + " ]";
         return s;
      }
   }
}