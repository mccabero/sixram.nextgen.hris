import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { LoadingScreen } from '../components/LoadingScreen'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { isAuthenticated, isReady } = useAuth()
  const location = useLocation()

  if (!isReady) {
    return <LoadingScreen label="Restoring your session..." />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

export function RequireAdmin() {
  const { isAdmin, isReady } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your permissions..." />
  }

  if (!isAdmin) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireEmployeeLink() {
  const { hasLinkedEmployee, isReady } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Loading your employee profile..." />
  }

  if (!hasLinkedEmployee) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireManager() {
  const { isManager, isReady } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your team access..." />
  }

  if (!isManager) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireApprovalAccess() {
  const { isAdmin, isManager, isReady, user } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your approval access..." />
  }

  const canAccess =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)

  if (!canAccess) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireReportsAccess() {
  const { isAdmin, isManager, isReady, user } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your reporting access..." />
  }

  const canAccess =
    isAdmin ||
    isManager ||
    (user?.roles.includes('HR') ?? false) ||
    (user?.roles.includes('PayrollOfficer') ?? false)

  if (!canAccess) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireComplianceAccess() {
  const { isAdmin, isManager, isReady, user } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your compliance access..." />
  }

  const canAccess = isAdmin || isManager || (user?.roles.includes('HR') ?? false)

  if (!canAccess) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireAuditLogAccess() {
  const { isAdmin, isReady, user } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your audit access..." />
  }

  const canAccess = isAdmin || (user?.roles.includes('HR') ?? false) || (user?.roles.includes('PayrollOfficer') ?? false)

  if (!canAccess) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function RequireProvidentFundAccess() {
  const { isAdmin, isReady, user } = useAuth()

  if (!isReady) {
    return <LoadingScreen label="Checking your provident fund access..." />
  }

  const canAccess = isAdmin || (user?.roles.includes('HR') ?? false) || (user?.roles.includes('PayrollOfficer') ?? false)

  if (!canAccess) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
