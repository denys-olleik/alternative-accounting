using System.Reflection;

namespace Accounting.Common
{
  public class FeaturesIntegratedJournalConstants
  {
    public const string JournalInvoiceInvoiceLine = "JournalInvoiceInvoiceLine";
    public const string JournalInvoiceInvoiceLinePayment = "JournalInvoiceInvoiceLinePayment";
    public const string JournalReconciliationTransaction = "JournalReconciliationTransaction";

    private static readonly List<string> _all = new List<string>();

    static FeaturesIntegratedJournalConstants()
    {
      var fields = typeof(FeaturesIntegratedJournalConstants).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
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