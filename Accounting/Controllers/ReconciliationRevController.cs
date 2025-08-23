using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models;
using Accounting.Models.ReconciliationViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace Accounting.Controllers
{
  [Route("recrev")]
  [AuthorizeWithOrganizationId]
  public class ReconciliationRevController
    : BaseController
  {
    private readonly ReconciliationTransactionService _reconciliationTransactionService;
    private readonly ReconciliationService _reconciliationService;
    private readonly ReconciliationAttachmentService _reconciliationAttachmentService;
    private readonly AccountService _accountService;

    public ReconciliationRevController(
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationAttachmentService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _accountService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet]
    [Route("reconciliation-transactions/{id}")]
    public async Task<IActionResult> ReconciliationTransactions(
      int id,
      int page,
      int pageSize)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var reconciliation = await _reconciliationService.GetByIdAsync(id, GetOrganizationId()!.Value);
      var accounts = await _accountService.GetAllAsync(GetOrganizationId()!.Value, false);

      if (reconciliation == null)
        return NotFound();

      reconciliation.ReconciliationAttachment = await _reconciliationAttachmentService.GetByReconciliationIdAsync(reconciliation.ReconciliationID, GetOrganizationId()!.Value);

      var model = new ReconciliationTransactionsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,

        Accounts = accounts.Select(a => new ReconciliationTransactionsPaginatedViewModel.AccountViewModel
        {
          AccountID = a.AccountID,
          Name = a.Name,
          Type = a.Type.ToString()
        }).ToList(),

        ReconciliationID = reconciliation.ReconciliationID,
        Name = reconciliation.Name,
        Status = reconciliation.Status,
        OriginalFileName = reconciliation.ReconciliationAttachment?.OriginalFileName,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(model);
    }

    public class CsvAnalysisResult
    {
      public bool isCsv { get; set; }
      public int firstDataRow { get; set; }
    }

    public class ReconciliationTransactionResult
    {
      public DateTime TransactionDate { get; set; }
      public string Description { get; set; }
      public decimal Amount { get; set; }
    }

    [HttpGet]
    [Route("create")]
    public async Task<IActionResult> Create()
    {
      var model = new CreateReconciliationRevViewModel();
      // Populate any dropdowns or defaults here if needed
      return View(model);
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(CreateReconciliationRevViewModel model)
    {
      var validator = new CreateReconciliationRevViewModel.CreateReconciliationViewModelValidator();
      var validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      // Scope only around DB work (create reconciliation + insert transactions)
      Reconciliation reconciliation;
      List<ReconciliationTransaction> transactions = new();

      // Everything outside DB scope
      string allLines = string.Empty;
      if (model.StatementCsv != null && model.StatementCsv.Length > 0)
      {
        using (var reader = new StreamReader(model.StatementCsv.OpenReadStream()))
        {
          allLines = await reader.ReadToEndAsync();
        }
      }
      else
      {
        allLines = model.StatementCsvText!;
      }

      LanguageModelService languageModelService = new LanguageModelService();
      var (response, structuredResponse) = await languageModelService.GenerateResponse<CsvAnalysisResult>("""
        is this a CSV file and on what line is the first row of data?

        example response:
        {
          "isCsv": true,
          "firstDataRow": 2
        }
        """,
        string.Join("\n", allLines.Split('\n').Take(10)),
        false,
        true
      );

      if (structuredResponse?.isCsv != true)
        throw new InvalidOperationException("The uploaded file is not a valid CSV file.");

      var lines = allLines.Split('\n');
      // Build transactions in memory (no DB yet)
      for (int i = structuredResponse.firstDataRow - 1; i < lines.Length; i++)
      {
        string row = lines[i].Trim();
        if (string.IsNullOrWhiteSpace(row))
          continue;

        await languageModelService.GenerateResponse<ReconciliationTransactionResult>($"""
        Process this CSV row into a ReconciliationTransaction object:
        ```csv
        {row}
        ```
      
        Respond with a JSON object that includes TransactionDate, Description, and Amount fields.
        **IMPORTANT:** The TransactionDate field must be in ISO 8601 format (`yyyy-MM-dd`), for example: "2024-06-03".
        Respond with only the JSON object, no code block or extra text.
        """,
          string.Empty,
          true,
          true
        ).ContinueWith(t =>
        {
          if (t.Result.structuredResponse != null)
          {
            // ReconciliationId is not known yet; set placeholder, assign after create
            transactions.Add(new ReconciliationTransaction
            {
              ReconciliationId = 0,
              RawData = row,
              TransactionDate = t.Result.structuredResponse.TransactionDate,
              Description = t.Result.structuredResponse.Description,
              Amount = t.Result.structuredResponse.Amount,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value,
            });
          }
        });
      }

      // Only DB work inside this scope
      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        reconciliation = await _reconciliationService.CreateAsync(new Reconciliation
        {
          Name = model.Name,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        // Assign the generated ReconciliationID now that we have it
        foreach (var t in transactions)
          t.ReconciliationId = reconciliation.ReconciliationID;

        // Option A: bulk insert (single call)
        //await _reconciliationTransactionService.ImportAsync(transactions);

        //Option B: per - row insert(uncomment this block and comment Option A to use foreach style)
        foreach (var transaction in transactions)
        {
          await _reconciliationTransactionService.ImportAsync(transaction);
        }

        scope.Complete();
      }

      return RedirectToAction("Reconciliations");
    }

    [Route("reconciliationsrev")]
    [HttpGet]
    public async Task<IActionResult> Reconciliations(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new ReconciliationsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }
  }

  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/rclrev")]
  public class ReconciliationRevApiController : BaseController
  {
    private readonly ReconciliationTransactionService _reconciliationTransactionService;
    private readonly ReconciliationService _reconciliationService;
    private readonly JournalReconciliationTransactionService _journalReconciliationTransactionService;
    private readonly JournalService _journalService;
    private readonly AccountService _accountService;

    public ReconciliationRevApiController(
      ReconciliationService reconciliationService,
      //ReconciliationTransactionService reconciliationTransactionService,
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new ReconciliationTransactionService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new ReconciliationService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalReconciliationTransactionService = new JournalReconciliationTransactionService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalService = new JournalService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _accountService = new AccountService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpPost("record")]
    public async Task<IActionResult> ToggleReconciliationTransaction(ToggleReconciliationTransactionInstructionViewModel model)
    {
      ReconciliationTransaction reconciliationTransaction = await _reconciliationTransactionService.GetAsync(model.ReconciliationTransactionID);

      if (reconciliationTransaction == null)
        return NotFound();

      List<JournalReconciliationTransaction> lastTransaction =
        await _journalReconciliationTransactionService.GetLastTransactionAsync(
          reconciliationTransaction.ReconciliationTransactionID,
          GetOrganizationId()!.Value,
          true);

      List<JournalReconciliationTransaction> thisTransaction = new();

      var transactionGuid = GuidExtensions.CreateSecureGuid();

      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        if (lastTransaction.Count == 0)
        {
          Journal debitEntry = await _journalService.CreateAsync(new Journal
          {
            AccountId = Convert.ToInt32(model.DebitAccount),
            Debit = reconciliationTransaction.Amount,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value
          });
          debitEntry.Account = await _accountService.GetAsync(debitEntry.AccountId, GetOrganizationId()!.Value);

          Journal creditEntry = await _journalService.CreateAsync(new Journal
          {
            AccountId = Convert.ToInt32(model.CreditAccount),
            Credit = reconciliationTransaction.Amount,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value
          });
          creditEntry.Account = await _accountService.GetAsync(creditEntry.AccountId, GetOrganizationId()!.Value);

          var debitJrt = await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
          {
            JournalId = debitEntry.JournalID,
            ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
            ReversedJournalReconciliationTransactionId = null,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value,
            TransactionGuid = transactionGuid,
            Journal = debitEntry
          }, true);

          debitJrt.Journal.Account = debitEntry.Account;
          thisTransaction.Add(debitJrt);

          var creditJrt = await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
          {
            JournalId = creditEntry.JournalID,
            ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
            ReversedJournalReconciliationTransactionId = null,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value,
            TransactionGuid = transactionGuid,
            Journal = creditEntry
          }, true);

          creditJrt.Journal.Account = creditEntry.Account;
          thisTransaction.Add(creditJrt);
        }
        else
        {
          // Determine if the last transaction was a reversal (all entries have ReversedJournalReconciliationTransactionId set)
          bool lastWasReversal = lastTransaction.All(x => x.ReversedJournalReconciliationTransactionId != null);

          if (lastWasReversal)
          {
            // This is a reassignment (toggle ON) - use accounts from the request
            Journal debitEntry = await _journalService.CreateAsync(new Journal
            {
              AccountId = Convert.ToInt32(model.DebitAccount),
              Debit = reconciliationTransaction.Amount,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value
            });
            debitEntry.Account = await _accountService.GetAsync(debitEntry.AccountId, GetOrganizationId()!.Value);

            Journal creditEntry = await _journalService.CreateAsync(new Journal
            {
              AccountId = Convert.ToInt32(model.CreditAccount),
              Credit = reconciliationTransaction.Amount,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value
            });
            creditEntry.Account = await _accountService.GetAsync(creditEntry.AccountId, GetOrganizationId()!.Value);

            var debitJrt = await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
            {
              JournalId = debitEntry.JournalID,
              ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
              ReversedJournalReconciliationTransactionId = null,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value,
              TransactionGuid = transactionGuid,
              Journal = debitEntry
            }, true);

            debitJrt.Journal.Account = debitEntry.Account;
            thisTransaction.Add(debitJrt);

            var creditJrt = await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
            {
              JournalId = creditEntry.JournalID,
              ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
              ReversedJournalReconciliationTransactionId = null,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value,
              TransactionGuid = transactionGuid,
              Journal = creditEntry
            }, true);

            creditJrt.Journal.Account = creditEntry.Account;
            thisTransaction.Add(creditJrt);
          }
          else
          {
            // This is a reversal (toggle OFF) - undo previous entries
            foreach (var item in lastTransaction)
            {
              var undoEntry = await _journalService.CreateAsync(new Journal
              {
                AccountId = item.Journal.AccountId,
                Debit = item.Journal.Credit,
                Credit = item.Journal.Debit,
                CreatedById = GetUserId(),
                OrganizationId = GetOrganizationId()!.Value
              });
              undoEntry.Account = await _accountService.GetAsync(undoEntry.AccountId, GetOrganizationId()!.Value);

              var undoJrt = await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
              {
                JournalId = undoEntry.JournalID,
                ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
                ReversedJournalReconciliationTransactionId = (item.ReversedJournalReconciliationTransactionId == null) ? item.Identifiable : null,
                TransactionGuid = transactionGuid,
                CreatedById = GetUserId(),
                OrganizationId = GetOrganizationId()!.Value,
                Journal = undoEntry
              }, true);

              undoJrt.Journal.Account = undoEntry.Account;
              thisTransaction.Add(undoJrt);
            }
          }
        }

        scope.Complete();
      }

      return Ok(new
      {
        Instruction = (lastTransaction.Count > 0 && lastTransaction[0].ReversedJournalReconciliationTransactionId == null)
          ? "none"
          : $"D: {thisTransaction.Single(x => x.Journal.Debit != null).Journal.Account.Name}, C: {thisTransaction.Single(x => x.Journal.Credit != null).Journal.Account.Name}"
      });
    }

    [HttpGet("get-reconciliation-transactions")]
    public async Task<IActionResult> GetReconciliationTransactions(
      int reconciliationId,
      int page = 1,
      int pageSize = 2)
    {
      var (transactions, nextPage) = await _reconciliationTransactionService.GetReconciliationTransactionsAsync(reconciliationId, page, pageSize, GetOrganizationId()!.Value);

      foreach (ReconciliationTransaction transaction in transactions)
      {
        List<JournalReconciliationTransaction> lastTransaction = await _journalReconciliationTransactionService.GetLastTransactionAsync(transaction.ReconciliationTransactionID, GetOrganizationId()!.Value, true);

        foreach (var journalReconciliationTransaction in lastTransaction)
        {
          Account account = await _accountService.GetAsync(journalReconciliationTransaction.Journal.AccountId, GetOrganizationId()!.Value);

          journalReconciliationTransaction.Journal!.Account = account;
        }

        transaction.JournalReconciliationTransactions!.AddRange(lastTransaction);
      }

      GetReconciliationTransactionsViewModel model = new GetReconciliationTransactionsViewModel
      {
        ReconciliationID = reconciliationId,
        Page = page,
        PageSize = pageSize,
        NextPage = nextPage,
        ReconciliationTransactions = transactions.Select(t => new GetReconciliationTransactionsViewModel.ReconciliationTransactionViewModel
        {
          ReconciliationTransactionID = t.ReconciliationTransactionID,
          ReconciliationInstruction =
            !t.JournalReconciliationTransactions.Any() ||
            t.JournalReconciliationTransactions.Last().ReversedJournalReconciliationTransactionId != null
              ? "none"
              : $"D: {t.JournalReconciliationTransactions.Single(x => x.Journal.Debit != null).Journal.Account.Name}, C: {t.JournalReconciliationTransactions.Single(x => x.Journal.Credit != null).Journal.Account.Name}",
          RowNumber = t.RowNumber,
          TransactionDate = t.TransactionDate,
          Description = t.Description,
          Amount = t.Amount,
          RawData = t.RawData
        }).ToList()
      };

      return Ok(model);
    }

    [HttpGet("get-reconciliations")]
    public async Task<IActionResult> GetReconciliations(int page = 1, int pageSize = 2)
    {
      var (reconciliations, nextPage) = await _reconciliationService.GetAllAsync(page, pageSize, GetOrganizationId()!.Value);

      var getReconciliationsViewModel = new GetReconciliationsViewModel
      {
        Reconciliations = reconciliations.Select(r => new GetReconciliationsViewModel.ReconciliationViewModel
        {
          ReconciliationID = r.ReconciliationID,
          RowNumber = r.RowNumber!.Value,
          Name = r.Name,
          Status = r.Status
        }).ToList(),
        Page = page,
        NextPage = nextPage,
      };

      return Ok(getReconciliationsViewModel);
    }
  }
}