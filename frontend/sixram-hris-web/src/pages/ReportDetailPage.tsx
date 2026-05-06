/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState, type Dispatch, type SetStateAction } from 'react'
import { Link, useParams } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { EmptyState, ErrorState, LoadingState } from '../components/ContentState'
import { PaginationControls } from '../components/PaginationControls'
import { PageSection } from '../components/PageSection'
import type { ReportDefinition, ReportOptions, ReportQuery, ReportResult, SavedReport } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { downloadBlob } from '../utils/files'

const defaultQuery: ReportQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  employmentTypeId: '',
  employmentStatusId: '',
  leaveTypeId: '',
  documentTypeId: '',
  payPeriodId: '',
  payrollRunId: '',
  status: '',
  source: '',
  issueType: '',
  severity: '',
  entityType: '',
  action: '',
  dateFrom: '',
  dateTo: '',
  year: undefined,
  month: undefined,
  includeInactive: false,
  sortBy: '',
  descending: false,
  pageNumber: 1,
  pageSize: 20,
}

const issueTypeOptions = [
  { value: 'missing_required_document', label: 'Missing required document' },
  { value: 'expired_document', label: 'Expired document' },
  { value: 'expiring_soon_document', label: 'Expiring soon document' },
  { value: 'missing_government_id', label: 'Missing government ID' },
  { value: 'missing_emergency_contact', label: 'Missing emergency contact' },
  { value: 'missing_schedule_assignment', label: 'Missing schedule assignment' },
  { value: 'missing_compensation_profile', label: 'Missing compensation profile' },
  { value: 'incomplete_attendance', label: 'Incomplete attendance' },
  { value: 'pending_profile_change', label: 'Pending profile change' },
  { value: 'pending_attendance_adjustment', label: 'Pending attendance adjustment' },
  { value: 'pending_leave_request', label: 'Pending leave request' },
]

const statusOptions = [
  'active',
  'inactive',
  'pending',
  'approved',
  'rejected',
  'cancelled',
  'paid',
  'draft',
  'for_review',
  'held',
]

const sourceOptions = ['manual', 'web_clock', 'import', 'system', 'leave']
const severityOptions = ['critical', 'high', 'medium', 'low']
const entityTypeOptions = [
  'employee',
  'department',
  'position',
  'branch',
  'employment_type',
  'employment_status',
  'document_type',
  'employee_document',
  'attendance_record',
  'attendance_adjustment_request',
  'leave_request',
  'leave_balance',
  'profile_change_request',
  'compensation_profile',
  'recurring_earning',
  'recurring_deduction',
  'payroll_run',
  'payroll_adjustment',
  'user',
  'role_assignment',
  'payslip',
]

const actionOptions = ['create', 'update', 'delete', 'deactivate', 'approve', 'reject', 'cancel', 'mark_paid', 'download', 'archive']

