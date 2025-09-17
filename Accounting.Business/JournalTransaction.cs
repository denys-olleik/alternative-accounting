using Accounting.Common;

namespace Accounting.Business;

public class JournalTransaction : IIdentifiable<int>
{
  public int JournalTransactionID { get; set; }
  public string TransactionGuid { get; set; } = null!;
  public int JournalId { get; set; }
  public int LinkId { get; set; }
  public string LinkType { get; set; } = null!;
  public DateTime Created { get; set; }

  #region Extra properties
  public List<Invoice>? Invoices { get; set; }
  public List<InvoiceLine>? InvoiceLines { get; set; }
  public Payment? Payment { get; set; }
  public List<Journal> Journals { get; set; } = null!;
  public ReconciliationTransaction? ReconciliationTransaction { get; set; }
  #endregion

  public int Identifiable => JournalTransactionID;
}