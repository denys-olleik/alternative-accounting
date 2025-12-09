using Accounting.Common;
using System.Reflection;

namespace Accounting.Business
{
  public class BlogAttachment : IIdentifiable<int>
  {
    public int BlogAttachmentID { get; set; }
    public int BlogId { get; set; }
    public string OriginalFileName { get; set; }
    public string FilePath { get; set; }
    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public int Identifiable => this.BlogAttachmentID;

    public static class BlogAttachmentEncoderOptions
    {
      public const string Mp3 = "mp3";
      public const string Video720p = "720p";
      public const string Original = "original";

      private static readonly List<string> _all = new List<string>();

      static BlogAttachmentEncoderOptions()
      {
        var fields = typeof(BlogAttachmentEncoderOptions)
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