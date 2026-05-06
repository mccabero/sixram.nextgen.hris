import { useEffect, useMemo, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import type { LeaveCalendarQuery, LeaveCalendarResponse, LeaveManagementOptions } from '../types/models'
import { formatDate } from '../utils/date'
import { formatError } from '../utils/errors'

function getDefaultCalendarQuery(): LeaveCalendarQuery {
  const today = new Date()
  return {
    year: today.getFullYear(),
    month: today.getMonth() + 1,
    departmentId: '',
    branchId: '',
    employeeId: '',
    leaveTypeId: '',
    status: '',
  }
}

export function LeaveCalendarPage() {
  const [options, setOptions] = useState<LeaveManagementOptions | null>(null)
  const [calendar, setCalendar] = useState<LeaveCalendarResponse | null>(null)
  const [query, setQuery] = useState<LeaveCalendarQuery>(getDefaultCalendarQuery())
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    void loadOptions()
  }, [])

  useEffect(() => {
    void loadCalendar()
  }, [query])

  async function loadOptions() {
    try {
      const response = await sixramApi.getLeaveOptions()
      setOptions(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadCalendar() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getLeaveCalendar(query)
      setCalendar(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  const days = useMemo(() => buildCalendarDays(calendar), [calendar])

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Leave Planning</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Leave calendar</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review approved and pending leaves in a monthly calendar view so HR can spot overlaps and upcoming absences quickly.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            {calendar?.entries.length ?? 0} requests in view
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 xl:grid-cols-6">
            <div>
              <label className="shell-label">Year</label>
              <input
                className="shell-input"
                min="2000"
                onChange={(event) => setQuery((current) => ({ ...current, year: Number(event.target.value) || current.year }))}
                type="number"
                value={query.year}
              />
            </div>

            <div>
              <label className="shell-label">Month</label>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, month: Number(event.target.value) }))}
                value={query.month}
              >
                {Array.from({ length: 12 }, (_, index) => (
                  <option key={index + 1} value={index + 1}>
                    {new Date(query.year, index, 1).toLocaleString('en-US', { month: 'long' })}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label">Department</label>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, departmentId: event.target.value }))}
                value={query.departmentId ?? ''}
              >
                <option value="">All departments</option>
                {options?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label">Branch</label>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, branchId: event.target.value }))}
                value={query.branchId ?? ''}
              >
                <option value="">All branches</option>
                {options?.branches.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label">Employee</label>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, employeeId: event.target.value }))}
                value={query.employeeId ?? ''}
              >
                <option value="">All employees</option>
                {options?.employees.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.employeeCode} | {option.fullName}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label">Leave Type</label>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, leaveTypeId: event.target.value }))}
                value={query.leaveTypeId ?? ''}
              >
                <option value="">All leave types</option>
                {options?.leaveTypes.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap gap-3">
            <button className="shell-button-secondary" onClick={() => void loadCalendar()} type="button">
              Refresh
            </button>
            <button className="shell-button-secondary" onClick={() => setQuery(getDefaultCalendarQuery())} type="button">
              Reset
            </button>
          </div>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
        ) : null}

        <div className="mt-6 overflow-x-auto">
          <div className="grid min-w-[980px] grid-cols-7 gap-4">
            {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((day) => (
              <div className="px-2 text-xs font-semibold uppercase tracking-[0.16em] text-slate-400" key={day}>
                {day}
              </div>
            ))}

            {isLoading ? (
              <div className="col-span-7 rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-sm text-slate-500">
                Loading leave calendar...
              </div>
            ) : days.length ? (
              days.map((day) => (
                <div
                  className={[
                    'min-h-[150px] rounded-2xl border p-3',
                    day.inMonth ? 'border-slate-200 bg-white' : 'border-slate-100 bg-slate-50 text-slate-400',
                  ].join(' ')}
                  key={day.key}
                >
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-semibold">{day.date.getDate()}</p>
                    {day.entries.length ? <span className="shell-badge-muted">{day.entries.length}</span> : null}
                  </div>

                  <div className="mt-3 space-y-2">
                    {day.entries.slice(0, 3).map((entry) => (
                      <div className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2" key={`${day.key}-${entry.id}`}>
                        <p className="text-xs font-semibold text-slate-900">{entry.employeeFullName}</p>
                        <p className="mt-1 text-xs text-slate-500">{entry.leaveTypeName}</p>
                        <div className="mt-2 flex items-center justify-between gap-2">
                          <LeaveStatusBadge status={entry.status} />
                          <span className="text-[11px] text-slate-500">{entry.totalLeaveDays}d</span>
                        </div>
                      </div>
                    ))}
                    {day.entries.length > 3 ? <p className="text-xs text-slate-500">+{day.entries.length - 3} more</p> : null}
                  </div>
                </div>
              ))
            ) : (
              <div className="col-span-7 rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-sm text-slate-500">
                No leave requests are visible for this calendar view.
              </div>
            )}
          </div>
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Request list for the selected month</h4>
            <p className="mt-1 text-sm text-slate-500">
              A compact list view for the same calendar month, useful for quick cross-checking and export-style review.
            </p>
          </div>
          <span className="shell-badge-muted">{calendar?.entries.length ?? 0} rows</span>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Leave</th>
                <th>Date Range</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {calendar?.entries.length ? (
                calendar.entries.map((entry) => (
                  <tr key={entry.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{entry.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{entry.employeeCode}</div>
                      <div className="mt-1 text-slate-500">
                        {[entry.departmentName, entry.branchName].filter(Boolean).join(' | ') || 'No organization assignment'}
                      </div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">{entry.leaveTypeName}</div>
                      <div className="mt-1 text-slate-500">{entry.leaveTypeIsPaid ? 'Paid leave' : 'Unpaid leave'}</div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatDate(entry.startDate)} - {formatDate(entry.endDate)}</div>
                      <div className="mt-1">{entry.totalLeaveDays} day(s)</div>
                    </td>
                    <td>
                      <LeaveStatusBadge status={entry.status} />
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={4}>
                    No leave requests are visible for the selected month.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

function buildCalendarDays(calendar: LeaveCalendarResponse | null) {
  if (!calendar) {
    return []
  }

  const firstDay = new Date(calendar.year, calendar.month - 1, 1)
  const lastDay = new Date(calendar.year, calendar.month, 0)
  const firstGridDay = new Date(firstDay)
  firstGridDay.setDate(firstDay.getDate() - firstDay.getDay())
  const lastGridDay = new Date(lastDay)
  lastGridDay.setDate(lastDay.getDate() + (6 - lastDay.getDay()))

  const days: Array<{
    key: string
    date: Date
    inMonth: boolean
    entries: LeaveCalendarResponse['entries']
  }> = []

  for (const cursor = new Date(firstGridDay); cursor <= lastGridDay; cursor.setDate(cursor.getDate() + 1)) {
    const dateKey = toDateKey(cursor)
    const entries = calendar.entries.filter((entry) => entry.startDate <= dateKey && entry.endDate >= dateKey)

    days.push({
      key: dateKey,
      date: new Date(cursor),
      inMonth: cursor.getMonth() === firstDay.getMonth(),
      entries,
    })
  }

  return days
}

function toDateKey(value: Date) {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}
