import { useEffect, useMemo, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  LeaveActionInput,
  LeaveCalendarQuery,
  LeaveCalendarResponse,
  LeaveRequest,
  LeaveRequestListQuery,
  LookupOption,
  ManagerPortalOptions,
  PagedResult,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

type ActionState = {
  mode: 'approve' | 'reject'
  request: LeaveRequest
}

const today = new Date()

const defaultRequestQuery: LeaveRequestListQuery = {
  employeeId: '',
  departmentId: '',
  branchId: '',
  status: '',
  dateFrom: '',
  dateTo: '',
  pageNumber: 1,
  pageSize: 8,
  sortBy: 'submitted',
  descending: true,
}

export function TeamLeavePage() {
  const [requests, setRequests] = useState<PagedResult<LeaveRequest> | null>(null)
  const [calendar, setCalendar] = useState<LeaveCalendarResponse | null>(null)
  const [options, setOptions] = useState<ManagerPortalOptions | null>(null)
  const [query, setQuery] = useState<LeaveRequestListQuery>(defaultRequestQuery)
  const [calendarQuery, setCalendarQuery] = useState<LeaveCalendarQuery>({
    year: today.getFullYear(),
    month: today.getMonth() + 1,
    employeeId: '',
    departmentId: '',
    branchId: '',
    status: 'approved',
  })
  const [actionState, setActionState] = useState<ActionState | null>(null)
  const [actionRemarks, setActionRemarks] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoadingRequests, setIsLoadingRequests] = useState(true)
  const [isLoadingCalendar, setIsLoadingCalendar] = useState(true)
  const [isSubmittingAction, setIsSubmittingAction] = useState(false)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadRequests(query)
  }, [options, query.pageNumber, query.employeeId, query.departmentId, query.branchId, query.status, query.dateFrom, query.dateTo])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadCalendar(calendarQuery)
  }, [options, calendarQuery.year, calendarQuery.month, calendarQuery.employeeId, calendarQuery.departmentId, calendarQuery.branchId, calendarQuery.status])

  async function loadBootstrap() {
    try {
      const response = await sixramApi.getManagerOptions()
      setOptions(response)

      await Promise.all([loadRequests(defaultRequestQuery), loadCalendar({
        year: today.getFullYear(),
        month: today.getMonth() + 1,
        employeeId: '',
        departmentId: '',
        branchId: '',
        status: 'approved',
      })])
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRequests(nextQuery: LeaveRequestListQuery) {
    setIsLoadingRequests(true)

    try {
      const response = await sixramApi.getTeamLeaveRequests(nextQuery)
      setRequests(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingRequests(false)
    }
  }

  async function loadCalendar(nextQuery: LeaveCalendarQuery) {
    setIsLoadingCalendar(true)

    try {
      const response = await sixramApi.getTeamLeaveCalendar(nextQuery)
      setCalendar(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingCalendar(false)
    }
  }

  async function handleSubmitAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!actionState) {
      return
    }

    setIsSubmittingAction(true)
    const payload: LeaveActionInput = { remarks: actionRemarks }

    try {
      if (actionState.mode === 'approve') {
        await sixramApi.approveApprovalLeaveRequest(actionState.request.id, payload)
      } else {
        await sixramApi.rejectApprovalLeaveRequest(actionState.request.id, payload)
      }

      setActionState(null)
      setActionRemarks('')
      await Promise.all([loadRequests(query), loadCalendar(calendarQuery)])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmittingAction(false)
    }
  }

  const calendarEntriesByDay = useMemo(() => {
    const lookup = new Map<string, LeaveRequest[]>()
    if (!calendar) {
      return lookup
    }

    calendar.entries.forEach((entry) => {
      const start = new Date(`${entry.startDate}T00:00:00`)
      const end = new Date(`${entry.endDate}T00:00:00`)
      for (let cursor = new Date(start); cursor <= end; cursor.setDate(cursor.getDate() + 1)) {
        const key = `${cursor.getFullYear()}-${String(cursor.getMonth() + 1).padStart(2, '0')}-${String(cursor.getDate()).padStart(2, '0')}`
        const list = lookup.get(key) ?? []
        list.push({
          id: entry.id,
          employeeId: entry.employeeId,
          employeeCode: entry.employeeCode,
          employeeFullName: entry.employeeFullName,
          departmentName: entry.departmentName,
          branchName: entry.branchName,
          leaveTypeId: '',
          leaveTypeCode: '',
          leaveTypeName: entry.leaveTypeName,
          leaveTypeIsPaid: entry.leaveTypeIsPaid,
          startDate: entry.startDate,
          endDate: entry.endDate,
          startDayType: 'full_day',
          endDayType: 'full_day',
          totalLeaveDays: entry.totalLeaveDays,
          reason: '',
          status: entry.status,
          currentApproverDisplayName: '',
          decisionRemarks: '',
          hasAttachment: false,
          attachmentOriginalFileName: '',
          hasAttendanceConflict: false,
          attendanceConflictCount: 0,
          availableBalanceAfterApproval: 0,
          createdByDisplayName: '',
          updatedByDisplayName: '',
          createdAtUtc: '',
        })
        lookup.set(key, list)
      }
    })

    return lookup
  }, [calendar])

  const monthDays = useMemo(
    () => buildMonthGrid(calendarQuery.year, calendarQuery.month),
    [calendarQuery.month, calendarQuery.year],
  )

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">Team leave</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Leaves, coverage, and approvals</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Review pending leave requests for your team and check approved leave coverage on a simple monthly planner.
            </p>
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
        <section className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Calendar</p>
              <h3 className="mt-2 text-xl font-semibold text-slate-950">Monthly leave view</h3>
            </div>
            <div className="flex items-center gap-2">
              <button
                className="shell-button-secondary px-3 py-2"
                onClick={() =>
                  setCalendarQuery((current) => {
                    const month = current.month === 1 ? 12 : current.month - 1
                    const year = current.month === 1 ? current.year - 1 : current.year
                    return { ...current, year, month }
                  })
                }
                type="button"
              >
                Prev
              </button>
              <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-slate-900">
                {new Date(calendarQuery.year, calendarQuery.month - 1, 1).toLocaleDateString('en-US', {
                  month: 'long',
                  year: 'numeric',
                })}
              </div>
              <button
                className="shell-button-secondary px-3 py-2"
                onClick={() =>
                  setCalendarQuery((current) => {
                    const month = current.month === 12 ? 1 : current.month + 1
                    const year = current.month === 12 ? current.year + 1 : current.year
                    return { ...current, year, month }
                  })
                }
                type="button"
              >
                Next
              </button>
            </div>
          </div>

          <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <FilterSelect
              label="Employee"
              onChange={(value) => setCalendarQuery((current) => ({ ...current, employeeId: value }))}
              options={(options?.employees ?? []).map((member) => ({
                id: member.id,
                code: member.employeeCode,
                name: member.fullName,
                isActive: true,
              }))}
              value={calendarQuery.employeeId ?? ''}
            />
            <FilterSelect
              label="Department"
              onChange={(value) => setCalendarQuery((current) => ({ ...current, departmentId: value }))}
              options={options?.departments ?? []}
              value={calendarQuery.departmentId ?? ''}
            />
            <FilterSelect
              label="Branch"
              onChange={(value) => setCalendarQuery((current) => ({ ...current, branchId: value }))}
              options={options?.branches ?? []}
              value={calendarQuery.branchId ?? ''}
            />
            <label className="block space-y-2">
              <span className="shell-label mb-0">Status</span>
              <select
                className="shell-select"
                onChange={(event) => setCalendarQuery((current) => ({ ...current, status: event.target.value }))}
                value={calendarQuery.status ?? ''}
              >
                <option value="">All</option>
                <option value="approved">Approved</option>
                <option value="pending">Pending</option>
              </select>
            </label>
          </div>

          <div className="mt-6 grid grid-cols-7 gap-2 text-center text-xs font-semibold uppercase tracking-[0.16em] text-slate-400">
            {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((label) => (
              <div key={label}>{label}</div>
            ))}
          </div>

          <div className="mt-3 grid grid-cols-7 gap-2">
            {monthDays.map((day) => {
              const entries = day ? calendarEntriesByDay.get(day.isoDate) ?? [] : []
              return (
                <div
                  className={[
                    'min-h-[110px] rounded-2xl border p-3',
                    day ? 'border-slate-200 bg-white' : 'border-transparent bg-transparent',
                  ].join(' ')}
                  key={day?.key ?? Math.random().toString(36)}
                >
                  {day ? (
                    <>
                      <div className="text-sm font-semibold text-slate-900">{day.day}</div>
                      <div className="mt-2 space-y-1">
                        {entries.slice(0, 2).map((entry) => (
                          <div className="rounded-lg bg-slate-100 px-2 py-1 text-left text-[11px] text-slate-700" key={`${day.isoDate}-${entry.id}`}>
                            <div className="truncate font-semibold">{entry.employeeFullName}</div>
                            <div className="truncate">{entry.leaveTypeName}</div>
                          </div>
                        ))}
                        {entries.length > 2 ? <div className="text-[11px] text-slate-400">+{entries.length - 2} more</div> : null}
                      </div>
                    </>
                  ) : null}
                </div>
              )
            })}
          </div>

          {isLoadingCalendar ? <div className="mt-4 text-sm text-slate-500">Refreshing calendar...</div> : null}
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Requests</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Team leave requests</h3>
          </div>

          <div className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            <FilterSelect
              label="Employee"
              onChange={(value) => setQuery((current) => ({ ...current, employeeId: value, pageNumber: 1 }))}
              options={(options?.employees ?? []).map((member) => ({
                id: member.id,
                code: member.employeeCode,
                name: member.fullName,
                isActive: true,
              }))}
              value={query.employeeId ?? ''}
            />
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
          </div>

          <div className="shell-table-wrap mt-6">
            <table className="shell-table">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Leave</th>
                  <th>Status</th>
                  <th>Submitted</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {isLoadingRequests ? (
                  <tr>
                    <td className="text-slate-500" colSpan={5}>
                      Loading leave requests...
                    </td>
                  </tr>
                ) : !requests || requests.items.length === 0 ? (
                  <tr>
                    <td className="text-slate-500" colSpan={5}>
                      No leave requests matched the current filters.
                    </td>
                  </tr>
                ) : (
                  requests.items.map((request) => (
                    <tr key={request.id}>
                      <td>
                        <div className="font-semibold text-slate-900">{request.employeeFullName}</div>
                        <div className="mt-1 text-slate-500">{request.employeeCode}</div>
                      </td>
                      <td>
                        <div className="font-semibold text-slate-900">{request.leaveTypeName}</div>
                        <div className="mt-1 text-slate-500">
                          {formatDate(request.startDate)} to {formatDate(request.endDate)}
                        </div>
                        <div className="mt-1 text-slate-400">{request.reason || 'No reason provided.'}</div>
                      </td>
                      <td>
                        <LeaveStatusBadge status={request.status} />
                      </td>
                      <td className="text-slate-500">{formatDateTime(request.submittedAtUtc ?? request.createdAtUtc)}</td>
                      <td className="text-right">
                        {request.status.toLowerCase() === 'pending' ? (
                          <div className="flex justify-end gap-2">
                            <button
                              className="shell-button-secondary px-3 py-2"
                              onClick={() => {
                                setActionState({ mode: 'reject', request })
                                setActionRemarks('')
                              }}
                              type="button"
                            >
                              Reject
                            </button>
                            <button
                              className="shell-button px-3 py-2"
                              onClick={() => {
                                setActionState({ mode: 'approve', request })
                                setActionRemarks('')
                              }}
                              type="button"
                            >
                              Approve
                            </button>
                          </div>
                        ) : (
                          <span className="text-sm text-slate-500">{request.currentApproverDisplayName || '-'}</span>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {requests ? (
            <PaginationControls
              pageNumber={requests.pageNumber}
              pageSize={requests.pageSize}
              totalCount={requests.totalCount}
              totalPages={requests.totalPages}
              onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
            />
          ) : null}
        </section>
      </section>

      <Modal
        description={
          actionState
            ? actionState.mode === 'approve'
              ? 'Approval will post the leave into the employee balance and update attendance where needed.'
              : 'Rejecting the request will keep balances unchanged and notify the requester.'
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
            ? `${actionState.mode === 'approve' ? 'Approve' : 'Reject'} ${actionState.request.employeeFullName}`
            : 'Leave request action'
        }
      >
        <form className="space-y-5" onSubmit={handleSubmitAction}>
          {actionState ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-600">
              <div className="font-semibold text-slate-900">{actionState.request.leaveTypeName}</div>
              <div className="mt-1">
                {formatDate(actionState.request.startDate)} to {formatDate(actionState.request.endDate)} | {actionState.request.totalLeaveDays} day(s)
              </div>
              <div className="mt-2">{actionState.request.reason || 'No reason provided.'}</div>
            </div>
          ) : null}

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
              {isSubmittingAction ? 'Submitting...' : actionState?.mode === 'approve' ? 'Approve request' : 'Reject request'}
            </button>
          </div>
        </form>
      </Modal>
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

function buildMonthGrid(year: number, month: number) {
  const firstDay = new Date(year, month - 1, 1)
  const lastDay = new Date(year, month, 0)
  const items: Array<{ key: string; day: number; isoDate: string } | null> = []

  for (let index = 0; index < firstDay.getDay(); index += 1) {
    items.push(null)
  }

  for (let day = 1; day <= lastDay.getDate(); day += 1) {
    items.push({
      key: `${year}-${month}-${day}`,
      day,
      isoDate: `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`,
    })
  }

  while (items.length % 7 !== 0) {
    items.push(null)
  }

  return items
}
