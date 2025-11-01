using Accounting.CustomAttributes;
using Accounting.Models.VideoViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("video")]
  public class VideoController : BaseController
  {
    [HttpGet]
    [Route("videos")]
    public IActionResult Videos(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new VideosPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }

    [HttpGet]
    [Route("upload")]
    public IActionResult Upload()
    {
      return View();
    }

    [HttpPost]
    [Route("upload")]
    public IActionResult Upload(UploadVideoViewModel model)
    {
      return RedirectToAction("Videos");
    }
  }
}