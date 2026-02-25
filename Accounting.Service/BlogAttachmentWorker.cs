using Accounting.Business;
using Accounting.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using static Accounting.Business.BlogAttachment;

namespace Accounting.Workers
{
  public class BlogAttachmentWorker : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Tenant> _tenants;

    public BlogAttachmentWorker(IServiceScopeFactory scopeFactory)
    {
      _scopeFactory = scopeFactory;

      using var scope = _scopeFactory.CreateScope();
      var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
      _tenants = tenantService.GetAllTenantsAsync().GetAwaiter().GetResult();
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        foreach (var tenant in _tenants)
        {
          var blogAttachmentService = new BlogAttachmentService(
            tenant.DatabaseName,
            tenant.DatabasePassword);

          // Fetch oldest queued variant (atomic status update inside manager)
          BlogAttachment? blogAttachment = await blogAttachmentService.GetOldestAsync(
            BlogAttachmentVariant.StateConstants.Queued);

          if (blogAttachment == null)
            continue;

          foreach (var kvp in blogAttachment.TranscodeStatus) // iterate variants
          {
            var variantKey = kvp.Key;
            var variantStatus = kvp.Value;

            if (variantStatus.State != BlogAttachmentVariant.StateConstants.Queued)
              continue;

            // 1️⃣ Generate unique progress file path
            string progressFilePath = Path.Combine(
              Path.GetDirectoryName(blogAttachment.FilePath)!,
              $"{variantKey}_{Guid.NewGuid():N}.progress");

            // 2️⃣ Update database: mark as Processing and save progress file path
            //await blogAttachmentService.UpdateTranscodeStatusJSONBAsync(
            //  blogAttachment.BlogAttachmentID,
            //  variantKey, 
            //  BlogAttachmentEncoderStatusConstants.Processing,
            //  progressFilePath,
            //  variantStatus.VariantPath!,
            //  command: null, // will set after building command
            //  userId: blogAttachment.CreatedById,
            //  organizationId: blogAttachment.OrganizationId
            //);

            // 3️⃣ Build full FFmpeg command with progress file
            string ffmpegArgs = BuildFfmpegArgs(
              variantKey,
              blogAttachment.FilePath,
              variantStatus.VariantPath!,
              progressFilePath);

            // 4️⃣ Start detached process
            var processStartInfo = new ProcessStartInfo
            {
              FileName = "ffmpeg",
              Arguments = ffmpegArgs,
              RedirectStandardOutput = false,
              RedirectStandardError = false,
              UseShellExecute = true,
              CreateNoWindow = true
            };

            Process.Start(processStartInfo);
          }
        }

        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }
    }

    private static string BuildFfmpegArgs(
      string encoderOption,
      string inputPath,
      string outputPath,
      string progressFilePath)
    {
      string args = encoderOption.Trim().ToLowerInvariant() switch
      {
        "mp3" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -vn -c:a libmp3lame -q:a 2 \"{outputPath}\"",
        "720p" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -vf \"scale=-2:720\" -c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"",
        "original" =>
          $"-y -hide_banner -nostdin -i \"{inputPath}\" -c copy \"{outputPath}\"",
        _ => throw new ArgumentOutOfRangeException(nameof(encoderOption), encoderOption, "Unsupported encoder option")
      };

      // Append progress file argument (overwrites in-place each update)
      return $"{args} -progress \"{progressFilePath}\" -nostats";
    }
  }
}