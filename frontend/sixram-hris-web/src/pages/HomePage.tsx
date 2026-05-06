import { useAuth } from '../auth/AuthContext'
import { Link } from 'react-router-dom'
import { DashboardPage } from './DashboardPage'
import { MyDashboardPage } from './MyDashboardPage'

export function HomePage() {
  const { isAdmin, hasLinkedEmployee, isManager, user } = useAuth()
  const canAccessApprovals =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)
  const canAccessReports =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)
  const canAccessCompliance = isAdmin || isManager || (user?.roles.includes('HR') ?? false)
  const canAccessAuditLogs = isAdmin || (user?.roles.includes('HR') ?? false) || (user?.roles.includes('PayrollOfficer') ?? false)

  if (isAdmin) {
    return <DashboardPage />
  }

  if (hasLinkedEmployee) {
    return <MyDashboardPage />
  }

  return (
    <section className="shell-card fade-up p-6 sm:p-7">
      <span className="shell-badge-warning">Employee link required</span>
      <h2 className="mt-4 text-2xl font-semibold text-slate-950">Your account is signed in, but self-service is not ready yet.</h2>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
        {user?.displayName || 'This user'} does not have a linked employee record. HR or an administrator needs to connect this account to an employee profile before the employee portal can be used.
      </p>
      {canAccessApprovals ? (
        <div className="mt-5 flex flex-wrap gap-3">
          <Link className="shell-button-secondary" to="/approvals">
            Open approval center
          </Link>
          <Link className="shell-button-secondary" to="/notifications">
            View notifications
          </Link>
          {canAccessReports ? (
            <Link className="shell-button-secondary" to="/analytics">
              Open analytics
            </Link>
          ) : null}
          {canAccessReports ? (
            <Link className="shell-button-secondary" to="/reports">
              Open reports
            </Link>
          ) : null}
          {canAccessCompliance ? (
            <Link className="shell-button-secondary" to="/compliance">
              Compliance center
            </Link>
          ) : null}
          {canAccessAuditLogs ? (
            <Link className="shell-button-secondary" to="/audit-logs">
              Audit trail
            </Link>
          ) : null}
        </div>
      ) : null}
    </section>
  )
}
