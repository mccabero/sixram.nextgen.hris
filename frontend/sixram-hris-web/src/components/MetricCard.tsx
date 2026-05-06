type MetricCardProps = {
  label: string
  value: string
  detail?: string
  tone?: 'default' | 'brand' | 'success' | 'warning' | 'danger'
  className?: string
}

export function MetricCard({ label, value, detail, tone = 'default', className = '' }: MetricCardProps) {
  const toneClass =
    tone === 'brand'
      ? 'border-[#465fff]/15 bg-[#465fff]/5'
      : tone === 'success'
        ? 'border-emerald-200 bg-emerald-50/80'
        : tone === 'warning'
          ? 'border-amber-200 bg-amber-50/80'
          : tone === 'danger'
            ? 'border-rose-200 bg-rose-50/80'
            : 'border-slate-200 bg-white/90'

  return (
    <div className={['shell-summary-card', toneClass, className].join(' ').trim()}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950 sm:text-[28px]">{value}</p>
      {detail ? <p className="mt-2 text-sm leading-6 text-slate-500">{detail}</p> : null}
    </div>
  )
}
