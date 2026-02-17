using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IBlogAttachmentManager : IGenericRepository<BlogAttachment, int>
  {
    Task<int> DeleteAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<IEnumerable<BlogAttachment>> GetAllAsync(int[] blogAttachmentIds, int organizationId);
    Task<BlogAttachment> GetAsync(int blogAttachmentId, int organizationId);
    Task<int> UpdateBlogIdAsync(int blogAttachmentID, int blogID, int organizationId);
    Task<int> UpdateFilePathAsync(int blogAttachmentID, string newPath, int organizationId);

    Task<TranscodeStatus?> UpdateTranscodeStatusJSONBAsync(
      int blogAttachmentId,
      string encoderOption,
      string state,
      string? progressFilePath,
      string path,
      string command,
      int userId,
      int organizationId);

    Task<TranscodeStatus> GetTranscodeStatusAsync(int blogAttachmentId, string encoderOption, int organizationId);
    Task<IEnumerable<BlogAttachment>> GetAllAsync(int blogId, int organizationId);
    Task<BlogAttachment> GetOldestAsync(string blogAttachmentEncoderStatusConstant);
  }
}