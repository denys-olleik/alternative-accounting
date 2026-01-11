![Double-entry_example_from_1926.png](Double-entry_example_from_1926.png)

# Getting started

1. Clone this repository.
2. Have PostgreSQL installed.
3. Update `appsettings.json`.`DatabasePassword`.
4. Set `database-reset.json`.`Reset` to `true`.
5. Run the application.

## Model any financial operation

1. Create Invoice
2. Receive Payment
3. Record Foreign Currency Transaction
4. Issue or Assign Equity (e.g., owner�s capital, shares issued)
5. Purchase Inventory
6. Move Inventory Between Locations
7. Record Expense Payment (e.g., rent, utilities)
8. Record Payroll
9. Record Depreciation of Assets
10. Accrue Liabilities (e.g., taxes payable, interest payable)
11. ...

## Core concepts

* Context
* Accounts
* Journal
  * debit/credit
  * forward-only
* Operational transaction

### Journal, Chart of Accounts, and Transactions

```markdown invoice with one line item and sales tax
| Date       | Account               | Debit | Credit |
|------------|-----------------------|-------|--------|
| 2024-06-20 | Accounts Receivable   | 123   |        |asset
| 2024-06-20 | Sales Revenue         |       | 100    |revenue
| 2024-06-20 | Sales Tax             |       | 23     |liability
```

```markdown
| Account    | Debit    | Credit   |
|------------|----------|----------|
| Asset      | Increase | Decrease |
| Liability  | Decrease | Increase |
| Equity     | Decrease | Increase |
| Revenue    | Decrease | Increase |
| Expense    | Increase | Decrease |
```

### Double-entry and forward-only

Once entered, journal entries cannot be updated or deleted.

```cs
public int Update(Journal entity)
{
  throw new NotImplementedException();
}

public int Delete(int id)
{
  throw new NotImplementedException();
}
```

## Implemented functionality

* Creating invoice `JournalInvoiceInvoiceLine`, can be further broken up to `JournalInvoiceInvoiceLineCreated`, `JournalInvoiceInvoiceLineUpdated`, and `JournalInvoiceInvoiceLineVoid` for even greater reporting and custom event handling.
* Receiving payment `JournalInvoiceInvoiceLinePayment`
* Reconciliations `JournalReconciliationTransaction`

Helper selects for testing...

```sql
select * from "Journal" order by "JournalID" desc limit 10;
select * from "JournalInvoiceInvoiceLine" order by "JournalInvoiceInvoiceLineID" desc limit 10;
select * from "JournalInvoiceInvoiceLinePayment" order by "JournalInvoiceInvoiceLinePaymentID" desc limit 10;
select * from "JournalReconciliationTransaction" order by "JournalReconciliationTransactionID" desc limit 10;
-- add new select statement for each feature journal integration.
```

... and update `FeaturesIntegratedJournalConstants.cs`...

```cs
using System.Reflection;

namespace Accounting.Common
{
  public class FeaturesIntegratedJournalConstants
  {
    public const string JournalInvoiceInvoiceLine = "JournalInvoiceInvoiceLine";
    public const string JournalInvoiceInvoiceLinePayment = "JournalInvoiceInvoiceLinePayment";
    public const string JournalReconciliationTransaction = "JournalReconciliationTransaction";

    ...
  }
}
```

...when adding new features.
