import { useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { DocumentStatusBadge } from '../components/DocumentStatusBadge'
import type { EmployeeDocumentProfile } from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { downloadBlob, formatFileSize, openBlobInNewTab } from '../utils/files'

export function MyDocumentsPage() {
  const [profile, setProfile] = useState<EmployeeDocumentProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [activeDownloadId, setActiveDownloadId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    const loadDocuments = async () => {
      setIsLoading(true)

      try {
        const response = await sixramApi.getMyDocuments()
        if (!cancelled) {
          setProfile(response)
          setError(null)
        }
      } catch (caughtError) {
        if (!cancelled) {
          setError(formatError(caughtError))
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadDocuments()

    return () => {
      cancelled = true
    }
  }, [])

  async function handleOpenDocument(documentId: string) {
    setActiveDownloadId(documentId)

    try {
      const file = await sixramApi.downloadMyDocument(documentId)
      if (file.contentType.includes('pdf') || file.contentType.startsWith('image/')) {
        openBlobInNewTab(file.blob, file.fileName)
      } else {
        downloadBlob(file.blob, file.fileName)
      }
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveDownloadId(null)
    }
  }

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading your documents...</div>
  }

  if (error && !profile) {
    return <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div>
  }

  if (!profile) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">No document profile is available.</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My documents</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">{profile.employeeFullName}</h2>
            <p className="mt-3 text-sm text-slate-500">
              Review official documents, expiry status, and any required file gaps flagged by HR.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <StatCard label="Active" value={String(profile.summary.activeDocuments)} />
            <StatCard label="Missing required" value={String(profile.summary.missingRequiredDocuments)} />
            <StatCard label="Expired" value={String(profile.summary.expiredDocuments)} />
            <StatCard label="Expiring soon" value={String(profile.summary.expiringSoonDocuments)} />
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Missing required</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Compliance gaps</h3>
          </div>
        </div>

        {profile.missingRequiredDocuments.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-sm text-emerald-700">
            All currently required document types are on file.
          </div>
        ) : (
          <div className="mt-6 flex flex-wrap gap-2">
            {profile.missingRequiredDocuments.map((document) => (
              <span className="shell-badge-warning" key={document.documentTypeId}>
                {document.name}
              </span>
            ))}
          </div>
        )}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Library</p>
          <h3 className="mt-2 text-2xl font-semibold text-slate-950">My document records</h3>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Document</th>
                <th>Type</th>
                <th>Dates</th>
                <th>Status</th>
                <th>Uploaded</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {profile.documents.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No documents are available for your profile yet.
                  </td>
                </tr>
              ) : (
                profile.documents.map((document) => (
                  <tr key={document.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{document.title}</div>
                      <div className="mt-1 text-slate-500">{document.originalFileName}</div>
                      <div className="mt-1 text-slate-400">{formatFileSize(document.fileSize)}</div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">{document.documentTypeName}</div>
                      {document.documentTypeIsRequired ? <div className="mt-1 text-slate-500">Required</div> : null}
                    </td>
                    <td className="text-slate-500">
                      <div>Issue: {formatDate(document.issueDate)}</div>
                      <div className="mt-1">Expiry: {formatDate(document.expiryDate)}</div>
                    </td>
                    <td>
                      <DocumentStatusBadge label={document.statusLabel} statusCode={document.statusCode} />
                    </td>
                    <td className="text-slate-500">{formatDateTime(document.createdAtUtc)}</td>
                    <td className="text-right">
                      <button
                        className="shell-button-secondary px-3 py-2"
                        disabled={activeDownloadId === document.id}
                        onClick={() => void handleOpenDocument(document.id)}
                        type="button"
                      >
                        {activeDownloadId === document.id ? 'Opening...' : 'Open'}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}
