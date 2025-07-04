﻿using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.Account;
using Accounting.Service;
using Accounting.Validators;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("a")]
  public class AccountController
    : BaseController
  {
    private readonly AccountService _accountService;
    private readonly JournalService _journalService;

    public AccountController(
      AccountService accountService, 
      JournalService journalService,
      RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _journalService = new JournalService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    }

    [HttpGet]
    [Route("delete/{accountId}")]
    public async Task<IActionResult> Delete(int accountId)
    {
      Account account = await _accountService.GetAsync(accountId, GetOrganizationId()!.Value);
      if (account == null)
        return NotFound();
      DeleteAccountViewModel model = new DeleteAccountViewModel
      {
        AccountID = account.AccountID,
        Name = account.Name
      };
      return View(model);
    }

    [HttpPost]
    [Route("delete/{accountId}")]
    public async Task<IActionResult> Delete(DeleteAccountViewModel model)
    {
      await _accountService.DeleteAsync(model.AccountID, GetOrganizationId()!.Value);
      return RedirectToAction("Accounts");
    }

    [Route("accounts")]
    [HttpGet]
    public IActionResult Accounts(
      int page = 1, 
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new AccountsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }


    [Route("create/{parentAccountId?}")]
    [HttpGet]
    public async Task<IActionResult> Create(int? parentAccountId)
    {
      Account parentAccount = null;

      if (parentAccountId.HasValue)
      {
        parentAccount = await _accountService.GetAsync(parentAccountId.Value, GetOrganizationId()!.Value);
        if (parentAccount == null)
          return NotFound();
      }

      var model = new CreateAccountViewModel();
      model.AvailableAccountTypes = Account.AccountTypeConstants.All.ToList();

      if (parentAccount != null)
      {
        model.ParentAccountId = parentAccountId;
        model.ParentAccount = new AccountViewModel()
        {
          AccountID = parentAccount!.AccountID,
          Name = parentAccount!.Name
        };
      }

      return View(model);
    }

    [Route("create/{parentAccountId?}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountViewModel model)
    {
      CreateAccountViewModelValidator validator = new CreateAccountViewModelValidator(_accountService);
      ValidationResult validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        model.AvailableAccountTypes = Account.AccountTypeConstants.All.ToList();

        return View(model);
      }

      Account account = new Account
      {
        Name = model.AccountName,
        Type = model.SelectedAccountType,
        ParentAccountId = model.ParentAccountId,
        OrganizationId = GetOrganizationId()!.Value,
        CreatedById = GetUserId()
      };

      await _accountService.CreateAsync(account);

      return RedirectToAction("Accounts");
    }

    [Route("update/{accountId}")]
    [HttpGet]
    public async Task<IActionResult> Update(int accountId)
    {
      Account account = await _accountService.GetAsync(accountId, GetOrganizationId()!.Value);
      if (account == null)
        return NotFound();

      Account? parentAccount = null;

      if (account.ParentAccountId.HasValue)
        parentAccount = await _accountService.GetAsync(account.ParentAccountId!.Value, GetOrganizationId()!.Value);

      UpdateAccountViewModel model = new UpdateAccountViewModel()
      {
        AccountID = account.AccountID,
        AccountName = account.Name,
        SelectedAccountType = account.Type,
        ShowInInvoiceCreationDropDownForCredit = account.InvoiceCreationForCredit,
        ShowInInvoiceCreationDropDownForDebit = account.InvoiceCreationForDebit,
        ShowInReceiptOfPaymentDropDownForCredit = account.ReceiptOfPaymentForCredit,
        ShowInReceiptOfPaymentDropDownForDebit = account.ReceiptOfPaymentForDebit,
        ReconciliationExpense = account.ReconciliationExpense,
        ReconciliationLiabilitiesAndAssets = account.ReconciliationLiabilitiesAndAssets,
        AvailableAccountTypes = Account.AccountTypeConstants.All.ToList()
      };

      if (parentAccount != null)
      {
        model.ParentAccountId = parentAccount.AccountID;
        model.ParentAccount = new AccountViewModel()
        {
          AccountID = parentAccount.AccountID,
          Name = parentAccount.Name
        };
      }

      return View(model);
    }

    [Route("update/{accountId}")]
    [HttpPost]
    public async Task<IActionResult> Update(int accountId, UpdateAccountViewModel model)
    {
      UpdateAccountViewModelValidator validator = new UpdateAccountViewModelValidator(_accountService, _journalService, GetOrganizationId()!.Value);
      ValidationResult validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        model.AvailableAccountTypes = Account.AccountTypeConstants.All.ToList();

        if (model.ParentAccountId.HasValue)
        {
          var parentAccount = await _accountService.GetAsync(model.ParentAccountId.Value, GetOrganizationId()!.Value);
          if (parentAccount != null)
          {
            model.ParentAccount = new AccountViewModel()
            {
              AccountID = parentAccount.AccountID,
              Name = parentAccount.Name
            };
          }
        }

        return View(model);
      }

      Account account = await _accountService.GetAsync(model.AccountID, GetOrganizationId()!.Value);
      if (account == null)
        return NotFound();

      account.Name = model.AccountName;
      account.Type = model.SelectedAccountType;
      account.InvoiceCreationForCredit = model.ShowInInvoiceCreationDropDownForCredit;
      account.InvoiceCreationForDebit = model.ShowInInvoiceCreationDropDownForDebit;
      account.ReceiptOfPaymentForCredit = model.ShowInReceiptOfPaymentDropDownForCredit;
      account.ReceiptOfPaymentForDebit = model.ShowInReceiptOfPaymentDropDownForDebit;
      account.ReconciliationExpense = model.ReconciliationExpense;
      account.ReconciliationLiabilitiesAndAssets = model.ReconciliationLiabilitiesAndAssets;

      await _accountService.UpdateAsync(account);

      return RedirectToAction("Accounts");
    }
  }

  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/a")]
  public class AccountApiController : BaseController
  {
    private readonly AccountService _accountService;

    public AccountApiController(AccountService accountService, RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount(CreateAccountViewModel model)
    {
      Account existingAccount = await _accountService.GetByAccountNameAsync(model.AccountName, GetOrganizationId()!.Value);

      if (existingAccount != null)
      {
        AccountViewModel existingAccountViewModel = new()
        {
          AccountID = existingAccount.AccountID,
          Name = existingAccount.Name
        };
        return Ok(existingAccountViewModel);
      }

      model.SelectedAccountType = Account.AccountTypeConstants.Assets;
      CreateAccountViewModelValidator validator = new CreateAccountViewModelValidator(_accountService);
      ValidationResult validationResult = await validator.ValidateAsync(model);
      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return BadRequest(model);
      }
      Account account = new Account
      {
        Name = model.AccountName,
        Type = Account.AccountTypeConstants.Assets,
        OrganizationId = GetOrganizationId()!.Value,
        CreatedById = GetUserId()
      };

      account = await _accountService.CreateAsync(account);

      AccountViewModel newAccount = new()
      {
        AccountID = account.AccountID,
        Name = account.Name
      };

      return Ok(newAccount);
    }

    [HttpGet("account-types")]
    public IActionResult AccountConstants()
    {
      return Ok(Account.AccountTypeConstants.All);
    }

    [HttpGet("get-all-accounts")]
    public async Task<IActionResult> GetAllAccounts(
      bool includeDescendants,
      bool includeJournalEntriesCount,
      int page = 1,
      int pageSize = 10)
    {
      (List<Account> accounts, int? nextPage)
        = await _accountService.GetAllAsync(
          page,
          pageSize,
          GetOrganizationId()!.Value,
          includeJournalEntriesCount,
          includeDescendants);

      AccountViewModel ConvertToViewModel(Account account)
      {
        var viewModel = new AccountViewModel
        {
          AccountID = account.AccountID,
          Name = account.Name,
          Type = account.Type,
          JournalEntryCount = account.JournalEntryCount,
          InvoiceCreationForCredit = account.InvoiceCreationForCredit,
          InvoiceCreationForDebit = account.InvoiceCreationForDebit,
          ReceiptOfPaymentForCredit = account.ReceiptOfPaymentForCredit,
          ReceiptOfPaymentForDebit = account.ReceiptOfPaymentForDebit,
          Created = account.Created,
          ParentAccountId = account.ParentAccountId,
          CreatedById = account.CreatedById,
          Children = new List<AccountViewModel>()
        };

        if (account.Children != null)
        {
          foreach (var child in account.Children)
          {
            viewModel.Children.Add(ConvertToViewModel(child));
          }
        }

        return viewModel;
      }

      return Ok(new GetAllAccountsViewModel
      {
        Accounts = accounts.Select(ConvertToViewModel).ToList(),
        Page = page,
        NextPage = nextPage,
        PageSize = pageSize,
      });
    }

    [HttpGet("all-reconciliation-expense")]
    public async Task<IActionResult> GetAllReconciliationExpenseAccounts()
    {
      var organizationId = GetOrganizationId()!.Value;
      List<Account> accounts = await _accountService.GetAllReconciliationExpenseAccountsAsync(organizationId);

      List<AccountViewModel> accountsViewmodel = accounts.Select(x => new AccountViewModel
      {
        AccountID = x.AccountID,
        Name = x.Name,
        Type = x.Type,
        InvoiceCreationForCredit = x.InvoiceCreationForCredit,
        InvoiceCreationForDebit = x.InvoiceCreationForDebit,
        ReceiptOfPaymentForCredit = x.ReceiptOfPaymentForCredit,
        ReceiptOfPaymentForDebit = x.ReceiptOfPaymentForDebit,
        Created = x.Created,
        ParentAccountId = x.ParentAccountId,
        CreatedById = x.CreatedById,
        Children = new List<AccountViewModel>()
      }).ToList();

      return Ok(accountsViewmodel);
    }

    [HttpGet("all-reconciliation-liabilities-and-assets")]
    public async Task<IActionResult> GetAllReconciliationLiabilitiesAndAssetsAccounts()
    {
      var organizationId = GetOrganizationId()!.Value;
      List<Account> accounts = await _accountService.GetAllReconciliationLiabilitiesAndAssetsAsync(organizationId);

      List<AccountViewModel> accountsViewmodel = accounts.Select(x => new AccountViewModel
      {
        AccountID = x.AccountID,
        Name = x.Name,
        Type = x.Type,
        InvoiceCreationForCredit = x.InvoiceCreationForCredit,
        InvoiceCreationForDebit = x.InvoiceCreationForDebit,
        ReceiptOfPaymentForCredit = x.ReceiptOfPaymentForCredit,
        ReceiptOfPaymentForDebit = x.ReceiptOfPaymentForDebit,
        Created = x.Created,
        ParentAccountId = x.ParentAccountId,
        CreatedById = x.CreatedById,
        Children = new List<AccountViewModel>()
      }).ToList();

      return Ok(accountsViewmodel);
    }

    
  }
}

