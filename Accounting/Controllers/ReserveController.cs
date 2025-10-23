using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.MetalViewModels;
using Accounting.Service;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/reserve")]
  public class ReserveApiController : BaseController
  {
    private readonly MetalService _metalService;

    public ReserveApiController(RequestContext requestContext)
    {
      _metalService = new MetalService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-reserves")]    
    public async Task<IActionResult> GetReserves()
    {
      var reserves = await _metalService.GetAllAsync(GetOrganizationId()!.Value);
      return Ok(new { metals = reserves });
    }
  }

  [AuthorizeWithOrganizationId]
  [Route("res")]
  public class ReserveController : BaseController
  {
    private readonly MetalService _metalService;

    public ReserveController(MetalService metalService)
    {
      _metalService = metalService;
    }

    [Route("reserve")]
    [HttpGet]
    public IActionResult Reserve()
    {
      return View();
    }

    [Route("deposit")]
    [HttpGet]
    public async Task<IActionResult> Deposit()
    {
      DepositReserveViewModel vm = new();

      return View(vm);
    }

    [Route("deposit")]
    [HttpPost]
    public async Task<IActionResult> Deposit(DepositReserveViewModel depositMetalViewModel)
    {
      DepositReserveViewModel.DepositMetalViewModelValidator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(depositMetalViewModel);

      if (!validationResult.IsValid)
      {
        depositMetalViewModel.ValidationResult = validationResult;
        return View(depositMetalViewModel);
      }

      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        Reserve metal = await _metalService.CreateAsync(new Reserve()
        {
          Name = depositMetalViewModel.Name,
          Type = depositMetalViewModel.Type,
          Weight = depositMetalViewModel.Weight,
          Unit = depositMetalViewModel.Unit,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        scope.Complete();
      }

      return RedirectToAction("Reserve");
    }
  }
}