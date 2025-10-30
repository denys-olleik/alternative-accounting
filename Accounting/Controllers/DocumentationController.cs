using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  public class DocumentationController : BaseController
  {
    public IActionResult Introduction()
    {
      return View();
    }

    public IActionResult Pricing()
    {
      return View();
    }

    public IActionResult Features()
    {
      return View();
    }

    public IActionResult Security()
    {
      return View();
    }

    public IActionResult GettingStarted()
    {
      return View();
    }

    public IActionResult Support()
    {
      return View();
    }
  }
}