namespace Accounting.Models.Account
{
  public abstract class BaseAccountViewModel
  {
    public int? ParentAccountId { get; set; }
    public AccountViewModel? ParentAccount { get; set; }
    public List<string> AvailableAccountTypes { get; set; } = new List<string>();
  }

  public class DeleteAccountViewModel
  {
    public int AccountID { get; set; }
    public string Name { get; set; }
  }

  public class AccountsViewModel
  {
    public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
  }

  public class AccountViewModel
  {
    public AccountViewModel()
    {
      Name = string.Empty;
      Type = string.Empty;
    }

    public int AccountID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int JournalEntryCount { get; set; }
    public bool InvoiceCreationForCredit { get; set; }
    public bool InvoiceCreationForDebit { get; set; }
    public bool ReceiptOfPaymentForCredit { get; set; }
    public bool ReceiptOfPaymentForDebit { get; set; }
    public bool ReconciliationExpense { get; set; }
    public bool ReconciliationLiabilitiesAndAssets { get; set; }
    public DateTime Created { get; set; }
    public int? ParentAccountId { get; set; }
    public int CreatedById { get; set; }
    public List<AccountViewModel>? Children { get; set; }
  }

  public class CreateAccountViewModel : BaseAccountViewModel
  {
    private string? _accountName;
    public string? AccountName
    {
      get => _accountName;
      set => _accountName = value?.Trim();
    }
    public string? SelectedAccountType { get; set; }
    public bool ShowInInvoiceCreationDropDownForCredit { get; set; }
    public bool ShowInInvoiceCreationDropDownForDebit { get; set; }
    public bool ShowInReceiptOfPaymentDropDownForCredit { get; set; }
    public bool ShowInReceiptOfPaymentDropDownForDebit { get; set; }
    public bool ReconciliationExpense { get; set; }
    public bool ReconciliationLiabilitiesAndAssets { get; set; }
    public ValidationResult? ValidationResult { get; set; }
  }

