using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models.BlogAttachmentViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Components.Forms;
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

      string? encoderOption = request.EncoderOption; // "mp3", "720p", "original"
      if (string.IsNullOrWhiteSpace(encoderOption))
      {
        return BadRequest();
      }

      blogAttachment.TranscodeStatus = await _blogAttachmentService.GetTranscodeStatusAsync(blogAttachment.BlogAttachmentID, encoderOption, GetOrganizationId()!.Value);

      string expectedPath = blogAttachment.FilePath;

      // 1. Check if variant already exists on disk
      if (System.IO.File.Exists(expectedPath))
      {
        // Variant already materialized; nothing to schedule
        return Ok(blogAttachment.TranscodeStatus);
      }

      bool alreadyQueued = blogAttachment.TranscodeStatus.State == BlogAttachment.BlogAttachmentEncoderStatusConstants.Queued
        || blogAttachment.TranscodeStatus.State == BlogAttachment.BlogAttachmentEncoderStatusConstants.Processing;
      if (alreadyQueued)
      {
        // Job already enqueued/processing; don't enqueue duplicate
        return Ok(blogAttachment.TranscodeStatus);
      }

      // FilePath is already an absolute path on disk; derive variant path from it
      string filePath = blogAttachment.FilePath;
      string? directoryPart = Path.GetDirectoryName(filePath);
      string file = Path.GetFileName(filePath);              // e.g. "{random}.mp4"
      string variantFileName = $"{encoderOption}.{file}";    // e.g. "mp3.{random}.mp4"

      string inputPath = Path.Combine(directoryPart, file);

      //string ffmpegArgsForOption =

      string outputPath = DeriveVariantOutputPath(inputPath, encoderOption);

      TranscodeStatus status = await _blogAttachmentService.UpdateAsync(
        blogAttachment.BlogAttachmentID,
        request.EncoderOption,
        BlogAttachment.BlogAttachmentEncoderStatusConstants.Queued,
        0,
        "",
        $"ffmpeg -y -hide_banner -i \"{inputPath}\" {ffmpegArgsForOption} \"{outputPath}\"",
        GetUserId(),
        GetOrganizationId()!.Value);

      //await _blogAttachmentService.ScheduleTranscodeAsync(
      //  blogAttachmentId,
      //  request.EncoderOption,
      //  GetUserId(),
      //  GetOrganizationId()!.Value,
      //  GetDatabaseName()
      //);

      return Ok(status);
    }

    private static string DeriveVariantOutputPath(string inputPath, string encoderOption)
    {
      string directory = Path.GetDirectoryName(inputPath);
      string inputFileName = Path.GetFileNameWithoutExtension(inputPath);
      string inputExt = Path.GetExtension(inputPath);

      string normalized = encoderOption.Trim().ToLowerInvariant();

      string outputExt = normalized switch
      {
        "mp3" => ".mp3",
        "720p" => inputExt,
        "original" => inputExt,
        _ => inputExt
      };

      string outputFileName = $"{normalized}.{inputFileName}{outputExt}";
      return Path.Combine(directory, outputFileName);
    }
  }
}