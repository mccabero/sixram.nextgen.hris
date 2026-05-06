type LeaveStatusBadgeProps = {
  status: string
}

export function LeaveStatusBadge({ status }: LeaveStatusBadgeProps) {
  const normalizedStatus = status.trim().toLowerCase()

  const className =
    normalizedStatus === 'approved'
      ? 'shell-badge-success'
      : normalizedStatus === 'pending'
        ? 'shell-badge-brand'
        : normalizedStatus === 'rejected'
          ? 'shell-badge-danger'
          : normalizedStatus === 'cancelled'
            ? 'shell-badge-muted'
            : 'shell-badge-muted'

  return <span className={className}>{status || 'Unknown'}</span>
}