  public class UpdateAccountViewModel : BaseAccountViewModel
  {
    public int AccountID { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? SelectedAccountType { get; set; }
    public bool ShowInInvoiceCreationDropDownForCredit { get; set; }
    public bool ShowInInvoiceCreationDropDownForDebit { get; set; }
    public bool ShowInReceiptOfPaymentDropDownForCredit { get; set; }
    public bool ShowInReceiptOfPaymentDropDownForDebit { get; set; }
    public bool ReconciliationExpense { get; set; }
    public bool ReconciliationLiabilitiesAndAssets { get; set; }
    public ValidationResult? ValidationResult { get; set; }
  }

  public class GetAllAccountsViewModel : PaginatedViewModel
  {
    public List<AccountViewModel>? Accounts { get; set; }
  }

  public class AccountsPaginatedViewModel : PaginatedViewModel
  {

  }
}

namespace Accounting.Validators
{
  public class UpdateAccountViewModelValidator : AbstractValidator<UpdateAccountViewModel>
  {
    private readonly AccountService _accountService;
    private readonly JournalService _journalService;

    public UpdateAccountViewModelValidator(AccountService accountService, JournalService journalService, int organizationId)
    {
      _accountService = accountService;
      _journalService = journalService;

      RuleFor(x => x.AccountID)
          .NotEmpty().WithMessage("'Account ID' is required.");

      RuleFor(x => x.AccountName)
          .NotEmpty().WithMessage("'Account Name' is required.")
          .MaximumLength(200).WithMessage("'Account Name' cannot be longer than 200 characters.")
          .MustAsync(async (model, accountName, cancellation) =>
              await BeUniqueAccountName(model.AccountID, accountName, organizationId))
          .WithMessage("Account Name must be unique within the organization.");

      RuleFor(x => x.SelectedAccountType)
          .NotEmpty().WithMessage("'Account Type' is required.")
          .MaximumLength(50).WithMessage("'Account Type' cannot be longer than 50 characters.")
          .Must(x => Account.AccountTypeConstants.All.Contains(x)).WithMessage("'Account Type' is invalid.")
          .MustAsync(async (model, accountType, cancellation) =>
              await CanUpdateAccountType(model.AccountID, accountType, organizationId, model.SelectedAccountType))
          .WithMessage("Account Type cannot be changed if there are existing journal entries.");
    }

