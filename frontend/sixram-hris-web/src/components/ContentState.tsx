import type { ReactNode } from 'react'

type ContentStateProps = {
  title?: string
  description?: string
  message?: string
  action?: ReactNode
  compact?: boolean
  className?: string
}

export function LoadingState({ description, message, compact = false, className = '' }: Omit<ContentStateProps, 'action' | 'title'>) {
  return (
    <div
      className={[
        'shell-state-loading shell-card text-sm',
        compact ? 'px-0 py-0 shadow-none border-0 bg-transparent' : 'px-5 py-4',
        className,
      ].join(' ').trim()}
    >
      {message ?? description}
    </div>
  )
}

export function ErrorState({ title = 'Something needs attention', description, message, action, className = '' }: ContentStateProps) {
  return (
    <div className={['shell-state-error', className].join(' ').trim()} role="alert">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-sm font-semibold text-rose-800">{title}</p>
          <p className="mt-1 leading-6">{message ?? description}</p>
        </div>
        {action ? <div className="shrink-0">{action}</div> : null}
      </div>
    </div>
  )
}

export function EmptyState({ title = 'No data available', description, message, action, className = '' }: ContentStateProps) {
  return (
    <div className={['shell-state-empty', className].join(' ').trim()}>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-sm font-semibold text-slate-800">{title}</p>
          <p className="mt-1 leading-6">{message ?? description}</p>
        </div>
        {action ? <div className="shrink-0">{action}</div> : null}
      </div>
    </div>
  )
}
