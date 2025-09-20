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
    public List<JournalViewModel> Journals { get; set; } = null!;
    public List<InvoiceViewModel> Invoices { get; set; } = null!;
    public List<InvoiceLineViewModel> InvoiceLines { get; set; } = null!;
  }
  
  public class InvoiceLineViewModel
  {
    public int InvoiceLineID { get; set; }
    public string Title { get; set; } = null!;
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
  }

  public class JournalViewModel
  {
    public int JournalID { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
  }
  
  public class InvoiceViewModel
  {
    public int InvoiceID { get; set; }
    public string InvoiceNumber { get; set; }
    public decimal Total { get; set; }
  }

  public class BusinessEntityViewModel
  {
    public int BusinessEntityViewModelID { get; set; }
    public string Name { get; set; } = null!;
  }
}