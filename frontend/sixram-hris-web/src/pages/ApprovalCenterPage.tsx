import { useEffect, useMemo, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import { RequestStatusBadge } from '../components/RequestStatusBadge'
import { useAuth } from '../auth/AuthContext'
import type {
  ApprovalActionInput,
  ApprovalCenterInboxItem,
  ApprovalCenterOptions,
  ApprovalCenterQuery,
  ApprovalCenterSummary,
  AttendanceAdjustmentRequest,
  LeaveRequest,
  LookupOption,
  PagedResult,
  ProfileChangeRequest,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

type DetailState =
  | { type: 'leave'; record: LeaveRequest }
  | { type: 'attendance'; record: AttendanceAdjustmentRequest }
  | { type: 'profile'; record: ProfileChangeRequest }
  | { type: 'payroll'; record: ApprovalCenterInboxItem }

type ActionState = {
  item: ApprovalCenterInboxItem
  mode: 'approve' | 'reject'
}

const defaultQuery: ApprovalCenterQuery = {
  type: '',
  status: 'pending',
  search: '',
  departmentId: '',
  branchId: '',
  dateFrom: '',
  dateTo: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'submitted',
  descending: true,
}

export function ApprovalCenterPage() {
  const { isAdmin, isManager, user } = useAuth()
  const [summary, setSummary] = useState<ApprovalCenterSummary | null>(null)
  const [inbox, setInbox] = useState<PagedResult<ApprovalCenterInboxItem> | null>(null)
  const [options, setOptions] = useState<ApprovalCenterOptions | null>(null)
  const [query, setQuery] = useState<ApprovalCenterQuery>(defaultQuery)
  const [searchDraft, setSearchDraft] = useState('')
  const [detailState, setDetailState] = useState<DetailState | null>(null)
  const [actionState, setActionState] = useState<ActionState | null>(null)
  const [actionRemarks, setActionRemarks] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [isSubmittingAction, setIsSubmittingAction] = useState(false)

  const canUseAdminFilters =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!summary && isLoading) {
      return
    }

    void loadInbox(query)
  }, [query.pageNumber, query.type, query.status, query.search, query.departmentId, query.branchId, query.dateFrom, query.dateTo])

  async function loadBootstrap() {
    setIsLoading(true)

    try {
      await Promise.all([loadSummary(), loadInbox(defaultQuery), loadFilterOptions()])
    } catch {
      // Individual loaders already set error state.
    } finally {
      setIsLoading(false)
    }
  }

  async function loadSummary() {
    try {
      const response = await sixramApi.getApprovalCenterSummary()
      setSummary(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadInbox(nextQuery: ApprovalCenterQuery) {
    try {
      const response = await sixramApi.getApprovalCenterInbox(nextQuery)
      setInbox(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadFilterOptions() {
    try {
      if (canUseAdminFilters) {
        const response = await sixramApi.getApprovalCenterOptions()
        setOptions(response)
      }
    } catch {
      setOptions(null)
    }
  }

  async function handleOpenDetail(item: ApprovalCenterInboxItem) {
    setIsLoadingDetail(true)

    try {
      if (item.approvalType === 'leave_request') {
        const record = await sixramApi.getApprovalLeaveRequest(item.requestId)
        setDetailState({ type: 'leave', record })
      } else if (item.approvalType === 'attendance_adjustment_request') {
        const record = await sixramApi.getApprovalAttendanceAdjustment(item.requestId)
        setDetailState({ type: 'attendance', record })
      } else if (item.approvalType === 'profile_change_request') {
        const record = await sixramApi.getApprovalProfileChangeRequest(item.requestId)
        setDetailState({ type: 'profile', record })
      } else {
        setDetailState({ type: 'payroll', record: item })
      }
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingDetail(false)
    }
  }

  async function handleSubmitAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!actionState) {
      return
    }

    setIsSubmittingAction(true)

    try {
      if (actionState.item.approvalType === 'leave_request') {
        if (actionState.mode === 'approve') {
          await sixramApi.approveApprovalLeaveRequest(actionState.item.requestId, { remarks: actionRemarks })
        } else {
          await sixramApi.rejectApprovalLeaveRequest(actionState.item.requestId, { remarks: actionRemarks })
        }
      } else if (actionState.item.approvalType === 'attendance_adjustment_request') {
        const payload: ApprovalActionInput = { remarks: actionRemarks }
        if (actionState.mode === 'approve') {
          await sixramApi.approveAttendanceAdjustment(actionState.item.requestId, payload)
        } else {
          await sixramApi.rejectAttendanceAdjustment(actionState.item.requestId, payload)
        }
      } else if (actionState.item.approvalType === 'profile_change_request') {
        const payload: ApprovalActionInput = { remarks: actionRemarks }
        if (actionState.mode === 'approve') {
          await sixramApi.approveProfileChangeRequest(actionState.item.requestId, payload)
        } else {
          await sixramApi.rejectProfileChangeRequest(actionState.item.requestId, payload)
        }
      } else {
        const payload = { remarks: actionRemarks }
        if (actionState.mode === 'approve') {
          await sixramApi.approveApprovalPayrollAdjustment(actionState.item.requestId, payload)
        } else {
          await sixramApi.rejectApprovalPayrollAdjustment(actionState.item.requestId, payload)
        }
      }

      setActionState(null)
      setActionRemarks('')
      setDetailState(null)
      await Promise.all([loadSummary(), loadInbox(query)])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmittingAction(false)
    }
  }

  const allowedTypes = useMemo(() => {
    const values = [
      { value: 'leave_request', label: 'Leave requests' },
      { value: 'attendance_adjustment_request', label: 'Attendance corrections' },
      { value: 'profile_change_request', label: 'Profile changes' },
    ]

    if (isAdmin || (user?.roles.includes('PayrollOfficer') ?? false)) {
      values.push({ value: 'payroll_adjustment', label: 'Payroll adjustments' })
    }

    return values
  }, [isAdmin, user?.roles])

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">Approval center</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">A single inbox for review work</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Review employee requests, compare details, and record approval or rejection remarks from one shared workflow space.
            </p>
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <SummaryCard label="All pending" value={String(summary?.totalPendingCount ?? 0)} tone="brand" />
        <SummaryCard label="Leave" value={String(summary?.pendingLeaveRequestCount ?? 0)} />
        <SummaryCard label="Attendance" value={String(summary?.pendingAttendanceAdjustmentRequestCount ?? 0)} />
        <SummaryCard label="Profile" value={String(summary?.pendingProfileChangeRequestCount ?? 0)} />
        <SummaryCard label="Payroll" value={String(summary?.pendingPayrollAdjustmentCount ?? 0)} />
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="grid gap-4 xl:grid-cols-[1.5fr_repeat(6,minmax(0,1fr))]">
          <label className="block space-y-2">
            <span className="shell-label mb-0">Search</span>
            <input
              className="shell-input"
              onChange={(event) => setSearchDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  setQuery((current) => ({ ...current, search: searchDraft, pageNumber: 1 }))
                }
              }}
              placeholder="Employee code, name, title..."
              value={searchDraft}
            />
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Type</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, type: event.target.value, pageNumber: 1 }))}
              value={query.type ?? ''}
            >
              <option value="">All</option>
              {allowedTypes.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Status</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
              value={query.status ?? ''}
            >
              <option value="">All</option>
              <option value="pending">Pending</option>
              <option value="approved">Approved</option>
              <option value="rejected">Rejected</option>
              <option value="cancelled">Cancelled</option>
            </select>
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">From</span>
            <input
              className="shell-input"
              onChange={(event) => setQuery((current) => ({ ...current, dateFrom: event.target.value, pageNumber: 1 }))}
              type="date"
              value={query.dateFrom ?? ''}
            />
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">To</span>
            <input
              className="shell-input"
              onChange={(event) => setQuery((current) => ({ ...current, dateTo: event.target.value, pageNumber: 1 }))}
              type="date"
              value={query.dateTo ?? ''}
            />
          </label>
          <FilterSelect
            label="Department"
            onChange={(value) => setQuery((current) => ({ ...current, departmentId: value, pageNumber: 1 }))}
            options={options?.departments ?? []}
            value={query.departmentId ?? ''}
          />
          <FilterSelect
            label="Branch"
            onChange={(value) => setQuery((current) => ({ ...current, branchId: value, pageNumber: 1 }))}
            options={options?.branches ?? []}
            value={query.branchId ?? ''}
          />
        </div>

        <div className="mt-4 flex flex-wrap gap-3">
          <button
            className="shell-button"
            onClick={() => setQuery((current) => ({ ...current, search: searchDraft, pageNumber: 1 }))}
            type="button"
          >
            Apply filters
          </button>
          <button
            className="shell-button-secondary"
            onClick={() => {
              setSearchDraft('')
              setQuery(defaultQuery)
            }}
            type="button"
          >
            Reset
          </button>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Employee</th>
                <th>Details</th>
                <th>Status</th>
                <th>Submitted</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading approval items...
                  </td>
                </tr>
              ) : !inbox || inbox.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No approval items matched the current filters.
                  </td>
                </tr>
              ) : (
                inbox.items.map((item) => (
                  <tr key={`${item.approvalType}-${item.requestId}`}>
                    <td>
                      <div className="font-semibold text-slate-900">{item.approvalTypeLabel}</div>
                      <div className="mt-1 text-slate-500">{item.currentApproverDisplayName || 'Assigned queue'}</div>
                    </td>
                    <td>
                      <div className="font-semibold text-slate-900">{item.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{item.employeeCode}</div>
                    </td>
                    <td>
                      <div className="font-semibold text-slate-900">{item.title}</div>
                      <div className="mt-1 text-slate-500">{item.subtitle || 'No extra details provided.'}</div>
                    </td>
                    <td>
                      <RequestStatusBadge status={item.status} />
                    </td>
                    <td className="text-slate-500">{formatDateTime(item.submittedAtUtc)}</td>
                    <td className="text-right">
                      <button className="shell-button-secondary px-3 py-2" onClick={() => void handleOpenDetail(item)} type="button">
                        Review
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {inbox ? (
          <PaginationControls
            pageNumber={inbox.pageNumber}
            pageSize={inbox.pageSize}
            totalCount={inbox.totalCount}
            totalPages={inbox.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>

      <Modal
        description="Compare the request details before recording your decision."
        onClose={() => {
          if (!isLoadingDetail) {
            setDetailState(null)
          }
        }}
        open={Boolean(detailState)}
        title={detailState ? `Review ${detailState.type}` : 'Review approval'}
      >
        {isLoadingDetail ? (
          <div className="text-sm text-slate-500">Loading request details...</div>
        ) : detailState ? (
          <div className="space-y-5">
            {detailState.type === 'leave' ? (
              <div className="space-y-3">
                <DetailRow label="Leave type" value={detailState.record.leaveTypeName} />
                <DetailRow label="Dates" value={`${formatDate(detailState.record.startDate)} to ${formatDate(detailState.record.endDate)}`} />
                <DetailRow label="Days" value={`${detailState.record.totalLeaveDays}`} />
                <DetailRow label="Reason" value={detailState.record.reason || '-'} />
                <DetailRow label="Current status" value={detailState.record.status} />
              </div>
            ) : detailState.type === 'attendance' ? (
              <div className="space-y-3">
                <DetailRow label="Attendance date" value={formatDate(detailState.record.attendanceDate)} />
                <DetailRow label="Request type" value={detailState.record.requestType.replace(/_/g, ' ')} />
                <DetailRow label="Current time in" value={detailState.record.currentTimeIn ? formatDateTime(detailState.record.currentTimeIn) : '-'} />
                <DetailRow label="Requested time in" value={detailState.record.requestedTimeIn ? formatDateTime(detailState.record.requestedTimeIn) : '-'} />
                <DetailRow label="Current time out" value={detailState.record.currentTimeOut ? formatDateTime(detailState.record.currentTimeOut) : '-'} />
                <DetailRow label="Requested time out" value={detailState.record.requestedTimeOut ? formatDateTime(detailState.record.requestedTimeOut) : '-'} />
                <DetailRow label="Reason" value={detailState.record.reason || '-'} />
              </div>
            ) : detailState.type === 'profile' ? (
              <div className="space-y-3">
                <DetailRow label="Request type" value={detailState.record.requestType.replace(/_/g, ' ')} />
                <DetailRow label="Reason" value={detailState.record.reason || '-'} />
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                  <div className="text-sm font-semibold text-slate-900">Requested field changes</div>
                  <div className="mt-3 space-y-3">
                    {detailState.record.fieldChanges.map((change) => (
                      <div className="rounded-xl border border-slate-200 bg-white px-4 py-3" key={change.fieldKey}>
                        <div className="text-sm font-semibold text-slate-900">{change.label}</div>
                        <div className="mt-1 text-sm text-slate-500">From: {change.oldValue || '-'}</div>
                        <div className="mt-1 text-sm text-slate-500">To: {change.newValue || '-'}</div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                <DetailRow label="Employee" value={detailState.record.employeeFullName} />
                <DetailRow label="Title" value={detailState.record.title} />
                <DetailRow label="Details" value={detailState.record.subtitle || '-'} />
                <DetailRow label="Submitted" value={formatDateTime(detailState.record.submittedAtUtc)} />
              </div>
            )}

            <div className="flex justify-end gap-3">
              <button className="shell-button-secondary" onClick={() => setDetailState(null)} type="button">
                Close
              </button>
              <button
                className="shell-button-secondary"
                onClick={() => {
                  if (!detailState) {
                    return
                  }

                  setActionState({
                    item: toApprovalItem(detailState),
                    mode: 'reject',
                  })
                  setActionRemarks('')
                }}
                type="button"
              >
                Reject
              </button>
              <button
                className="shell-button"
                onClick={() => {
                  if (!detailState) {
                    return
                  }

                  setActionState({
                    item: toApprovalItem(detailState),
                    mode: 'approve',
                  })
                  setActionRemarks('')
                }}
                type="button"
              >
                Approve
              </button>
            </div>
          </div>
        ) : null}
      </Modal>

      <Modal
        description="Decision remarks are stored with the approval action and included in the workflow audit trail."
        onClose={() => {
          if (!isSubmittingAction) {
            setActionState(null)
            setActionRemarks('')
          }
        }}
        open={Boolean(actionState)}
        title={actionState ? `${actionState.mode === 'approve' ? 'Approve' : 'Reject'} request` : 'Decision'}
      >
        <form className="space-y-5" onSubmit={handleSubmitAction}>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Remarks</span>
            <textarea
              className="shell-textarea"
              onChange={(event) => setActionRemarks(event.target.value)}
              rows={4}
              value={actionRemarks}
            />
          </label>

          <div className="flex justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setActionState(null)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSubmittingAction} type="submit">
              {isSubmittingAction ? 'Submitting...' : actionState?.mode === 'approve' ? 'Approve' : 'Reject'}
            </button>
          </div>
        </form>
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
  tone?: 'default' | 'brand'
}) {
  const className = tone === 'brand' ? 'border-[#465fff]/20 bg-[#465fff]/5' : 'border-slate-200 bg-slate-50'

  return (
    <div className={`rounded-2xl border px-4 py-4 ${className}`}>
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}

function FilterSelect({
  label,
  onChange,
  options,
  value,
}: {
  label: string
  onChange: (value: string) => void
  options: LookupOption[]
  value: string
}) {
  return (
    <label className="block space-y-2">
      <span className="shell-label mb-0">{label}</span>
      <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">All</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.name}
          </option>
        ))}
      </select>
    </label>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
      <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400">{label}</div>
      <div className="mt-2 text-sm text-slate-900">{value}</div>
    </div>
  )
}

function toApprovalItem(detailState: DetailState): ApprovalCenterInboxItem {
  if (detailState.type === 'leave') {
    return {
      approvalType: 'leave_request',
      approvalTypeLabel: 'Leave request',
      requestId: detailState.record.id,
      employeeId: detailState.record.employeeId,
      employeeCode: detailState.record.employeeCode,
      employeeFullName: detailState.record.employeeFullName,
      departmentName: detailState.record.departmentName,
      branchName: detailState.record.branchName,
      title: `${formatDate(detailState.record.startDate)} to ${formatDate(detailState.record.endDate)}`,
      subtitle: detailState.record.reason,
      status: detailState.record.status,
      currentApproverDisplayName: detailState.record.currentApproverDisplayName,
      submittedAtUtc: detailState.record.submittedAtUtc ?? detailState.record.createdAtUtc,
      lastUpdatedAtUtc: detailState.record.updatedAtUtc ?? detailState.record.createdAtUtc,
    }
  }

  if (detailState.type === 'attendance') {
    return {
      approvalType: 'attendance_adjustment_request',
      approvalTypeLabel: 'Attendance correction',
      requestId: detailState.record.id,
      employeeId: detailState.record.employeeId,
      employeeCode: detailState.record.employeeCode,
      employeeFullName: detailState.record.employeeFullName,
      departmentName: '',
      branchName: '',
      title: formatDate(detailState.record.attendanceDate),
      subtitle: detailState.record.reason,
      status: detailState.record.status,
      currentApproverDisplayName: detailState.record.currentApproverDisplayName,
      submittedAtUtc: detailState.record.createdAtUtc,
      lastUpdatedAtUtc: detailState.record.updatedAtUtc ?? detailState.record.createdAtUtc,
    }
  }

  if (detailState.type === 'profile') {
    return {
      approvalType: 'profile_change_request',
      approvalTypeLabel: 'Profile change',
      requestId: detailState.record.id,
      employeeId: detailState.record.employeeId,
      employeeCode: detailState.record.employeeCode,
      employeeFullName: detailState.record.employeeFullName,
      departmentName: '',
      branchName: '',
      title: 'Personal profile update',
      subtitle: detailState.record.reason,
      status: detailState.record.status,
      currentApproverDisplayName: detailState.record.reviewedByDisplayName,
      submittedAtUtc: detailState.record.createdAtUtc,
      lastUpdatedAtUtc: detailState.record.updatedAtUtc ?? detailState.record.createdAtUtc,
    }
  }

  return detailState.record
}
