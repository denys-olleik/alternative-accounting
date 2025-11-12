using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IBlogAttachmentManager : IGenericRepository<BlogAttachment, int>
  {
    System.Threading.Tasks.Task DeleteAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<IEnumerable<BlogAttachment>> GetAllAsync(int[] ids, int organizationId);
    Task<int> UpdateBlogIdAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<int> UpdateFilePathAsync(int blogAttachmentID, string newPath, int organizationId);
  }
}