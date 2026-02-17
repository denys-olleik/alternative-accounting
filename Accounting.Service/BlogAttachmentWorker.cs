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

      // Blocking here is acceptable since this runs once at startup
      _tenants = tenantService.GetAllTenantsAsync().GetAwaiter().GetResult();
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        if (!Process.GetProcessesByName("ffmpeg").Any())
        {
          foreach (var tenant in _tenants)
          {
            using var scope = _scopeFactory.CreateScope();

            var blogAttachmentService = new BlogAttachmentService(
                tenant.DatabaseName,
                tenant.DatabasePassword);

            BlogAttachment blogAttachment = await blogAttachmentService.GetOldestAsync(BlogAttachmentEncoderStatusConstants.Processing);
          }
        }

        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }
    }
  }
}