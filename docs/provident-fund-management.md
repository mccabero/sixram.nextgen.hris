# Provident Fund Management

This module adds ledger-backed provident fund administration for HR, Finance, and employee self-service.

## Scope

- Policy setup with active/inactive status, contribution rules, voluntary contribution, withdrawal, and loan flags.
- Vesting rules per policy with duplicate threshold validation.
- Employee enrollment with duplicate active enrollment prevention and status changes.
- Monthly contribution batches with draft, reviewed, posted, and cancelled states.
- Ledger posting for employee, employer, voluntary, withdrawal, adjustment, reversal, forfeiture, interest, and settlement entries.
- Read-only employee balances computed from ledger entries.
- Withdrawal requests with review, approval, rejection, payment, and paid-ledger posting.
- Manual adjustments with approval before posting.
- HR/Finance dashboard, reports, and employee self-service fund view.

## Ledger Design

The ledger is the source of truth. Balances are computed from `ProvidentFundLedgerTransactions`; no editable balance field is used as the authoritative value.

Posting operations use database transactions:

- Contribution batch posting creates contribution ledger entries per batch line.
- Withdrawal payment creates a withdrawal ledger entry.
- Adjustment posting creates an adjustment ledger entry.
- Reversal creates a separate reversing ledger entry and marks the original transaction reversed.

Posted contribution batches are locked from edit. Corrections should use reversal or adjustment records.

## Seed Data

Database seeding creates one active policy:

- Name: Regular Employee Provident Fund
- Employee contribution: 5% of basic salary
- Employer contribution: 5% of basic salary
- Frequency: Monthly
- Status: Active

Seeded vesting rules:

- 0 years: 0%
- 1 year: 20%
- 2 years: 40%
- 3 years: 60%
- 4 years: 80%
- 5 years: 100%

## API Surfaces

Admin/HR/Finance APIs are under `/api/provident-fund`.

Employee self-service APIs are under `/api/me/provident-fund`.

The controller protects admin endpoints for Administrator, HR, and PayrollOfficer roles. The self-service controller limits data access to the authenticated user's linked employee record.

## UI Surfaces

Admin/HR/Finance menu:

- Provident Fund Dashboard
- Fund Policies
- Vesting Rules
- Employee Enrollment
- Monthly Contributions
- Fund Ledger
- Withdrawals
- Adjustments
- Reports

Employee self-service menu:

- My Provident Fund

## Manual Test Checklist

- Create or update a provident fund policy.
- Add, edit, and delete vesting rules; verify duplicate year thresholds are rejected.
- Enroll an employee; verify duplicate active enrollment is rejected.
- Generate a monthly contribution batch and inspect calculated employee and employer shares.
- Review and post the batch; verify ledger transactions are created and the posted batch is locked.
- Open employee balance and verify totals are computed from ledger entries.
- Submit a withdrawal request; approve it; mark it paid; verify withdrawal ledger posting.
- Try to withdraw more than the withdrawable balance and verify validation blocks it.
- Create, approve, and post a manual adjustment; verify the ledger and balance update.
- Reverse a ledger transaction; verify a reversing entry is created.
- Run contribution, balance, withdrawal, and ledger reports with filters.
- Log in as an employee and verify only that employee's provident fund data is visible.

## Compliance Note

Actual provident fund rules, eligibility, vesting, tax treatment, withdrawal rules, final settlement rules, forfeiture handling, and reporting obligations must be validated by HR, Finance, Legal, and local labor compliance before production use.
