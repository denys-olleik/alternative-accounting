using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models.UserViewModels;
using Accounting.Service;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Abstractions;
using System.Transactions;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("u")]
  public class UserController : BaseController
  {
    private readonly UserOrganizationService _userOrganizationService;
    private readonly UserService _userService;
    private readonly SecretService _secretService;
    private readonly TenantService _tenantService;
    private readonly ClaimService _claimService;

    public UserController(
      RequestContext requestContext,
      UserOrganizationService userOrganizationService,
      UserService userService,
      SecretService secretService,
      TenantService tenantService,
      ClaimService claimService)
    {
      _userOrganizationService = new UserOrganizationService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _userService = new UserService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _secretService = new SecretService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _tenantService = new TenantService();
      _claimService = new ClaimService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    }

    [HttpGet]
    [Route("update-email/{id}")]
    public async Task<IActionResult> UpdateEmail(int id)
    {
      var user = await _userService.GetAsync(id);
      if (user == null) return NotFound();

      if (user.UserID != GetUserId())
      {
        return Unauthorized();
      }

      var model = new UpdateEmailViewModel
      {
        UserID = user.UserID,
        CurrentEmail = user.Email,
        NewEmail = string.Empty
      };

      return View(model);
    }

    [HttpPost]
    [Route("update-email/{id}")]
    public async Task<IActionResult> UpdateEmail(UpdateEmailViewModel model, int id)
    {
      var user = await _userService.GetAsync(id);
      if (user == null) return NotFound();

      if (user.UserID != GetUserId())
      {
        return Unauthorized();
      }

      var validator = new UpdateEmailViewModel.UpdateEmailViewModelValidator();
      var validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      var existingUser = await _userService.GetAsync(model.NewEmail);
      if (existingUser != null)
      {
        model.ValidationResult = new ValidationResult(new List<ValidationFailure>()
        {
            new ValidationFailure("NewEmail", "Email already exists.")
        });
        model.UserID = id;
        return View(model);
      }

      await _tenantService.UpdateUserEmailAsync(user.Email, model.NewEmail);

      return RedirectToAction("Users", new { page = 1, pageSize = 2 });
    }

    private async Task PopulateUpdateUserViewModelAsync(UpdateUserViewModel model)
    {
      // Fetch organizations
      OrganizationService organizationService = new OrganizationService(GetDatabaseName(), GetDatabasePassword());
      var organizations = await organizationService.GetAllAsync(GetDatabaseName(), GetDatabasePassword());
      model.AvailableOrganizations = organizations.Select(x => new UpdateUserViewModel.OrganizationViewModel
      {
        OrganizationID = x.OrganizationID,
        Name = x.Name
      }).ToList();

      // Fetch user organizations if not already set
      if (string.IsNullOrEmpty(model.SelectedOrganizationIdsCsv))
      {
        UserOrganizationService userOrganizationService = new UserOrganizationService(GetDatabaseName(), GetDatabasePassword());
        var userOrganizations = await userOrganizationService.GetByUserIdAsync(model.UserID, GetDatabaseName(), GetDatabasePassword());
        model.SelectedOrganizationIdsCsv = string.Join(',', userOrganizations.Select(x => x.OrganizationID));
      }

      // Set available roles
      model.AvailableRoles = new List<string>
      {
        UserRoleClaimConstants.TenantManager,
        UserRoleClaimConstants.RoleManager,
        UserRoleClaimConstants.OrganizationManager
      };

      // Fetch selected roles if not already set
      if (model.SelectedRoles == null || !model.SelectedRoles.Any())
      {
        var claimService = new ClaimService(GetDatabaseName(), GetDatabasePassword());
        model.SelectedRoles = await claimService.GetUserRolesAsync(model.UserID, GetOrganizationId() ?? 0, Claim.CustomClaimTypeConstants.Role);
      }

      // Set current requesting user id
      model.CurrentRequestingUserId = GetUserId();
    }

    [AllowWithoutOrganizationId]
    [HttpGet]
    [Route("update/{userId}")]
    public async Task<IActionResult> UpdateUser(int userId)
    {
      User user = await _userService.GetAsync(userId);
      if (user == null) return NotFound();

      var viewModel = new UpdateUserViewModel
      {
        UserID = user.UserID,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
      };

      await PopulateUpdateUserViewModelAsync(viewModel);

      return View(viewModel);
    }

    [AllowWithoutOrganizationId]
    [HttpPost]
    [Route("update/{userId}")]
    public async Task<IActionResult> UpdateUser(UpdateUserViewModel model)
    {
      User user = await _userService.GetAsync(model.UserID);
      if (user == null) return NotFound();

      await PopulateUpdateUserViewModelAsync(model);

      using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        if (user.UserID == GetUserId())
        {
          await _tenantService.UpdateUserAsync(user.Email!, model.FirstName!, model.LastName!);
        }

        foreach (var role in model.AvailableRoles)
        {
          Claim roleClaim = await _claimService.GetAsync(user.UserID, GetDatabaseName(), role);
          int count = await _claimService.GetUserCountWithRoleAsync(role, GetOrganizationId()!.Value);

          if (model.SelectedRoles.Contains(role) && roleClaim == null)
          {
            if (User.IsInRole(role))
              await _claimService.CreateRoleAsync(user.UserID, GetOrganizationId()!.Value, role);
          }
          else if (!model.SelectedRoles.Contains(role) && roleClaim != null)
          {
            if (count > 1)
              await _claimService.RemoveRoleAsync(user.UserID, GetOrganizationId()!.Value, role);
          }
        }

        scope.Complete();
      }

      return RedirectToAction("Users");
    }

    [Route("users")]
    [HttpGet]
    public IActionResult Users(int page = 1, int pageSize = 2)
    {
      var refererHeader = Request.Headers["Referer"];

      var usersViewModel = new UsersPaginatedViewModel()
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(refererHeader)
      };

      return View(usersViewModel);
    }

    [HttpGet]
    [Route("create")]
    public async Task<IActionResult> Create()
    {
      return View();
    }

    [HttpPost]
    [Route("create")]
    public async Task<ActionResult> Create(CreateUserViewModel model)
    {
      CreateUserViewModel.CreateUserViewModelValidator validator = new();
      ValidationResult validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      User existingUser = await _userService.GetAsync(model.Email);
      if (existingUser != null)
      {
        model.ValidationResult = new ValidationResult(new List<ValidationFailure>()
        {
          new ValidationFailure("Email", "Email already exists.")
        });
        return View(model);
      }

      var (existingUser2, tenant) = await _userService.GetFirstOfAnyTenantAsync(model.Email);

      EmailService emailService = new EmailService(_secretService);
      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        User user = await _userService.CreateAsync(new User()
        {
          Email = model.Email,
          FirstName = existingUser2?.FirstName,
          LastName = existingUser2?.LastName,
          Password = existingUser2?.Password ?? PasswordStorage.CreateHash(model.Password),
          CreatedById = GetUserId()
        });

        await _userOrganizationService.CreateAsync(new UserOrganization()
        {
          OrganizationId = GetOrganizationId()!.Value,
          UserId = user.UserID
        });

        scope.Complete();
      }

      return RedirectToAction("Users");
    }

    [HttpGet]
    [Route("details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
      User user = (await _userOrganizationService.GetAsync(id, GetOrganizationId()!.Value)).User!;

      if (user == null)
      {
        return NotFound();
      }

      UserDetailsViewModel userDetailsViewModel = new UserDetailsViewModel();
      userDetailsViewModel.Email = user.Email;
      userDetailsViewModel.FirstName = user.FirstName;
      userDetailsViewModel.LastName = user.LastName;
      userDetailsViewModel.CreatedById = user.CreatedById;
      userDetailsViewModel.Created = user.Created;

      return View(userDetailsViewModel);
    }

    [HttpGet]
    [Route("update-password")]
    public async Task<IActionResult> UpdatePassword()
    {
      return View();
    }

    [HttpPost]
    [Route("update-password")]
    public async Task<IActionResult> UpdatePassword(UpdatePasswordViewModel model)
    {
      UpdatePasswordViewModel.UpdatePasswordViewModelValidator validator
        = new UpdatePasswordViewModel.UpdatePasswordViewModelValidator();
      ValidationResult validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      await _userService.UpdatePasswordAllTenantsAsync(GetEmail(), PasswordStorage.CreateHash(model.NewPassword));

      return RedirectToAction("Users");
    }
  }

  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/user")]
  public class UserApiController : BaseController
  {
    private readonly UserService _userService;

    public UserApiController(RequestContext requestContext, UserService userService)
    {
      _userService = new UserService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-users")]
    public async Task<IActionResult> GetUsers(
      int page = 1,
      int pageSize = 10)
    {
      var (users, nextPage) = await _userService.GetAllAsync(
          page,
          pageSize);

      Models.UserViewModels.GetUsersViewModel getUsersViewModel = new Models.UserViewModels.GetUsersViewModel()
      {
        Users = users.Select(u => new Models.UserViewModels.GetUsersViewModel.UserViewModel()
        {
          UserID = u.UserID,
          RowNumber = u.RowNumber,
          FirstName = u.FirstName,
          LastName = u.LastName,
          Email = u.Email
        }).ToList(),
        Page = page,
        NextPage = nextPage
      };

      return Ok(getUsersViewModel);
    }

    [HttpGet("get-users-filtered")]
    public async Task<IActionResult> GetUsersFiltered(
      string search = null,
      int page = 1,
      int pageSize = 10)
    {
      var users = await _userService.GetFilteredAsync(search);

      Models.UserViewModels.GetUsersViewModel getUsersViewModel = new Models.UserViewModels.GetUsersViewModel()
      {
        Users = users.Select(u => new Models.UserViewModels.GetUsersViewModel.UserViewModel()
        {
          UserID = u.UserID,
          RowNumber = u.RowNumber,
          FirstName = u.FirstName,
          LastName = u.LastName,
          Email = u.Email
        }).ToList()
      };

      return Ok(getUsersViewModel);
    }
  }
}