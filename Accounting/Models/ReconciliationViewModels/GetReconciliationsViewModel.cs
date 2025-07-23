using AngleSharp.Css.Values;
using Google.Protobuf.WellKnownTypes;

namespace Accounting.Models.ReconciliationViewModels
{
  public class GetReconciliationsViewModel : PaginatedViewModel
  {
    public List<ReconciliationViewModel>? Reconciliations { get; set; }

    public class ReconciliationViewModel
    {
      public int ReconciliationID { get; set; }
      public int RowNumber { get; set; }
      public string Name { get; set; }
      public string Status { get; set; } 
    }
  }
}