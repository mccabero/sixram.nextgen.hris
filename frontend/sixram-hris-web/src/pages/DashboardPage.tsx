import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { useAuth } from '../auth/AuthContext'
import { ErrorState, LoadingState } from '../components/ContentState'
import { PageSection } from '../components/PageSection'
import type { AttendanceDashboardSummary, DocumentComplianceSummary, LeaveDashboardSummary, PayrollDashboardSummary, RbacSummary } from '../types/models'
import { formatDate } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

export function DashboardPage() {
  const { isAdmin, user } = useAuth()
  const [rbacSummary, setRbacSummary] = useState<RbacSummary | null>(null)
  const [documentSummary, setDocumentSummary] = useState<DocumentComplianceSummary | null>(null)
  const [attendanceSummary, setAttendanceSummary] = useState<AttendanceDashboardSummary | null>(null)
  const [leaveSummary, setLeaveSummary] = useState<LeaveDashboardSummary | null>(null)
  const [payrollSummary, setPayrollSummary] = useState<PayrollDashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!isAdmin) {
      return
    }

    let cancelled = false

    const loadSummary = async () => {
      setIsLoading(true)

      try {
        const [rbacResponse, documentResponse, attendanceResponse, leaveResponse, payrollResponse] = await Promise.all([
          sixramApi.getRbacSummary(),
          sixramApi.getDocumentComplianceSummary(),
          sixramApi.getAttendanceSummary(),
          sixramApi.getLeaveSummary(),
          sixramApi.getPayrollSummary(),
        ])

        if (!cancelled) {
          setRbacSummary(rbacResponse)
          setDocumentSummary(documentResponse)
          setAttendanceSummary(attendanceResponse)
          setLeaveSummary(leaveResponse)
          setPayrollSummary(payrollResponse)
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

    void loadSummary()

    return () => {
      cancelled = true
    }
  }, [isAdmin])

  const accessCards = [
    {
      label: 'Users',
      value: isAdmin ? rbacSummary?.users.length ?? 0 : 1,
      description: isAdmin ? 'Provisioned accounts in the identity store.' : 'Current authenticated account.',
    },
    {
      label: 'Roles',
      value: isAdmin ? rbacSummary?.roles.length ?? 0 : user?.roles.length ?? 0,
      description: isAdmin ? 'Available authorization roles.' : 'Roles attached to your account.',
    },
    {
      label: 'Assignments',
      value: isAdmin ? rbacSummary?.assignments.length ?? 0 : user?.roles.length ?? 0,
      description: isAdmin ? 'Live role-to-user mappings.' : 'Assignments currently granted to you.',
    },
  ]

  const complianceCards = [
    { label: 'Total documents', value: documentSummary?.totalDocuments ?? 0, tone: 'default' as const },
    { label: 'Missing required', value: documentSummary?.missingRequiredDocuments ?? 0, tone: 'danger' as const },
    { label: 'Expired', value: documentSummary?.expiredDocuments ?? 0, tone: 'danger' as const },
    { label: 'Expiring soon', value: documentSummary?.expiringSoonDocuments ?? 0, tone: 'warning' as const },
    { label: 'Incomplete employees', value: documentSummary?.employeesWithIncompleteDocuments ?? 0, tone: 'warning' as const },
  ]

  return (
    <div className="space-y-6">
      <PageSection
        description="Sixram HRIS is running with API-issued JWT access tokens, a rotating refresh token cookie, employee master records, organization setup tables, document-compliance tracking, and attendance timekeeping for the HR baseline."
        kicker="Secure session"
        title={`Welcome back, ${user?.displayName}.`}
      >
        <div className="flex flex-wrap gap-2">
          {user?.roles.map((role) => (
            <span className="shell-badge-muted" key={role}>
              {role}
            </span>
          ))}
        </div>

        <div className="mt-6 shell-summary-grid">
          {accessCards.map((card) => (
            <div className="shell-summary-card" key={card.label}>
              <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{card.label}</p>
              <p className="mt-3 text-3xl font-semibold text-slate-950">{card.value}</p>
              <p className="mt-2 text-sm text-slate-500">{card.description}</p>
            </div>
          ))}
        </div>
      </PageSection>

      <section className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Compliance</p>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Document snapshot</h3>
              <p className="mt-2 text-sm text-slate-500">
                Keep an eye on required-document gaps, expired records, and employees who need follow-up.
              </p>
            </div>
            {isAdmin ? <span className="shell-badge-brand">Admin</span> : <span className="shell-badge-muted">Member</span>}
          </div>

          {!isAdmin ? (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
              Compliance analytics are available to administrators only.
            </div>
          ) : (
            <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {complianceCards.map((card) => (
                <SummaryCard key={card.label} label={card.label} tone={card.tone} value={String(card.value)} />
              ))}
            </div>
          )}
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Actions</p>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Administrator shortcuts</h3>
              <p className="mt-2 text-sm text-slate-500">
                Jump into employee, organization, document, and security administration from one place.
              </p>
            </div>
            {isAdmin ? <span className="shell-badge-brand">Admin</span> : <span className="shell-badge-muted">Member</span>}
          </div>

          {!isAdmin ? (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
              Administrator-only sections are unavailable for this account.
            </div>
          ) : (
            <div className="mt-6 space-y-3">
              <ShortcutLink
                description="Search, review, and maintain employee master records."
                to="/admin/employees"
                title="Employee Master Profiles"
              />
              <ShortcutLink
                description="Track uploaded files, expiry status, and compliance issues."
                to="/admin/documents"
                title="Employee Documents"
              />
              <ShortcutLink
                description="Review leave requests, adjust balances, and keep approvals moving."
                to="/admin/leave"
                title="Leave Management"
              />
              <ShortcutLink
                description="Generate payroll runs, review payroll totals, and process payroll adjustments."
                to="/admin/payroll"
                title="Payroll Dashboard"
              />
              <ShortcutLink
                description="Maintain document categories and required-document rules."
                to="/admin/document-types"
                title="Document Types"
              />
              <ShortcutLink
                description="Manage departments, positions, branches, and employment setup tables."
                to="/admin/organization"
                title="Organization Setup"
              />
              <ShortcutLink
                description="Create, disable, edit, and reset user credentials."
                to="/admin/users"
                title="Manage User Accounts"
              />
            </div>
          )}
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Attendance</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Today&apos;s timekeeping snapshot</h3>
            <p className="mt-2 text-sm text-slate-500">
              Operational counts for {attendanceSummary ? formatDate(attendanceSummary.attendanceDate) : 'today'}, plus
              a quick jump into attendance records and schedule setup.
            </p>
          </div>
          {isAdmin ? <span className="shell-badge-brand">Admin</span> : <span className="shell-badge-muted">Member</span>}
        </div>

        {!isAdmin ? (
          <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
            Attendance operations are available to administrators only.
          </div>
        ) : (
          <div className="mt-6 grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <SummaryCard label="Present" tone="default" value={String(attendanceSummary?.presentCount ?? 0)} />
              <SummaryCard label="Late" tone="warning" value={String(attendanceSummary?.lateCount ?? 0)} />
              <SummaryCard label="Absent" tone="danger" value={String(attendanceSummary?.absentCount ?? 0)} />
              <SummaryCard label="Incomplete" tone="warning" value={String(attendanceSummary?.incompleteCount ?? 0)} />
              <SummaryCard label="No schedule" tone="default" value={String(attendanceSummary?.noScheduleCount ?? 0)} />
              <SummaryCard label="Undertime" tone="warning" value={String(attendanceSummary?.undertimeCount ?? 0)} />
              <SummaryCard label="Rest day" tone="default" value={String(attendanceSummary?.restDayCount ?? 0)} />
              <SummaryCard label="No assignment" tone="warning" value={String(attendanceSummary?.employeesWithoutScheduleAssignmentCount ?? 0)} />
            </div>

            <div className="space-y-3">
              <ShortcutLink
                description="Review daily logs, late minutes, undertime, incomplete records, and manual corrections."
                to="/admin/attendance"
                title="Attendance Records"
              />
              <ShortcutLink
                description="Maintain schedule policy templates for fixed, flexible, and shifting work arrangements."
                to="/admin/attendance/work-schedules"
                title="Work Schedules"
              />
              <ShortcutLink
                description="Maintain reusable shift windows and assign schedules to employees."
                to="/admin/attendance/assignments"
                title="Schedule Assignments"
              />
            </div>
          </div>
        )}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Payroll</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Payroll preparation overview</h3>
            <p className="mt-2 text-sm text-slate-500">
              Current payroll workload, missing compensation profiles, held payroll items, and the latest payroll totals.
            </p>
          </div>
          {isAdmin ? <span className="shell-badge-brand">Admin</span> : <span className="shell-badge-muted">Member</span>}
        </div>

        {!isAdmin ? (
          <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
            Payroll administration is available to administrators only.
          </div>
        ) : (
          <div className="mt-6 grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <SummaryCard label="Open period" tone="default" value={payrollSummary?.currentOpenPayPeriod?.code ?? 'None'} />
              <SummaryCard label="Draft runs" tone="warning" value={String(payrollSummary?.draftRunCount ?? 0)} />
              <SummaryCard label="For review" tone="warning" value={String(payrollSummary?.forReviewRunCount ?? 0)} />
              <SummaryCard label="Approved runs" tone="default" value={String(payrollSummary?.approvedRunCount ?? 0)} />
              <SummaryCard label="Missing compensation" tone="danger" value={String(payrollSummary?.employeesMissingCompensationProfileCount ?? 0)} />
              <SummaryCard label="Attendance issues" tone="danger" value={String(payrollSummary?.employeesWithAttendanceIssuesCount ?? 0)} />
              <SummaryCard label="Held items" tone="danger" value={String(payrollSummary?.payrollItemsOnHoldCount ?? 0)} />
              <SummaryCard label="Net pay" tone="default" value={formatCurrency(payrollSummary?.totalNetPay ?? 0)} />
            </div>

            <div className="space-y-3">
              <ShortcutLink
                description="Create pay periods, generate payroll runs, and process payroll actions."
                to="/admin/payroll"
                title="Payroll Dashboard"
              />
              <ShortcutLink
                description="Maintain compensation history plus recurring allowances and deductions."
                to="/admin/payroll/compensation"
                title="Compensation Management"
              />
              <ShortcutLink
                description="Maintain payroll defaults, contribution tables, and tax tables."
                to="/admin/payroll/setup"
                title="Payroll Setup"
              />
              <ShortcutLink
                description="Review payroll register data and grouped payroll totals."
                to="/admin/payroll/reports"
                title="Payroll Reports"
              />
            </div>
          </div>
        )}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Leave</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Leave overview</h3>
            <p className="mt-2 text-sm text-slate-500">
              Pending approvals, employees on leave today, low balances, and attendance conflicts that need HR review.
            </p>
          </div>
          {isAdmin ? <span className="shell-badge-brand">Admin</span> : <span className="shell-badge-muted">Member</span>}
        </div>

        {!isAdmin ? (
          <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
            Leave administration is available to administrators only.
          </div>
        ) : (
          <div className="mt-6 grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <SummaryCard label="Pending" tone="warning" value={String(leaveSummary?.pendingLeaveRequestCount ?? 0)} />
              <SummaryCard label="On leave today" tone="default" value={String(leaveSummary?.employeesOnLeaveTodayCount ?? 0)} />
              <SummaryCard label="Low balances" tone="warning" value={String(leaveSummary?.lowBalanceCount ?? 0)} />
              <SummaryCard label="Conflicts" tone="danger" value={String(leaveSummary?.attendanceConflictCount ?? 0)} />
              <SummaryCard label="Approved today" tone="default" value={String(leaveSummary?.approvedLeavesTodayCount ?? 0)} />
              <SummaryCard label="Upcoming" tone="default" value={String(leaveSummary?.upcomingApprovedLeaveCount ?? 0)} />
              <SummaryCard label="Negative balances" tone="danger" value={String(leaveSummary?.negativeBalanceCount ?? 0)} />
              <SummaryCard label="Business date" tone="default" value={leaveSummary?.businessDate ? formatDate(leaveSummary.businessDate) : '-'} />
            </div>

            <div className="space-y-3">
              <ShortcutLink
                description="Process pending leave requests, inspect conflicts, and adjust employee balances."
                to="/admin/leave"
                title="Leave Management"
              />
              <ShortcutLink
                description="Review the monthly leave calendar for approved and pending absences."
                to="/admin/leave/calendar"
                title="Leave Calendar"
              />
              <ShortcutLink
                description="Maintain leave categories, annual credits, and filing rules."
                to="/admin/leave/types"
                title="Leave Types"
              />
            </div>
          </div>
        )}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Session</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Access and token model</h3>
            <p className="mt-2 text-sm text-slate-500">
              The backend remains the source of truth for account status, roles, refresh eligibility, and admin-only
              API enforcement.
            </p>
          </div>
          <span className="shell-badge-success">Authenticated</span>
        </div>

        <dl className="mt-6 grid gap-4 md:grid-cols-2">
          <div className="rounded-2xl border border-slate-200 bg-white p-5">
            <dt className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">Email</dt>
            <dd className="mt-2 text-sm font-semibold text-slate-900">{user?.email}</dd>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-white p-5">
            <dt className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">Role set</dt>
            <dd className="mt-3 flex flex-wrap gap-2">
              {user?.roles.map((role) => (
                <span className="shell-badge-brand" key={role}>
                  {role}
                </span>
              ))}
            </dd>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-white p-5 md:col-span-2">
            <dt className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">Token model</dt>
            <dd className="mt-2 text-sm leading-6 text-slate-500">
              Access tokens are held in memory for the active session. Refresh tokens stay in an HTTP-only cookie,
              and the API rotates them during refresh while checking user state and role authorization.
            </dd>
          </div>
        </dl>
      </section>

      {isAdmin && error ? <ErrorState message={error} /> : null}

      {isAdmin && isLoading ? <LoadingState message="Loading administrator metrics..." /> : null}
    </div>
  )
}

function ShortcutLink({
  title,
  description,
  to,
}: {
  title: string
  description: string
  to: string
}) {
  return (
    <Link
      className="block rounded-2xl border border-slate-200 bg-white px-5 py-4 transition hover:border-[#465fff]/30 hover:bg-[#465fff]/5"
      to={to}
    >
      <p className="text-sm font-semibold text-slate-900">{title}</p>
      <p className="mt-1 text-sm text-slate-500">{description}</p>
    </Link>
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
  const toneClasses =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : 'border-slate-200 bg-slate-50'

  return (
    <div className={`rounded-2xl border p-5 ${toneClasses}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{label}</p>
      <p className="mt-3 text-3xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}
