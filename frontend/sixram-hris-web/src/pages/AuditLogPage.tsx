import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type { AuditLog, AuditLogQuery, ReportOptions } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

const defaultQuery: AuditLogQuery = {
  search: '',
  entityType: '',
  action: '',
  employeeId: '',
  dateFrom: '',
  dateTo: '',
  sortBy: 'created',
  descending: true,
  pageNumber: 1,
  pageSize: 15,
}

export function AuditLogPage() {
  const [options, setOptions] = useState<ReportOptions | null>(null)
  const [logs, setLogs] = useState<{ items: AuditLog[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number } | null>(null)
  const [filters, setFilters] = useState<AuditLogQuery>(defaultQuery)
  const [query, setQuery] = useState<AuditLogQuery>(defaultQuery)
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingLogs, setIsLoadingLogs] = useState(false)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadLogs(query)
  }, [options, query])

  async function loadBootstrap() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getReportOptions()
      setOptions(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadLogs(nextQuery: AuditLogQuery) {
    setIsLoadingLogs(true)

    try {
      const response = await sixramApi.getAuditLogs(nextQuery)
      setLogs(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingLogs(false)
    }
  }

  async function handleOpenDetail(auditLogId: string) {
    try {
      const response = await sixramApi.getAuditLogById(auditLogId)
      setSelectedLog(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <span className="shell-badge-brand">Audit Trail</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Sensitive activity history</h2>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              Review who changed what, when it happened, and which employee or module was affected. Sensitive values are redacted server-side before display.
            </p>
          </div>

          <div className="flex gap-3">
            <Link className="shell-button-secondary" to="/reports/audit_activity">
              Audit report
            </Link>
            <button className="shell-button-secondary" onClick={() => void loadBootstrap()} type="button">
              Refresh
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      {isLoading ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading audit trail...</div>
      ) : (
        <section className="shell-card fade-up p-6 sm:p-7">
          <div className="grid gap-4 xl:grid-cols-[1.4fr_repeat(5,minmax(0,1fr))]">
            <label className="block space-y-2">
              <span className="shell-label mb-0">Search</span>
              <input
                className="shell-input"
                onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))}
                placeholder="Actor, action, employee, entity..."
                value={filters.search ?? ''}
              />
            </label>

            <LookupSelect
              label="Employee"
              onChange={(value) => setFilters((current) => ({ ...current, employeeId: value }))}
              options={options?.employees.map((item) => ({ id: item.id, label: `${item.employeeCode} | ${item.fullName}` })) ?? []}
              value={filters.employeeId ?? ''}
            />

            <label className="block space-y-2">
              <span className="shell-label mb-0">Entity type</span>
              <input
                className="shell-input"
                onChange={(event) => setFilters((current) => ({ ...current, entityType: event.target.value }))}
                placeholder="employee, payroll_run..."
                value={filters.entityType ?? ''}
              />
            </label>

            <label className="block space-y-2">
              <span className="shell-label mb-0">Action</span>
              <input
                className="shell-input"
                onChange={(event) => setFilters((current) => ({ ...current, action: event.target.value }))}
                placeholder="create, update, approve..."
                value={filters.action ?? ''}
              />
            </label>

            <label className="block space-y-2">
              <span className="shell-label mb-0">From</span>
              <input
                className="shell-input"
                onChange={(event) => setFilters((current) => ({ ...current, dateFrom: event.target.value }))}
                type="date"
                value={filters.dateFrom ?? ''}
              />
            </label>

            <label className="block space-y-2">
              <span className="shell-label mb-0">To</span>
              <input
                className="shell-input"
                onChange={(event) => setFilters((current) => ({ ...current, dateTo: event.target.value }))}
                type="date"
                value={filters.dateTo ?? ''}
              />
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
                  <th>Timestamp</th>
                  <th>Actor</th>
                  <th>Action</th>
                  <th>Entity</th>
                  <th>Employee</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {isLoadingLogs ? (
                  <tr>
                    <td className="text-slate-500" colSpan={6}>
                      Loading audit logs...
                    </td>
                  </tr>
                ) : !logs || logs.items.length === 0 ? (
                  <tr>
                    <td className="text-slate-500" colSpan={6}>
                      No audit activity matched the current filters.
                    </td>
                  </tr>
                ) : (
                  logs.items.map((item) => (
                    <tr key={item.id}>
                      <td className="text-slate-500">{formatDateTime(item.createdAtUtc)}</td>
                      <td>
                        <div className="font-semibold text-slate-900">{item.actorName || 'System'}</div>
                        <div className="mt-1 text-slate-500">{item.actorUserId || '-'}</div>
                      </td>
                      <td>
                        <span className="shell-badge-muted">{item.action.replace(/_/g, ' ')}</span>
                      </td>
                      <td>
                        <div className="font-semibold text-slate-900">{item.entityType}</div>
                        <div className="mt-1 text-slate-500">{item.entityId || '-'}</div>
                      </td>
                      <td>
                        <div className="font-semibold text-slate-900">{item.employeeFullName || 'N/A'}</div>
                        <div className="mt-1 text-slate-500">{item.employeeCode || '-'}</div>
                      </td>
                      <td className="text-right">
                        <button className="shell-button-secondary px-3 py-2" onClick={() => void handleOpenDetail(item.id)} type="button">
                          View
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {logs ? (
            <PaginationControls
              pageNumber={logs.pageNumber}
              pageSize={logs.pageSize}
              totalCount={logs.totalCount}
              totalPages={logs.totalPages}
              onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
            />
          ) : null}
        </section>
      )}

      <Modal
        description="Review the recorded before and after values captured for this action."
        onClose={() => setSelectedLog(null)}
        open={Boolean(selectedLog)}
        title={selectedLog ? `${selectedLog.entityType} · ${selectedLog.action}` : 'Audit log detail'}
      >
        {selectedLog ? (
          <div className="space-y-5">
            <div className="grid gap-4 md:grid-cols-2">
              <DetailCard label="Actor" value={selectedLog.actorName || 'System'} />
              <DetailCard label="Timestamp" value={formatDateTime(selectedLog.createdAtUtc)} />
              <DetailCard label="Entity" value={selectedLog.entityType} />
              <DetailCard label="Entity ID" value={selectedLog.entityId || '-'} />
              <DetailCard label="Employee" value={selectedLog.employeeFullName || '-'} />
              <DetailCard label="IP Address" value={selectedLog.ipAddress || '-'} />
            </div>

            {selectedLog.remarks ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
                <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400">Remarks</div>
                <div className="mt-2 text-sm text-slate-700">{selectedLog.remarks}</div>
              </div>
            ) : null}

            <JsonPanel title="Previous values" value={selectedLog.oldValuesJson} />
            <JsonPanel title="New values" value={selectedLog.newValuesJson} />
          </div>
        ) : null}
      </Modal>
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

function DetailCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400">{label}</div>
      <div className="mt-2 text-sm text-slate-900">{value}</div>
    </div>
  )
}

function JsonPanel({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-950 p-4 text-sm text-slate-100">
      <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400">{title}</div>
      <pre className="mt-3 overflow-x-auto whitespace-pre-wrap break-words text-xs leading-6 text-slate-200">
        {formatJson(value)}
      </pre>
    </div>
  )
}

function formatJson(value: string): string {
  if (!value.trim()) {
    return 'No values recorded.'
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}
