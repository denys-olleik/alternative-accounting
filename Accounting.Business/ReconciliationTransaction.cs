using Accounting.Common;
using System.ComponentModel;
using System.Reflection;

namespace Accounting.Business
{
  public class ReconciliationTransaction : IIdentifiable<int>
  {
    public int ReconciliationTransactionID { get; set; }
    public string? Status { get; set; }
    public string? RawData { get; set; }
    public Guid? TransactionGuid { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string? Category { get; set; }
    public DateTime Created { get; set; }
    public int ReconciliationId { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public User? CreatedBy { get; set; }
    public Organization? Organization { get; set; }

    #region Extra properties
    public int? RowNumber { get; set; }
    public List<JournalReconciliationTransaction>? JournalReconciliationTransactions { get; set; } = new ();
    #endregion

    public int Identifiable => this.ReconciliationTransactionID;

    public static class ImportStatuses
    {
      public const string Pending = "pending";
      public const string Processed = "processed";
      public const string Error = "error";

      private static readonly List<string> _all = new List<string>();

      static ImportStatuses()
      {
        var fields = typeof(ImportStatuses)
            .GetFields(
            BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly);

        foreach (var field in fields)
        {
          if (field.FieldType == typeof(string) && field.GetValue(null) is string value)
          {
            _all.Add(value);
          }
        }
      }

      public static IReadOnlyList<string> All => _all.AsReadOnly();
    }
  }
}