export function ReportDetailPage() {
  const { reportKey = '' } = useParams()
  const [registry, setRegistry] = useState<ReportDefinition[]>([])
  const [options, setOptions] = useState<ReportOptions | null>(null)
  const [savedReports, setSavedReports] = useState<SavedReport[]>([])
  const [filters, setFilters] = useState<ReportQuery>(defaultQuery)
  const [query, setQuery] = useState<ReportQuery>(defaultQuery)
  const [report, setReport] = useState<ReportResult | null>(null)
  const [saveViewName, setSaveViewName] = useState('')
  const [saveAsDefault, setSaveAsDefault] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isLoadingBootstrap, setIsLoadingBootstrap] = useState(true)
  const [isLoadingReport, setIsLoadingReport] = useState(false)
  const [isSavingView, setIsSavingView] = useState(false)
  const [initializedReportKey, setInitializedReportKey] = useState('')

  const definition = useMemo(
    () => registry.find((item) => item.key === reportKey) ?? null,
    [registry, reportKey],
  )

  const reportSavedViews = savedReports.filter((item) => item.reportKey === reportKey)

  async function loadBootstrap() {
    setIsLoadingBootstrap(true)

    try {
      const [registryResponse, optionsResponse, savedViewsResponse] = await Promise.all([
        sixramApi.getReportsCenter(),
        sixramApi.getReportOptions(),
        sixramApi.getSavedReports(),
      ])

      setRegistry(registryResponse.reports)
      setOptions(optionsResponse)
      setSavedReports(savedViewsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingBootstrap(false)
    }
  }

  async function loadReport(nextQuery: ReportQuery) {
    if (!reportKey) {
      return
    }

    setIsLoadingReport(true)

    try {
      const response = await sixramApi.runReport(reportKey, nextQuery)
      setReport(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingReport(false)
    }
  }

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!definition || !options || initializedReportKey === reportKey) {
      return
    }

    const defaultSavedView = reportSavedViews.find((item) => item.isDefault)
    const nextQuery = defaultSavedView ? parseSavedQuery(defaultSavedView.filtersJson) : { ...defaultQuery }

    setFilters(nextQuery)
    setQuery(nextQuery)
    setInitializedReportKey(reportKey)
  }, [definition, initializedReportKey, options, reportKey, reportSavedViews])

  useEffect(() => {
    if (!definition) {
      return
    }

    void loadReport(query)
  }, [definition, query])

  async function handleExport() {
    if (!reportKey) {
      return
    }

    try {
      const file = await sixramApi.exportReportCsv(reportKey, query)
      downloadBlob(file.blob, file.fileName)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handleSaveView() {
    if (!reportKey || !saveViewName.trim()) {
      return
    }

    setIsSavingView(true)

    try {
      await sixramApi.createSavedReport({
        reportKey,
        name: saveViewName.trim(),
        filtersJson: JSON.stringify(query),
        isDefault: saveAsDefault,
      })

      const response = await sixramApi.getSavedReports()
      setSavedReports(response)
      setSaveViewName('')
      setSaveAsDefault(false)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSavingView(false)
    }
  }

  async function handleSetDefaultView(view: SavedReport) {
    try {
      await sixramApi.updateSavedReport(view.id, {
        reportKey: view.reportKey,
        name: view.name,
        filtersJson: view.filtersJson,
        isDefault: true,
      })

      setSavedReports(await sixramApi.getSavedReports())
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handleDeleteView(viewId: string) {
    try {
      await sixramApi.deleteSavedReport(viewId)
      setSavedReports(await sixramApi.getSavedReports())
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  function applySavedView(view: SavedReport) {
    const nextQuery = parseSavedQuery(view.filtersJson)
    setFilters(nextQuery)
    setQuery(nextQuery)
  }

  if (isLoadingBootstrap) {
    return <LoadingState message="Loading report workspace..." />
  }

  if (!definition) {
    return <EmptyState message="This report is not available for your account." title="Report unavailable" />
  }

  return (
    <div className="space-y-6">
      <PageSection
        actions={(
          <>
            <Link className="shell-button-secondary" to="/reports">
              Back to center
            </Link>
            {definition.supportsExport ? (
              <button className="shell-button-secondary" onClick={() => void handleExport()} type="button">
                Export CSV
              </button>
            ) : null}
          </>
        )}
        description={definition.description}
        kicker="Report"
        title={definition.name}
      >
        <div className="flex flex-wrap gap-2">
          <span className="shell-badge-muted">{definition.category.replace(/_/g, ' ')}</span>
          {report ? <span className="shell-badge-brand">Generated {formatDateTime(report.generatedAtUtc)}</span> : null}
        </div>
      </PageSection>

      {error ? <ErrorState message={error} /> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="shell-toolbar-grid">{definition.filters.map((filterKey) => renderFilter(filterKey, filters, setFilters, options))}</div>

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
      </section>

      {definition.supportsSavedViews ? (
        <section className="shell-card fade-up p-6 sm:p-7">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
            <div>
              <h3 className="text-xl font-semibold text-slate-950">Saved views</h3>
              <p className="mt-2 text-sm text-slate-500">Keep reusable filter sets for recurring report reviews.</p>
            </div>

            <div className="grid gap-3 sm:grid-cols-[minmax(260px,1fr)_auto_auto]">
              <input
                className="shell-input"
                onChange={(event) => setSaveViewName(event.target.value)}
                placeholder="View name"
                value={saveViewName}
              />
              <label className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
                <input checked={saveAsDefault} onChange={(event) => setSaveAsDefault(event.target.checked)} type="checkbox" />
                <span>Set as default</span>
              </label>
              <button className="shell-button" disabled={isSavingView || !saveViewName.trim()} onClick={() => void handleSaveView()} type="button">
                {isSavingView ? 'Saving...' : 'Save view'}
              </button>
            </div>
          </div>

          <div className="mt-5 grid gap-3 lg:grid-cols-2">
            {reportSavedViews.length ? (
              reportSavedViews.map((view) => (
                <div className="rounded-2xl border border-slate-200 bg-white p-4" key={view.id}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="flex flex-wrap gap-2">
                        <span className="text-sm font-semibold text-slate-900">{view.name}</span>
                        {view.isDefault ? <span className="shell-badge-brand">Default</span> : null}
                      </div>
                      <p className="mt-2 text-xs uppercase tracking-[0.16em] text-slate-400">Updated {formatDateTime(view.updatedAtUtc ?? view.createdAtUtc)}</p>
                    </div>
                    <div className="flex gap-2">
                      <button className="shell-button-secondary px-3 py-2" onClick={() => applySavedView(view)} type="button">
                        Apply
                      </button>
                      {!view.isDefault ? (
                        <button className="shell-button-secondary px-3 py-2" onClick={() => void handleSetDefaultView(view)} type="button">
                          Default
                        </button>
                      ) : null}
                      <button className="shell-button-secondary px-3 py-2" onClick={() => void handleDeleteView(view.id)} type="button">
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <EmptyState message="No saved views for this report yet." title="No saved views" />
            )}
          </div>
        </section>
      ) : null}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {(report?.metrics ?? []).map((metric) => (
          <MetricCard key={metric.key} label={metric.label} tone={metric.tone} value={metric.value} />
        ))}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h3 className="text-xl font-semibold text-slate-950">{report?.title ?? definition.name}</h3>
            <p className="mt-2 text-sm text-slate-500">{report?.description ?? definition.description}</p>
          </div>
          <span className="shell-badge-muted">{report?.totalCount ?? 0} rows</span>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                {(report?.columns ?? []).map((column) => (
                  <th className={column.alignment === 'right' ? 'text-right' : ''} key={column.key}>
                    {column.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {isLoadingReport ? (
                <tr>
                  <td colSpan={Math.max(report?.columns.length ?? 1, 1)}>
                    <LoadingState compact message="Loading report data..." />
                  </td>
                </tr>
              ) : report && report.rows.length > 0 ? (
                report.rows.map((row) => (
                  <tr key={row.id}>
                    {report.columns.map((column, index) => (
                      <td className={column.alignment === 'right' ? 'text-right' : ''} key={`${row.id}-${column.key}`}>
                        {index === 0 && row.linkPath ? (
                          <Link className="font-semibold text-[#3641f5] hover:text-[#2430cf]" to={row.linkPath}>
                            {row.values[column.key] ?? ''}
                          </Link>
                        ) : (
                          <span className={index === 0 ? 'font-semibold text-slate-900' : 'text-slate-600'}>{row.values[column.key] ?? ''}</span>
                        )}
                      </td>
                    ))}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={Math.max(report?.columns.length ?? 1, 1)}>
                    <EmptyState message="No results matched the selected filters." title="No report results" />
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {report ? (
          <PaginationControls
            pageNumber={report.pageNumber}
            pageSize={report.pageSize}
            totalCount={report.totalCount}
            totalPages={report.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>
    </div>
  )
}

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string
  value: string
  tone: string
}) {
  const className = tone === 'success'
    ? 'border-emerald-200 bg-emerald-50'
    : tone === 'warning'
      ? 'border-amber-200 bg-amber-50'
      : tone === 'danger'
        ? 'border-rose-200 bg-rose-50'
        : 'border-slate-200 bg-white'

  return (
    <div className={`shell-card p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function renderFilter(
  filterKey: string,
  filters: ReportQuery,
  setFilters: Dispatch<SetStateAction<ReportQuery>>,
  options: ReportOptions | null,
) {
  switch (filterKey) {
    case 'search':
      return (
        <label className="block space-y-2" key={filterKey}>
          <span className="shell-label mb-0">Search</span>
          <input
            className="shell-input"
            onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))}
            placeholder="Search..."
            value={filters.search ?? ''}
          />
        </label>
      )
    case 'employeeId':
      return createLookupFilter(filterKey, 'Employee', filters.employeeId ?? '', options?.employees.map((item) => ({
        id: item.id,
        label: `${item.employeeCode} | ${item.fullName}`,
      })) ?? [], (value) => setFilters((current) => ({ ...current, employeeId: value })))
    case 'departmentId':
      return createLookupFilter(filterKey, 'Department', filters.departmentId ?? '', options?.departments.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, departmentId: value })))
    case 'branchId':
      return createLookupFilter(filterKey, 'Branch', filters.branchId ?? '', options?.branches.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, branchId: value })))
    case 'employmentTypeId':
      return createLookupFilter(filterKey, 'Employment Type', filters.employmentTypeId ?? '', options?.employmentTypes.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, employmentTypeId: value })))
    case 'employmentStatusId':
      return createLookupFilter(filterKey, 'Employment Status', filters.employmentStatusId ?? '', options?.employmentStatuses.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, employmentStatusId: value })))
    case 'leaveTypeId':
      return createLookupFilter(filterKey, 'Leave Type', filters.leaveTypeId ?? '', options?.leaveTypes.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, leaveTypeId: value })))
    case 'documentTypeId':
      return createLookupFilter(filterKey, 'Document Type', filters.documentTypeId ?? '', options?.documentTypes.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, documentTypeId: value })))
    case 'payPeriodId':
      return createLookupFilter(filterKey, 'Pay Period', filters.payPeriodId ?? '', options?.payPeriods.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, payPeriodId: value })))
    case 'payrollRunId':
      return createLookupFilter(filterKey, 'Payroll Run', filters.payrollRunId ?? '', options?.payrollRuns.map((item) => ({
        id: item.id,
        label: item.name,
      })) ?? [], (value) => setFilters((current) => ({ ...current, payrollRunId: value })))
    case 'status':
      return createOptionFilter(filterKey, 'Status', filters.status ?? '', statusOptions, (value) => setFilters((current) => ({ ...current, status: value })))
    case 'source':
      return createOptionFilter(filterKey, 'Source', filters.source ?? '', sourceOptions, (value) => setFilters((current) => ({ ...current, source: value })))
    case 'issueType':
      return (
        <label className="block space-y-2" key={filterKey}>
          <span className="shell-label mb-0">Issue Type</span>
          <select
            className="shell-select"
            onChange={(event) => setFilters((current) => ({ ...current, issueType: event.target.value }))}
            value={filters.issueType ?? ''}
          >
            <option value="">All</option>
            {issueTypeOptions.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </label>
      )
    case 'severity':
      return createOptionFilter(filterKey, 'Severity', filters.severity ?? '', severityOptions, (value) => setFilters((current) => ({ ...current, severity: value })))
    case 'entityType':
      return createOptionFilter(filterKey, 'Entity Type', filters.entityType ?? '', entityTypeOptions, (value) => setFilters((current) => ({ ...current, entityType: value })))
    case 'action':
      return createOptionFilter(filterKey, 'Action', filters.action ?? '', actionOptions, (value) => setFilters((current) => ({ ...current, action: value })))
    case 'dateFrom':
      return createDateFilter(filterKey, 'Date From', filters.dateFrom ?? '', (value) => setFilters((current) => ({ ...current, dateFrom: value })))
    case 'dateTo':
      return createDateFilter(filterKey, 'Date To', filters.dateTo ?? '', (value) => setFilters((current) => ({ ...current, dateTo: value })))
    case 'year':
      return (
        <label className="block space-y-2" key={filterKey}>
          <span className="shell-label mb-0">Year</span>
          <input
            className="shell-input"
            min={2000}
            onChange={(event) => setFilters((current) => ({ ...current, year: event.target.value ? Number(event.target.value) : undefined }))}
            placeholder="Year"
            type="number"
            value={filters.year ?? ''}
          />
        </label>
      )
    case 'includeInactive':
      return (
        <label className="block space-y-2" key={filterKey}>
          <span className="shell-label mb-0">Lifecycle</span>
          <select
            className="shell-select"
            onChange={(event) =>
              setFilters((current) => ({
                ...current,
                includeInactive: event.target.value === '' ? false : event.target.value === 'true',
              }))
            }
            value={filters.includeInactive ? 'true' : ''}
          >
            <option value="">Active only</option>
            <option value="true">Include inactive</option>
          </select>
        </label>
      )
    default:
      return null
  }
}

function createLookupFilter(
  key: string,
  label: string,
  value: string,
  options: Array<{ id: string; label: string }>,
  onChange: (value: string) => void,
) {
  return (
    <label className="block space-y-2" key={key}>
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

function createOptionFilter(
  key: string,
  label: string,
  value: string,
  options: string[],
  onChange: (value: string) => void,
) {
  return (
    <label className="block space-y-2" key={key}>
      <span className="shell-label mb-0">{label}</span>
      <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">All</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option.replace(/_/g, ' ')}
          </option>
        ))}
      </select>
    </label>
  )
}

function createDateFilter(
  key: string,
  label: string,
  value: string,
  onChange: (value: string) => void,
) {
  return (
    <label className="block space-y-2" key={key}>
      <span className="shell-label mb-0">{label}</span>
      <input className="shell-input" onChange={(event) => onChange(event.target.value)} type="date" value={value} />
    </label>
  )
}

function parseSavedQuery(filtersJson: string): ReportQuery {
  try {
    const parsed = JSON.parse(filtersJson) as ReportQuery
    return {
      ...defaultQuery,
      ...parsed,
      pageNumber: parsed.pageNumber ?? 1,
      pageSize: parsed.pageSize ?? 20,
    }
  } catch {
    return { ...defaultQuery }
  }
}
