namespace Accounting.Models.ReconciliationViewModels
{
  public class ReconciliationTransactionsPaginatedViewModel : PaginatedViewModel
  {
    public int ReconciliationID { get; set; }
    public string Name { get; set; }
    public string? Status { get; set; }
    public string? OriginalFileName { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public List<AccountViewModel> Accounts { get; set; }

    public DateTime Created { get; set; }

    public class AccountViewModel
    {
      public int AccountID { get; set; }
      public string Name { get; set; }
      public string Type { get; set; }
    }
  }
}