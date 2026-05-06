type RequestStatusBadgeProps = {
  status: string
}

export function RequestStatusBadge({ status }: RequestStatusBadgeProps) {
  const normalizedStatus = status.trim().toLowerCase()

  const className =
    normalizedStatus === 'approved'
      ? 'shell-badge-success'
      : normalizedStatus === 'rejected'
        ? 'shell-badge-danger'
        : normalizedStatus === 'cancelled'
          ? 'shell-badge-muted'
          : normalizedStatus === 'paid'
            ? 'shell-badge-brand'
            : 'shell-badge-warning'

  const label = status
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (value) => value.toUpperCase())

  return <span className={className}>{label}</span>
}
