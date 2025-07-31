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
        // Repopulate dropdowns or other data if needed
        return View(model);
      }

      // 1. Create Reconciliation
      // 2. Process statement transactions into ReconciliationTransaction
      // 3. Create ReconciliationAttachment
      // 4. Save CSV file to disk
      // have private methods for each step if needed
      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        Reconciliation reconciliation = await _reconciliationService.CreateAsync(new Reconciliation
        {
          Name = model.Name,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

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
          """, string.Join("\n", allLines.Split('\n').Take(10)), false, true);

        if (structuredResponse?.isCsv != true)
          throw new InvalidOperationException("The uploaded file is not a valid CSV file.");

        List<ReconciliationTransaction> transactions = new();
        var lines = allLines.Split('\n');
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
              transactions.Add(new ReconciliationTransaction
              {
                ReconciliationId = reconciliation.ReconciliationID,
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

        await _reconciliationTransactionService.ImportAsync(transactions);

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

    public ReconciliationRevApiController(
      ReconciliationService reconciliationService,
      ReconciliationTransactionService reconciliationTransactionService,
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new ReconciliationTransactionService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new ReconciliationService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalReconciliationTransactionService = new JournalReconciliationTransactionService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalService = new JournalService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpPost("record")]
    public async Task<IActionResult> ToggleReconciliationTransaction(ToggleReconciliationTransactionInstructionViewModel model)
    {
      ReconciliationTransaction reconciliationTransaction = await _reconciliationTransactionService.GetAsync(model.ReconciliationTransactionID);

      List<JournalReconciliationTransaction> lastTransaction =
        await _journalReconciliationTransactionService.GetLastTransactionAsync(
          reconciliationTransaction.ReconciliationTransactionID,
          GetOrganizationId()!.Value,
          true);

      List<JournalReconciliationTransaction> thisTransaction = new();

      if (reconciliationTransaction == null)
        return NotFound();

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

          Journal creditEntry = await _journalService.CreateAsync(new Journal
          {
            AccountId = Convert.ToInt32(model.CreditAccount),
            Credit = reconciliationTransaction.Amount,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value
          });

          thisTransaction.Add(await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
          {
            JournalId = debitEntry.JournalID,
            ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
            ReversedJournalReconciliationTransactionId = null,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value,
            TransactionGuid = transactionGuid
          }));

          thisTransaction.Add(await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
          {
            JournalId = creditEntry.JournalID,
            ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
            ReversedJournalReconciliationTransactionId = null,
            CreatedById = GetUserId(),
            OrganizationId = GetOrganizationId()!.Value,
            TransactionGuid = transactionGuid
          }));
        }
        else
        {
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

            thisTransaction.Add(await _journalReconciliationTransactionService.CreateAsync(new JournalReconciliationTransaction
            {
              JournalId = undoEntry.JournalID,
              ReconciliationTransactionId = reconciliationTransaction.ReconciliationTransactionID,
              ReversedJournalReconciliationTransactionId = (item.ReversedJournalReconciliationTransactionId == null) ? item.ReconciliationTransactionId : null,
              TransactionGuid = transactionGuid,
              CreatedById = GetUserId(),
              OrganizationId = GetOrganizationId()!.Value,
            }));
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
          ReconciliationInstruction = (t.JournalReconciliationTransactions == null || !t.JournalReconciliationTransactions.Any())
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