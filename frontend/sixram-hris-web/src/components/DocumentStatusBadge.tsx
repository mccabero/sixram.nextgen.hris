import type { EmployeeDocumentStatusCode } from '../types/models'

const statusClasses: Record<EmployeeDocumentStatusCode, string> = {
  valid: 'shell-badge-success',
  'expiring-soon': 'inline-flex items-center justify-center rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700',
  expired: 'shell-badge-danger',
  'no-expiry': 'shell-badge-muted',
  archived: 'inline-flex items-center justify-center rounded-full bg-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-600',
}

export function DocumentStatusBadge({
  statusCode,
  label,
}: {
  statusCode: EmployeeDocumentStatusCode
  label: string
}) {
  return <span className={statusClasses[statusCode]}>{label}</span>
}
