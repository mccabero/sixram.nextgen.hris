import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { useAuth } from '../auth/AuthContext'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/ContentState'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { MetricCard } from '../components/MetricCard'
import { PageSection } from '../components/PageSection'
import type { EmployeePortalDashboard } from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

export function MyDashboardPage() {
  const { user, isManager } = useAuth()
  const [dashboard, setDashboard] = useState<EmployeePortalDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    const loadDashboard = async () => {
      setIsLoading(true)

      try {
        const response = await sixramApi.getMyDashboard()
        if (!cancelled) {
          setDashboard(response)
          setError(null)
        }
      } catch (caughtError) {
        if (!cancelled) {
          setError(formatError(caughtError))
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadDashboard()

    return () => {
      cancelled = true
    }
  }, [])

  if (isLoading) {
    return <LoadingState message="Loading your employee dashboard..." />
  }

  if (error) {
    return <ErrorState message={error} />
  }

  if (!dashboard) {
    return <EmptyState message="No dashboard data is available right now." title="Nothing to show yet" />
  }

  return (
    <div className="space-y-6">
      <PageSection
        description="Your self-service workspace brings together attendance, leave, documents, and payslips in one place."
        kicker="Employee portal"
        title={`Welcome back, ${user?.displayName}.`}
      >
        <div className="flex flex-wrap gap-2 text-sm text-slate-500">
          <span className="rounded-full bg-slate-100 px-3 py-1">{dashboard.employeeCode}</span>
          {isManager ? <span className="rounded-full bg-emerald-50 px-3 py-1 text-emerald-700">Manager access enabled</span> : null}
        </div>

        <div className="mt-6 shell-summary-grid">
          <MetricCard label="Profile completion" tone="brand" value={`${dashboard.profileCompletionPercent}%`} />
          <MetricCard label="Pending leave" value={String(dashboard.pendingLeaveRequestCount)} />
          <MetricCard
            label="Pending requests"
            tone="warning"
            value={String(dashboard.pendingAttendanceAdjustmentRequestCount + dashboard.pendingProfileChangeRequestCount)}
          />
        </div>
      </PageSection>

      <section className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Today</p>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Attendance snapshot</h3>
            </div>
            {dashboard.todayAttendance ? <AttendanceStatusBadge status={dashboard.todayAttendance.status} /> : null}
          </div>

          {dashboard.todayAttendance ? (
            <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <DetailCard label="Status" value={dashboard.todayAttendance.status} />
              <DetailCard label="Time in" value={dashboard.todayAttendance.actualTimeIn ? formatDateTime(dashboard.todayAttendance.actualTimeIn) : '-'} />
              <DetailCard label="Time out" value={dashboard.todayAttendance.actualTimeOut ? formatDateTime(dashboard.todayAttendance.actualTimeOut) : '-'} />
              <DetailCard label="Last recorded" value={dashboard.lastAttendance?.actualTimeIn ? formatDateTime(dashboard.lastAttendance.actualTimeIn) : 'No recent clock'} />
            </div>
          ) : (
            <div className="mt-6">
              <EmptyState message="No attendance record is available for today yet." title="No attendance logged today" />
            </div>
          )}

          <div className="mt-6 flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/me/attendance">
              View attendance
            </Link>
            <Link className="shell-button-secondary" to="/me/requests">
              View requests
            </Link>
          </div>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Leave</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Balances and upcoming time off</h3>
          </div>

          <div className="mt-6 grid gap-3">
            {dashboard.leaveBalances.slice(0, 4).map((balance) => (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3" key={balance.id}>
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <div className="font-semibold text-slate-900">{balance.leaveTypeName}</div>
                    <div className="text-sm text-slate-500">Available {balance.availableBalance.toFixed(2)} day(s)</div>
                  </div>
                  <span className={balance.isNegativeBalance ? 'shell-badge-danger' : balance.isLowBalance ? 'shell-badge-warning' : 'shell-badge-success'}>
                    {balance.availableBalance.toFixed(2)}
                  </span>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-6 space-y-3">
            {dashboard.upcomingApprovedLeaves.length === 0 ? (
              <EmptyState message="No approved upcoming leave is scheduled yet." title="No upcoming leave" />
            ) : (
              dashboard.upcomingApprovedLeaves.map((request) => (
                <div className="flex items-center justify-between gap-4 rounded-2xl border border-slate-200 px-4 py-3" key={request.id}>
                  <div>
                    <div className="font-semibold text-slate-900">{request.leaveTypeName}</div>
                    <div className="text-sm text-slate-500">
                      {formatDate(request.startDate)} to {formatDate(request.endDate)}
                    </div>
                  </div>
                  <LeaveStatusBadge status={request.status} />
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1fr_1fr_0.9fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Documents</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Compliance reminders</h3>
          </div>

          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            <SummaryCard label="Missing required" value={String(dashboard.documentSummary.missingRequiredDocuments)} />
            <SummaryCard label="Expiring soon" value={String(dashboard.documentSummary.expiringSoonDocuments)} />
            <SummaryCard label="Expired" value={String(dashboard.documentSummary.expiredDocuments)} />
            <SummaryCard label="Active documents" value={String(dashboard.documentSummary.activeDocuments)} />
          </div>

          <div className="mt-6">
            <Link className="shell-button-secondary" to="/me/documents">
              Open my documents
            </Link>
          </div>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Payslip</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Latest payroll</h3>
          </div>

          {!dashboard.latestPayslip ? (
            <div className="mt-6">
              <EmptyState message="No visible payslip is available yet." title="No payslip available" />
            </div>
          ) : (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5">
              <p className="text-sm text-slate-500">{dashboard.latestPayslip.payPeriodName}</p>
              <p className="mt-2 text-2xl font-semibold text-slate-950">
                {formatCurrency(dashboard.latestPayslip.netPay, dashboard.latestPayslip.currency)}
              </p>
              <p className="mt-2 text-sm text-slate-500">Payroll date {formatDate(dashboard.latestPayslip.payrollDate)}</p>
              <div className="mt-4">
                <Link className="shell-button-secondary" to="/me/payslips">
                  View payslips
                </Link>
              </div>
            </div>
          )}
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Notifications</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Recent updates</h3>
          </div>

          <div className="mt-6 space-y-3">
            {dashboard.notifications.length === 0 ? (
              <EmptyState message="No new notifications right now." title="All caught up" />
            ) : (
              dashboard.notifications.slice(0, 4).map((notification) => (
                <div className="rounded-2xl border border-slate-200 px-4 py-3" key={notification.id}>
                  <div className="text-sm font-semibold text-slate-900">{notification.title}</div>
                  <div className="mt-1 text-sm text-slate-500">{notification.message}</div>
                  <div className="mt-2 text-xs uppercase tracking-[0.16em] text-slate-400">{formatDateTime(notification.createdAtUtc)}</div>
                </div>
              ))
            )}
          </div>

          <div className="mt-6">
            <Link className="shell-button-secondary" to="/notifications">
              Open notifications
            </Link>
          </div>
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

function DetailCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-sm font-semibold text-slate-900">{value || '-'}</div>
    </div>
  )
}
