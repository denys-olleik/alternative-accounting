using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models.BlogAttachmentViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/blog-attachment")]
  public class BlogAttachmentApiController : BaseController
  {
    private readonly BlogAttachmentService _blogAttachmentService;

    public BlogAttachmentApiController(
      RequestContext requestContext)
    {
      _blogAttachmentService = new BlogAttachmentService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);
    }

    [Route("upload")]
    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile formFile)
    {
      Common.File fileUpload = new Common.File
      {
        FileName = formFile.FileName,
        Stream = formFile.OpenReadStream()
      };

      BlogAttachment blogAttachment = await _blogAttachmentService.UploadBlogAttachmentAsync(fileUpload, GetUserId(), GetOrganizationId()!.Value, GetDatabaseName());

      return Ok(new { BlogAttachmentID = blogAttachment.BlogAttachmentID, FileName = blogAttachment.OriginalFileName });
    }

    [HttpPost("schedule-transcode/{blogAttachmentId:int}")]
    public async Task<IActionResult> ScheduleTranscode(int blogAttachmentId, BlogAttachmentTranscodeRequest request)
    {
      BlogAttachment blogAttachment = await _blogAttachmentService.GetAsync(
        blogAttachmentId,
        GetOrganizationId()!.Value
      );

      if (blogAttachment is null)
      {
        return BadRequest();
      }

      // Determine expected on-disk variant path for this encoder option
      string baseDirectory = ConfigurationSingleton.Instance.PermPath!;
      string path = blogAttachment.FilePath;              // e.g. "{random}.mp4"
      string? encoderOption = request.EncoderOption;            // "mp3", "720p", "original"

      string expectedFileName = $"{encoderOption}.{path}"; // e.g. "mp3.{random}.mp4"
      string expectedPath = Path.Combine(baseDirectory, expectedFileName);

      // 1. Check if variant already exists on disk
      if (System.IO.File.Exists(expectedPath))
      {
        // Variant already materialized; nothing to schedule
        return Ok();
      }

      // 2. (Stub) Check if an identical job is already queued or running
      // TODO: Replace with real lookup, e.g. _blogAttachmentService.HasActiveJobAsync(...)
      bool alreadyQueued = false;
      if (alreadyQueued)
      {
        // Job already enqueued/processing; don't enqueue duplicate
        return Ok();
      }

      await _blogAttachmentService.ScheduleTranscodeAsync(
        blogAttachmentId,
        request.EncoderOption,
        GetUserId(),
        GetOrganizationId()!.Value,
        GetDatabaseName()
      );

      return Ok();
    }
  }
}