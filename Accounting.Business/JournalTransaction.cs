using System.Reflection;
using Accounting.Common;

namespace Accounting.Business;

public class JournalTransaction : IIdentifiable<int>
{
  public int JournalTransactionID { get; set; }
  public string TransactionGuid { get; set; } = null!;
  public int JournalId { get; set; }
  public DateTime Created { get; set; }

  #region Extra properties
  public string LinkType { get; set; } = null!;
  #endregion

  public int Identifiable => JournalTransactionID;
  
  public class LinkTypeConstants
  {
    public const string Invoice = "invoice";
    public const string Payment = "payment";
    public const string Reconciliation = "reconciliation";
    
    private static readonly List<string> _all = new ();

    static LinkTypeConstants()
    {
      var fields = typeof(LinkTypeConstants).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
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