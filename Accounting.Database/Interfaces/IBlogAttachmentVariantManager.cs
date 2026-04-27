using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IBlogAttachmentVariantManager : IGenericRepository<BlogAttachmentVariant, int>
  {
    Task<BlogAttachmentVariant> GetBlogAttachmentVariantAsync(
      int blogAttachmentID, 
      string encoderOption, 
      int organizationId);
    Task<BlogAttachmentVariant?> ScheduleAsync(
      int blogAttachmentID, 
      string encoderOption, 
      string state, 
      string vaprogressFilePathlue, 
      string variantPath, 
      string command, 
      int userId, 
      int organizationId);
  }
}