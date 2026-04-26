using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class BlogAttachmentVariantService : BaseService
  {
    public BlogAttachmentVariantService() : base()
    {
    }

    public BlogAttachmentVariantService(string databaseName, string databasePassword)
      : base(databaseName, databasePassword)
    {
    }

    public async Task<BlogAttachmentVariant> CreateAsync(BlogAttachmentVariant variant)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentVariantManager().CreateAsync(variant);
    }

    public async Task<BlogAttachmentVariant?> UpdateAsync(
     int blogAttachmentID,
     string encoderOption,
     string state,
     int progress,
     string outputPath,
     string command,
     int userId,
     int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentVariantManager().UpdateAsync(
        blogAttachmentID,
        encoderOption,
        state,
        null,
        outputPath,
        command,
        userId,
        organizationId);
    }

    public async Task<BlogAttachmentVariant> GetBlogAttachmentVariantAsync(
      int blogAttachmentID, 
      string encoderOption, 
      int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentVariantManager().GetBlogAttachmentVariantAsync(blogAttachmentID, encoderOption, organizationId);
    }
  }
}