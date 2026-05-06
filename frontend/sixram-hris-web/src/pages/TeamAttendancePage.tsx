/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { PaginationControls } from '../components/PaginationControls'
import type {
  AttendanceRecordListItem,
  AttendanceRecordListQuery,
  ManagerPortalOptions,
  PagedResult,
} from '../types/models'
import { addDaysToDate, formatDate, formatDateTime, formatMinutes } from '../utils/date'
import { formatError } from '../utils/errors'

const today = new Date().toISOString().slice(0, 10)

const defaultQuery: AttendanceRecordListQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  dateFrom: addDaysToDate(today, -7),
  dateTo: today,
  status: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'date',
  descending: true,
}

export function TeamAttendancePage() {
  const [records, setRecords] = useState<PagedResult<AttendanceRecordListItem> | null>(null)
  const [options, setOptions] = useState<ManagerPortalOptions | null>(null)
  const [query, setQuery] = useState<AttendanceRecordListQuery>(defaultQuery)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function loadBootstrap() {
    try {
      const response = await sixramApi.getManagerOptions()
      setOptions(response)
      await loadAttendance(defaultQuery)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadAttendance(nextQuery: AttendanceRecordListQuery) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getTeamAttendance(nextQuery)
      setRecords(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadAttendance(query)
  }, [query.pageNumber, query.employeeId, query.departmentId, query.branchId, query.status, query.dateFrom, query.dateTo])

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div>
          <span className="shell-badge-brand">Team attendance</span>
          <h2 className="mt-4 text-3xl font-semibold text-slate-950">Attendance visibility for your direct reports</h2>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
            Review current and historical attendance across your team, including lateness, undertime, incomplete logs, and leave-generated attendance markers.
          </p>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="grid gap-4 xl:grid-cols-[1.3fr_repeat(5,minmax(0,1fr))]">
          <label className="block space-y-2">
            <span className="shell-label mb-0">Employee</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, employeeId: event.target.value, pageNumber: 1 }))}
              value={query.employeeId ?? ''}
            >
              <option value="">All team members</option>
              {(options?.employees ?? []).map((member) => (
                <option key={member.id} value={member.id}>
                  {member.employeeCode} | {member.fullName}
                </option>
              ))}
            </select>
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Department</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))}
              value={query.departmentId ?? ''}
            >
              <option value="">All departments</option>
              {(options?.departments ?? []).map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Branch</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, branchId: event.target.value, pageNumber: 1 }))}
              value={query.branchId ?? ''}
            >
              <option value="">All branches</option>
              {(options?.branches ?? []).map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
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
          <label className="block space-y-2">
            <span className="shell-label mb-0">Status</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
              value={query.status ?? ''}
            >
              <option value="">All statuses</option>
              <option value="Present">Present</option>
              <option value="Late">Late</option>
              <option value="Undertime">Undertime</option>
              <option value="Incomplete">Incomplete</option>
              <option value="Absent">Absent</option>
              <option value="On Leave">On Leave</option>
              <option value="Rest Day">Rest Day</option>
              <option value="No Schedule">No Schedule</option>
            </select>
          </label>
        </div>

        <div className="mt-4 flex flex-wrap gap-3">
          <button className="shell-button-secondary" onClick={() => setQuery(defaultQuery)} type="button">
            Reset filters
          </button>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Date</th>
                <th>Status</th>
                <th>Scheduled</th>
                <th>Actual</th>
                <th>Minutes</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading attendance records...
                  </td>
                </tr>
              ) : !records || records.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No attendance records matched the current filters.
                  </td>
                </tr>
              ) : (
                records.items.map((record) => (
                  <tr key={`${record.attendanceRecordId ?? 'synthetic'}-${record.employeeId}-${record.attendanceDate}`}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                    </td>
                    <td className="text-slate-500">{formatDate(record.attendanceDate)}</td>
                    <td>
                      <AttendanceStatusBadge status={record.status} />
                    </td>
                    <td className="text-slate-500">
                      <div>{record.scheduledStartTime || '-'}</div>
                      <div className="mt-1">{record.scheduledEndTime || '-'}</div>
                    </td>
                    <td className="text-slate-500">
                      <div>In: {record.actualTimeIn ? formatDateTime(record.actualTimeIn) : '-'}</div>
                      <div className="mt-1">Out: {record.actualTimeOut ? formatDateTime(record.actualTimeOut) : '-'}</div>
                    </td>
                    <td className="text-slate-500">
                      <div>Late: {formatMinutes(record.lateMinutes)}</div>
                      <div className="mt-1">UT: {formatMinutes(record.undertimeMinutes)}</div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {records ? (
          <PaginationControls
            pageNumber={records.pageNumber}
            pageSize={records.pageSize}
            totalCount={records.totalCount}
            totalPages={records.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>
    </div>
  )
}
