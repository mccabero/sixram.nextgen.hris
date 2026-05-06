import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type { AnalyticsDashboard, AnalyticsSeriesPoint, ReportMetric } from '../types/models'
import { formatCurrency, formatNumber } from '../utils/money'
import { formatError } from '../utils/errors'

export function AnalyticsDashboardPage() {
  const [dashboard, setDashboard] = useState<AnalyticsDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    void loadDashboard()
  }, [])

  async function loadDashboard() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getAnalyticsDashboard()
      setDashboard(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <span className="shell-badge-brand">Analytics</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">HR and operations analytics</h2>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              Monitor headcount, attendance, leave, approvals, compliance pressure, and payroll movement from a single summary dashboard.
            </p>
          </div>

          <div className="flex gap-3">
            <Link className="shell-button-secondary" to="/reports">
              Reports center
            </Link>
            <button className="shell-button-secondary" onClick={() => void loadDashboard()} type="button">
              Refresh
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      {isLoading ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading analytics dashboard...</div>
      ) : (
        <>
          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {(dashboard?.metrics ?? []).map((metric) => (
              <MetricCard key={metric.key} metric={metric} />
            ))}
          </section>

          <section className="grid gap-6 xl:grid-cols-2">
            <SeriesCard items={dashboard?.headcountByDepartment ?? []} title="Headcount by department" />
            <SeriesCard items={dashboard?.headcountByBranch ?? []} title="Headcount by branch" />
          </section>

          <section className="grid gap-6 xl:grid-cols-3">
            <SeriesCard items={dashboard?.attendanceTrend ?? []} title="Attendance trend" />
            <SeriesCard items={dashboard?.leaveUsageTrend ?? []} title="Leave usage trend" />
            <SeriesCard items={dashboard?.approvalVolume ?? []} title="Approval workload" />
          </section>

          {(dashboard?.payrollCostTrend?.length ?? 0) > 0 ? (
            <section className="shell-card fade-up p-6 sm:p-7">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h3 className="text-xl font-semibold text-slate-950">Payroll cost trend</h3>
                  <p className="mt-2 text-sm text-slate-500">Monthly net-pay movement from payroll snapshots that are visible to your role.</p>
                </div>
                <Link className="shell-button-secondary px-3 py-2" to="/reports/payroll_register">
                  Open payroll register
                </Link>
              </div>

              <div className="mt-5 space-y-3">
                {dashboard?.payrollCostTrend.map((item) => (
                  <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3" key={item.label}>
                    <span className="text-sm font-medium text-slate-700">{item.label}</span>
                    <span className="text-sm font-semibold text-slate-950">{formatCurrency(item.value)}</span>
                  </div>
                ))}
              </div>
            </section>
          ) : null}
        </>
      )}
    </div>
  )
}

function MetricCard({ metric }: { metric: ReportMetric }) {
  const className = metric.tone === 'success'
    ? 'border-emerald-200 bg-emerald-50'
    : metric.tone === 'warning'
      ? 'border-amber-200 bg-amber-50'
      : metric.tone === 'danger'
        ? 'border-rose-200 bg-rose-50'
        : 'border-slate-200 bg-white'

  return (
    <div className={`shell-card p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{metric.label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{metric.value}</p>
    </div>
  )
}

function SeriesCard({
  title,
  items,
}: {
  title: string
  items: AnalyticsSeriesPoint[]
}) {
  const maxValue = Math.max(...items.map((item) => item.value), 0)

  return (
    <section className="shell-card fade-up p-6 sm:p-7">
      <h3 className="text-xl font-semibold text-slate-950">{title}</h3>
      <div className="mt-5 space-y-4">
        {items.length ? (
          items.map((item) => (
            <div key={item.label}>
              <div className="mb-2 flex items-center justify-between gap-3 text-sm">
                <span className="font-medium text-slate-700">{item.label}</span>
                <span className="font-semibold text-slate-900">{formatValue(item.value, title)}</span>
              </div>
              <div className="h-2.5 rounded-full bg-slate-100">
                <div
                  className="h-2.5 rounded-full bg-[#465fff]"
                  style={{ width: `${maxValue <= 0 ? 0 : Math.max(8, (item.value / maxValue) * 100)}%` }}
                />
              </div>
            </div>
          ))
        ) : (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No analytics data available for this view yet.</div>
        )}
      </div>
    </section>
  )
}

function formatValue(value: number, title: string): string {
  if (title.toLowerCase().includes('payroll')) {
    return formatCurrency(value)
  }

  return Number.isInteger(value) ? String(value) : formatNumber(value, 2)
}
