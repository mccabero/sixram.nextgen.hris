import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type { ReportDefinition } from '../types/models'
import { formatError } from '../utils/errors'

const categoryLabels: Record<string, string> = {
  employee: 'Employee Reports',
  organization: 'Organization Reports',
  document_compliance: 'Document & Compliance Reports',
  attendance: 'Attendance Reports',
  leave: 'Leave Reports',
  payroll: 'Payroll Reports',
  approval: 'Approval Reports',
  audit: 'Audit Reports',
}

export function ReportsCenterPage() {
  const [reports, setReports] = useState<ReportDefinition[]>([])
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    void loadReports()
  }, [])

  async function loadReports() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getReportsCenter()
      setReports(response.reports)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  const filteredReports = reports.filter((report) => {
    if (!search.trim()) {
      return true
    }

    const value = search.trim().toLowerCase()
    return (
      report.name.toLowerCase().includes(value) ||
      report.description.toLowerCase().includes(value) ||
      categoryLabels[report.category]?.toLowerCase().includes(value)
    )
  })

  const groupedReports = filteredReports.reduce<Record<string, ReportDefinition[]>>((groups, report) => {
    const key = report.category
    groups[key] = [...(groups[key] ?? []), report]
    return groups
  }, {})

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <span className="shell-badge-brand">Reports Center</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Centralized HR reporting</h2>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              Explore permission-aware workforce, attendance, leave, payroll, compliance, approval, and audit reports from one organized workspace.
            </p>
          </div>

          <div className="flex gap-3">
            <Link className="shell-button-secondary" to="/analytics">
              Open analytics
            </Link>
            <Link className="shell-button-secondary" to="/compliance">
              Compliance center
            </Link>
          </div>
        </div>

        <div className="mt-6 grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto]">
          <label className="block space-y-2">
            <span className="shell-label mb-0">Search reports</span>
            <input
              className="shell-input"
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by report name, category, or description..."
              value={search}
            />
          </label>

          <div className="flex items-end">
            <button className="shell-button-secondary" onClick={() => void loadReports()} type="button">
              Refresh
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      {isLoading ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading report registry...</div>
      ) : filteredReports.length === 0 ? (
        <section className="shell-card fade-up p-6 sm:p-7">
          <p className="text-sm text-slate-500">No reports matched the current search.</p>
        </section>
      ) : (
        Object.entries(groupedReports).map(([category, items]) => (
          <section className="shell-card fade-up p-6 sm:p-7" key={category}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Category</p>
                <h3 className="mt-2 text-xl font-semibold text-slate-950">{categoryLabels[category] ?? category}</h3>
                <p className="mt-2 text-sm text-slate-500">{items.length} report view{items.length === 1 ? '' : 's'} available.</p>
              </div>
            </div>

            <div className="mt-5 grid gap-4 xl:grid-cols-2">
              {items.map((report) => (
                <Link
                  className="rounded-3xl border border-slate-200 bg-white p-5 transition hover:-translate-y-0.5 hover:border-[#465fff]/30 hover:shadow-[0_20px_40px_-24px_rgba(70,95,255,0.35)]"
                  key={report.key}
                  to={report.routePath}
                >
                  <div className="flex flex-wrap items-center gap-2">
                    {report.supportsExport ? <span className="shell-badge-success">CSV export</span> : null}
                    {report.supportsSavedViews ? <span className="shell-badge-muted">Saved filters</span> : null}
                  </div>

                  <h4 className="mt-4 text-lg font-semibold text-slate-950">{report.name}</h4>
                  <p className="mt-2 text-sm leading-6 text-slate-500">{report.description}</p>

                  <div className="mt-4 flex flex-wrap gap-2">
                    {report.allowedRoles.map((role) => (
                      <span className="shell-badge-muted" key={`${report.key}-${role}`}>
                        {role}
                      </span>
                    ))}
                  </div>

                  <div className="mt-5 flex items-center justify-between text-sm">
                    <span className="text-slate-500">{report.filters.length} filter{report.filters.length === 1 ? '' : 's'}</span>
                    <span className="font-semibold text-[#3641f5]">Open report</span>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        ))
      )}
    </div>
  )
}
