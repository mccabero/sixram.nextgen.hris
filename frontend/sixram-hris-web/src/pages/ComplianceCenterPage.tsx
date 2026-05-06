import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { PaginationControls } from '../components/PaginationControls'
import type { ComplianceIssue, ComplianceIssueQuery, ComplianceSummary, ReportOptions } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

const defaultQuery: ComplianceIssueQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  issueType: '',
  severity: '',
  sortBy: 'severity',
  descending: true,
  pageNumber: 1,
  pageSize: 12,
}

const issueTypeLabels: Record<string, string> = {
  missing_required_document: 'Missing required document',
  expired_document: 'Expired document',
  expiring_soon_document: 'Expiring soon document',
  missing_government_id: 'Missing government ID',
  missing_emergency_contact: 'Missing emergency contact',
  missing_schedule_assignment: 'Missing schedule assignment',
  missing_compensation_profile: 'Missing compensation profile',
  incomplete_attendance: 'Incomplete attendance',
  pending_profile_change: 'Pending profile change',
  pending_attendance_adjustment: 'Pending attendance adjustment',
  pending_leave_request: 'Pending leave request',
}

export function ComplianceCenterPage() {
  const [summary, setSummary] = useState<ComplianceSummary | null>(null)
  const [options, setOptions] = useState<ReportOptions | null>(null)
  const [issues, setIssues] = useState<{ items: ComplianceIssue[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number } | null>(null)
  const [filters, setFilters] = useState<ComplianceIssueQuery>(defaultQuery)
  const [query, setQuery] = useState<ComplianceIssueQuery>(defaultQuery)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingIssues, setIsLoadingIssues] = useState(false)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadIssues(query)
  }, [options, query])

  async function loadBootstrap() {
    setIsLoading(true)

    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getComplianceSummary(),
        sixramApi.getReportOptions(),
      ])

      setSummary(summaryResponse)
      setOptions(optionsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadIssues(nextQuery: ComplianceIssueQuery) {
    setIsLoadingIssues(true)

    try {
      const response = await sixramApi.getComplianceIssues(nextQuery)
      setIssues(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingIssues(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <span className="shell-badge-brand">Compliance Center</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Operational compliance monitoring</h2>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              Track missing requirements, expiring documents, incomplete master data, and attendance or payroll readiness gaps across the workforce.
            </p>
          </div>

          <div className="flex gap-3">
            <Link className="shell-button-secondary" to="/reports/document_compliance_issues">
              Compliance report
            </Link>
            <button className="shell-button-secondary" onClick={() => void loadBootstrap()} type="button">
              Refresh
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      {isLoading ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading compliance center...</div>
      ) : (
        <>
          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <SummaryCard label="Open issues" value={String(summary?.openIssueCount ?? 0)} tone="default" />
            <SummaryCard label="Critical" value={String(summary?.criticalIssueCount ?? 0)} tone="danger" />
            <SummaryCard label="High" value={String(summary?.highIssueCount ?? 0)} tone="warning" />
            <SummaryCard label="Expired docs" value={String(summary?.expiredDocumentCount ?? 0)} tone="danger" />
            <SummaryCard label="Missing comp" value={String(summary?.missingCompensationProfileCount ?? 0)} tone="warning" />
          </section>

          <section className="shell-card fade-up p-6 sm:p-7">
            <div className="grid gap-4 xl:grid-cols-[1.4fr_repeat(5,minmax(0,1fr))]">
              <label className="block space-y-2">
                <span className="shell-label mb-0">Search</span>
                <input
                  className="shell-input"
                  onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))}
                  placeholder="Employee, issue, or description..."
                  value={filters.search ?? ''}
                />
              </label>

              <LookupSelect
                label="Employee"
                onChange={(value) => setFilters((current) => ({ ...current, employeeId: value }))}
                options={options?.employees.map((item) => ({ id: item.id, label: `${item.employeeCode} | ${item.fullName}` })) ?? []}
                value={filters.employeeId ?? ''}
              />
              <LookupSelect
                label="Department"
                onChange={(value) => setFilters((current) => ({ ...current, departmentId: value }))}
                options={options?.departments.map((item) => ({ id: item.id, label: item.name })) ?? []}
                value={filters.departmentId ?? ''}
              />
              <LookupSelect
                label="Branch"
                onChange={(value) => setFilters((current) => ({ ...current, branchId: value }))}
                options={options?.branches.map((item) => ({ id: item.id, label: item.name })) ?? []}
                value={filters.branchId ?? ''}
              />

              <label className="block space-y-2">
                <span className="shell-label mb-0">Issue type</span>
                <select
                  className="shell-select"
                  onChange={(event) => setFilters((current) => ({ ...current, issueType: event.target.value }))}
                  value={filters.issueType ?? ''}
                >
                  <option value="">All issues</option>
                  {Object.entries(issueTypeLabels).map(([key, label]) => (
                    <option key={key} value={key}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block space-y-2">
                <span className="shell-label mb-0">Severity</span>
                <select
                  className="shell-select"
                  onChange={(event) => setFilters((current) => ({ ...current, severity: event.target.value }))}
                  value={filters.severity ?? ''}
                >
                  <option value="">All levels</option>
                  <option value="critical">Critical</option>
                  <option value="high">High</option>
                  <option value="medium">Medium</option>
                  <option value="low">Low</option>
                </select>
              </label>
            </div>

            <div className="mt-4 flex flex-wrap gap-3">
              <button className="shell-button" onClick={() => setQuery({ ...filters, pageNumber: 1 })} type="button">
                Apply filters
              </button>
              <button
                className="shell-button-secondary"
                onClick={() => {
                  setFilters(defaultQuery)
                  setQuery(defaultQuery)
                }}
                type="button"
              >
                Reset
              </button>
            </div>

            <div className="shell-table-wrap mt-6">
              <table className="shell-table">
                <thead>
                  <tr>
                    <th>Severity</th>
                    <th>Employee</th>
                    <th>Issue</th>
                    <th>Scope</th>
                    <th>Detected</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {isLoadingIssues ? (
                    <tr>
                      <td className="text-slate-500" colSpan={6}>
                        Loading compliance issues...
                      </td>
                    </tr>
                  ) : !issues || issues.items.length === 0 ? (
                    <tr>
                      <td className="text-slate-500" colSpan={6}>
                        No compliance issues matched the current filters.
                      </td>
                    </tr>
                  ) : (
                    issues.items.map((issue) => (
                      <tr key={issue.id}>
                        <td>
                          <SeverityBadge severity={issue.severity} />
                        </td>
                        <td>
                          <div className="font-semibold text-slate-900">{issue.employeeFullName}</div>
                          <div className="mt-1 text-slate-500">{issue.employeeCode}</div>
                        </td>
                        <td>
                          <div className="font-semibold text-slate-900">{issue.title}</div>
                          <div className="mt-1 text-slate-500">{issue.description}</div>
                        </td>
                        <td className="text-slate-600">
                          <div>{issue.departmentName || 'Unassigned'}</div>
                          <div className="mt-1 text-xs text-slate-500">{issue.branchName || 'No branch'}</div>
                        </td>
                        <td className="text-slate-500">{formatDateTime(issue.detectedAtUtc)}</td>
                        <td className="text-right">
                          {issue.linkPath ? (
                            <Link className="shell-button-secondary px-3 py-2" to={issue.linkPath}>
                              Open
                            </Link>
                          ) : (
                            <span className="text-xs text-slate-400">No link</span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {issues ? (
              <PaginationControls
                pageNumber={issues.pageNumber}
                pageSize={issues.pageSize}
                totalCount={issues.totalCount}
                totalPages={issues.totalPages}
                onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
              />
            ) : null}
          </section>
        </>
      )}
    </div>
  )
}

function SummaryCard({
  label,
  value,
  tone,
}: {
  label: string
  value: string
  tone: 'default' | 'warning' | 'danger'
}) {
  const className = tone === 'danger'
    ? 'border-rose-200 bg-rose-50'
    : tone === 'warning'
      ? 'border-amber-200 bg-amber-50'
      : 'border-slate-200 bg-white'

  return (
    <div className={`shell-card p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function LookupSelect({
  label,
  onChange,
  options,
  value,
}: {
  label: string
  onChange: (value: string) => void
  options: Array<{ id: string; label: string }>
  value: string
}) {
  return (
    <label className="block space-y-2">
      <span className="shell-label mb-0">{label}</span>
      <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">All</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  )
}

function SeverityBadge({ severity }: { severity: string }) {
  const className = severity === 'critical'
    ? 'shell-badge-danger'
    : severity === 'high'
      ? 'shell-badge-warning'
      : severity === 'medium'
        ? 'shell-badge-brand'
        : 'shell-badge-muted'

  return <span className={className}>{severity.replace(/_/g, ' ')}</span>
}
