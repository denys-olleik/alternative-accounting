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
    //private readonly BlogAttachmentVariant _blogAttachmentVariant;

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
      BlogAttachment? blogAttachment = await _blogAttachmentService.GetAsync(
        blogAttachmentId,
        GetOrganizationId()!.Value);

      if (blogAttachment is null)
        return BadRequest("Blog attachment not found.");

      if (string.IsNullOrWhiteSpace(request.EncoderOption))
        return BadRequest("Encoder option is required.");

      string encoderOption = request.EncoderOption.Trim().ToLowerInvariant();

      if (!IsSupportedEncoderOption(encoderOption))
        return BadRequest("Unsupported encoder option.");

      var existingStatus = await _blogAttachmentService.GetTranscodeStatusAsync(
        blogAttachment.BlogAttachmentID,
        encoderOption,
        GetOrganizationId()!.Value);

      if (existingStatus != null)
        return Ok(existingStatus);

      string inputPath = blogAttachment.FilePath;
      string outputPath = DeriveVariantOutputPath(inputPath, encoderOption);

      // Persist structured execution intent only (NO ffmpeg command yet)
      TranscodeStatus? status = await _blogAttachmentService.UpdateAsync(
        blogAttachment.BlogAttachmentID,
        encoderOption,
        BlogAttachmentVariant.StateConstants.Queued,
        0,
        outputPath,
        command: null, // Worker will construct actual command
        GetUserId(),
        GetOrganizationId()!.Value
      );

      return Ok(status);
    }

    private static bool IsSupportedEncoderOption(string option)
    {
      return option switch
      {
        "mp3" => true,
        "720p" => true,
        "original" => true,
        _ => false
      };
    }

    private static string DeriveVariantOutputPath(string inputPath, string encoderOption)
    {
      string directory = Path.GetDirectoryName(inputPath)!;
      string inputFileName = Path.GetFileNameWithoutExtension(inputPath);
      string inputExt = Path.GetExtension(inputPath);

      string outputExt = encoderOption switch
      {
        "mp3" => ".mp3",
        "720p" => inputExt,
        "original" => inputExt,
        _ => inputExt
      };

      string outputFileName = $"{encoderOption}.{inputFileName}{outputExt}";
      return Path.Combine(directory, outputFileName);
    }
  }
}