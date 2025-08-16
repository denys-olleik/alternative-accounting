using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  public class IndexController : BaseController
  {
    public IActionResult Index()
    {
      return View();
    }
  }
}