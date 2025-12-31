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

      blogAttachment.TranscodeStatus = await _blogAttachmentService.GetTranscodeStatusAsync(
        blogAttachment.BlogAttachmentID,
        encoderOption,
        GetOrganizationId()!.Value
      ) ?? new TranscodeStatus { State = BlogAttachment.BlogAttachmentEncoderStatusConstants.None };

      bool alreadyQueued = blogAttachment.TranscodeStatus.State == BlogAttachment.BlogAttachmentEncoderStatusConstants.Queued
        || blogAttachment.TranscodeStatus.State == BlogAttachment.BlogAttachmentEncoderStatusConstants.Processing;
      if (alreadyQueued)
      {
        return Ok(blogAttachment.TranscodeStatus);
      }

      string filePath = blogAttachment.FilePath;
      string? directoryPart = Path.GetDirectoryName(filePath);
      string file = Path.GetFileName(filePath);

      string inputPath = Path.Combine(directoryPart!, file);
      string outputPath = DeriveVariantOutputPath(inputPath, encoderOption);

      string ffmpegArgsForOption = BuildFfmpegArgs(encoderOption, inputPath, outputPath);
      string command = $"ffmpeg {ffmpegArgsForOption}";

      TranscodeStatus status = await _blogAttachmentService.UpdateAsync(
        blogAttachment.BlogAttachmentID,
        encoderOption,
        BlogAttachment.BlogAttachmentEncoderStatusConstants.Queued,
        0,
        outputPath,
        command,
        GetUserId(),
        GetOrganizationId()!.Value
      );

      return Ok(status);
    }

    private static string BuildFfmpegArgs(string encoderOption, string inputPath, string outputPath)
    {
      if (string.IsNullOrWhiteSpace(encoderOption)) throw new ArgumentException(nameof(encoderOption));
      if (string.IsNullOrWhiteSpace(inputPath)) throw new ArgumentException(nameof(inputPath));
      if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException(nameof(outputPath));

      var opt = encoderOption.Trim().ToLowerInvariant();

      return opt switch
      {
        "mp3" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -vn -c:a libmp3lame -q:a 2 \"{outputPath}\"",

        "720p" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -vf \"scale=-2:720\" -c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"",

        "original" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -c copy \"{outputPath}\"",

        _ => throw new ArgumentOutOfRangeException(nameof(encoderOption), encoderOption, "Unsupported encoder option")
      };
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