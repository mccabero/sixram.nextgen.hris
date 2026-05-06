import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '../components/ContentState'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  EmployeeLeaveProfile,
  LeaveActionInput,
  LeaveBalance,
  LeaveBalanceAdjustmentInput,
  LeaveBalanceListQuery,
  LeaveDashboardSummary,
  LeaveManagementOptions,
  LeaveRequest,
  LeaveRequestListQuery,
  LeaveTypeOption,
  PagedResult,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'
import { downloadBlob, formatFileSize } from '../utils/files'

type LeaveEditorState = {
  employeeId: string
  leaveTypeId: string
  startDate: string
  endDate: string
  startDayType: string
  endDayType: string
  reason: string
  attachment: File | null
}

type LeaveActionState = {
  mode: 'approve' | 'reject' | 'cancel'
  request: LeaveRequest
}

const defaultRequestQuery: LeaveRequestListQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  leaveTypeId: '',
  status: '',
  approverId: '',
  dateFrom: '',
  dateTo: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'submitted',
  descending: true,
}

const defaultBalanceQuery: LeaveBalanceListQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  leaveTypeId: '',
  periodYear: new Date().getFullYear(),
  lowBalanceOnly: null,
  negativeBalanceOnly: null,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'employee',
  descending: false,
}

const emptyLeaveEditor: LeaveEditorState = {
  employeeId: '',
  leaveTypeId: '',
  startDate: '',
  endDate: '',
  startDayType: 'full_day',
  endDayType: 'full_day',
  reason: '',
  attachment: null,
}

const emptyAdjustmentEditor: LeaveBalanceAdjustmentInput = {
  employeeId: null,
  leaveTypeId: null,
  periodYear: new Date().getFullYear(),
  amount: 0,
  remarks: '',
  effectiveDate: '',
}

