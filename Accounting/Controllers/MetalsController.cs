using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models.MetalsViewModels;
using Accounting.Service;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("metals")]
  public class MetalsController : BaseController
  {
    private readonly MetalsService _metalsService;

    public MetalsController(MetalsService metalsService)
    {
      _metalsService = metalsService;
    }

    [Route("metals")]
    [HttpGet]
    public IActionResult Metals()
    {
      return View();
    }

    [Route("deposit")]
    [HttpGet]
    public async Task<IActionResult> Deposit()
    {
      DepositMetalViewModel vm = new();

      return View(vm);
    }

    [Route("deposit")]
    [HttpPost]
    public async Task<IActionResult> Deposit(DepositMetalViewModel depositMetalViewModel)
    {
      DepositMetalViewModel.DepositMetalViewModelValidator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(depositMetalViewModel);

      if (!validationResult.IsValid)
      {
        depositMetalViewModel.ValidationResult = validationResult;
        return View(depositMetalViewModel);
      }

      using (TransactionScope scope = new ())
      {
        

        scope.Complete();
      }

      throw new NotImplementedException();
    }
  }
}