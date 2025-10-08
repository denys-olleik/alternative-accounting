using Accounting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("metals")]
  public class MetalsController : BaseController
  {
    [Route("metals")]
    public IActionResult Metals()
    {
      return View();
    }
  }
}