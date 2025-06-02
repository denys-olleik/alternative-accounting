using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models;
using Accounting.Models.HomeViewModels;
using Accounting.Service;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  public class HomeController : BaseController
  {
    private readonly BlogService _blogService;

    public HomeController(RequestContext requestContext, BlogService blogService)
    {
      _blogService = new BlogService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);
    }

    public async Task<IActionResult> Index()
    {
      if (!string.IsNullOrWhiteSpace(ConfigurationSingleton.Instance.Whitelabel))
      {
        return WhiteLabelIndex();
      }

      Blog latestPublicPost = await _blogService.GetFirstPublicAsync();

      var markdownPipeline = new Markdig.MarkdownPipelineBuilder().Build();

      LatestPostViewModel indexHomeViewModel = new LatestPostViewModel
      {
        Title = latestPublicPost?.Title,
        Created = latestPublicPost?.Created,
        BlogHtmlSanitizedContent = latestPublicPost?.Content != null
          ? new HtmlSanitizer().Sanitize(
              Markdig.Markdown.ToHtml(latestPublicPost.Content, markdownPipeline))
          : null,
      };

      return View(indexHomeViewModel);
    }

    [HttpGet("game")]
    public IActionResult Game()
    {
      return View();
    }

    public IActionResult WhiteLabelIndex()
    {
      return View("WhiteLabelIndex");
    }

    [HttpGet("unauthorized")]
    public IActionResult Unauthorized()
    {
      return View("Unauthorized");
    }

    [Route("careers")]
    public IActionResult Careers()
    {
      return View();
    }
  }

  [AuthorizeWithOrganizationId]
  [Route("api/player")]
  [ApiController]
  public class PlayerApiController : BaseController
  {
    private readonly PlayerService _playerService;
    private readonly SecretService _secretService;

    public PlayerApiController(
      RequestContext requestContext, 
      PlayerService playerService, 
      SecretService secretService)
    {
      _playerService = new PlayerService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);
      _secretService = new SecretService();
    }

    public class ReportPositionViewModel
    {
      public int X { get; set; }
      public int Y { get; set; }
      public string UserId { get; set; } = null!;
      public List<Player>? Players { get; set; }

      public class Player
      {
        public string? UserId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
      }
    }

    [AllowAnonymous]
    [HttpPost("report-position")]
    public async Task<IActionResult> ReportPosition(ReportPositionViewModel model)
    {
      string ipAddress = GetClientIpAddress();

      string? country = await _playerService.GetCountryAsync(ipAddress, 5);

      if (string.IsNullOrWhiteSpace(country))
      {
        country = await GetCountryAsync(ipAddress);
      }

      if (!string.IsNullOrWhiteSpace(country))
      {
        await _playerService.ReportPosition(model.UserId, model.X, model.Y, ipAddress, country);
      }

      List<Player> players = await _playerService.GetPlayersAsync(300);

      model.Players = players.Select(p => new ReportPositionViewModel.Player
      {
        X = p.X,
        Y = p.Y,
        UserId = p.UserId
      }).ToList();

      return Ok(model);
    }

    private async Task<string?> GetCountryAsync(string ipAddress)
    {
      if (ipAddress == "::1")
      {
        return "localhost";
      }

      Secret abuseIpDbSecret = await _secretService.GetAsync(Secret.SecretTypeConstants.AbuseIpDb, 1);
      if (abuseIpDbSecret == null || string.IsNullOrWhiteSpace(abuseIpDbSecret.Value))
      {
        return null;
      }

      using (var httpClient = new HttpClient())
      {
        httpClient.DefaultRequestHeaders.Add("Key", abuseIpDbSecret.Value);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var url = $"https://api.abuseipdb.com/api/v2/check?ipAddress={ipAddress}";
        HttpResponseMessage response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
          return null;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        using (var doc = System.Text.Json.JsonDocument.Parse(jsonString))
        {
          var root = doc.RootElement;
          if (root.TryGetProperty("data", out var data) && data.TryGetProperty("countryCode", out var countryCode))
          {
            return countryCode.GetString();
          }
        }
      }

      return null;
    }
  }
}