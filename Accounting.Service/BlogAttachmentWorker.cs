using Accounting.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Accounting.Workers
{
  public class BlogAttachmentWorker : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;

    public BlogAttachmentWorker(IServiceScopeFactory scopeFactory)
    {
      _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        using (var scope = _scopeFactory.CreateScope())
        {
          var blogAttachmentService = scope.ServiceProvider.GetRequiredService<BlogAttachmentService>();

          // TODO:
          // 1. Discover queued variants
          //blogAttachmentService.
          // 2. Atomically transition to processing
          // 3. Execute ffmpeg
          // 4. Update progress + completion
        }

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }
    }
  }
}