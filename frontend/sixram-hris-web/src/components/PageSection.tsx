import type { ReactNode } from 'react'

type PageSectionProps = {
  kicker?: string
  title: string
  description?: string
  actions?: ReactNode
  children?: ReactNode
  className?: string
}

export function PageSection({ kicker, title, description, actions, children, className = '' }: PageSectionProps) {
  return (
    <section className={['shell-card fade-up p-6 sm:p-7', className].join(' ').trim()}>
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          {kicker ? <p className="shell-kicker">{kicker}</p> : null}
          <h2 className="mt-2 text-2xl font-semibold text-slate-950 sm:text-[30px]">{title}</h2>
          {description ? <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">{description}</p> : null}
        </div>
        {actions ? <div className="shell-form-actions sm:justify-start lg:justify-end">{actions}</div> : null}
      </div>
      {children ? <div className="mt-6">{children}</div> : null}
    </section>
  )
}
