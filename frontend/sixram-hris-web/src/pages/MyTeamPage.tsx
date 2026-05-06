import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { PaginationControls } from '../components/PaginationControls'
import type { ManagerPortalOptions, ManagerTeamMember, ManagerTeamMemberListQuery, PagedResult } from '../types/models'
import { formatError } from '../utils/errors'

const defaultQuery: ManagerTeamMemberListQuery = {
  search: '',
  departmentId: '',
  branchId: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'name',
  descending: false,
}

export function MyTeamPage() {
  const [team, setTeam] = useState<PagedResult<ManagerTeamMember> | null>(null)
  const [options, setOptions] = useState<ManagerPortalOptions | null>(null)
  const [query, setQuery] = useState<ManagerTeamMemberListQuery>(defaultQuery)
  const [searchDraft, setSearchDraft] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (team === null && isLoading) {
      return
    }

    void loadTeam(query)
  }, [query.pageNumber, query.departmentId, query.branchId, query.search])

  async function loadTeam(nextQuery: ManagerTeamMemberListQuery) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getMyTeam(nextQuery)
      setTeam(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadBootstrap() {
    try {
      const response = await sixramApi.getManagerOptions()
      setOptions(response)
      await loadTeam(defaultQuery)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  const summary = useMemo(() => {
    const items = team?.items ?? []
    return {
      active: items.filter((item) => item.isActive).length,
      late: items.filter((item) => item.todayAttendanceStatus === 'Late').length,
      leave: items.filter((item) => item.leaveStatus).length,
    }
  }, [team?.items])

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My team</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Direct reports at a glance</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Review core team information, current attendance status, and leave visibility while staying within your direct-report scope.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-3">
            <SummaryCard label="Visible team" value={String(team?.totalCount ?? 0)} />
            <SummaryCard label="Active today" value={String(summary.active)} />
            <SummaryCard label="On leave" value={String(summary.leave)} />
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="grid gap-4 xl:grid-cols-[1.5fr_repeat(3,minmax(0,1fr))]">
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
              placeholder="Employee code or name"
              value={searchDraft}
            />
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
          <div className="flex items-end gap-3">
            <button
              className="shell-button w-full"
              onClick={() => setQuery((current) => ({ ...current, search: searchDraft, pageNumber: 1 }))}
              type="button"
            >
              Apply
            </button>
            <button
              className="shell-button-secondary w-full"
              onClick={() => {
                setSearchDraft('')
                setQuery(defaultQuery)
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
                <th>Organization</th>
                <th>Contact</th>
                <th>Attendance Today</th>
                <th>Leave</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    Loading team members...
                  </td>
                </tr>
              ) : !team || team.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    No direct reports matched the current filters.
                  </td>
                </tr>
              ) : (
                team.items.map((member) => (
                  <tr key={member.employeeId}>
                    <td>
                      <div className="font-semibold text-slate-900">{member.fullName}</div>
                      <div className="mt-1 text-slate-500">{member.employeeCode}</div>
                      <div className="mt-2">
                        <span className={member.isActive ? 'shell-badge-success' : 'shell-badge-muted'}>
                          {member.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                    </td>
                    <td className="text-slate-500">
                      <div>{member.departmentName || '-'}</div>
                      <div className="mt-1">{member.positionName || '-'}</div>
                      <div className="mt-1">{member.branchName || '-'}</div>
                    </td>
                    <td className="text-slate-500">
                      <div>{member.mobileNumber || '-'}</div>
                      <div className="mt-1">{member.email || '-'}</div>
                    </td>
                    <td>
                      {member.todayAttendanceStatus ? (
                        <div className="space-y-2">
                          <AttendanceStatusBadge status={member.todayAttendanceStatus} />
                          <div className="text-sm text-slate-500">{member.todayAttendanceTimeInLabel || 'No time in yet'}</div>
                        </div>
                      ) : (
                        <span className="text-sm text-slate-500">No record</span>
                      )}
                    </td>
                    <td>
                      {member.leaveStatus ? <LeaveStatusBadge status={member.leaveStatus} /> : <span className="text-sm text-slate-500">None</span>}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {team ? (
          <PaginationControls
            pageNumber={team.pageNumber}
            pageSize={team.pageSize}
            totalCount={team.totalCount}
            totalPages={team.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-wrap gap-3">
          <Link className="shell-button-secondary" to="/manager/attendance">
            Open team attendance
          </Link>
          <Link className="shell-button-secondary" to="/manager/leave">
            Open team leave
          </Link>
        </div>
      </section>
    </div>
  )
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}
