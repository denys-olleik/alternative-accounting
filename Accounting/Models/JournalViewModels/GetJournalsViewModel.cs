namespace Accounting.Models.JournalViewModels;

public class GetJournalsViewModel : PaginatedViewModel
{
  public List<JournalTransactionViewModel> Transactions { get; set; } = null!;
  
  public class JournalTransactionViewModel
  {
    public int JournalTransactionID { get; set; }
    public string TransactionGuid { get; set; } = null!;
    public int JournalId { get; set; }
    public int LinkId { get; set; }
    public string LinkType { get; set; } = null!;
    public DateTime Created { get; set; }
  }
}