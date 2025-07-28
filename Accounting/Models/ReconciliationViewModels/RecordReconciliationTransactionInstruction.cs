namespace Accounting.Models.ReconciliationViewModels
{
  public class RecordReconciliationTransactionInstruction
  {
    public int ReconciliationTransactionID { get; set; }
    public int DebitAccount { get; set; }
    public int CreditAccount { get; set; }
  }
}