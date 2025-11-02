
using Accounting.Business;
using Accounting.Common;
using Accounting.Database;

namespace Accounting.Service
{
  public class BlogAttachmentService : BaseService
  {
    public BlogAttachmentService()
      : base()
    {

    }

    public BlogAttachmentService(
      string databaseName,
      string databasePassword) : base(databaseName, databasePassword)
    {

    }

    public async Task<BlogAttachment> UploadBlogAttachmentAsync(Common.File fileUpload, int userId, int organizationId, string databaseName)
    {
      string path = RandomHelper.GenerateSecureAlphanumericString(15) + Path.GetExtension(fileUpload.FileName);

      string temporaryDirectory = ConfigurationSingleton.Instance.TempPath;

      string databaseDirectory = Path.Combine(temporaryDirectory, databaseName);
      if (!Directory.Exists(databaseDirectory))
      {
        Directory.CreateDirectory(databaseDirectory);
      }

      string fullPath = Path.Combine(databaseDirectory, path);
      using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
      {
        await fileUpload.Stream.CopyToAsync(fileStream);
      }

      var attachment = new BlogAttachment
      {
        OriginalFileName = fileUpload.FileName,
        FilePath = fullPath,
        CreatedById = userId,
        OrganizationId = organizationId
      };

      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      attachment = await factoryManager.GetBlogAttachmentManager().CreateAsync(attachment);

      return attachment;
    }
  }
}