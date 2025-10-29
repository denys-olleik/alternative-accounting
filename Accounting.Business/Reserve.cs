using Accounting.Common;

namespace Accounting.Business
{
  public class Reserve : IIdentifiable<int>
  {
    public int ReserveID { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public decimal Weight { get; set; }
    public string? Unit { get; set; }
    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public int Identifiable => this.ReserveID;

    public static class ReserveTypesConstants
    {
      public const string Gold = "gold";
      public const string Silver = "silver";
      
      private static readonly IReadOnlyList<string> _all;
      
      static ReserveTypesConstants()
      {
        var fields = typeof(ReserveTypesConstants).GetFields(
          System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.DeclaredOnly);
        _all = fields
          .Where(f => f.FieldType == typeof(string))
          .Select(f => (string)f.GetValue(null)!)
          .ToList()
          .AsReadOnly();
      }

      public static IReadOnlyList<string> All => _all;
    }
  }
}