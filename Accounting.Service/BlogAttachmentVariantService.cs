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
  }
}