export type WorkspaceAccess = {
  hasLinkedEmployee: boolean
  isAdmin: boolean
  isManager: boolean
  roles?: readonly string[]
}

export function hasRole(access: WorkspaceAccess, role: string) {
  return access.roles?.includes(role) ?? false
}

export function canAccessApprovalCenter(access: WorkspaceAccess) {
  return access.isAdmin || access.isManager || hasRole(access, 'HR') || hasRole(access, 'PayrollOfficer')
}

export function canAccessReports(access: WorkspaceAccess) {
  return access.isAdmin || access.isManager || hasRole(access, 'HR') || hasRole(access, 'PayrollOfficer')
}

export function canAccessCompliance(access: WorkspaceAccess) {
  return access.isAdmin || access.isManager || hasRole(access, 'HR')
}

export function canAccessAuditLogs(access: WorkspaceAccess) {
  return access.isAdmin || hasRole(access, 'HR') || hasRole(access, 'PayrollOfficer')
}

export function canAccessProvidentFund(access: WorkspaceAccess) {
  return access.isAdmin || hasRole(access, 'HR') || hasRole(access, 'PayrollOfficer')
}
