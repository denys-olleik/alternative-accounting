using System.Reflection;

namespace Accounting.Common
{
  public class UnitTypeConstants
  {
    public const string Piece = "piece";
    public const string Kilogram = "kilogram";
    public const string Gram = "g";
    public const string Meter = "meter";
    public const string Liter = "liter";
    public const string Ounce = "oz";

    private static readonly List<string> _all = new List<string>();

    static UnitTypeConstants()
    {
      var fields = typeof(UnitTypeConstants).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
      foreach (var field in fields)
      {
        if (field.FieldType == typeof(string) && field.GetValue(null) is string value)
        {
          _all.Add(value);
        }
      }
    }

    public static List<string> All => _all;
  }
}