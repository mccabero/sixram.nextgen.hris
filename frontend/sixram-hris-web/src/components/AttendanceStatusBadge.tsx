type AttendanceStatusBadgeProps = {
  status: string
}

export function AttendanceStatusBadge({ status }: AttendanceStatusBadgeProps) {
  const normalizedStatus = status.trim().toLowerCase()

  const className =
    normalizedStatus === 'present'
      ? 'shell-badge-success'
      : normalizedStatus === 'late' || normalizedStatus === 'undertime' || normalizedStatus === 'half day'
        ? 'shell-badge-brand'
        : normalizedStatus === 'rest day' || normalizedStatus === 'no schedule'
          ? 'shell-badge-muted'
          : normalizedStatus === 'incomplete'
            ? 'shell-badge-warning'
            : 'shell-badge-danger'

  return <span className={className}>{status}</span>
}
