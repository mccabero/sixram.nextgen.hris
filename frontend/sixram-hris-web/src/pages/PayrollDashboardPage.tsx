import { useEffect, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import { PayrollStatusBadge } from '../components/PayrollStatusBadge'
import type {
  GeneratePayrollRunInput,
  PayPeriodListQuery,
  PayPeriodRecord,
  PayrollAdjustmentListQuery,
  PayrollAdjustmentRecord,
  PayrollDashboardSummary,
  PayrollOptions,
  PayrollRunActionInput,
  PayrollRunListQuery,
  PayrollRunSummary,
  PagedResult,
  SavePayPeriodInput,
  SavePayrollAdjustmentInput,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'
import { formatCurrency } from '../utils/money'

type RunActionState = {
  mode: 'submit' | 'approve' | 'paid' | 'cancel'
  run: PayrollRunSummary
}

type AdjustmentActionState = {
  mode: 'approve' | 'reject' | 'cancel'
  adjustment: PayrollAdjustmentRecord
}

const defaultPayPeriodQuery: PayPeriodListQuery = {
  search: '',
  status: '',
  payFrequency: '',
  pageNumber: 1,
  pageSize: 5,
  sortBy: 'period_start',
  descending: true,
}

const defaultRunQuery: PayrollRunListQuery = {
  search: '',
  payPeriodId: '',
  status: '',
  pageNumber: 1,
  pageSize: 6,
  sortBy: 'generated',
  descending: true,
}

const defaultAdjustmentQuery: PayrollAdjustmentListQuery = {
  search: '',
  payPeriodId: '',
  payrollRunId: '',
  status: '',
  adjustmentType: '',
  pageNumber: 1,
  pageSize: 6,
  sortBy: 'created',
  descending: true,
}

const emptyPayPeriodEditor: SavePayPeriodInput = {
  code: '',
  name: '',
  payFrequency: 'semi_monthly',
  periodStartDate: '',
  periodEndDate: '',
  payrollDate: '',
  cutoffStartDate: '',
  cutoffEndDate: '',
  status: 'open',
  remarks: '',
  payPeriodTemplateId: '',
}

const emptyRunEditor: GeneratePayrollRunInput = {
  payPeriodId: '',
  referenceNumber: '',
  name: '',
  departmentId: '',
  branchId: '',
  employmentTypeId: '',
  employmentStatusId: '',
  selectedEmployeeIds: [],
  remarks: '',
}

const emptyAdjustmentEditor: SavePayrollAdjustmentInput = {
  employeeId: '',
  payPeriodId: '',
  payrollRunId: '',
  adjustmentType: 'earning',
  earningTypeId: '',
  deductionTypeId: '',
  amount: 0,
  reason: '',
}

export function PayrollDashboardPage() {
  const [summary, setSummary] = useState<PayrollDashboardSummary | null>(null)
  const [options, setOptions] = useState<PayrollOptions | null>(null)
  const [payPeriods, setPayPeriods] = useState<PagedResult<PayPeriodRecord> | null>(null)
  const [runs, setRuns] = useState<PagedResult<PayrollRunSummary> | null>(null)
  const [adjustments, setAdjustments] = useState<PagedResult<PayrollAdjustmentRecord> | null>(null)
  const [payPeriodQuery, setPayPeriodQuery] = useState<PayPeriodListQuery>(defaultPayPeriodQuery)
  const [runQuery, setRunQuery] = useState<PayrollRunListQuery>(defaultRunQuery)
  const [adjustmentQuery, setAdjustmentQuery] = useState<PayrollAdjustmentListQuery>(defaultAdjustmentQuery)
  const [error, setError] = useState<string | null>(null)
  const [payPeriodErrors, setPayPeriodErrors] = useState<Record<string, string[]>>({})
  const [runErrors, setRunErrors] = useState<Record<string, string[]>>({})
  const [adjustmentErrors, setAdjustmentErrors] = useState<Record<string, string[]>>({})
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isLoadingPayPeriods, setIsLoadingPayPeriods] = useState(false)
  const [isLoadingRuns, setIsLoadingRuns] = useState(false)
  const [isLoadingAdjustments, setIsLoadingAdjustments] = useState(false)
  const [payPeriodModalOpen, setPayPeriodModalOpen] = useState(false)
  const [editingPayPeriod, setEditingPayPeriod] = useState<PayPeriodRecord | null>(null)
  const [payPeriodEditor, setPayPeriodEditor] = useState<SavePayPeriodInput>(emptyPayPeriodEditor)
  const [runModalOpen, setRunModalOpen] = useState(false)
  const [runEditor, setRunEditor] = useState<GeneratePayrollRunInput>(emptyRunEditor)
  const [adjustmentModalOpen, setAdjustmentModalOpen] = useState(false)
  const [editingAdjustment, setEditingAdjustment] = useState<PayrollAdjustmentRecord | null>(null)
  const [adjustmentEditor, setAdjustmentEditor] = useState<SavePayrollAdjustmentInput>(emptyAdjustmentEditor)
  const [runActionState, setRunActionState] = useState<RunActionState | null>(null)
  const [adjustmentActionState, setAdjustmentActionState] = useState<AdjustmentActionState | null>(null)
  const [actionRemarks, setActionRemarks] = useState('')
  const [deletePayPeriodTarget, setDeletePayPeriodTarget] = useState<PayPeriodRecord | null>(null)
  const [deleteAdjustmentTarget, setDeleteAdjustmentTarget] = useState<PayrollAdjustmentRecord | null>(null)
  const [isSavingPayPeriod, setIsSavingPayPeriod] = useState(false)
  const [isSavingRun, setIsSavingRun] = useState(false)
  const [isSavingAdjustment, setIsSavingAdjustment] = useState(false)
  const [isSubmittingAction, setIsSubmittingAction] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadPayPeriods()
  }, [options, payPeriodQuery])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadRuns()
  }, [options, runQuery])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadAdjustments()
  }, [options, adjustmentQuery])

  async function loadInitialData() {
    setIsInitialLoading(true)

    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getPayrollSummary(),
        sixramApi.getPayrollOptions(),
      ])

      setSummary(summaryResponse)
      setOptions(optionsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsInitialLoading(false)
    }
  }

  async function loadSummary() {
    try {
      const response = await sixramApi.getPayrollSummary()
      setSummary(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadPayPeriods() {
    setIsLoadingPayPeriods(true)

    try {
      const response = await sixramApi.getPayPeriods(payPeriodQuery)
      setPayPeriods(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingPayPeriods(false)
    }
  }

  async function loadRuns() {
    setIsLoadingRuns(true)

    try {
      const response = await sixramApi.getPayrollRuns(runQuery)
      setRuns(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingRuns(false)
    }
  }

  async function loadAdjustments() {
    setIsLoadingAdjustments(true)

    try {
      const response = await sixramApi.getPayrollAdjustments(adjustmentQuery)
      setAdjustments(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingAdjustments(false)
    }
  }

  async function refreshAll() {
    await Promise.all([loadSummary(), loadPayPeriods(), loadRuns(), loadAdjustments()])
  }

  function openCreatePayPeriodModal() {
    setEditingPayPeriod(null)
    setPayPeriodErrors({})
    setPayPeriodEditor({
      ...emptyPayPeriodEditor,
      payFrequency: options?.payFrequencies[0] ?? 'semi_monthly',
      status: 'open',
      payPeriodTemplateId: options?.payPeriodTemplates[0]?.id ?? '',
    })
    setPayPeriodModalOpen(true)
  }

  function openEditPayPeriodModal(record: PayPeriodRecord) {
    setEditingPayPeriod(record)
    setPayPeriodErrors({})
    setPayPeriodEditor({
      code: record.code,
      name: record.name,
      payFrequency: record.payFrequency,
      periodStartDate: record.periodStartDate,
      periodEndDate: record.periodEndDate,
      payrollDate: record.payrollDate,
      cutoffStartDate: record.cutoffStartDate,
      cutoffEndDate: record.cutoffEndDate,
      status: record.status,
      remarks: record.remarks,
      payPeriodTemplateId: record.payPeriodTemplateId ?? '',
    })
    setPayPeriodModalOpen(true)
  }

  function openGenerateRunModal() {
    const payPeriodId = options?.payPeriods[0]?.id ?? ''
    setRunErrors({})
    setRunEditor({
      ...emptyRunEditor,
      payPeriodId,
      referenceNumber: buildRunReference(),
      name: options?.payPeriods[0] ? `${options.payPeriods[0].name} Payroll` : 'Payroll Run',
    })
    setRunModalOpen(true)
  }

  function openCreateAdjustmentModal() {
    setEditingAdjustment(null)
    setAdjustmentErrors({})
    setAdjustmentEditor({
      ...emptyAdjustmentEditor,
      employeeId: options?.employees[0]?.id ?? '',
      payPeriodId: options?.payPeriods[0]?.id ?? '',
      earningTypeId: options?.earningTypes[0]?.id ?? '',
    })
    setAdjustmentModalOpen(true)
  }

  function openEditAdjustmentModal(record: PayrollAdjustmentRecord) {
    setEditingAdjustment(record)
    setAdjustmentErrors({})
    setAdjustmentEditor({
      employeeId: record.employeeId,
      payPeriodId: record.payPeriodId ?? '',
      payrollRunId: record.payrollRunId ?? '',
      adjustmentType: record.adjustmentType,
      earningTypeId: record.earningTypeId ?? '',
      deductionTypeId: record.deductionTypeId ?? '',
      amount: record.amount,
      reason: record.reason,
    })
    setAdjustmentModalOpen(true)
  }

  async function handleSavePayPeriod(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingPayPeriod(true)
    setPayPeriodErrors({})
    setError(null)

    try {
      if (editingPayPeriod) {
        await sixramApi.updatePayPeriod(editingPayPeriod.id, payPeriodEditor)
      } else {
        await sixramApi.createPayPeriod(payPeriodEditor)
      }

      setPayPeriodModalOpen(false)
      await Promise.all([loadSummary(), loadPayPeriods()])
    } catch (caughtError) {
      setError(formatError(caughtError))
      setPayPeriodErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingPayPeriod(false)
    }
  }

  async function handleGenerateRun(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingRun(true)
    setRunErrors({})
    setError(null)

    try {
      await sixramApi.generatePayrollRun(runEditor)
      setRunModalOpen(false)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setRunErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingRun(false)
    }
  }

  async function handleSaveAdjustment(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingAdjustment(true)
    setAdjustmentErrors({})
    setError(null)

    try {
      if (editingAdjustment) {
        await sixramApi.updatePayrollAdjustment(editingAdjustment.id, adjustmentEditor)
      } else {
        await sixramApi.createPayrollAdjustment(adjustmentEditor)
      }

      setAdjustmentModalOpen(false)
      await Promise.all([loadSummary(), loadAdjustments()])
    } catch (caughtError) {
      setError(formatError(caughtError))
      setAdjustmentErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingAdjustment(false)
    }
  }

  async function handleRunAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!runActionState) {
      return
    }

    setIsSubmittingAction(true)
    setError(null)

    const payload: PayrollRunActionInput = { remarks: actionRemarks }

    try {
      if (runActionState.mode === 'submit') {
        await sixramApi.submitPayrollRunForReview(runActionState.run.id, payload)
      } else if (runActionState.mode === 'approve') {
        await sixramApi.approvePayrollRun(runActionState.run.id, payload)
      } else if (runActionState.mode === 'paid') {
        await sixramApi.markPayrollRunPaid(runActionState.run.id, payload)
      } else {
        await sixramApi.cancelPayrollRun(runActionState.run.id, payload)
      }

      setRunActionState(null)
      setActionRemarks('')
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmittingAction(false)
    }
  }

  async function handleAdjustmentAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!adjustmentActionState) {
      return
    }

    setIsSubmittingAction(true)
    setError(null)

    const payload: PayrollRunActionInput = { remarks: actionRemarks }

    try {
      if (adjustmentActionState.mode === 'approve') {
        await sixramApi.approvePayrollAdjustment(adjustmentActionState.adjustment.id, payload)
      } else if (adjustmentActionState.mode === 'reject') {
        await sixramApi.rejectPayrollAdjustment(adjustmentActionState.adjustment.id, payload)
      } else {
        await sixramApi.cancelPayrollAdjustment(adjustmentActionState.adjustment.id, payload)
      }

      setAdjustmentActionState(null)
      setActionRemarks('')
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmittingAction(false)
    }
  }

  async function handleRecalculateRun(run: PayrollRunSummary) {
    setError(null)

    try {
      await sixramApi.recalculatePayrollRun(run.id)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handleDeletePayPeriod() {
    if (!deletePayPeriodTarget) {
      return
    }

    setIsDeleting(true)
    try {
      await sixramApi.deletePayPeriod(deletePayPeriodTarget.id)
      setDeletePayPeriodTarget(null)
      await Promise.all([loadSummary(), loadPayPeriods()])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleDeleteAdjustment() {
    if (!deleteAdjustmentTarget) {
      return
    }

    setIsDeleting(true)
    try {
      await sixramApi.deletePayrollAdjustment(deleteAdjustmentTarget.id)
      setDeleteAdjustmentTarget(null)
      await Promise.all([loadSummary(), loadAdjustments()])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  if (isInitialLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading payroll workspace...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Payroll Preparation</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Payroll dashboard and processing</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Configure pay periods, generate payroll runs from attendance and leave data, manage adjustments, and review payroll before approval.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/admin/payroll/setup">
              Setup
            </Link>
            <Link className="shell-button-secondary" to="/admin/payroll/compensation">
              Compensation
            </Link>
            <Link className="shell-button-secondary" to="/admin/payroll/reports">
              Reports
            </Link>
            <button className="shell-button-secondary" onClick={() => void refreshAll()} type="button">
              Refresh
            </button>
            <button className="shell-button" onClick={openGenerateRunModal} type="button">
              Generate Payroll
            </button>
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Open pay period" value={summary?.currentOpenPayPeriod?.code ?? 'None'} />
          <SummaryCard label="Draft runs" value={String(summary?.draftRunCount ?? 0)} tone="warning" />
          <SummaryCard label="For review" value={String(summary?.forReviewRunCount ?? 0)} tone="warning" />
          <SummaryCard label="Approved runs" value={String(summary?.approvedRunCount ?? 0)} tone="success" />
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Missing compensation" value={String(summary?.employeesMissingCompensationProfileCount ?? 0)} tone="danger" />
          <SummaryCard label="Attendance issues" value={String(summary?.employeesWithAttendanceIssuesCount ?? 0)} tone="danger" />
          <SummaryCard label="Pending adjustments" value={String(summary?.pendingPayrollAdjustmentCount ?? 0)} tone="warning" />
          <SummaryCard label="Items on hold" value={String(summary?.payrollItemsOnHoldCount ?? 0)} tone="danger" />
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <SummaryCard label="Gross pay" value={formatCurrency(summary?.totalGrossPay ?? 0)} tone="brand" />
          <SummaryCard label="Total deductions" value={formatCurrency(summary?.totalDeductions ?? 0)} tone="brand" />
          <SummaryCard label="Net pay" value={formatCurrency(summary?.totalNetPay ?? 0)} tone="success" />
        </div>
      </section>

      {error ? (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="grid gap-6 xl:grid-cols-2">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Pay periods</h4>
              <p className="mt-1 text-sm text-slate-500">Maintain payroll cutoffs, payroll dates, and pay-period status.</p>
            </div>
            <button className="shell-button-secondary" onClick={openCreatePayPeriodModal} type="button">
              Add Pay Period
            </button>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <FormField label="Search">
              <input
                className="shell-input"
                onChange={(event) => setPayPeriodQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))}
                placeholder="Search period code or name"
                value={payPeriodQuery.search ?? ''}
              />
            </FormField>
            <FormField label="Status">
              <select
                className="shell-select"
                onChange={(event) => setPayPeriodQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={payPeriodQuery.status ?? ''}
              >
                <option value="">All statuses</option>
                <option value="open">Open</option>
                <option value="processing">Processing</option>
                <option value="locked">Locked</option>
                <option value="paid">Paid</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </FormField>
          </div>

          <div className="shell-table-wrap mt-5">
            <table className="shell-table">
              <thead>
                <tr>
                  <th>Period</th>
                  <th>Dates</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {isLoadingPayPeriods ? (
                  <tr>
                    <td className="text-slate-500" colSpan={4}>
                      Loading pay periods...
                    </td>
                  </tr>
                ) : payPeriods?.items.length ? (
                  payPeriods.items.map((record) => (
                    <tr key={record.id}>
                      <td>
                        <div className="font-semibold text-slate-900">{record.name}</div>
                        <div className="mt-1 text-slate-500">{record.code}</div>
                        <div className="mt-1 text-slate-500">{record.payFrequency.replace('_', ' ')}</div>
                      </td>
                      <td className="text-slate-600">
                        <div>{formatDate(record.periodStartDate)} - {formatDate(record.periodEndDate)}</div>
                        <div className="mt-1 text-xs text-slate-500">Payroll date {formatDate(record.payrollDate)}</div>
                      </td>
                      <td>
                        <PayrollStatusBadge status={record.status} />
                      </td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          <button className="shell-button-secondary px-3 py-2" onClick={() => openEditPayPeriodModal(record)} type="button">
                            Edit
                          </button>
                          <button className="shell-button-danger px-3 py-2" onClick={() => setDeletePayPeriodTarget(record)} type="button">
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td className="text-slate-500" colSpan={4}>
                      No pay periods found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <PaginationControls
              onPageChange={(pageNumber) => setPayPeriodQuery((current) => ({ ...current, pageNumber }))}
              pageNumber={payPeriods?.pageNumber ?? 1}
              pageSize={payPeriods?.pageSize ?? payPeriodQuery.pageSize ?? 5}
              totalCount={payPeriods?.totalCount ?? 0}
              totalPages={payPeriods?.totalPages ?? 0}
            />
          </div>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Recent runs</h4>
              <p className="mt-1 text-sm text-slate-500">Monitor calculated payroll totals, hold counts, and approval status.</p>
            </div>
            <span className="shell-badge-muted">{runs?.totalCount ?? 0} runs</span>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <FormField label="Search">
              <input
                className="shell-input"
                onChange={(event) => setRunQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))}
                placeholder="Search run or reference"
                value={runQuery.search ?? ''}
              />
            </FormField>
            <FormField label="Status">
              <select
                className="shell-select"
                onChange={(event) => setRunQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={runQuery.status ?? ''}
              >
                <option value="">All statuses</option>
                {options?.runStatuses.map((status) => (
                  <option key={status} value={status}>
                    {status.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="mt-5 space-y-3">
            {isLoadingRuns ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">Loading payroll runs...</div>
            ) : runs?.items.length ? (
              runs.items.map((run) => (
                <div className="rounded-2xl border border-slate-200 bg-white p-4" key={run.id}>
                  <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="text-sm font-semibold text-slate-900">{run.name}</p>
                        <PayrollStatusBadge status={run.status} />
                        {run.holdCount > 0 ? <span className="shell-badge-danger">{run.holdCount} held</span> : null}
                      </div>
                      <p className="mt-1 text-sm text-slate-500">{run.referenceNumber} | {run.payPeriodName}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        Generated {formatDateTime(run.generatedAtUtc)} by {run.generatedByDisplayName || 'System'}
                      </p>
                    </div>

                    <div className="text-left xl:text-right">
                      <p className="text-sm font-semibold text-slate-900">{formatCurrency(run.totalNetPay)}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        Gross {formatCurrency(run.totalGrossPay)} | Deduct {formatCurrency(run.totalDeductions)}
                      </p>
                    </div>
                  </div>

                  <div className="mt-4 flex flex-wrap gap-2">
                    <Link className="shell-button-secondary px-3 py-2" to={`/admin/payroll/runs/${run.id}`}>
                      View
                    </Link>
                    <button className="shell-button-secondary px-3 py-2" onClick={() => void handleRecalculateRun(run)} type="button">
                      Recalculate
                    </button>
                    <button className="shell-button-secondary px-3 py-2" onClick={() => {
                      setRunActionState({ mode: 'submit', run })
                      setActionRemarks('')
                    }} type="button">
                      Submit
                    </button>
                    <button className="shell-button-secondary px-3 py-2" onClick={() => {
                      setRunActionState({ mode: 'approve', run })
                      setActionRemarks('')
                    }} type="button">
                      Approve
                    </button>
                    <button className="shell-button-secondary px-3 py-2" onClick={() => {
                      setRunActionState({ mode: 'paid', run })
                      setActionRemarks('')
                    }} type="button">
                      Mark Paid
                    </button>
                    <button className="shell-button-danger px-3 py-2" onClick={() => {
                      setRunActionState({ mode: 'cancel', run })
                      setActionRemarks('')
                    }} type="button">
                      Cancel
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No payroll runs found.</div>
            )}
          </div>

          <PaginationControls
            onPageChange={(pageNumber) => setRunQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={runs?.pageNumber ?? 1}
            pageSize={runs?.pageSize ?? runQuery.pageSize ?? 6}
            totalCount={runs?.totalCount ?? 0}
            totalPages={runs?.totalPages ?? 0}
          />
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Payroll adjustments</h4>
            <p className="mt-1 text-sm text-slate-500">Track one-time earnings and deductions before they are applied into a run.</p>
          </div>

          <button className="shell-button-secondary" onClick={openCreateAdjustmentModal} type="button">
            Add Adjustment
          </button>
        </div>

        <div className="mt-5 grid gap-4 md:grid-cols-4">
          <FormField label="Search">
            <input
              className="shell-input"
              onChange={(event) => setAdjustmentQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))}
              placeholder="Employee or reason"
              value={adjustmentQuery.search ?? ''}
            />
          </FormField>
          <FormField label="Status">
            <select
              className="shell-select"
              onChange={(event) => setAdjustmentQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
              value={adjustmentQuery.status ?? ''}
            >
              <option value="">All statuses</option>
              {options?.adjustmentStatuses.map((status) => (
                <option key={status} value={status}>
                  {status.replace('_', ' ')}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Type">
            <select
              className="shell-select"
              onChange={(event) => setAdjustmentQuery((current) => ({ ...current, adjustmentType: event.target.value, pageNumber: 1 }))}
              value={adjustmentQuery.adjustmentType ?? ''}
            >
              <option value="">All types</option>
              {options?.adjustmentTypes.map((status) => (
                <option key={status} value={status}>
                  {status.replace('_', ' ')}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Pay Period">
            <select
              className="shell-select"
              onChange={(event) => setAdjustmentQuery((current) => ({ ...current, payPeriodId: event.target.value, pageNumber: 1 }))}
              value={adjustmentQuery.payPeriodId ?? ''}
            >
              <option value="">All pay periods</option>
              {options?.payPeriods.map((period) => (
                <option key={period.id} value={period.id}>
                  {period.name}
                </option>
              ))}
            </select>
          </FormField>
        </div>

        <div className="shell-table-wrap mt-5">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Adjustment</th>
                <th>Status</th>
                <th>Requested</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoadingAdjustments ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    Loading adjustments...
                  </td>
                </tr>
              ) : adjustments?.items.length ? (
                adjustments.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                      <div className="mt-1 text-slate-500">{[record.departmentName, record.branchName].filter(Boolean).join(' | ')}</div>
                    </td>
                    <td>
                      <div className="font-semibold capitalize text-slate-900">{record.adjustmentType}</div>
                      <div className="mt-1 text-slate-500">{record.earningTypeName || record.deductionTypeName || 'Manual line'}</div>
                      <div className="mt-1 text-slate-500">{formatCurrency(record.amount)}</div>
                    </td>
                    <td>
                      <PayrollStatusBadge status={record.status} />
                    </td>
                    <td className="text-slate-600">
                      <div>{record.requestedByDisplayName || 'System'}</div>
                      <div className="mt-1 text-xs text-slate-500">{formatDateTime(record.createdAtUtc)}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary px-3 py-2" onClick={() => openEditAdjustmentModal(record)} type="button">
                          Edit
                        </button>
                        <button className="shell-button-secondary px-3 py-2" onClick={() => {
                          setAdjustmentActionState({ mode: 'approve', adjustment: record })
                          setActionRemarks('')
                        }} type="button">
                          Approve
                        </button>
                        <button className="shell-button-secondary px-3 py-2" onClick={() => {
                          setAdjustmentActionState({ mode: 'reject', adjustment: record })
                          setActionRemarks('')
                        }} type="button">
                          Reject
                        </button>
                        <button className="shell-button-danger px-3 py-2" onClick={() => {
                          setAdjustmentActionState({ mode: 'cancel', adjustment: record })
                          setActionRemarks('')
                        }} type="button">
                          Cancel
                        </button>
                        <button className="shell-button-danger px-3 py-2" onClick={() => setDeleteAdjustmentTarget(record)} type="button">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    No adjustments found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <PaginationControls
            onPageChange={(pageNumber) => setAdjustmentQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={adjustments?.pageNumber ?? 1}
            pageSize={adjustments?.pageSize ?? adjustmentQuery.pageSize ?? 6}
            totalCount={adjustments?.totalCount ?? 0}
            totalPages={adjustments?.totalPages ?? 0}
          />
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Recent run snapshot</h4>
            <p className="mt-1 text-sm text-slate-500">A quick glance at the latest processed payroll runs from the dashboard.</p>
          </div>
          <span className="shell-badge-muted">{summary?.recentRuns.length ?? 0} recent runs</span>
        </div>

        <div className="mt-5 grid gap-3 lg:grid-cols-2">
          {(summary?.recentRuns ?? []).map((run) => (
            <div className="rounded-2xl border border-slate-200 bg-white p-4" key={run.id}>
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-900">{run.name}</p>
                  <p className="mt-1 text-xs text-slate-500">{run.referenceNumber}</p>
                </div>
                <PayrollStatusBadge status={run.status} />
              </div>
              <div className="mt-3 grid gap-3 sm:grid-cols-3">
                <MiniMetric label="Employees" value={String(run.employeeCount)} />
                <MiniMetric label="Held" value={String(run.holdCount)} />
                <MiniMetric label="Net Pay" value={formatCurrency(run.totalNetPay)} />
              </div>
            </div>
          ))}
        </div>
      </section>

      <Modal
        description={editingPayPeriod ? 'Update pay-period dates, status, or template linkage.' : 'Create a new pay period for payroll processing.'}
        onClose={() => {
          if (!isSavingPayPeriod) {
            setPayPeriodModalOpen(false)
          }
        }}
        open={payPeriodModalOpen}
        title={editingPayPeriod ? `Edit ${editingPayPeriod.name}` : 'Create Pay Period'}
      >
        <form className="space-y-5" onSubmit={handleSavePayPeriod}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(payPeriodErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, code: event.target.value }))} value={payPeriodEditor.code} />
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, name: event.target.value }))} value={payPeriodEditor.name} />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(payPeriodErrors, 'PayFrequency', 'payFrequency')} label="Frequency">
              <select className="shell-select" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, payFrequency: event.target.value }))} value={payPeriodEditor.payFrequency}>
                {options?.payFrequencies.map((item) => (
                  <option key={item} value={item}>
                    {item.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'PayPeriodTemplateId', 'payPeriodTemplateId')} label="Template">
              <select className="shell-select" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, payPeriodTemplateId: event.target.value }))} value={payPeriodEditor.payPeriodTemplateId ?? ''}>
                <option value="">Manual</option>
                {options?.payPeriodTemplates.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'Status', 'status')} label="Status">
              <select className="shell-select" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, status: event.target.value }))} value={payPeriodEditor.status}>
                <option value="open">Open</option>
                <option value="processing">Processing</option>
                <option value="locked">Locked</option>
                <option value="paid">Paid</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(payPeriodErrors, 'PeriodStartDate', 'periodStartDate')} label="Period Start">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, periodStartDate: event.target.value }))} type="date" value={payPeriodEditor.periodStartDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'PeriodEndDate', 'periodEndDate')} label="Period End">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, periodEndDate: event.target.value }))} type="date" value={payPeriodEditor.periodEndDate ?? ''} />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(payPeriodErrors, 'CutoffStartDate', 'cutoffStartDate')} label="Cutoff Start">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, cutoffStartDate: event.target.value }))} type="date" value={payPeriodEditor.cutoffStartDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'CutoffEndDate', 'cutoffEndDate')} label="Cutoff End">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, cutoffEndDate: event.target.value }))} type="date" value={payPeriodEditor.cutoffEndDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(payPeriodErrors, 'PayrollDate', 'payrollDate')} label="Payroll Date">
              <input className="shell-input" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, payrollDate: event.target.value }))} type="date" value={payPeriodEditor.payrollDate ?? ''} />
            </FormField>
          </div>

          <FormField error={getFieldError(payPeriodErrors, 'Remarks', 'remarks')} label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setPayPeriodEditor((current) => ({ ...current, remarks: event.target.value }))} rows={3} value={payPeriodEditor.remarks} />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setPayPeriodModalOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSavingPayPeriod} type="submit">
              {isSavingPayPeriod ? 'Saving...' : editingPayPeriod ? 'Save Changes' : 'Create Pay Period'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Generate a payroll run using the selected pay period, filters, attendance, leave, and active compensation data."
        onClose={() => {
          if (!isSavingRun) {
            setRunModalOpen(false)
          }
        }}
        open={runModalOpen}
        title="Generate Payroll Run"
      >
        <form className="space-y-5" onSubmit={handleGenerateRun}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(runErrors, 'PayPeriodId', 'payPeriodId')} label="Pay Period">
              <select className="shell-select" onChange={(event) => setRunEditor((current) => ({ ...current, payPeriodId: event.target.value }))} value={runEditor.payPeriodId ?? ''}>
                <option value="">Select pay period</option>
                {options?.payPeriods.map((period) => (
                  <option key={period.id} value={period.id}>
                    {period.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(runErrors, 'ReferenceNumber', 'referenceNumber')} label="Reference Number">
              <input className="shell-input" onChange={(event) => setRunEditor((current) => ({ ...current, referenceNumber: event.target.value }))} value={runEditor.referenceNumber} />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(runErrors, 'Name', 'name')} label="Run Name">
              <input className="shell-input" onChange={(event) => setRunEditor((current) => ({ ...current, name: event.target.value }))} value={runEditor.name} />
            </FormField>
            <FormField label="Department">
              <select className="shell-select" onChange={(event) => setRunEditor((current) => ({ ...current, departmentId: event.target.value }))} value={runEditor.departmentId ?? ''}>
                <option value="">All departments</option>
                {options?.departments.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            <FormField label="Branch">
              <select className="shell-select" onChange={(event) => setRunEditor((current) => ({ ...current, branchId: event.target.value }))} value={runEditor.branchId ?? ''}>
                <option value="">All branches</option>
                {options?.branches.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Employment Type">
              <select className="shell-select" onChange={(event) => setRunEditor((current) => ({ ...current, employmentTypeId: event.target.value }))} value={runEditor.employmentTypeId ?? ''}>
                <option value="">All employment types</option>
                {options?.employmentTypes.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Employment Status">
              <select className="shell-select" onChange={(event) => setRunEditor((current) => ({ ...current, employmentStatusId: event.target.value }))} value={runEditor.employmentStatusId ?? ''}>
                <option value="">All employment statuses</option>
                {options?.employmentStatuses.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <FormField label="Selected Employees">
            <select
              className="shell-select min-h-36"
              multiple
              onChange={(event) =>
                setRunEditor((current) => ({
                  ...current,
                  selectedEmployeeIds: Array.from(event.target.selectedOptions).map((option) => option.value),
                }))
              }
              value={runEditor.selectedEmployeeIds}
            >
              {options?.employees.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.employeeCode} | {employee.fullName}
                </option>
              ))}
            </select>
          </FormField>

          <FormField error={getFieldError(runErrors, 'Remarks', 'remarks')} label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setRunEditor((current) => ({ ...current, remarks: event.target.value }))} rows={3} value={runEditor.remarks} />
          </FormField>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
            Leave the employee selection empty to include all eligible employees that match the selected filters.
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setRunModalOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSavingRun} type="submit">
              {isSavingRun ? 'Generating...' : 'Generate Payroll'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description={editingAdjustment ? 'Update this payroll adjustment before it is applied.' : 'Create a one-time earning or deduction adjustment.'}
        onClose={() => {
          if (!isSavingAdjustment) {
            setAdjustmentModalOpen(false)
          }
        }}
        open={adjustmentModalOpen}
        title={editingAdjustment ? `Edit ${editingAdjustment.employeeFullName}` : 'Create Payroll Adjustment'}
      >
        <form className="space-y-5" onSubmit={handleSaveAdjustment}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(adjustmentErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, employeeId: event.target.value }))} value={adjustmentEditor.employeeId ?? ''}>
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(adjustmentErrors, 'AdjustmentType', 'adjustmentType')} label="Adjustment Type">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, adjustmentType: event.target.value }))} value={adjustmentEditor.adjustmentType}>
                {options?.adjustmentTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField label="Pay Period">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, payPeriodId: event.target.value }))} value={adjustmentEditor.payPeriodId ?? ''}>
                <option value="">Optional</option>
                {options?.payPeriods.map((period) => (
                  <option key={period.id} value={period.id}>
                    {period.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Payroll Run">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, payrollRunId: event.target.value }))} value={adjustmentEditor.payrollRunId ?? ''}>
                <option value="">Optional</option>
                {runs?.items.map((run) => (
                  <option key={run.id} value={run.id}>
                    {run.referenceNumber}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          {adjustmentEditor.adjustmentType === 'earning' ? (
            <FormField error={getFieldError(adjustmentErrors, 'EarningTypeId', 'earningTypeId')} label="Earning Type">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, earningTypeId: event.target.value }))} value={adjustmentEditor.earningTypeId ?? ''}>
                <option value="">Select earning type</option>
                {options?.earningTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
              </select>
            </FormField>
          ) : (
            <FormField error={getFieldError(adjustmentErrors, 'DeductionTypeId', 'deductionTypeId')} label="Deduction Type">
              <select className="shell-select" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, deductionTypeId: event.target.value }))} value={adjustmentEditor.deductionTypeId ?? ''}>
                <option value="">Select deduction type</option>
                {options?.deductionTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
              </select>
            </FormField>
          )}

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(adjustmentErrors, 'Amount', 'amount')} label="Amount">
              <input className="shell-input" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, amount: Number(event.target.value) }))} step="0.01" type="number" value={adjustmentEditor.amount} />
            </FormField>
            <FormField error={getFieldError(adjustmentErrors, 'Reason', 'reason')} label="Reason">
              <textarea className="shell-textarea" onChange={(event) => setAdjustmentEditor((current) => ({ ...current, reason: event.target.value }))} rows={3} value={adjustmentEditor.reason} />
            </FormField>
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setAdjustmentModalOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSavingAdjustment} type="submit">
              {isSavingAdjustment ? 'Saving...' : editingAdjustment ? 'Save Changes' : 'Create Adjustment'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description={
          runActionState
            ? runActionState.mode === 'submit'
              ? 'Move this payroll run to the review queue.'
              : runActionState.mode === 'approve'
                ? 'Approve this payroll run and lock it for payroll release.'
                : runActionState.mode === 'paid'
                  ? 'Mark this payroll run as paid.'
                  : 'Cancel this payroll run. This should only be used when the run should no longer proceed.'
            : ''
        }
        onClose={() => {
          if (!isSubmittingAction) {
            setRunActionState(null)
            setActionRemarks('')
          }
        }}
        open={Boolean(runActionState)}
        title={runActionState ? `${runActionState.mode.replace('_', ' ')} payroll run` : 'Payroll Run Action'}
      >
        <form className="space-y-5" onSubmit={handleRunAction}>
          <FormField label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setActionRemarks(event.target.value)} rows={4} value={actionRemarks} />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setRunActionState(null)} type="button">
              Close
            </button>
            <button className="shell-button" disabled={isSubmittingAction} type="submit">
              {isSubmittingAction ? 'Working...' : 'Submit'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description={
          adjustmentActionState
            ? adjustmentActionState.mode === 'approve'
              ? 'Approve this payroll adjustment so it can be applied into payroll.'
              : adjustmentActionState.mode === 'reject'
                ? 'Reject this payroll adjustment.'
                : 'Cancel this payroll adjustment.'
            : ''
        }
        onClose={() => {
          if (!isSubmittingAction) {
            setAdjustmentActionState(null)
            setActionRemarks('')
          }
        }}
        open={Boolean(adjustmentActionState)}
        title={adjustmentActionState ? `${adjustmentActionState.mode} payroll adjustment` : 'Payroll Adjustment Action'}
      >
        <form className="space-y-5" onSubmit={handleAdjustmentAction}>
          <FormField label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setActionRemarks(event.target.value)} rows={4} value={actionRemarks} />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setAdjustmentActionState(null)} type="button">
              Close
            </button>
            <button className="shell-button" disabled={isSubmittingAction} type="submit">
              {isSubmittingAction ? 'Working...' : 'Submit'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Pay Period"
        description={deletePayPeriodTarget ? `Delete ${deletePayPeriodTarget.name}? This is only allowed while it has no payroll runs.` : ''}
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeletePayPeriodTarget(null)
          }
        }}
        onConfirm={() => void handleDeletePayPeriod()}
        open={Boolean(deletePayPeriodTarget)}
        title="Delete Pay Period"
      />

      <ConfirmDialog
        confirmLabel="Delete Adjustment"
        description={deleteAdjustmentTarget ? `Delete the ${deleteAdjustmentTarget.adjustmentType} adjustment for ${deleteAdjustmentTarget.employeeFullName}?` : ''}
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteAdjustmentTarget(null)
          }
        }}
        onConfirm={() => void handleDeleteAdjustment()}
        open={Boolean(deleteAdjustmentTarget)}
        title="Delete Payroll Adjustment"
      />
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
  tone?: 'default' | 'warning' | 'danger' | 'success' | 'brand'
}) {
  const className =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : tone === 'success'
          ? 'border-emerald-200 bg-emerald-50'
          : tone === 'brand'
            ? 'border-[#465fff]/20 bg-[#465fff]/5'
            : 'border-slate-200 bg-white'

  return (
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function MiniMetric({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  )
}

function FormField({
  label,
  error,
  children,
}: {
  label: string
  error?: string | null
  children: ReactNode
}) {
  return (
    <div>
      <label className="shell-label">{label}</label>
      {children}
      {error ? <p className="mt-2 text-sm text-rose-600">{error}</p> : null}
    </div>
  )
}

function buildRunReference() {
  const now = new Date()
  const year = now.getFullYear()
  const month = `${now.getMonth() + 1}`.padStart(2, '0')
  const day = `${now.getDate()}`.padStart(2, '0')
  const time = `${now.getHours()}`.padStart(2, '0') + `${now.getMinutes()}`.padStart(2, '0')
  return `PR-${year}${month}${day}-${time}`
}
