using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.ReserveViewModels;
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
    private readonly ReserveService _reserveService;

    public ReserveApiController(RequestContext requestContext)
    {
      _reserveService = new ReserveService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-reserves")]    
    public async Task<IActionResult> GetReserves()
    {
      var reserves = await _reserveService.GetAllAsync(GetOrganizationId()!.Value);
      return Ok(new { reserves = reserves });
    }
  }

  [AuthorizeWithOrganizationId]
  [Route("res")]
  public class ReserveController : BaseController
  {
    private readonly ReserveService _reserveService;

    public ReserveController(ReserveService reserveService)
    {
      _reserveService = reserveService;
    }

    [Route("monetize/{id}")]
    [HttpGet]
    public async Task<IActionResult> Monetize(int id)
    {
      Reserve? reserve = await _reserveService.GetAsync(id, GetOrganizationId()!.Value);
      if (reserve == null) return NotFound();

      MonetizeReserveViewModel vm = new();
      vm.ReserveId = id;

      return View(vm);
    }

    [Route("monetize/{id}")]
    [HttpPost]
    public async Task<IActionResult> Monetize(MonetizeReserveViewModel monetizeReserveViewModel)
    {
      Reserve? reserve = await _reserveService.GetAsync(monetizeReserveViewModel.ReserveId, GetOrganizationId()!.Value);
      if (reserve == null) return NotFound();

      MonetizeReserveViewModel.MonetizeReserveViewModelValidator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(monetizeReserveViewModel);

      if (!validationResult.IsValid)
      {
        monetizeReserveViewModel.ValidationResult = validationResult;
        return View(monetizeReserveViewModel);
      }

      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        // Logic to monetize the reserve would go here
        scope.Complete();
      }

      return RedirectToAction("Reserve");
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
    public async Task<IActionResult> Deposit(DepositReserveViewModel depositReserveViewModel)
    {
      DepositReserveViewModel.DepositReserveViewModelValidator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(depositReserveViewModel);

      if (!validationResult.IsValid)
      {
        depositReserveViewModel.ValidationResult = validationResult;
        return View(depositReserveViewModel);
      }

      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        Reserve metal = await _reserveService.CreateAsync(new Reserve()
        {
          Name = depositReserveViewModel.Name,
          Type = depositReserveViewModel.Type,
          Weight = depositReserveViewModel.Weight,
          Unit = depositReserveViewModel.Unit,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        scope.Complete();
      }

      return RedirectToAction("Reserve");
    }
  }
}