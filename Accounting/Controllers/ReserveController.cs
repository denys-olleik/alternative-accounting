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
  public class MetalApiController : BaseController
  {
    private readonly MetalService _metalService;

    public MetalApiController(RequestContext requestContext)
    {
      _metalService = new MetalService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-metals")]    
    public async Task<IActionResult> GetMetals()
    {
      var metals = await _metalService.GetAllAsync(GetOrganizationId()!.Value);
      return Ok(new { metals = metals });
    }
  }

  [AuthorizeWithOrganizationId]
  [Route("reserves")]
  public class ReserveController : BaseController
  {
    private readonly MetalService _metalService;

    public ReserveController(MetalService metalService)
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

      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        Metal metal = await _metalService.CreateAsync(new Metal()
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

      return RedirectToAction("Metals");
    }
  }
}