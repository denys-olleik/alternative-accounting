using Accounting.Common;
using System.Reflection;

namespace Accounting.Business
{
  public class BlogAttachmentVariant : IIdentifiable<int>
  {
    public int BlogAttachmentVariantID { get; set; }
    public int BlogAttachmentID { get; set; }
    public string? EncoderOption { get; set; }
    public string? State { get; set; }
    public string? ProgressFilePath { get; set; }
    public string? VariantPath { get; set; }
    public string? Command { get; set; }
    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public BlogAttachment? BlogAttachment { get; set; }

    public int Identifiable => this.BlogAttachmentVariantID;

    public static class EncoderOptionConstants
    {
      public const string Mp3 = "mp3";
      public const string P720 = "720p";
      public const string Original = "original";

      private static readonly List<string> _all = new List<string>();

      static EncoderOptionConstants()
      {
        var fields = typeof(EncoderOptionConstants)
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

    public static class StateConstants
    {
      public const string Queued = "queued";
      public const string Processing = "processing";
      public const string Completed = "completed";

      private static readonly List<string> _all = new List<string>();

      static StateConstants()
      {
        var fields = typeof(StateConstants)
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