using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IBlogAttachmentManager : IGenericRepository<BlogAttachment, int>
  {
    Task<int> DeleteAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<IEnumerable<BlogAttachment>> GetAllAsync(int[] ids, int organizationId);
    Task<BlogAttachment> GetAsync(int blogAttachmentId, int organizationId);
    Task<int> UpdateBlogIdAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<int> UpdateFilePathAsync(int blogAttachmentID, string newPath, int organizationId);

    Task<string?> UpdateTranscodeStatusJSONBAsync(
      int blogAttachmentId,
      string encoderOption,
      string state,
      int percent,
      int organizationId);

    Task<TranscodeStatus> GetTranscodeStatusAsync(int blogAttachmentId, int organizationId);
  }
}