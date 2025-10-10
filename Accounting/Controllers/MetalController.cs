using Accounting.Business;
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
  public class MetalController : BaseController
  {
    private readonly MetalService _metalService;

    public MetalController(MetalService metalService)
    {
      _metalService = metalService;
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

      using (TransactionScope scope = new())
      {
        Metal metal = await _metalService.CreateAsync(new Metal()
        {
          Type = depositMetalViewModel.Type,
          Weight = depositMetalViewModel.Weight,
          Unit = depositMetalViewModel.Unit,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        scope.Complete();
      }

      return RedirectToAction("Metals");
    }
  }
}