using Accounting.Business;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [Route("wallet")]
  public class WalletController : BaseController
  {
    private readonly WalletService _walletService;

    public WalletController(RequestContext requestContext, WalletService walletService)
    {
      _walletService = new WalletService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [Route("transfer")]
    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
      return View();
    }

    //[Route("transfer")]
    //[HttpPost]
    //public async Task<IActionResult> Transfer(TransferViewModel model)
    //{
    //  _walletService.TransferAsync(model.PublicId, model.DestinationWalletPublicId)
    //}
  }
}