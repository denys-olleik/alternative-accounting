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

    public async Task<TranscodeStatus?> UpdateAsync(
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
      return await factoryManager.GetBlogAttachmentManager().UpdateTranscodeStatusJSONBAsync(
        blogAttachmentID, 
        encoderOption,
        state, 
        0,
        outputPath,
        command,
        userId, 
        organizationId);
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

    public async Task<IEnumerable<BlogAttachment>> GetAllAsync(int[] blogAttachmentIds, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetAllAsync(blogAttachmentIds, organizationId);
    }

    public async Task<BlogAttachment> GetAsync(int blogAttachmentId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetAsync(blogAttachmentId, organizationId);
    }

    public async Task<TranscodeStatus?> GetTranscodeStatusAsync(int blogAttachmentID, string encoderOption, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetTranscodeStatusAsync(blogAttachmentID, encoderOption, organizationId);
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
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      var blogAttachmentManager = factoryManager.GetBlogAttachmentManager();
      var attachment = await blogAttachmentManager.GetAsync(blogAttachmentId, organizationId);

      if (attachment == null)
      {
        return; // silently do nothing
      }

      string fileNameOnly = Path.GetFileNameWithoutExtension(attachment.FilePath);

      string permPath = ConfigurationSingleton.Instance.PermPath;
      if (string.IsNullOrWhiteSpace(permPath))
      {
        return; // silently do nothing
      }

      string dbDirectory = Path.Combine(permPath, databaseName);
      if (!Directory.Exists(dbDirectory))
      {
        Directory.CreateDirectory(dbDirectory);
      }

      string markerFileName = $"queued.{encoderOption}.{fileNameOnly}";
      string markerFilePath = Path.Combine(dbDirectory, markerFileName);

      // Single-line TODO placeholder; real ffmpeg command can replace this later
      string todoLine =
        $"TODO_FFMPEG blogAttachmentId={blogAttachmentId} path=\"{attachment.FilePath}\" encoder={encoderOption}";

      System.IO.File.WriteAllText(markerFilePath, todoLine);
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

    public async Task<IEnumerable<BlogAttachment>> GetAllAsync(int blogId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetBlogAttachmentManager().GetAllAsync(blogId, organizationId);
    }
  }
}