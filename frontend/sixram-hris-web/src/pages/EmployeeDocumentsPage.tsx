import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { DocumentStatusBadge } from '../components/DocumentStatusBadge'
import { PaginationControls } from '../components/PaginationControls'
import type {
  DocumentComplianceSummary,
  EmployeeDocument,
  EmployeeDocumentListOptions,
  EmployeeDocumentListQuery,
  PagedResult,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { downloadBlob, formatFileSize, openBlobInNewTab } from '../utils/files'

const defaultQuery: EmployeeDocumentListQuery = {
  search: '',
  documentTypeId: '',
  departmentId: '',
  branchId: '',
  status: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'uploaded',
  descending: true,
}

export function EmployeeDocumentsPage() {
  const [result, setResult] = useState<PagedResult<EmployeeDocument> | null>(null)
  const [summary, setSummary] = useState<DocumentComplianceSummary | null>(null)
  const [options, setOptions] = useState<EmployeeDocumentListOptions | null>(null)
  const [query, setQuery] = useState<EmployeeDocumentListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState(defaultQuery.search ?? '')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [activeFileActionId, setActiveFileActionId] = useState<string | null>(null)

  useEffect(() => {
    void loadReferenceData()
  }, [])

  useEffect(() => {
    void loadDocuments()
  }, [query])

  async function loadReferenceData() {
    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getDocumentComplianceSummary(),
        sixramApi.getDocumentListOptions(),
      ])

      setSummary(summaryResponse)
      setOptions(optionsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadDocuments() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getEmployeeDocuments(query)
      setResult(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handleFileAction(document: EmployeeDocument, mode: 'view' | 'download') {
    setActiveFileActionId(document.id)

    try {
      const file = await sixramApi.downloadDocument(document.id)
      if (mode === 'view') {
        openBlobInNewTab(file.blob, file.fileName)
      } else {
        downloadBlob(file.blob, file.fileName)
      }
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveFileActionId(null)
    }
  }

  const cards = [
    { label: 'Total documents', value: summary?.totalDocuments ?? 0, tone: 'default' as const },
    { label: 'Missing required', value: summary?.missingRequiredDocuments ?? 0, tone: 'danger' as const },
    { label: 'Expired', value: summary?.expiredDocuments ?? 0, tone: 'danger' as const },
    { label: 'Expiring soon', value: summary?.expiringSoonDocuments ?? 0, tone: 'warning' as const },
    { label: 'Incomplete employees', value: summary?.employeesWithIncompleteDocuments ?? 0, tone: 'warning' as const },
  ]

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Employee Documents</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Document library and compliance tracking</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Review employee documents, expiry status, and required-document coverage across the workforce.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            Required document types: {summary?.requiredDocumentTypes ?? 0}
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          {cards.map((card) => (
            <SummaryCard key={card.label} label={card.label} tone={card.tone} value={String(card.value)} />
          ))}
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 xl:grid-cols-[1.3fr_repeat(5,minmax(0,1fr))]">
            <div>
              <label className="shell-label" htmlFor="employee-documents-search">
                Search
              </label>
              <input
                className="shell-input"
                id="employee-documents-search"
                onChange={(event) => setDraftSearch(event.target.value)}
                placeholder="Search employee, title, or file..."
                value={draftSearch}
              />
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-documents-type">
                Document Type
              </label>
              <select
                className="shell-select"
                id="employee-documents-type"
                onChange={(event) => setQuery((current) => ({ ...current, documentTypeId: event.target.value, pageNumber: 1 }))}
                value={query.documentTypeId ?? ''}
              >
                <option value="">All types</option>
                {options?.documentTypes.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-documents-department">
                Department
              </label>
              <select
                className="shell-select"
                id="employee-documents-department"
                onChange={(event) => setQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))}
                value={query.departmentId ?? ''}
              >
                <option value="">All departments</option>
                {options?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-documents-branch">
                Branch
              </label>
              <select
                className="shell-select"
                id="employee-documents-branch"
                onChange={(event) => setQuery((current) => ({ ...current, branchId: event.target.value, pageNumber: 1 }))}
                value={query.branchId ?? ''}
              >
                <option value="">All branches</option>
                {options?.branches.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-documents-status">
                Status
              </label>
              <select
                className="shell-select"
                id="employee-documents-status"
                onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={query.status ?? ''}
              >
                <option value="">All statuses</option>
                <option value="valid">Valid</option>
                <option value="expiring-soon">Expiring soon</option>
                <option value="expired">Expired</option>
                <option value="no-expiry">No expiry</option>
                <option value="archived">Archived</option>
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-documents-sort">
                Sort By
              </label>
              <select
                className="shell-select"
                id="employee-documents-sort"
                onChange={(event) => setQuery((current) => ({ ...current, sortBy: event.target.value, pageNumber: 1 }))}
                value={query.sortBy ?? 'uploaded'}
              >
                <option value="uploaded">Upload date</option>
                <option value="expiry">Expiry date</option>
                <option value="employee">Employee</option>
                <option value="title">Title</option>
              </select>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap gap-3">
            <button
              className="shell-button"
              onClick={() => setQuery((current) => ({ ...current, search: draftSearch, pageNumber: 1 }))}
              type="button"
            >
              Apply
            </button>
            <button
              className="shell-button-secondary"
              onClick={() => {
                setDraftSearch('')
                setQuery(defaultQuery)
              }}
              type="button"
            >
              Reset
            </button>
            <button
              className="shell-button-secondary"
              onClick={() => {
                void loadReferenceData()
                void loadDocuments()
              }}
              type="button"
            >
              Refresh
            </button>
          </div>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
        ) : null}

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Document</th>
                <th>Status</th>
                <th>Uploaded</th>
                <th>Expiry</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading employee documents...
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((document) => {
                  const isFileBusy = activeFileActionId === document.id

                  return (
                    <tr key={document.id}>
                      <td>
                        <div className="font-semibold text-slate-900">{document.employeeFullName}</div>
                        <div className="mt-1 text-slate-500">{document.employeeCode}</div>
                        <div className="mt-1 text-slate-500">
                          {[document.departmentName, document.branchName].filter(Boolean).join(' | ') || 'No assignment'}
                        </div>
                      </td>
                      <td>
                        <div className="font-semibold text-slate-900">{document.title}</div>
                        <div className="mt-1 text-slate-500">{document.documentTypeName}</div>
                        <div className="mt-1 text-slate-500">
                          {document.originalFileName} | {formatFileSize(document.fileSize)}
                        </div>
                      </td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          <DocumentStatusBadge label={document.statusLabel} statusCode={document.statusCode} />
                          {document.documentTypeIsRequired ? <span className="shell-badge-brand">Required</span> : null}
                        </div>
                      </td>
                      <td className="text-slate-600">
                        <div>{document.uploadedByDisplayName || document.uploadedByEmail || 'System'}</div>
                        <div className="mt-1">{formatDateTime(document.createdAtUtc)}</div>
                      </td>
                      <td className="text-slate-600">{formatDate(document.expiryDate)}</td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          <button
                            className="shell-button-secondary px-3 py-2"
                            disabled={isFileBusy}
                            onClick={() => void handleFileAction(document, 'view')}
                            type="button"
                          >
                            {isFileBusy ? 'Opening...' : 'View'}
                          </button>
                          <button
                            className="shell-button-secondary px-3 py-2"
                            disabled={isFileBusy}
                            onClick={() => void handleFileAction(document, 'download')}
                            type="button"
                          >
                            Download
                          </button>
                          <Link className="shell-button-secondary px-3 py-2" to={`/admin/employees/${document.employeeId}`}>
                            Open Profile
                          </Link>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No documents found for the current filters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <PaginationControls
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={result?.pageNumber ?? 1}
            pageSize={result?.pageSize ?? query.pageSize ?? 10}
            totalCount={result?.totalCount ?? 0}
            totalPages={result?.totalPages ?? 0}
          />
        </div>
      </section>
    </div>
  )
}

function SummaryCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: string
  tone?: 'default' | 'warning' | 'danger'
}) {
  const toneClasses =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : 'border-slate-200 bg-slate-50'

  return (
    <div className={`rounded-2xl border p-4 ${toneClasses}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}
