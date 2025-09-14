using Accounting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;
using Accounting.Models.JournalViewModels;

namespace Accounting.Controllers;

[AuthorizeWithOrganizationId]
[Route("journal")]
public class JournalController : BaseController
{
  [Route("journal")]
  public IActionResult Journal(
    int page = 1,
    int pageSize = 2)
  {
    var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

    var vm = new JournalPaginatedViewModel
    {
      Page = page,
      PageSize = pageSize,
      RememberPageSize = string.IsNullOrEmpty(referer),
    };

    return View(vm);
  }
}