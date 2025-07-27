using Accounting.Common;

namespace Accounting.Business
{
  public class JournalReconciliationTransactionDebitCredit : JournalTableBase, IIdentifiable<int>
  {
		public int JournalReconciliationTransactionDebitCreditID { get; set; }
		public int JournalId { get; set; }
    public int ReconciliationTransactionId { get; set; }
		public int? ReversedJournalReconciliationTransactionId { get; set; }
		public int CreatedById { get; set; }
		public int OrganizationId { get; set; }

    public int Identifiable => this.JournalReconciliationTransactionDebitCreditID;
  }
}