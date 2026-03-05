using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IBlogAttachmentVariantManager : IGenericRepository<BlogAttachmentVariant, int>
  {
    Task<BlogAttachmentVariant> GetBlogAttachmentVariantAsync(
      int blogAttachmentID, 
      string encoderOption, 
      int organizationId);
    Task<TranscodeStatus?> UpdateTranscodeStatusJSONBAsync(
      int blogAttachmentID, 
      string encoderOption, 
      string state, 
      string vaprogressFilePathlue, 
      string outputPath, 
      string command, 
      int userId, 
      int organizationId);
  }
}