/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type { ManagerDashboard } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

export function ManagerDashboardPage() {
  const [dashboard, setDashboard] = useState<ManagerDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function loadDashboard() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getManagerDashboard()
      setDashboard(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadDashboard()
  }, [])

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading your team dashboard...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">Manager portal</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Your direct report view for today</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Stay on top of attendance, leave, and pending approvals for your team without stepping into the broader HR admin workspace.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/manager/team">
              My team
            </Link>
            <Link className="shell-button" to="/approvals">
              Open approvals
            </Link>
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SummaryCard label="Direct reports" value={String(dashboard?.directReportCount ?? 0)} />
        <SummaryCard label="Pending approvals" value={String(dashboard?.pendingApprovalCount ?? 0)} tone="warning" />
        <SummaryCard label="Late today" value={String(dashboard?.lateTodayCount ?? 0)} />
        <SummaryCard label="On leave today" value={String(dashboard?.onLeaveTodayCount ?? 0)} />
        <SummaryCard label="Present today" value={String(dashboard?.presentTodayCount ?? 0)} />
        <SummaryCard label="Absent today" value={String(dashboard?.absentTodayCount ?? 0)} tone="danger" />
        <SummaryCard label="Incomplete logs" value={String(dashboard?.incompleteLogCount ?? 0)} />
        <SummaryCard label="No schedule" value={String(dashboard?.employeesWithoutScheduleCount ?? 0)} />
      </section>

      <section className="grid gap-6 xl:grid-cols-[1fr_1fr]">
        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Actions</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Manager shortcuts</h3>
          </div>

          <div className="mt-6 grid gap-3">
            <Link className="rounded-2xl border border-slate-200 px-4 py-4 transition hover:bg-slate-50" to="/manager/attendance">
              <div className="font-semibold text-slate-900">Team attendance</div>
              <div className="mt-1 text-sm text-slate-500">Review daily status, late logs, and attendance issues.</div>
            </Link>
            <Link className="rounded-2xl border border-slate-200 px-4 py-4 transition hover:bg-slate-50" to="/manager/leave">
              <div className="font-semibold text-slate-900">Team leave</div>
              <div className="mt-1 text-sm text-slate-500">Track leave requests, calendar coverage, and upcoming time off.</div>
            </Link>
            <Link className="rounded-2xl border border-slate-200 px-4 py-4 transition hover:bg-slate-50" to="/approvals">
              <div className="font-semibold text-slate-900">Approval center</div>
              <div className="mt-1 text-sm text-slate-500">Act on assigned leave and attendance requests with review notes.</div>
            </Link>
          </div>
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Notifications</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Recent team alerts</h3>
          </div>

          <div className="mt-6 space-y-3">
            {dashboard?.notifications.length ? (
              dashboard.notifications.slice(0, 6).map((notification) => (
                <div className="rounded-2xl border border-slate-200 px-4 py-3" key={notification.id}>
                  <div className="font-semibold text-slate-900">{notification.title}</div>
                  <div className="mt-1 text-sm text-slate-500">{notification.message}</div>
                  <div className="mt-2 text-xs uppercase tracking-[0.16em] text-slate-400">{formatDateTime(notification.createdAtUtc)}</div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                No manager notifications are waiting right now.
              </div>
            )}
          </div>
        </section>
      </section>
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
  tone?: 'default' | 'warning' | 'danger'
}) {
  const className =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : 'border-slate-200 bg-slate-50'

  return (
    <div className={`rounded-2xl border px-4 py-4 ${className}`}>
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}
