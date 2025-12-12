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

    public async System.Threading.Tasks.Task DeleteAttachmentsAsync(List<int> ids, int blogID, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      var blogAttachmentManager = factoryManager.GetBlogAttachmentManager();

      var attachments = await blogAttachmentManager.GetAllAsync(ids.ToArray(), organizationId);

      foreach (var attachment in attachments)
      {
        if (attachment != null)
        {
          if (System.IO.File.Exists(attachment.FilePath))
            System.IO.File.Delete(attachment.FilePath);

          await blogAttachmentManager.DeleteAsync(attachment.BlogAttachmentID, blogID, organizationId);
        }
      }
    }

    public async Task<IEnumerable<BlogAttachment>> GetAllAsync(int[] ints, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetAllAsync(ints, organizationId);
    }

    public async Task<BlogAttachment> GetAsync(int blogAttachmentId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetAsync(blogAttachmentId, organizationId);
    }

    public async System.Threading.Tasks.Task MoveAndUpdateBlogAttachmentPathAsync(BlogAttachment attachment, string? permPath, int organizationId, string databaseName)
    {
      if (permPath == null)
        throw new ArgumentNullException(nameof(permPath));

      string dbSpecificPath = Path.Combine(permPath, databaseName);
      if (!Directory.Exists(dbSpecificPath))
      {
        Directory.CreateDirectory(dbSpecificPath);
      }

      string fileName = Path.GetFileName(attachment.FilePath);
      string newPath = Path.Combine(dbSpecificPath, fileName);
      System.IO.File.Move(attachment.FilePath, newPath);

      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      await factoryManager.GetBlogAttachmentManager().UpdateFilePathAsync(attachment.BlogAttachmentID, newPath, organizationId);
    }

    public async System.Threading.Tasks.Task ScheduleTranscodeAsync(
      int blogAttachmentId, 
      string encoderOption, 
      int userId, 
      int organizationId, 
      string databaseName)
    {
      throw new NotImplementedException();
    }

    public async Task<int> UpdateBlogIdAsync(int blogAttachmentID, int blogID, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().UpdateBlogIdAsync(blogAttachmentID, blogID, organizationId);
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