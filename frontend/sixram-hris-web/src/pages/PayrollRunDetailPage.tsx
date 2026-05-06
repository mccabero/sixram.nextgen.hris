import { useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import { PayrollStatusBadge } from '../components/PayrollStatusBadge'
import type { PayrollRunActionInput, PayrollRunDetail, PayrollRunItem } from '../types/models'
import { formatDate, formatDateTime, formatMinutes } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

type RunActionMode = 'submit' | 'approve' | 'paid' | 'cancel'

export function PayrollRunDetailPage() {
  const { payrollRunId } = useParams<{ payrollRunId: string }>()
  const [detail, setDetail] = useState<PayrollRunDetail | null>(null)
  const [selectedItem, setSelectedItem] = useState<PayrollRunItem | null>(null)
  const [actionMode, setActionMode] = useState<RunActionMode | null>(null)
  const [actionRemarks, setActionRemarks] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!payrollRunId) {
      return
    }

    void loadDetail(payrollRunId)
  }, [payrollRunId])

  async function loadDetail(id: string) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getPayrollRunById(id)
      setDetail(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handleAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!detail || !payrollRunId || !actionMode) {
      return
    }

    setIsSubmitting(true)
    setError(null)

    const payload: PayrollRunActionInput = { remarks: actionRemarks }

    try {
      if (actionMode === 'submit') {
        await sixramApi.submitPayrollRunForReview(payrollRunId, payload)
      } else if (actionMode === 'approve') {
        await sixramApi.approvePayrollRun(payrollRunId, payload)
      } else if (actionMode === 'paid') {
        await sixramApi.markPayrollRunPaid(payrollRunId, payload)
      } else {
        await sixramApi.cancelPayrollRun(payrollRunId, payload)
      }

      setActionMode(null)
      setActionRemarks('')
      await loadDetail(payrollRunId)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleRecalculate() {
    if (!payrollRunId) {
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      await sixramApi.recalculatePayrollRun(payrollRunId)
      await loadDetail(payrollRunId)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading payroll run...</div>
  }

  if (!detail) {
    return (
      <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
        {error ?? 'Payroll run not found.'}
      </div>
    )
  }

  const { run, payPeriod, items, auditLogs } = detail

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <span className="shell-badge-brand">{run.referenceNumber}</span>
              <PayrollStatusBadge status={run.status} />
              {run.holdCount > 0 ? <span className="shell-badge-danger">{run.holdCount} held</span> : null}
            </div>
            <h3 className="mt-4 text-2xl font-semibold text-slate-950">{run.name}</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review payroll items, issues, and audit activity before approval or payroll release.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/admin/payroll">
              Back to Payroll
            </Link>
            <button className="shell-button-secondary" disabled={isSubmitting} onClick={() => void handleRecalculate()} type="button">
              Recalculate
            </button>
            <button className="shell-button-secondary" onClick={() => {
              setActionMode('submit')
              setActionRemarks('')
            }} type="button">
              Submit Review
            </button>
            <button className="shell-button-secondary" onClick={() => {
              setActionMode('approve')
              setActionRemarks('')
            }} type="button">
              Approve
            </button>
            <button className="shell-button-secondary" onClick={() => {
              setActionMode('paid')
              setActionRemarks('')
            }} type="button">
              Mark Paid
            </button>
            <button className="shell-button-danger" onClick={() => {
              setActionMode('cancel')
              setActionRemarks('')
            }} type="button">
              Cancel
            </button>
          </div>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
        ) : null}

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Employees" value={String(run.employeeCount)} />
          <SummaryCard label="Critical issues" value={String(run.criticalIssueCount)} tone="danger" />
          <SummaryCard label="Gross pay" value={formatCurrency(run.totalGrossPay)} tone="brand" />
          <SummaryCard label="Net pay" value={formatCurrency(run.totalNetPay)} tone="success" />
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <h4 className="text-lg font-semibold text-slate-950">Pay period snapshot</h4>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <InfoCard label="Pay Period" value={payPeriod.name} />
            <InfoCard label="Frequency" value={payPeriod.payFrequency.replace('_', ' ')} />
            <InfoCard label="Period Start" value={formatDate(payPeriod.periodStartDate)} />
            <InfoCard label="Period End" value={formatDate(payPeriod.periodEndDate)} />
            <InfoCard label="Cutoff Start" value={formatDate(payPeriod.cutoffStartDate)} />
            <InfoCard label="Cutoff End" value={formatDate(payPeriod.cutoffEndDate)} />
            <InfoCard label="Payroll Date" value={formatDate(payPeriod.payrollDate)} />
            <InfoCard label="Status" value={payPeriod.status} />
          </dl>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <h4 className="text-lg font-semibold text-slate-950">Run metadata</h4>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <InfoCard label="Generated By" value={run.generatedByDisplayName || 'System'} />
            <InfoCard label="Generated At" value={formatDateTime(run.generatedAtUtc)} />
            <InfoCard label="Approved By" value={run.approvedByDisplayName || '-'} />
            <InfoCard label="Approved At" value={run.approvedAtUtc ? formatDateTime(run.approvedAtUtc) : '-'} />
            <InfoCard label="Paid At" value={run.paidAtUtc ? formatDateTime(run.paidAtUtc) : '-'} />
            <InfoCard className="sm:col-span-2" label="Remarks" value={run.remarks || 'No remarks provided.'} />
          </dl>
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Payroll items</h4>
            <p className="mt-1 text-sm text-slate-500">Review employee-level payroll snapshots, issues, and payslips.</p>
          </div>
          <span className="shell-badge-muted">{items.length} items</span>
        </div>

        <div className="shell-table-wrap mt-5">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Work Summary</th>
                <th>Gross / Deductions</th>
                <th>Net</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.length ? (
                items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{item.employeeName}</div>
                      <div className="mt-1 text-slate-500">{item.employeeCode}</div>
                      <div className="mt-1 text-slate-500">{[item.departmentName, item.positionName, item.branchName].filter(Boolean).join(' | ')}</div>
                    </td>
                    <td className="text-slate-600">
                      <div>{item.regularWorkedDays} days | {item.regularWorkedHours} hrs</div>
                      <div className="mt-1 text-xs text-slate-500">
                        Paid leave {item.paidLeaveDays} | Unpaid leave {item.unpaidLeaveDays} | Absent {item.absentDays}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        Late {formatMinutes(item.lateMinutes)} | Under {formatMinutes(item.undertimeMinutes)} | OT {formatMinutes(item.overtimeMinutes)}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatCurrency(item.grossPay, item.currency)}</div>
                      <div className="mt-1 text-xs text-slate-500">{formatCurrency(item.totalDeductions, item.currency)}</div>
                    </td>
                    <td className="font-semibold text-slate-900">{formatCurrency(item.netPay, item.currency)}</td>
                    <td>
                      <div className="space-y-2">
                        <PayrollStatusBadge status={item.status} />
                        {item.hasCriticalIssues ? <span className="shell-badge-danger">Needs review</span> : null}
                      </div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary px-3 py-2" onClick={() => setSelectedItem(item)} type="button">
                          Breakdown
                        </button>
                        <Link className="shell-button-secondary px-3 py-2" to={`/admin/payroll/payslips/${item.id}`}>
                          Payslip
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No payroll items found for this run.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Audit trail</h4>
            <p className="mt-1 text-sm text-slate-500">Server-side payroll events recorded during generation and review.</p>
          </div>
          <span className="shell-badge-muted">{auditLogs.length} logs</span>
        </div>

        <div className="mt-5 space-y-3">
          {auditLogs.length ? (
            auditLogs.map((entry) => (
              <div className="rounded-2xl border border-slate-200 bg-white p-4" key={entry.id}>
                <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-900">{entry.action}</p>
                    <p className="mt-1 text-sm text-slate-500">{entry.summary}</p>
                    <p className="mt-1 text-xs text-slate-500">{entry.entityType} | {entry.entityId}</p>
                  </div>
                  <div className="text-left xl:text-right">
                    <p className="text-sm font-semibold text-slate-900">{entry.actorDisplayName || 'System'}</p>
                    <p className="mt-1 text-xs text-slate-500">{formatDateTime(entry.createdAtUtc)}</p>
                  </div>
                </div>
              </div>
            ))
          ) : (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No audit entries found.</div>
          )}
        </div>
      </section>

      <Modal
        description={actionMode ? `Apply the ${actionMode.replace('_', ' ')} action to this payroll run.` : ''}
        onClose={() => {
          if (!isSubmitting) {
            setActionMode(null)
            setActionRemarks('')
          }
        }}
        open={Boolean(actionMode)}
        title={actionMode ? `${actionMode.replace('_', ' ')} payroll run` : 'Payroll Action'}
      >
        <form className="space-y-5" onSubmit={handleAction}>
          <FormField label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setActionRemarks(event.target.value)} rows={4} value={actionRemarks} />
          </FormField>
          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setActionMode(null)} type="button">
              Close
            </button>
            <button className="shell-button" disabled={isSubmitting} type="submit">
              {isSubmitting ? 'Working...' : 'Submit'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Review the earnings, deductions, and issue list that make up this payroll item snapshot."
        onClose={() => setSelectedItem(null)}
        open={Boolean(selectedItem)}
        title={selectedItem ? `${selectedItem.employeeName} Payroll Item` : 'Payroll Item'}
      >
        {selectedItem ? (
          <div className="space-y-5">
            <div className="grid gap-4 sm:grid-cols-2">
              <InfoCard label="Net Pay" value={formatCurrency(selectedItem.netPay, selectedItem.currency)} />
              <InfoCard label="Gross Pay" value={formatCurrency(selectedItem.grossPay, selectedItem.currency)} />
              <InfoCard label="Total Deductions" value={formatCurrency(selectedItem.totalDeductions, selectedItem.currency)} />
              <InfoCard label="Employer Share" value={formatCurrency(selectedItem.employerContributionTotal, selectedItem.currency)} />
            </div>

            <div>
              <p className="text-sm font-semibold text-slate-900">Issues</p>
              <div className="mt-3 space-y-2">
                {selectedItem.issues.length ? (
                  selectedItem.issues.map((issue) => (
                    <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800" key={issue}>
                      {issue}
                    </div>
                  ))
                ) : (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No issues for this payroll item.</div>
                )}
              </div>
            </div>

            <div className="grid gap-5 xl:grid-cols-2">
              <div>
                <p className="text-sm font-semibold text-slate-900">Earnings</p>
                <div className="mt-3 space-y-2">
                  {selectedItem.earnings.map((line) => (
                    <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={line.id}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-900">{line.earningTypeName || line.description}</p>
                          <p className="mt-1 text-xs text-slate-500">{line.description}</p>
                        </div>
                        <p className="text-sm font-semibold text-slate-900">{formatCurrency(line.amount, selectedItem.currency)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <p className="text-sm font-semibold text-slate-900">Deductions</p>
                <div className="mt-3 space-y-2">
                  {selectedItem.deductions.map((line) => (
                    <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={line.id}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-900">{line.deductionTypeName || line.description}</p>
                          <p className="mt-1 text-xs text-slate-500">{line.description}</p>
                        </div>
                        <p className="text-sm font-semibold text-slate-900">{formatCurrency(line.amount, selectedItem.currency)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        ) : null}
      </Modal>
    </div>
  )
}

function SummaryCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: string
  tone?: 'default' | 'danger' | 'success' | 'brand'
}) {
  const className =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'success'
        ? 'border-emerald-200 bg-emerald-50'
        : tone === 'brand'
          ? 'border-[#465fff]/20 bg-[#465fff]/5'
          : 'border-slate-200 bg-white'

  return (
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function InfoCard({
  label,
  value,
  className = '',
}: {
  label: string
  value: string
  className?: string
}) {
  return (
    <div className={['rounded-xl border border-slate-200 bg-white p-4', className].join(' ')}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  )
}

function FormField({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div>
      <label className="shell-label">{label}</label>
      {children}
    </div>
  )
}
