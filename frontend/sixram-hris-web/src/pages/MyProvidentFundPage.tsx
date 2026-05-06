import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import type { PagedResult, ProvidentFundBalance, ProvidentFundLedgerTransaction, ProvidentFundWithdrawalInput, ProvidentFundWithdrawalRequest } from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

const today = new Date().toISOString().slice(0, 10)

const emptyWithdrawal: ProvidentFundWithdrawalInput = {
  requestDate: today,
  withdrawalType: 'partial',
  requestedAmount: 0,
  reason: '',
  attachmentPath: '',
  remarks: '',
}

export function MyProvidentFundPage() {
  const [balance, setBalance] = useState<ProvidentFundBalance | null>(null)
  const [ledger, setLedger] = useState<PagedResult<ProvidentFundLedgerTransaction> | null>(null)
  const [withdrawals, setWithdrawals] = useState<PagedResult<ProvidentFundWithdrawalRequest> | null>(null)
  const [withdrawalForm, setWithdrawalForm] = useState<ProvidentFundWithdrawalInput>(emptyWithdrawal)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  async function loadData() {
    setIsLoading(true)
    try {
      const [balanceResponse, ledgerResponse, withdrawalResponse] = await Promise.all([
        sixramApi.getMyProvidentFund(),
        sixramApi.getMyProvidentFundLedger({ pageSize: 12 }),
        sixramApi.getMyProvidentFundWithdrawals({ pageSize: 12 }),
      ])
      setBalance(balanceResponse)
      setLedger(ledgerResponse)
      setWithdrawals(withdrawalResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function createWithdrawal() {
    setIsSaving(true)
    try {
      const request = await sixramApi.createMyProvidentFundWithdrawal(withdrawalForm)
      await sixramApi.submitMyProvidentFundWithdrawal(request.id)
      setWithdrawalForm(emptyWithdrawal)
      await loadData()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData()
    }, 0)

    return () => window.clearTimeout(timer)
  }, [])

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading your provident fund...</div>
  }

  return (
    <div className="space-y-6">
      {error ? <div className="shell-state-error">{error}</div> : null}

      {balance ? (
        <>
          <section className="shell-summary-grid">
            <SummaryCard label="Current Balance" value={formatCurrency(balance.grossFundBalance)} />
            <SummaryCard label="Withdrawable" value={formatCurrency(balance.withdrawableBalance)} tone="success" />
            <SummaryCard label="Employee Contribution" value={formatCurrency(balance.totalEmployeeContribution)} />
            <SummaryCard label="Employer Contribution" value={formatCurrency(balance.totalEmployerContribution)} />
            <SummaryCard label="Vested Employer Share" value={formatCurrency(balance.vestedEmployerBalance)} />
            <SummaryCard label="Voluntary + Interest" value={formatCurrency(balance.totalVoluntaryContribution + balance.totalInterest)} />
            <SummaryCard label="Withdrawals" value={formatCurrency(balance.totalWithdrawals)} tone="danger" />
            <SummaryCard label="Vesting" value={`${balance.vestingPercentage}%`} tone="warning" />
          </section>

          <section className="shell-card p-6">
            <div className="grid gap-4 text-sm md:grid-cols-2 xl:grid-cols-4">
              <Detail label="Employee" value={`${balance.employeeCode} | ${balance.employeeFullName}`} />
              <Detail label="Policy" value={balance.policyName || 'Not enrolled'} />
              <Detail label="Enrollment" value={balance.enrollmentDate ? formatDate(balance.enrollmentDate) : 'Not enrolled'} />
              <Detail label="Latest Transaction" value={balance.latestTransactionDate ? formatDate(balance.latestTransactionDate) : 'No transactions'} />
            </div>
          </section>
        </>
      ) : (
        <div className="shell-state-empty">No provident fund enrollment is available for your employee record.</div>
      )}

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="shell-card p-6">
          <h3 className="text-lg font-semibold text-slate-950">Contribution and ledger history</h3>
          <div className="mt-5 shell-table-wrap">
            <table className="shell-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Transaction</th>
                  <th>Type</th>
                  <th>Debit</th>
                  <th>Credit</th>
                  <th>Remarks</th>
                </tr>
              </thead>
              <tbody>
                {ledger?.items.length ? ledger.items.map((record) => (
                  <tr key={record.id}>
                    <td>{formatDate(record.transactionDate)}</td>
                    <td>{record.transactionNumber}</td>
                    <td>{toTitle(record.transactionType)}</td>
                    <td>{formatCurrency(record.debitAmount)}</td>
                    <td>{formatCurrency(record.creditAmount)}</td>
                    <td>{record.remarks}</td>
                  </tr>
                )) : (
                  <tr><td className="text-slate-500" colSpan={6}>No provident fund ledger transactions yet.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <aside className="shell-card p-6">
          <h3 className="text-lg font-semibold text-slate-950">Withdrawal request</h3>
          <div className="mt-5 space-y-4">
            <FormField label="Type">
              <select className="shell-select" onChange={(event) => setWithdrawalForm((current) => ({ ...current, withdrawalType: event.target.value }))} value={withdrawalForm.withdrawalType ?? 'partial'}>
                {['partial', 'full', 'retirement', 'resignation', 'emergency', 'other'].map((type) => (
                  <option key={type} value={type}>{toTitle(type)}</option>
                ))}
              </select>
            </FormField>
            <FormField label="Request Date">
              <input className="shell-input" onChange={(event) => setWithdrawalForm((current) => ({ ...current, requestDate: event.target.value }))} type="date" value={withdrawalForm.requestDate} />
            </FormField>
            <FormField label="Amount">
              <input className="shell-input" min={0} onChange={(event) => setWithdrawalForm((current) => ({ ...current, requestedAmount: Number(event.target.value) }))} step="0.01" type="number" value={withdrawalForm.requestedAmount} />
            </FormField>
            <FormField label="Reason">
              <textarea className="shell-textarea" onChange={(event) => setWithdrawalForm((current) => ({ ...current, reason: event.target.value }))} value={withdrawalForm.reason} />
            </FormField>
            <button className="shell-button w-full" disabled={isSaving || !balance?.withdrawableBalance} onClick={() => void createWithdrawal()} type="button">
              {isSaving ? 'Submitting...' : 'Submit Request'}
            </button>
          </div>
        </aside>
      </section>

      <section className="shell-card p-6">
        <h3 className="text-lg font-semibold text-slate-950">Withdrawal history</h3>
        <div className="mt-5 shell-table-wrap">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Request</th>
                <th>Date</th>
                <th>Type</th>
                <th>Requested</th>
                <th>Approved</th>
                <th>Status</th>
                <th>Paid</th>
              </tr>
            </thead>
            <tbody>
              {withdrawals?.items.length ? withdrawals.items.map((record) => (
                <tr key={record.id}>
                  <td>{record.requestNumber}</td>
                  <td>{formatDate(record.requestDate)}</td>
                  <td>{toTitle(record.withdrawalType)}</td>
                  <td>{formatCurrency(record.requestedAmount)}</td>
                  <td>{formatCurrency(record.approvedAmount)}</td>
                  <td>{toTitle(record.status)}</td>
                  <td>{record.paymentDate ? formatDateTime(record.paymentDate) : 'Not paid'}</td>
                </tr>
              )) : (
                <tr><td className="text-slate-500" colSpan={7}>No withdrawal requests yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

function SummaryCard({ label, tone = 'default', value }: { label: string; tone?: 'default' | 'success' | 'warning' | 'danger'; value: string }) {
  const toneClass = tone === 'success' ? 'text-emerald-700' : tone === 'warning' ? 'text-amber-700' : tone === 'danger' ? 'text-rose-700' : 'text-slate-950'
  return (
    <div className="shell-summary-card">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">{label}</p>
      <p className={`mt-3 text-xl font-semibold ${toneClass}`}>{value}</p>
    </div>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="shell-detail-label">{label}</p>
      <p className="mt-1 font-semibold text-slate-900">{value}</p>
    </div>
  )
}

function FormField({ children, label }: { children: ReactNode; label: string }) {
  return <label className="block"><span className="shell-label">{label}</span>{children}</label>
}

function toTitle(value: string) {
  return value.replace(/_/g, ' ').replace(/\b\w/g, (match) => match.toUpperCase())
}
