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

    public class TransferViewModel
    {
      public string? PublicId { get; set; }
      public string? DestinationWalletPublicId { get; set; }
      public decimal Amount { get; set; }
      public string? Password { get; set; }
    }

    [Route("transfer")]
    [HttpPost]
    public async Task<IActionResult> Transfer(TransferViewModel model)
    {
      Wallet sourceWallet = await _walletService.TransferAsync(model.PublicId, model.DestinationWalletPublicId, model.Amount, model.Password);

      return RedirectToAction("TransactionComplete", "Transaction", new { publicId = sourceWallet.PublicId });
    }
  }
}