    private async Task<bool> BeUniqueAccountName(int accountId, string accountName, int organizationId)
    {
      var result = await _accountService.GetByAccountNameAsync(accountName, organizationId);
      return result == null || result.AccountID == accountId;
    }

    private async Task<bool> CanUpdateAccountType(int accountId, string newAccountType, int organizationId, string currentAccountType)
    {
      List<Account> accounts = await _accountService.GetAllAsync(organizationId, true);

      Account? account = accounts.SingleOrDefault(x => x.AccountID == accountId);

      if (account == null)
      {
        return false;
      }

      // Only perform checks if the account type is actually being changed
      if (account.Type == newAccountType)
      {
        return true;
      }

      // Check if parent account is of the same type
      if (account.ParentAccountId.HasValue)
      {
        Account? parentAccount = accounts.SingleOrDefault(x => x.AccountID == account.ParentAccountId.Value);

        if (parentAccount == null || parentAccount.Type != newAccountType)
        {
          return false;
        }
      }

      // Check for journal entries up the tree
      if (HasJournalEntriesInParents(account, accounts))
      {
        return false;
      }

      // Check for journal entries down the tree
      if (HasJournalEntriesInChildren(account, accounts))
      {
        return false;
      }

      return true;
    }

    private bool HasJournalEntriesInParents(Account account, List<Account> accounts)
    {
      Account? currentAccount = account;
      while (currentAccount != null)
      {
        if (currentAccount.JournalEntryCount > 0)
        {
          return true;
        }

        if (currentAccount.ParentAccountId.HasValue)
        {
          currentAccount = accounts.SingleOrDefault(x => x.AccountID == currentAccount.ParentAccountId.Value);
        }
        else
        {
          currentAccount = null;
        }
      }
      return false;
    }

    private bool HasJournalEntriesInChildren(Account account, List<Account> accounts)
    {
      foreach (var childAccount in accounts.Where(x => x.ParentAccountId == account.AccountID))
      {
        if (childAccount.JournalEntryCount > 0)
        {
          return true;
        }

        if (HasJournalEntriesInChildren(childAccount, accounts))
        {
          return true;
        }
      }

      return false;
    }
  }
}