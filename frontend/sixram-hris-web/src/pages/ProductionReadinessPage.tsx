import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { sixramApi } from '../api/sixramApi'
import type {
  DataImportApplyResult,
  DataImportDefinition,
  DataImportPreview,
  DataImportPreviewRow,
  ProductionReadinessItem,
  ProductionReadinessOverview,
} from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

const PREVIEW_ROW_LIMIT = 20

export function ProductionReadinessPage() {
  const [overview, setOverview] = useState<ProductionReadinessOverview | null>(null)
  const [importType, setImportType] = useState('')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<DataImportPreview | null>(null)
  const [applyResult, setApplyResult] = useState<DataImportApplyResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isPreviewing, setIsPreviewing] = useState(false)
  const [isApplying, setIsApplying] = useState(false)
  const [showApplyConfirm, setShowApplyConfirm] = useState(false)

  useEffect(() => {
    void loadOverview()
  }, [])

  const activeImport = useMemo(
    () => overview?.availableImports.find((item) => item.key === importType) ?? null,
    [importType, overview?.availableImports],
  )

  const previewRows = useMemo(() => {
    const rows = preview?.rows ?? applyResult?.rows ?? []
    return rows.slice(0, PREVIEW_ROW_LIMIT)
  }, [applyResult?.rows, preview?.rows])

  const invalidPreviewRows = useMemo(
    () => (preview?.rows ?? []).filter((row) => row.status !== 'valid'),
    [preview?.rows],
  )

  async function loadOverview() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getProductionReadinessOverview()
      setOverview(response)
      setImportType((current) => current || response.availableImports[0]?.key || '')
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handlePreview() {
    if (!importType || !selectedFile) {
      setError('Choose an import type and CSV file before previewing.')
      return
    }

    setIsPreviewing(true)
    setApplyResult(null)

    try {
      const response = await sixramApi.previewProductionImport(importType, selectedFile)
      setPreview(response)
      setError(null)
    } catch (caughtError) {
      setPreview(null)
      setError(formatError(caughtError))
    } finally {
      setIsPreviewing(false)
    }
  }

  async function handleApply() {
    if (!importType || !selectedFile || !preview?.canApply) {
      setShowApplyConfirm(false)
      return
    }

    setIsApplying(true)

    try {
      const response = await sixramApi.applyProductionImport(importType, selectedFile)
      setApplyResult(response)
      setPreview(null)
      setShowApplyConfirm(false)
      setError(null)
      await loadOverview()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsApplying(false)
    }
  }

  function handleImportTypeChange(nextImportType: string) {
    setImportType(nextImportType)
    setPreview(null)
    setApplyResult(null)
    setError(null)
  }

  function handleFileChange(file: File | null) {
    setSelectedFile(file)
    setPreview(null)
    setApplyResult(null)
    setError(null)
  }

  function downloadSampleCsv(definition: DataImportDefinition) {
    const columns = Array.from(new Set([...definition.requiredColumns, ...definition.optionalColumns]))
    downloadCsv(definition.sampleFileName || `${definition.key}.csv`, [columns])
  }

  function downloadErrorReport() {
    if (!preview || invalidPreviewRows.length === 0) {
      return
    }

    const rows = invalidPreviewRows.map((row) => {
      const values = preview.columns.map((column) => row.values[column] ?? '')
      return [String(row.rowNumber), row.identifier, row.operation, row.status, row.messages.join(' | '), ...values]
    })

    downloadCsv(
      `${preview.importType}-errors.csv`,
      [['row_number', 'identifier', 'operation', 'status', 'messages', ...preview.columns], ...rows],
    )
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <span className="shell-badge-brand">Production Readiness</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Go-live readiness, safeguards, and migration tools</h2>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              Use this workspace to review go-live blockers, validate operational setup, import clean master data, and confirm the controls that
              protect payroll, documents, exports, and audit visibility.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/analytics">
              Analytics dashboard
            </Link>
            <Link className="shell-button-secondary" to="/compliance">
              Compliance center
            </Link>
            <button className="shell-button-secondary" onClick={() => void loadOverview()} type="button">
              Refresh
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div> : null}

      {isLoading || !overview ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading production readiness overview...</div>
      ) : (
        <>
          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <SummaryCard label="Readiness score" tone="brand" value={`${overview.readinessPercent}%`} />
            <SummaryCard label="Ready checks" tone="success" value={String(overview.readyItemCount)} />
            <SummaryCard label="Needs attention" tone="warning" value={String(overview.attentionItemCount)} />
            <SummaryCard label="Blocked items" tone="danger" value={String(overview.blockedItemCount)} />
          </section>

          <section className="grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_minmax(320px,0.9fr)]">
            <div className="space-y-6">
              {overview.sections.map((section) => (
                <section className="shell-card fade-up p-6 sm:p-7" key={section.key}>
                  <div className="flex flex-col gap-2">
                    <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">{section.title}</p>
                    <h3 className="text-xl font-semibold text-slate-950">{section.description}</h3>
                  </div>

                  <div className="mt-5 space-y-3">
                    {section.items.map((item) => (
                      <ReadinessItemCard item={item} key={item.key} />
                    ))}
                  </div>
                </section>
              ))}
            </div>

            <div className="space-y-6">
              <section className="shell-card fade-up p-6 sm:p-7">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Operations</p>
                    <h3 className="mt-2 text-xl font-semibold text-slate-950">Go-live guidance</h3>
                  </div>
                  <span className="shell-badge-muted">{formatDateTime(overview.generatedAtUtc)}</span>
                </div>

                <div className="mt-5 space-y-4">
                  {overview.operationalGuidance.map((item) => (
                    <div className="rounded-3xl border border-slate-200 bg-slate-50/80 p-4" key={item.key}>
                      <h4 className="text-sm font-semibold text-slate-900">{item.title}</h4>
                      <p className="mt-2 text-sm leading-6 text-slate-500">{item.description}</p>
                    </div>
                  ))}
                </div>
              </section>

              <section className="shell-card fade-up p-6 sm:p-7">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Migration</p>
                    <h3 className="mt-2 text-xl font-semibold text-slate-950">Preview-first data import</h3>
                    <p className="mt-2 text-sm leading-6 text-slate-500">
                      Upload CSV files, review row-level validation, and apply only when every row is clean. Imports run inside one transaction so
                      partial bad data is not saved.
                    </p>
                  </div>
                </div>

                <div className="mt-5 grid gap-4">
                  <label className="block space-y-2">
                    <span className="shell-label mb-0">Import type</span>
                    <select className="shell-input" onChange={(event) => handleImportTypeChange(event.target.value)} value={importType}>
                      {overview.availableImports.map((item) => (
                        <option key={item.key} value={item.key}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="block space-y-2">
                    <span className="shell-label mb-0">CSV file</span>
                    <input
                      accept=".csv,text/csv"
                      className="shell-input file:mr-4 file:rounded-xl file:border-0 file:bg-[#465fff] file:px-4 file:py-2 file:text-sm file:font-semibold file:text-white"
                      onChange={(event) => handleFileChange(event.target.files?.[0] ?? null)}
                      type="file"
                    />
                  </label>

                  {activeImport ? (
                    <div className="rounded-3xl border border-slate-200 bg-slate-50/80 p-4 text-sm text-slate-600">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <h4 className="font-semibold text-slate-900">{activeImport.name}</h4>
                          <p className="mt-1 leading-6">{activeImport.description}</p>
                        </div>
                        <button className="shell-button-secondary px-3 py-2" onClick={() => downloadSampleCsv(activeImport)} type="button">
                          Download sample
                        </button>
                      </div>

                      <div className="mt-4 space-y-3">
                        <div>
                          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Required columns</p>
                          <div className="mt-2 flex flex-wrap gap-2">
                            {activeImport.requiredColumns.map((column) => (
                              <span className="shell-badge-muted" key={`${activeImport.key}-${column}`}>
                                {column}
                              </span>
                            ))}
                          </div>
                        </div>

                        {activeImport.optionalColumns.length > 0 ? (
                          <div>
                            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Optional columns</p>
                            <div className="mt-2 flex flex-wrap gap-2">
                              {activeImport.optionalColumns.map((column) => (
                                <span className="shell-badge-muted" key={`${activeImport.key}-optional-${column}`}>
                                  {column}
                                </span>
                              ))}
                            </div>
                          </div>
                        ) : null}
                      </div>
                    </div>
                  ) : null}

                  <div className="flex flex-wrap gap-3">
                    <button className="shell-button" disabled={!importType || !selectedFile || isPreviewing} onClick={() => void handlePreview()} type="button">
                      {isPreviewing ? 'Previewing...' : 'Preview import'}
                    </button>
                    <button
                      className="shell-button-secondary"
                      disabled={!preview?.canApply || !selectedFile || isApplying}
                      onClick={() => setShowApplyConfirm(true)}
                      type="button"
                    >
                      {isApplying ? 'Applying...' : 'Apply import'}
                    </button>
                    {invalidPreviewRows.length > 0 ? (
                      <button className="shell-button-secondary" onClick={downloadErrorReport} type="button">
                        Download errors
                      </button>
                    ) : null}
                  </div>
                </div>
              </section>
            </div>
          </section>

          {preview || applyResult ? (
            <section className="shell-card fade-up overflow-hidden">
              <div className="border-b border-slate-200 px-6 py-5 sm:px-7">
                <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">
                      {applyResult ? 'Import result' : 'Import preview'}
                    </p>
                    <h3 className="mt-2 text-xl font-semibold text-slate-950">{(applyResult?.importName || preview?.importName) ?? 'Import summary'}</h3>
                    <p className="mt-2 text-sm text-slate-500">
                      File: {(applyResult?.fileName || preview?.fileName) ?? selectedFile?.name ?? 'N/A'}
                    </p>
                  </div>

                  {applyResult ? (
                    <span className="shell-badge-success">Applied {formatDateTime(applyResult.appliedAtUtc)}</span>
                  ) : preview?.canApply ? (
                    <span className="shell-badge-success">Ready to apply</span>
                  ) : (
                    <span className="shell-badge-warning">Fix validation issues first</span>
                  )}
                </div>
              </div>

              <div className="grid gap-4 border-b border-slate-200 px-6 py-5 sm:grid-cols-2 xl:grid-cols-5 sm:px-7">
                <MetricCard label="Rows in file" value={String(preview?.totalRows ?? applyResult?.processedCount ?? 0)} />
                <MetricCard label="Valid rows" value={String(preview?.validRowCount ?? applyResult?.processedCount ?? 0)} />
                <MetricCard label="Invalid rows" value={String(preview?.invalidRowCount ?? applyResult?.errorCount ?? 0)} />
                <MetricCard label="Created" value={String(applyResult?.createdCount ?? countRowsByOperation(preview?.rows ?? [], 'create'))} />
                <MetricCard label="Updated" value={String(applyResult?.updatedCount ?? countRowsByOperation(preview?.rows ?? [], 'update'))} />
              </div>

              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50/90 text-left text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">
                    <tr>
                      <th className="px-5 py-3">Row</th>
                      <th className="px-5 py-3">Identifier</th>
                      <th className="px-5 py-3">Operation</th>
                      <th className="px-5 py-3">Status</th>
                      <th className="px-5 py-3">Messages</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 bg-white">
                    {previewRows.map((row) => (
                      <tr key={`${row.rowNumber}-${row.identifier}`}>
                        <td className="px-5 py-4 text-slate-500">{row.rowNumber}</td>
                        <td className="px-5 py-4 font-medium text-slate-900">{row.identifier || 'Row item'}</td>
                        <td className="px-5 py-4 text-slate-600">{formatImportOperation(row.operation)}</td>
                        <td className="px-5 py-4">
                          <span className={getPreviewStatusClass(row.status)}>{formatImportStatus(row.status)}</span>
                        </td>
                        <td className="px-5 py-4 text-slate-500">{row.messages.length > 0 ? row.messages.join(' ') : 'No issues detected.'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {(preview?.rows.length ?? applyResult?.rows.length ?? 0) > PREVIEW_ROW_LIMIT ? (
                <div className="border-t border-slate-200 px-5 py-4 text-sm text-slate-500 sm:px-7">
                  Showing the first {PREVIEW_ROW_LIMIT} rows of {(preview?.rows.length ?? applyResult?.rows.length) ?? 0}. Download the error file for
                  full row-level review when needed.
                </div>
              ) : null}
            </section>
          ) : null}
        </>
      )}

      <ConfirmDialog
        cancelLabel="Review again"
        confirmLabel="Apply import"
        confirmTone="primary"
        description="This will apply every valid row in one transaction. Existing records may be updated based on the import key definitions."
        isBusy={isApplying}
        onCancel={() => setShowApplyConfirm(false)}
        onConfirm={() => void handleApply()}
        open={showApplyConfirm}
        title="Apply data import?"
      />
    </div>
  )
}

function SummaryCard({ label, value, tone }: { label: string; value: string; tone: 'brand' | 'success' | 'warning' | 'danger' }) {
  return (
    <div className="shell-card p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <div className="mt-3 flex items-end justify-between gap-3">
        <p className="text-3xl font-semibold text-slate-950">{value}</p>
        <span className={getToneBadgeClass(tone)}>{label}</span>
      </div>
    </div>
  )
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-3xl border border-slate-200 bg-slate-50/80 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function ReadinessItemCard({ item }: { item: ProductionReadinessItem }) {
  return (
    <div className="rounded-3xl border border-slate-200 bg-white p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h4 className="text-sm font-semibold text-slate-900">{item.label}</h4>
            <span className={getReadinessStatusClass(item.status)}>{formatReadinessStatus(item.status)}</span>
          </div>
          <p className="mt-2 text-sm leading-6 text-slate-500">{item.detail}</p>
        </div>

        {item.actionUrl ? (
          <Link className="shell-button-secondary px-3 py-2" to={item.actionUrl}>
            Open
          </Link>
        ) : null}
      </div>
    </div>
  )
}

function getReadinessStatusClass(status: string) {
  switch (status) {
    case 'ready':
      return 'shell-badge-success'
    case 'attention':
      return 'shell-badge-warning'
    case 'blocked':
      return 'shell-badge-danger'
    default:
      return 'shell-badge-muted'
  }
}

function getPreviewStatusClass(status: string) {
  switch (status) {
    case 'valid':
      return 'shell-badge-success'
    case 'invalid':
      return 'shell-badge-danger'
    default:
      return 'shell-badge-muted'
  }
}

function getToneBadgeClass(tone: 'brand' | 'success' | 'warning' | 'danger') {
  switch (tone) {
    case 'brand':
      return 'shell-badge-brand'
    case 'success':
      return 'shell-badge-success'
    case 'warning':
      return 'shell-badge-warning'
    case 'danger':
      return 'shell-badge-danger'
  }
}

function formatReadinessStatus(status: string) {
  switch (status) {
    case 'ready':
      return 'Ready'
    case 'attention':
      return 'Needs attention'
    case 'blocked':
      return 'Blocked'
    case 'manual':
      return 'Manual review'
    default:
      return status
  }
}

function formatImportOperation(operation: string) {
  switch (operation) {
    case 'create':
      return 'Create'
    case 'update':
      return 'Update'
    default:
      return operation
  }
}

function formatImportStatus(status: string) {
  switch (status) {
    case 'valid':
      return 'Valid'
    case 'invalid':
      return 'Invalid'
    default:
      return status
  }
}

function countRowsByOperation(rows: DataImportPreviewRow[], operation: string) {
  return rows.filter((row) => row.operation === operation && row.status === 'valid').length
}

function downloadCsv(fileName: string, rows: string[][]) {
  const content = rows.map((row) => row.map(escapeCsvValue).join(',')).join('\r\n')
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

function escapeCsvValue(value: string) {
  const normalizedValue = value ?? ''
  if (/[",\r\n]/.test(normalizedValue)) {
    return `"${normalizedValue.replace(/"/g, '""')}"`
  }

  return normalizedValue
}
