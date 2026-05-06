/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { RequestStatusBadge } from '../components/RequestStatusBadge'
import type { EmployeeRequestHistoryItem } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

export function MyRequestsPage() {
  const [items, setItems] = useState<EmployeeRequestHistoryItem[]>([])
  const [requestType, setRequestType] = useState('')
  const [status, setStatus] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [activeCancelId, setActiveCancelId] = useState<string | null>(null)

  async function loadRequests() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getMyRequests()
      setItems(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadRequests()
  }, [])

  async function handleCancel(item: EmployeeRequestHistoryItem) {
    setActiveCancelId(item.requestId)

    try {
      if (item.requestType === 'leave_request') {
        await sixramApi.cancelMyLeaveRequest(item.requestId, { remarks: 'Cancelled by employee.' })
      } else if (item.requestType === 'attendance_adjustment_request') {
        await sixramApi.cancelMyAttendanceAdjustment(item.requestId, { remarks: 'Cancelled by employee.' })
      } else if (item.requestType === 'profile_change_request') {
        await sixramApi.cancelMyProfileChangeRequest(item.requestId, { remarks: 'Cancelled by employee.' })
      }

      await loadRequests()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveCancelId(null)
    }
  }

  const filteredItems = useMemo(
    () =>
      items.filter((item) => {
        const matchesType = !requestType || item.requestType === requestType
        const matchesStatus = !status || item.status.toLowerCase() === status.toLowerCase()
        return matchesType && matchesStatus
      }),
    [items, requestType, status],
  )

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My requests</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">A single place for employee request history</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Track self-service requests across leave, attendance corrections, and profile updates, then cancel any pending item when plans change.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="block space-y-2">
              <span className="shell-label mb-0">Request type</span>
              <select className="shell-select" onChange={(event) => setRequestType(event.target.value)} value={requestType}>
                <option value="">All</option>
                <option value="leave_request">Leave request</option>
                <option value="attendance_adjustment_request">Attendance correction</option>
                <option value="profile_change_request">Profile change</option>
              </select>
            </label>
            <label className="block space-y-2">
              <span className="shell-label mb-0">Status</span>
              <select className="shell-select" onChange={(event) => setStatus(event.target.value)} value={status}>
                <option value="">All</option>
                <option value="pending">Pending</option>
                <option value="approved">Approved</option>
                <option value="rejected">Rejected</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </label>
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Request history</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Submitted requests</h3>
          </div>
          <span className="shell-badge-muted">{filteredItems.length} items</span>
        </div>

        <div className="mt-6 space-y-4">
          {isLoading ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
              Loading your request history...
            </div>
          ) : filteredItems.length === 0 ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
              No requests matched the current filters.
            </div>
          ) : (
            filteredItems.map((item) => (
              <div className="rounded-2xl border border-slate-200 px-5 py-4" key={`${item.requestType}-${item.requestId}`}>
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="shell-badge-muted">{item.requestLabel}</span>
                      <RequestStatusBadge status={item.status} />
                    </div>
                    <h4 className="mt-3 text-lg font-semibold text-slate-950">{item.title}</h4>
                    <p className="mt-2 text-sm text-slate-500">{item.subtitle || 'No additional details provided.'}</p>
                    <div className="mt-3 flex flex-wrap gap-4 text-sm text-slate-500">
                      <span>Submitted {formatDateTime(item.submittedAtUtc)}</span>
                      <span>Updated {formatDateTime(item.lastUpdatedAtUtc)}</span>
                      <span>{item.currentApproverDisplayName || 'Waiting for review'}</span>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    {item.requestType === 'leave_request' ? (
                      <Link className="shell-button-secondary px-3 py-2" to="/me/leave">
                        Open leave
                      </Link>
                    ) : item.requestType === 'attendance_adjustment_request' ? (
                      <Link className="shell-button-secondary px-3 py-2" to="/me/attendance">
                        Open attendance
                      </Link>
                    ) : (
                      <Link className="shell-button-secondary px-3 py-2" to="/me/profile">
                        Open profile
                      </Link>
                    )}

                    {item.canCancel ? (
                      <button
                        className="shell-button-secondary px-3 py-2"
                        disabled={activeCancelId === item.requestId}
                        onClick={() => void handleCancel(item)}
                        type="button"
                      >
                        {activeCancelId === item.requestId ? 'Cancelling...' : 'Cancel'}
                      </button>
                    ) : null}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  )
}
