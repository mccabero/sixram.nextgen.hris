type PayrollStatusBadgeProps = {
  status: string
}

const statusClasses: Record<string, string> = {
  open: 'shell-badge-success',
  processing: 'shell-badge-warning',
  locked: 'shell-badge-muted',
  paid: 'shell-badge-brand',
  cancelled: 'shell-badge-danger',
  draft: 'shell-badge-muted',
  calculated: 'shell-badge-warning',
  for_review: 'shell-badge-warning',
  approved: 'shell-badge-success',
  held: 'shell-badge-danger',
  pending: 'shell-badge-warning',
  rejected: 'shell-badge-danger',
  applied: 'shell-badge-success',
  reviewed: 'shell-badge-brand',
}

export function PayrollStatusBadge({ status }: PayrollStatusBadgeProps) {
  const normalized = status.trim().toLowerCase()
  const label = normalized
    .split('_')
    .map((value) => value.charAt(0).toUpperCase() + value.slice(1))
    .join(' ')

  return <span className={statusClasses[normalized] ?? 'shell-badge-muted'}>{label}</span>
}
