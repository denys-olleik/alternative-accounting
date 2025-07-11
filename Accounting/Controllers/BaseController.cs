﻿using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using static Accounting.Business.Claim;

namespace Accounting.Controllers
{
  public abstract class BaseController : Controller
  {
    [NonAction]
    public string GetClientIpAddress()
    {
      if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
      {
        var ip = HttpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(ip))
          return ip.Split(',')[0].Trim();
      }
      return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    [NonAction]
    public int GetUserId()
    {
      var identity = User?.Identity as ClaimsIdentity;
      if (identity == null)
      {
        throw new InvalidOperationException("User identity is not available.");
      }

      var userIdClaim = identity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
      if (userIdClaim == null)
      {
        throw new InvalidOperationException("User identifier claim is not available.");
      }

      return Convert.ToInt32(userIdClaim.Value);
    }

    [NonAction]
    public int GetTenantId()
    {
      if (User?.Identity is not { IsAuthenticated: true })
      {
        throw new InvalidOperationException("User is not authenticated.");
      }
      var identity = (ClaimsIdentity)User.Identity;
      var tenantIdClaim = identity.FindFirst(CustomClaimTypeConstants.TenantId);
      if (tenantIdClaim == null)
      {
        throw new InvalidOperationException("Tenant identifier claim is not available.");
      }
      return Convert.ToInt32(tenantIdClaim.Value);
    }

    [NonAction]
    public int? GetOrganizationId()
    {
      if (User?.Identity is not { IsAuthenticated: true })
      {
        return null;
      }

      var identity = (ClaimsIdentity)User.Identity;
      var organizationIdClaim = identity.FindFirst(CustomClaimTypeConstants.OrganizationId);

      if (organizationIdClaim == null)
      {
        return null;
      }

      if (int.TryParse(organizationIdClaim.Value, out var organizationId))
      {
        return organizationId;
      }

      return null;
    }

    [NonAction]
    public string GetBaseUrl()
    {
      var request = HttpContext.Request;
      return $"{request.Scheme}://{request.Host}";
    }

    [NonAction]
    public string GetEmail()
    {
      var identity = User?.Identity as ClaimsIdentity;
      if (identity == null)
      {
        throw new InvalidOperationException("User identity is not available.");
      }

      var emailClaim = identity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Email);
      if (emailClaim == null || string.IsNullOrEmpty(emailClaim.Value))
      {
        throw new InvalidOperationException("Email claim is not available or invalid.");
      }

      return emailClaim.Value;
    }

    [NonAction]
    public string GetDatabaseName()
    {
      var identity = User?.Identity as ClaimsIdentity;
      if (identity == null)
      {
        throw new InvalidOperationException("User identity is not available.");
      }

      var databaseNameClaim = identity.Claims.SingleOrDefault(x => x.Type == CustomClaimTypeConstants.DatabaseName);
      if (databaseNameClaim == null || string.IsNullOrEmpty(databaseNameClaim.Value))
      {
        throw new InvalidOperationException("Database name claim is not available or invalid.");
      }

      return databaseNameClaim.Value;
    }

    [NonAction]
    public string GetDatabasePassword()
    {
      var identity = User?.Identity as ClaimsIdentity;
      if (identity == null)
      {
        throw new InvalidOperationException("User identity is not available.");
      }
      var databasePasswordClaim = identity.Claims.SingleOrDefault(x => x.Type == CustomClaimTypeConstants.DatabasePassword);
      if (databasePasswordClaim == null || string.IsNullOrEmpty(databasePasswordClaim.Value))
      {
        throw new InvalidOperationException("Database password claim is not available or invalid.");
      }
      return databasePasswordClaim.Value;
    }

    [NonAction]
    public string? GetRefererUrl()
    {
      return HttpContext.Request.Headers["Referer"].ToString();
    }
  }
}