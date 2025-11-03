using Accounting.Common;

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
  }
}