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

  public int Identifiable => JournalTransactionID;
}