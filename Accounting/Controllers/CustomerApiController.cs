﻿using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.BusinessEntityViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/c")]
  public class CustomerApiController : BaseController
  {
    private readonly BusinessEntityService _businessEntityService;
    private readonly RequestContext _requestContext;

    public CustomerApiController(RequestContext requestContext)
    {
      _requestContext = requestContext;
      _businessEntityService = new BusinessEntityService(_requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-customers")]
    public async Task<IActionResult> GetCustomers(int page = 1, int pageSize = 2)
    {
      var (businessEntities, nextPageNumber) = await _businessEntityService.GetAllAsync(page, pageSize, GetOrganizationId()!.Value);

      var getCustomersViewModel = new GetBusinessEntitiesViewModel
      {
        BusinessEntities = businessEntities.Select(c => new BusinessEntityViewModel
        {
          ID = c.BusinessEntityID,
          RowNumber = c.RowNumber,
          CustomerType = c.CustomerType,
          FirstName = c.FirstName,
          LastName = c.LastName,
          CompanyName = c.CompanyName,
        }).ToList(),
        Page = page,
        NextPage = nextPageNumber,
      };

      return Ok(getCustomersViewModel);
    }
  }
}