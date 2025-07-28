The goal of this project is to create one accounting system used by the whole world to reduce administrative costs of running any organization, from laundramat to an aircraft carrier.

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
4. Issue or Assign Equity (e.g., owner’s capital, shares issued)
5. Purchase Inventory
6. Move Inventory Between Locations
7. Record Expense Payment (e.g., rent, utilities)
8. Record Payroll
9. Record Depreciation of Assets
10. Accrue Liabilities (e.g., taxes payable, interest payable)

## How it works

Core concepts:

* Journal - chronological record of all financial transactions good enough to satisfy an audit.
* Account - appropriate architecture around chart of accounts.
* Transaction - grouping of multiple journal entries.
* Double-entry - method for recording journal entries.
* Forward-only - rule that dictates journal entries cannot be modified or deleted.

![Double-entry_example_from_1926.png](Double-entry_example_from_1926.png)

### Journal, Chart of Accounts, and Transactions

```markdown
| Date       | Account               | Debit | Credit |
|------------|-----------------------|-------|--------|
| 2024-06-20 | Accounts Receivable   | 55    |        |
| 2024-06-20 | Sales Revenue         |       | 50     |
| 2024-06-20 | Sales Tax             |       | 5      |
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

The system prevents journal entries from being modified or deleted by not implementing such functionality.

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

## Examples

* Invoice
* Payment
* Reconciliations

### Invoice

* Creating invoice
* Updating invoice line-items
* Removing line-items from invoice
* Void invoice

Order of operations:

1. Create invoice.
2. Create line-items.
3. Record journal entries
4. Join journal entries with invoice line-items and invoice.
  * Wrapping journal entries in a transaction using `TransactionGuid` column.

For each line item in the invoice, create the required journal entries.

```sql
-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "JournalInvoiceInvoiceLine";'
CREATE TABLE "JournalInvoiceInvoiceLine"
(
	"JournalInvoiceInvoiceLineID" SERIAL PRIMARY KEY NOT NULL,
	"JournalId" INT NOT NULL,
	"InvoiceId" INT NOT NULL,
	"InvoiceLineId" INT NOT NULL,
	"ReversedJournalInvoiceInvoiceLineId" INT NULL,
	"TransactionGuid" UUID NOT NULL,
	"Created" TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
	"CreatedById" INT NOT NULL,
	"OrganizationId" INT NOT NULL,
	FOREIGN KEY ("JournalId") REFERENCES "Journal"("JournalID"),
	FOREIGN KEY ("InvoiceId") REFERENCES "Invoice"("InvoiceID"),
	FOREIGN KEY ("InvoiceLineId") REFERENCES "InvoiceLine"("InvoiceLineID"),
	FOREIGN KEY ("ReversedJournalInvoiceInvoiceLineId") REFERENCES "JournalInvoiceInvoiceLine"("JournalInvoiceInvoiceLineID"),
	FOREIGN KEY ("CreatedById") REFERENCES "User"("UserID"),
	FOREIGN KEY ("OrganizationId") REFERENCES "Organization"("OrganizationID")
);
```

### Payment

* Creating payment
  * one invoice multiple payments
	* one payment multuple invoices
* Void payment

### Reconciliations