export function LeaveManagementPage() {
  const [summary, setSummary] = useState<LeaveDashboardSummary | null>(null)
  const [options, setOptions] = useState<LeaveManagementOptions | null>(null)
  const [requests, setRequests] = useState<PagedResult<LeaveRequest> | null>(null)
  const [balances, setBalances] = useState<PagedResult<LeaveBalance> | null>(null)
  const [requestQuery, setRequestQuery] = useState<LeaveRequestListQuery>(defaultRequestQuery)
  const [balanceQuery, setBalanceQuery] = useState<LeaveBalanceListQuery>(defaultBalanceQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [balanceSearch, setBalanceSearch] = useState('')
  const [requestEditorOpen, setRequestEditorOpen] = useState(false)
  const [editingRequest, setEditingRequest] = useState<LeaveRequest | null>(null)
  const [requestEditor, setRequestEditor] = useState<LeaveEditorState>(emptyLeaveEditor)
  const [actionState, setActionState] = useState<LeaveActionState | null>(null)
  const [actionRemarks, setActionRemarks] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<LeaveRequest | null>(null)
  const [adjustmentOpen, setAdjustmentOpen] = useState(false)
  const [adjustmentTarget, setAdjustmentTarget] = useState<LeaveBalance | null>(null)
  const [adjustmentEditor, setAdjustmentEditor] = useState<LeaveBalanceAdjustmentInput>(emptyAdjustmentEditor)
  const [profilePreview, setProfilePreview] = useState<EmployeeLeaveProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [adjustmentFieldErrors, setAdjustmentFieldErrors] = useState<Record<string, string[]>>({})
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isLoadingRequests, setIsLoadingRequests] = useState(false)
  const [isLoadingBalances, setIsLoadingBalances] = useState(false)
  const [isSavingRequest, setIsSavingRequest] = useState(false)
  const [isSubmittingAction, setIsSubmittingAction] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isAdjusting, setIsAdjusting] = useState(false)
  const [activeDownloadId, setActiveDownloadId] = useState<string | null>(null)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadRequests()
  }, [requestQuery, options])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadBalances()
  }, [balanceQuery, options])

  async function loadInitialData() {
    setIsInitialLoading(true)

    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getLeaveSummary(),
        sixramApi.getLeaveOptions(),
      ])

      setSummary(summaryResponse)
      setOptions(optionsResponse)
      setError(null)
      setBalanceQuery((current) => ({
        ...current,
        periodYear: optionsResponse.periodYears.includes(current.periodYear ?? 0)
          ? current.periodYear
          : optionsResponse.periodYears[optionsResponse.periodYears.length - 1] ?? new Date().getFullYear(),
      }))
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsInitialLoading(false)
    }
  }

  async function loadSummary() {
    try {
      const response = await sixramApi.getLeaveSummary()
      setSummary(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRequests() {
    setIsLoadingRequests(true)

    try {
      const response = await sixramApi.getLeaveRequests(requestQuery)
      setRequests(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingRequests(false)
    }
  }

  async function loadBalances() {
    setIsLoadingBalances(true)

    try {
      const response = await sixramApi.getLeaveBalances(balanceQuery)
      setBalances(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingBalances(false)
    }
  }

  async function refreshAll() {
    await Promise.all([loadSummary(), loadRequests(), loadBalances()])
  }

  function openCreateRequestModal() {
    const defaultLeaveTypeId = options?.leaveTypes[0]?.id ?? ''
    const defaultEmployeeId = options?.employees[0]?.id ?? ''
    setEditingRequest(null)
    setFieldErrors({})
    setRequestEditor({
      ...emptyLeaveEditor,
      employeeId: defaultEmployeeId,
      leaveTypeId: defaultLeaveTypeId,
      startDate: summary?.businessDate ?? '',
      endDate: summary?.businessDate ?? '',
    })
    setRequestEditorOpen(true)
  }

  function openEditRequestModal(record: LeaveRequest) {
    setEditingRequest(record)
    setFieldErrors({})
    setRequestEditor({
      employeeId: record.employeeId,
      leaveTypeId: record.leaveTypeId,
      startDate: record.startDate,
      endDate: record.endDate,
      startDayType: record.startDayType,
      endDayType: record.endDayType,
      reason: record.reason,
      attachment: null,
    })
    setRequestEditorOpen(true)
  }

  async function handleSaveRequest(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingRequest(true)
    setFieldErrors({})
    setError(null)

    try {
      const formData = new FormData()
      formData.append('employeeId', requestEditor.employeeId)
      formData.append('leaveTypeId', requestEditor.leaveTypeId)
      formData.append('startDate', requestEditor.startDate)
      formData.append('endDate', requestEditor.endDate)
      formData.append('startDayType', requestEditor.startDayType)
      formData.append('endDayType', requestEditor.endDayType)
      formData.append('reason', requestEditor.reason)

      if (requestEditor.attachment) {
        formData.append('attachment', requestEditor.attachment)
      }

      if (editingRequest) {
        await sixramApi.updateLeaveRequest(editingRequest.id, formData)
      } else {
        await sixramApi.createLeaveRequest(formData)
      }

      setRequestEditorOpen(false)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingRequest(false)
    }
  }

  async function handleSubmitAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!actionState) {
      return
    }

    setIsSubmittingAction(true)
    setError(null)

    const payload: LeaveActionInput = { remarks: actionRemarks }

    try {
      if (actionState.mode === 'approve') {
        await sixramApi.approveLeaveRequest(actionState.request.id, payload)
      } else if (actionState.mode === 'reject') {
        await sixramApi.rejectLeaveRequest(actionState.request.id, payload)
      } else {
        await sixramApi.cancelLeaveRequest(actionState.request.id, payload)
      }

      setActionState(null)
      setActionRemarks('')
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmittingAction(false)
    }
  }

  async function handleDeleteRequest() {
    if (!deleteTarget) {
      return
    }

    setIsDeleting(true)

    try {
      await sixramApi.deleteLeaveRequest(deleteTarget.id)
      setDeleteTarget(null)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  function openAdjustmentModal(balance?: LeaveBalance) {
    setAdjustmentTarget(balance ?? null)
    setAdjustmentFieldErrors({})
    setAdjustmentEditor({
      employeeId: balance?.employeeId ?? options?.employees[0]?.id ?? null,
      leaveTypeId: balance?.leaveTypeId ?? options?.leaveTypes[0]?.id ?? null,
      periodYear: balance?.periodYear ?? balanceQuery.periodYear ?? options?.periodYears[options.periodYears.length - 1] ?? new Date().getFullYear(),
      amount: 0,
      remarks: '',
      effectiveDate: summary?.businessDate ?? '',
    })
    setAdjustmentOpen(true)
  }

  async function handleAdjustmentSave(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsAdjusting(true)
    setAdjustmentFieldErrors({})
    setError(null)

    try {
      await sixramApi.adjustLeaveBalance(adjustmentEditor)
      setAdjustmentOpen(false)
      await refreshAll()

      if (adjustmentEditor.employeeId) {
        const profile = await sixramApi.getEmployeeLeaveProfile(adjustmentEditor.employeeId, adjustmentEditor.periodYear ?? undefined)
        setProfilePreview(profile)
      }
    } catch (caughtError) {
      setError(formatError(caughtError))
      setAdjustmentFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsAdjusting(false)
    }
  }

  async function handleDownloadAttachment(record: LeaveRequest) {
    setActiveDownloadId(record.id)

    try {
      const file = await sixramApi.downloadLeaveAttachment(record.id)
      downloadBlob(file.blob, file.fileName)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveDownloadId(null)
    }
  }

  async function handlePreviewEmployeeProfile(employeeId: string) {
    try {
      const profile = await sixramApi.getEmployeeLeaveProfile(employeeId, balanceQuery.periodYear ?? undefined)
      setProfilePreview(profile)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  if (isInitialLoading) {
    return <LoadingState message="Loading leave workspace..." />
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Leave Management</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Leave requests and balances</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review leave applications, approve or reject requests, adjust leave credits, and watch balance or attendance conflicts from one workspace.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <button className="shell-button-secondary" onClick={() => void refreshAll()} type="button">
              Refresh
            </button>
            <button className="shell-button-secondary" onClick={() => openAdjustmentModal()} type="button">
              Adjust Balance
            </button>
            <button className="shell-button" onClick={openCreateRequestModal} type="button">
              File Leave
            </button>
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Pending requests" value={summary?.pendingLeaveRequestCount ?? 0} tone="brand" />
          <SummaryCard label="On leave today" value={summary?.employeesOnLeaveTodayCount ?? 0} />
          <SummaryCard label="Low balances" value={summary?.lowBalanceCount ?? 0} tone="warning" />
          <SummaryCard label="Attendance conflicts" value={summary?.attendanceConflictCount ?? 0} tone="danger" />
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Approved today" value={summary?.approvedLeavesTodayCount ?? 0} />
          <SummaryCard label="Upcoming approved" value={summary?.upcomingApprovedLeaveCount ?? 0} />
          <SummaryCard label="Negative balances" value={summary?.negativeBalanceCount ?? 0} tone="danger" />
          <SummaryCard label="Business date" valueLabel={formatDate(summary?.businessDate)} />
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Leave requests</h4>
            <p className="mt-1 text-sm text-slate-500">
              Search and filter leave applications, then review approval status, attachment readiness, and balance impact.
            </p>
          </div>
          <span className="shell-badge-muted">{requests?.totalCount ?? 0} rows</span>
        </div>

        <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 xl:grid-cols-[1.7fr_repeat(6,minmax(0,1fr))]">
            <div>
              <label className="shell-label">Search</label>
              <input
                className="shell-input"
                onChange={(event) => setDraftSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    setRequestQuery((current) => ({ ...current, search: draftSearch, pageNumber: 1 }))
                  }
                }}
                placeholder="Employee, leave type, reason..."
                value={draftSearch}
              />
            </div>
            <FormField label="Date From">
              <input
                className="shell-input"
                onChange={(event) => setRequestQuery((current) => ({ ...current, dateFrom: event.target.value, pageNumber: 1 }))}
                type="date"
                value={requestQuery.dateFrom ?? ''}
              />
            </FormField>
            <FormField label="Date To">
              <input
                className="shell-input"
                onChange={(event) => setRequestQuery((current) => ({ ...current, dateTo: event.target.value, pageNumber: 1 }))}
                type="date"
                value={requestQuery.dateTo ?? ''}
              />
            </FormField>
            <FormField label="Department">
              <select
                className="shell-select"
                onChange={(event) => setRequestQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))}
                value={requestQuery.departmentId ?? ''}
              >
                <option value="">All departments</option>
                {options?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Branch">
              <select
                className="shell-select"
                onChange={(event) => setRequestQuery((current) => ({ ...current, branchId: event.target.value, pageNumber: 1 }))}
                value={requestQuery.branchId ?? ''}
              >
                <option value="">All branches</option>
                {options?.branches.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Leave Type">
              <select
                className="shell-select"
                onChange={(event) => setRequestQuery((current) => ({ ...current, leaveTypeId: event.target.value, pageNumber: 1 }))}
                value={requestQuery.leaveTypeId ?? ''}
              >
                <option value="">All leave types</option>
                {options?.leaveTypes.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Status">
              <select
                className="shell-select"
                onChange={(event) => setRequestQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={requestQuery.status ?? ''}
              >
                <option value="">All statuses</option>
                {options?.statuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="mt-4 flex flex-wrap gap-3">
            <button
              className="shell-button"
              onClick={() => setRequestQuery((current) => ({ ...current, search: draftSearch, pageNumber: 1 }))}
              type="button"
            >
              Apply Filters
            </button>
            <button
              className="shell-button-secondary"
              onClick={() => {
                setDraftSearch('')
                setRequestQuery(defaultRequestQuery)
              }}
              type="button"
            >
              Reset
            </button>
          </div>
        </div>

        {error ? <ErrorState className="mt-6" message={error} /> : null}

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Leave</th>
                <th>Status</th>
                <th>Submitted</th>
                <th>Notes</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoadingRequests ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading leave requests...
                  </td>
                </tr>
              ) : requests?.items.length ? (
                requests.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                      <div className="mt-1 text-slate-500">
                        {[record.departmentName, record.branchName].filter(Boolean).join(' | ') || 'No organization assignment'}
                      </div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">{record.leaveTypeName}</div>
                      <div className="mt-1 text-slate-500">
                        {formatDate(record.startDate)} - {formatDate(record.endDate)}
                      </div>
                      <div className="mt-1 text-slate-500">{formatDayCount(record.totalLeaveDays)}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <LeaveStatusBadge status={record.status} />
                        {record.leaveTypeIsPaid ? <span className="shell-badge-success">Paid</span> : <span className="shell-badge-muted">Unpaid</span>}
                        {record.hasAttendanceConflict ? <span className="shell-badge-danger">Conflict</span> : null}
                      </div>
                      <div className="mt-2 text-slate-500">
                        {record.currentApproverDisplayName ? `Approver: ${record.currentApproverDisplayName}` : 'No approver assigned'}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{record.submittedAtUtc ? formatDateTime(record.submittedAtUtc) : '-'}</div>
                      <div className="mt-1">Balance after approval: {formatNumber(record.availableBalanceAfterApproval)}</div>
                    </td>
                    <td className="text-slate-600">
                      <div className="line-clamp-2">{record.reason || 'No reason provided.'}</div>
                      {record.hasAttachment ? (
                        <div className="mt-2 text-xs text-slate-500">
                          Attachment: {record.attachmentOriginalFileName}
                          {record.attachmentFileSize ? ` | ${formatFileSize(record.attachmentFileSize)}` : ''}
                        </div>
                      ) : null}
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {(record.status === 'Pending' || record.status === 'Draft') ? (
                          <button className="shell-button-secondary" onClick={() => openEditRequestModal(record)} type="button">
                            Edit
                          </button>
                        ) : null}
                        {record.status === 'Pending' ? (
                          <>
                            <button className="shell-button-secondary" onClick={() => { setActionState({ mode: 'approve', request: record }); setActionRemarks('') }} type="button">
                              Approve
                            </button>
                            <button className="shell-button-secondary" onClick={() => { setActionState({ mode: 'reject', request: record }); setActionRemarks('') }} type="button">
                              Reject
                            </button>
                          </>
                        ) : null}
                        {(record.status === 'Pending' || record.status === 'Approved') ? (
                          <button className="shell-button-secondary" onClick={() => { setActionState({ mode: 'cancel', request: record }); setActionRemarks('') }} type="button">
                            Cancel
                          </button>
                        ) : null}
                        {record.hasAttachment ? (
                          <button
                            className="shell-button-secondary"
                            disabled={activeDownloadId === record.id}
                            onClick={() => void handleDownloadAttachment(record)}
                            type="button"
                          >
                            {activeDownloadId === record.id ? 'Downloading...' : 'Attachment'}
                          </button>
                        ) : null}
                        <button className="shell-button-secondary" onClick={() => void handlePreviewEmployeeProfile(record.employeeId)} type="button">
                          Profile
                        </button>
                        {record.status !== 'Approved' ? (
                          <button className="shell-button-danger" onClick={() => setDeleteTarget(record)} type="button">
                            Delete
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6}>
                    <EmptyState message="No leave requests found for the selected filters." title="No leave requests" />
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <PaginationControls
            onPageChange={(pageNumber) => setRequestQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={requests?.pageNumber ?? 1}
            pageSize={requests?.pageSize ?? requestQuery.pageSize ?? 10}
            totalCount={requests?.totalCount ?? 0}
            totalPages={requests?.totalPages ?? 0}
          />
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Leave balances</h4>
              <p className="mt-1 text-sm text-slate-500">
                Review opening balances, pending deductions, used credits, and remaining availability by leave type.
              </p>
            </div>
            <span className="shell-badge-muted">{balances?.totalCount ?? 0} rows</span>
          </div>

          <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="grid gap-4 xl:grid-cols-[1.5fr_repeat(4,minmax(0,1fr))]">
              <div>
                <label className="shell-label">Search</label>
                <input
                  className="shell-input"
                  onChange={(event) => setBalanceSearch(event.target.value)}
                  placeholder="Employee or leave type..."
                  value={balanceSearch}
                />
              </div>
              <FormField label="Year">
                <select
                  className="shell-select"
                  onChange={(event) => setBalanceQuery((current) => ({ ...current, periodYear: Number(event.target.value), pageNumber: 1 }))}
                  value={balanceQuery.periodYear ?? ''}
                >
                  {options?.periodYears.map((year) => (
                    <option key={year} value={year}>
                      {year}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Department">
                <select
                  className="shell-select"
                  onChange={(event) => setBalanceQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))}
                  value={balanceQuery.departmentId ?? ''}
                >
                  <option value="">All departments</option>
                  {options?.departments.map((option) => (
                    <option key={option.id} value={option.id}>
                      {option.name}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Leave Type">
                <select
                  className="shell-select"
                  onChange={(event) => setBalanceQuery((current) => ({ ...current, leaveTypeId: event.target.value, pageNumber: 1 }))}
                  value={balanceQuery.leaveTypeId ?? ''}
                >
                  <option value="">All leave types</option>
                  {options?.leaveTypes.map((option) => (
                    <option key={option.id} value={option.id}>
                      {option.name}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Focus">
                <select
                  className="shell-select"
                  onChange={(event) => {
                    const value = event.target.value
                    setBalanceQuery((current) => ({
                      ...current,
                      lowBalanceOnly: value === 'low' ? true : null,
                      negativeBalanceOnly: value === 'negative' ? true : null,
                      pageNumber: 1,
                    }))
                  }}
                  value={balanceQuery.negativeBalanceOnly ? 'negative' : balanceQuery.lowBalanceOnly ? 'low' : ''}
                >
                  <option value="">All balances</option>
                  <option value="low">Low balances</option>
                  <option value="negative">Negative balances</option>
                </select>
              </FormField>
            </div>

            <div className="mt-4 flex flex-wrap gap-3">
              <button
                className="shell-button"
                onClick={() => setBalanceQuery((current) => ({ ...current, search: balanceSearch, pageNumber: 1 }))}
                type="button"
              >
                Apply Filters
              </button>
              <button
                className="shell-button-secondary"
                onClick={() => {
                  setBalanceSearch('')
                  setBalanceQuery(defaultBalanceQuery)
                }}
                type="button"
              >
                Reset
              </button>
            </div>
          </div>

          <div className="shell-table-wrap mt-6">
            <table className="shell-table">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Leave Type</th>
                  <th>Totals</th>
                  <th>Available</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {isLoadingBalances ? (
                  <tr>
                    <td colSpan={5}>
                      <LoadingState compact message="Loading leave balances..." />
                    </td>
                  </tr>
                ) : balances?.items.length ? (
                  balances.items.map((balance) => (
                    <tr key={balance.id}>
                      <td>
                        <div className="font-semibold text-slate-900">{balance.employeeFullName}</div>
                        <div className="mt-1 text-slate-500">{balance.employeeCode}</div>
                        <div className="mt-1 text-slate-500">
                          {[balance.departmentName, balance.branchName].filter(Boolean).join(' | ') || 'No organization assignment'}
                        </div>
                      </td>
                      <td>
                        <div className="font-medium text-slate-900">{balance.leaveTypeName}</div>
                        <div className="mt-1 text-slate-500">{balance.leaveTypeCode}</div>
                        <div className="mt-1 text-slate-500">Year {balance.periodYear}</div>
                      </td>
                      <td className="text-slate-600">
                        <div>Opening {formatNumber(balance.openingBalance)}</div>
                        <div className="mt-1">Used {formatNumber(balance.used)} | Pending {formatNumber(balance.pending)}</div>
                        <div className="mt-1">Adjusted {formatNumber(balance.adjusted)}</div>
                      </td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          <span className={balance.isNegativeBalance ? 'shell-badge-danger' : balance.isLowBalance ? 'shell-badge-warning' : 'shell-badge-success'}>
                            {formatNumber(balance.availableBalance)}
                          </span>
                        </div>
                      </td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          <button className="shell-button-secondary" onClick={() => openAdjustmentModal(balance)} type="button">
                            Adjust
                          </button>
                          <button className="shell-button-secondary" onClick={() => void handlePreviewEmployeeProfile(balance.employeeId)} type="button">
                            Profile
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5}>
                      <EmptyState message="No leave balances found for the selected filters." title="No balance results" />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <PaginationControls
              onPageChange={(pageNumber) => setBalanceQuery((current) => ({ ...current, pageNumber }))}
              pageNumber={balances?.pageNumber ?? 1}
              pageSize={balances?.pageSize ?? balanceQuery.pageSize ?? 10}
              totalCount={balances?.totalCount ?? 0}
              totalPages={balances?.totalPages ?? 0}
            />
          </div>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Employee leave profile</h4>
              <p className="mt-1 text-sm text-slate-500">
                Use this quick preview to inspect balances, pending requests, and recent ledger entries before making changes.
              </p>
            </div>
            {profilePreview ? <span className="shell-badge-brand">{profilePreview.employeeCode}</span> : null}
          </div>

          {!profilePreview ? (
            <div className="mt-6">
              <EmptyState
                message="Choose Profile from a leave request or balance row to preview an employee leave summary here."
                title="No employee selected"
              />
            </div>
          ) : (
            <div className="mt-6 space-y-5">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <p className="text-lg font-semibold text-slate-950">{profilePreview.employeeFullName}</p>
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <InfoRow label="Pending requests" value={String(profilePreview.summary.pendingRequestCount)} />
                  <InfoRow label="Approved history" value={String(profilePreview.summary.approvedRequestCount)} />
                  <InfoRow label="Low balances" value={String(profilePreview.summary.lowBalanceCount)} />
                  <InfoRow label="Negative balances" value={String(profilePreview.summary.negativeBalanceCount)} />
                </div>
              </div>

              <div>
                <p className="text-sm font-semibold text-slate-900">Balances</p>
                <div className="mt-3 space-y-2">
                  {profilePreview.balances.length ? (
                    profilePreview.balances.map((balance) => (
                      <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={balance.id}>
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-slate-900">{balance.leaveTypeName}</p>
                            <p className="mt-1 text-xs text-slate-500">
                              Used {formatNumber(balance.used)} | Pending {formatNumber(balance.pending)}
                            </p>
                          </div>
                          <span className={balance.isNegativeBalance ? 'shell-badge-danger' : balance.isLowBalance ? 'shell-badge-warning' : 'shell-badge-success'}>
                            {formatNumber(balance.availableBalance)}
                          </span>
                        </div>
                      </div>
                    ))
                  ) : (
                    <EmptyState message="No leave balances are available for the selected year." title="No balances for this year" />
                  )}
                </div>
              </div>

              <div>
                <p className="text-sm font-semibold text-slate-900">Pending requests</p>
                <div className="mt-3 space-y-2">
                  {profilePreview.pendingRequests.length ? (
                    profilePreview.pendingRequests.map((record) => (
                      <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-slate-900">{record.leaveTypeName}</p>
                            <p className="mt-1 text-xs text-slate-500">
                              {formatDate(record.startDate)} - {formatDate(record.endDate)} | {formatDayCount(record.totalLeaveDays)}
                            </p>
                          </div>
                          <LeaveStatusBadge status={record.status} />
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
                      No pending leave requests for this employee.
                    </div>
                  )}
                </div>
              </div>

              <div>
                <p className="text-sm font-semibold text-slate-900">Recent ledger</p>
                <div className="mt-3 space-y-2">
                  {profilePreview.ledger.length ? (
                    profilePreview.ledger.slice(0, 5).map((entry) => (
                      <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={entry.id}>
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-slate-900">{entry.transactionType}</p>
                            <p className="mt-1 text-xs text-slate-500">{entry.remarks || 'No remarks provided.'}</p>
                          </div>
                          <div className="text-right text-xs text-slate-500">
                            <p>{formatNumber(entry.amount)}</p>
                            <p className="mt-1">{formatDateTime(entry.createdAtUtc)}</p>
                          </div>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
                      No ledger activity available for this employee yet.
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </section>

      <Modal
        description={editingRequest ? 'Update the leave request details or replace the attachment.' : 'File a new leave request on behalf of an employee.'}
        onClose={() => {
          if (!isSavingRequest) {
            setRequestEditorOpen(false)
          }
        }}
        open={requestEditorOpen}
        title={editingRequest ? `Edit ${editingRequest.employeeFullName}` : 'File Leave Request'}
      >
        <form className="space-y-5" onSubmit={handleSaveRequest}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select
                className="shell-select"
                onChange={(event) => setRequestEditor((current) => ({ ...current, employeeId: event.target.value }))}
                value={requestEditor.employeeId}
              >
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'LeaveTypeId', 'leaveTypeId')} label="Leave Type">
              <select
                className="shell-select"
                onChange={(event) => setRequestEditor((current) => ({ ...current, leaveTypeId: event.target.value }))}
                value={requestEditor.leaveTypeId}
              >
                <option value="">Select leave type</option>
                {getLeaveTypeOptions(options, editingRequest).map((leaveType) => (
                  <option key={leaveType.id} value={leaveType.id}>
                    {leaveType.name}
                    {!leaveType.isActive ? ' (Inactive)' : ''}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'StartDate', 'startDate')} label="Start Date">
              <input
                className="shell-input"
                onChange={(event) => setRequestEditor((current) => ({ ...current, startDate: event.target.value }))}
                type="date"
                value={requestEditor.startDate}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'EndDate', 'endDate')} label="End Date">
              <input
                className="shell-input"
                onChange={(event) => setRequestEditor((current) => ({ ...current, endDate: event.target.value }))}
                type="date"
                value={requestEditor.endDate}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'StartDayType', 'startDayType')} label="Start Day Type">
              <select
                className="shell-select"
                onChange={(event) => setRequestEditor((current) => ({ ...current, startDayType: event.target.value }))}
                value={requestEditor.startDayType}
              >
                <option value="full_day">Full day</option>
                <option value="first_half">First half</option>
                <option value="second_half">Second half</option>
              </select>
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'EndDayType', 'endDayType')} label="End Day Type">
              <select
                className="shell-select"
                onChange={(event) => setRequestEditor((current) => ({ ...current, endDayType: event.target.value }))}
                value={requestEditor.endDayType}
              >
                <option value="full_day">Full day</option>
                <option value="first_half">First half</option>
                <option value="second_half">Second half</option>
              </select>
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'Reason', 'reason')} label="Reason">
            <textarea
              className="shell-textarea"
              onChange={(event) => setRequestEditor((current) => ({ ...current, reason: event.target.value }))}
              rows={4}
              value={requestEditor.reason}
            />
          </FormField>

          <FormField error={getFieldError(fieldErrors, 'Attachment', 'attachment')} label="Attachment">
            <input
              accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
              className="shell-input file:mr-4 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-700"
              onChange={(event) =>
                setRequestEditor((current) => ({
                  ...current,
                  attachment: event.target.files?.[0] ?? null,
                }))
              }
              type="file"
            />
          </FormField>

          {editingRequest?.hasAttachment ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
              Current attachment: {editingRequest.attachmentOriginalFileName}
            </div>
          ) : null}

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setRequestEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSavingRequest} type="submit">
              {isSavingRequest ? 'Saving...' : editingRequest ? 'Save Changes' : 'Submit Request'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description={
          actionState
            ? actionState.mode === 'approve'
              ? 'Approve this leave request and post it into balances and attendance.'
              : actionState.mode === 'reject'
                ? 'Reject this leave request and release any pending balance reservation.'
                : 'Cancel this leave request. Approved leave attendance markers will be removed and balances restored.'
            : ''
        }
        onClose={() => {
          if (!isSubmittingAction) {
            setActionState(null)
            setActionRemarks('')
          }
        }}
        open={Boolean(actionState)}
        title={
          actionState
            ? `${actionState.mode.charAt(0).toUpperCase()}${actionState.mode.slice(1)} ${actionState.request.employeeFullName}`
            : 'Leave Action'
        }
      >
        <form className="space-y-5" onSubmit={handleSubmitAction}>
          <FormField label="Remarks">
            <textarea
              className="shell-textarea"
              onChange={(event) => setActionRemarks(event.target.value)}
              rows={4}
              value={actionRemarks}
            />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setActionState(null)} type="button">
              Close
            </button>
            <button className="shell-button" disabled={isSubmittingAction} type="submit">
              {isSubmittingAction ? 'Working...' : actionState ? actionState.mode.charAt(0).toUpperCase() + actionState.mode.slice(1) : 'Submit'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Apply a manual balance adjustment and keep the change in the leave ledger."
        onClose={() => {
          if (!isAdjusting) {
            setAdjustmentOpen(false)
          }
        }}
        open={adjustmentOpen}
        title={adjustmentTarget ? `Adjust ${adjustmentTarget.employeeFullName}` : 'Adjust Leave Balance'}
      >
        <form className="space-y-5" onSubmit={handleAdjustmentSave}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(adjustmentFieldErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select
                className="shell-select"
                onChange={(event) => setAdjustmentEditor((current) => ({ ...current, employeeId: event.target.value || null }))}
                value={adjustmentEditor.employeeId ?? ''}
              >
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(adjustmentFieldErrors, 'LeaveTypeId', 'leaveTypeId')} label="Leave Type">
              <select
                className="shell-select"
                onChange={(event) => setAdjustmentEditor((current) => ({ ...current, leaveTypeId: event.target.value || null }))}
                value={adjustmentEditor.leaveTypeId ?? ''}
              >
                <option value="">Select leave type</option>
                {options?.leaveTypes.map((leaveType) => (
                  <option key={leaveType.id} value={leaveType.id}>
                    {leaveType.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(adjustmentFieldErrors, 'PeriodYear', 'periodYear')} label="Year">
              <select
                className="shell-select"
                onChange={(event) => setAdjustmentEditor((current) => ({ ...current, periodYear: Number(event.target.value) || null }))}
                value={adjustmentEditor.periodYear ?? ''}
              >
                {options?.periodYears.map((year) => (
                  <option key={year} value={year}>
                    {year}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(adjustmentFieldErrors, 'Amount', 'amount')} label="Adjustment Amount">
              <input
                className="shell-input"
                onChange={(event) => setAdjustmentEditor((current) => ({ ...current, amount: Number(event.target.value) }))}
                step="0.5"
                type="number"
                value={adjustmentEditor.amount}
              />
            </FormField>
            <FormField error={getFieldError(adjustmentFieldErrors, 'EffectiveDate', 'effectiveDate')} label="Effective Date">
              <input
                className="shell-input"
                onChange={(event) => setAdjustmentEditor((current) => ({ ...current, effectiveDate: event.target.value }))}
                type="date"
                value={adjustmentEditor.effectiveDate ?? ''}
              />
            </FormField>
          </div>

          <FormField error={getFieldError(adjustmentFieldErrors, 'Remarks', 'remarks')} label="Remarks">
            <textarea
              className="shell-textarea"
              onChange={(event) => setAdjustmentEditor((current) => ({ ...current, remarks: event.target.value }))}
              rows={4}
              value={adjustmentEditor.remarks}
            />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setAdjustmentOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isAdjusting} type="submit">
              {isAdjusting ? 'Saving...' : 'Apply Adjustment'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Leave Request"
        description={
          deleteTarget
            ? `Delete the leave request for ${deleteTarget.employeeFullName} covering ${formatDate(deleteTarget.startDate)} to ${formatDate(deleteTarget.endDate)}?`
            : ''
        }
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteTarget(null)
          }
        }}
        onConfirm={() => void handleDeleteRequest()}
        open={Boolean(deleteTarget)}
        title="Delete Leave Request"
      />
    </div>
  )
}

function SummaryCard({
  label,
  value,
  valueLabel,
  tone = 'default',
}: {
  label: string
  value?: number
  valueLabel?: string
  tone?: 'default' | 'brand' | 'warning' | 'danger'
}) {
  const className =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : tone === 'brand'
          ? 'border-[#465fff]/20 bg-[#465fff]/5'
          : 'border-slate-200 bg-white'

  return (
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{label}</p>
      <p className="mt-3 text-3xl font-semibold text-slate-950">{valueLabel ?? value ?? 0}</p>
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

function InfoRow({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-3">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  )
}

function getLeaveTypeOptions(options: LeaveManagementOptions | null, editingRequest: LeaveRequest | null): LeaveTypeOption[] {
  const list = [...(options?.leaveTypes ?? [])]

  if (
    editingRequest &&
    !list.some((option) => option.id === editingRequest.leaveTypeId)
  ) {
    list.push({
      id: editingRequest.leaveTypeId,
      code: editingRequest.leaveTypeCode,
      name: editingRequest.leaveTypeName,
      allowHalfDay: true,
      requiresAttachment: false,
      requiresReason: true,
      allowNegativeBalance: false,
      defaultAnnualCredits: null,
      isActive: false,
    })
  }

  return list.sort((left, right) => left.name.localeCompare(right.name))
}

function formatDayCount(value: number) {
  const normalized = Number.isInteger(value) ? String(value) : value.toFixed(1)
  return `${normalized} day${value === 1 ? '' : 's'}`
}

function formatNumber(value: number) {
  return Number.isInteger(value) ? `${value}` : value.toFixed(1)
}
