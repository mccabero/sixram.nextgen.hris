import { type ReactNode, useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  OrganizationListQuery,
  OrganizationOptions,
  OrganizationRecord,
  OrganizationRecordInput,
  OrganizationResource,
  PagedResult,
} from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

type OrganizationLookupPageProps = {
  resource: OrganizationResource
  title: string
  description: string
}

const defaultQuery: OrganizationListQuery = {
  search: '',
  isActive: null,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'name',
  descending: false,
}

const emptyEditor: OrganizationRecordInput = {
  code: '',
  name: '',
  description: '',
  isActive: true,
  address: '',
  departmentId: null,
}

export function OrganizationLookupPage({ resource, title, description }: OrganizationLookupPageProps) {
  const [result, setResult] = useState<PagedResult<OrganizationRecord> | null>(null)
  const [options, setOptions] = useState<OrganizationOptions | null>(null)
  const [query, setQuery] = useState<OrganizationListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<OrganizationRecord | null>(null)
  const [editor, setEditor] = useState<OrganizationRecordInput>(emptyEditor)
  const [deleteTarget, setDeleteTarget] = useState<OrganizationRecord | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const isPositionPage = resource === 'positions'
  const isBranchPage = resource === 'branches'

  useEffect(() => {
    void loadOptions()
  }, [])

  useEffect(() => {
    void loadRecords()
  }, [resource, query])

  async function loadOptions() {
    try {
      const response = await sixramApi.getOrganizationOptions()
      setOptions(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRecords() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getOrganizationRecords(resource, query)
      setResult(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateModal() {
    setEditingRecord(null)
    setEditor(emptyEditor)
    setFieldErrors({})
    setEditorOpen(true)
  }

  function openEditModal(record: OrganizationRecord) {
    setEditingRecord(record)
    setEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      isActive: record.isActive,
      address: record.address,
      departmentId: record.departmentId ?? null,
    })
    setFieldErrors({})
    setEditorOpen(true)
  }

  async function handleSave(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      if (editingRecord) {
        await sixramApi.updateOrganizationRecord(resource, editingRecord.id, editor)
      } else {
        await sixramApi.createOrganizationRecord(resource, editor)
      }

      setEditorOpen(false)
      await loadRecords()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteConfirmed() {
    if (!deleteTarget) {
      return
    }

    setIsDeleting(true)

    try {
      await sixramApi.deleteOrganizationRecord(resource, deleteTarget.id)
      setDeleteTarget(null)
      await loadRecords()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  const activeCount = result?.items.filter((item) => item.isActive).length ?? 0

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Organization Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">{title}</h3>
            <p className="mt-2 text-sm text-slate-500">{description}</p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              {result?.totalCount ?? 0} total | {activeCount} active on page
            </div>
            <button className="shell-button" onClick={openCreateModal} type="button">
              Add Record
            </button>
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 lg:grid-cols-[2fr_repeat(2,minmax(0,1fr))]">
            <div>
              <label className="shell-label" htmlFor={`${resource}-search`}>
                Search
              </label>
              <input
                className="shell-input"
                id={`${resource}-search`}
                onChange={(event) => setDraftSearch(event.target.value)}
                placeholder="Search code, name, description..."
                value={draftSearch}
              />
            </div>

            <div>
              <label className="shell-label" htmlFor={`${resource}-active`}>
                Status
              </label>
              <select
                className="shell-select"
                id={`${resource}-active`}
                onChange={(event) => {
                  const value = event.target.value
                  setQuery((current) => ({
                    ...current,
                    isActive: value === '' ? null : value === 'true',
                    pageNumber: 1,
                  }))
                }}
                value={query.isActive === null || query.isActive === undefined ? '' : String(query.isActive)}
              >
                <option value="">All</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor={`${resource}-sort`}>
                Sort By
              </label>
              <select
                className="shell-select"
                id={`${resource}-sort`}
                onChange={(event) => setQuery((current) => ({ ...current, sortBy: event.target.value, pageNumber: 1 }))}
                value={query.sortBy ?? 'name'}
              >
                <option value="name">Name</option>
                <option value="code">Code</option>
                <option value="employees">Employee Count</option>
                <option value="created">Created</option>
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
            <button className="shell-button-secondary" onClick={() => void loadRecords()} type="button">
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
                <th>Record</th>
                <th>{isPositionPage ? 'Department' : isBranchPage ? 'Address' : 'Details'}</th>
                <th>Employees</th>
                <th>Status</th>
                <th>Updated</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading records...
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.name}</div>
                      <div className="mt-1 text-slate-500">{record.code}</div>
                    </td>
                    <td className="text-slate-600">
                      {isPositionPage
                        ? record.departmentName || 'No department'
                        : isBranchPage
                          ? record.address || 'No address'
                          : record.description || 'No description'}
                    </td>
                    <td>
                      <span className={record.employeeCount > 0 ? 'shell-badge-brand' : 'shell-badge-muted'}>
                        {record.employeeCount}
                      </span>
                    </td>
                    <td>
                      <span className={record.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                        {record.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="text-slate-600">
                      {record.updatedAtUtc ? formatDateTime(record.updatedAtUtc) : formatDateTime(record.createdAtUtc)}
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary" onClick={() => openEditModal(record)} type="button">
                          Edit
                        </button>
                        <button className="shell-button-danger" onClick={() => setDeleteTarget(record)} type="button">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No records found for the current filters.
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

      <Modal
        description={editingRecord ? `Update ${title.toLowerCase()} details.` : `Create a new ${title.toLowerCase()} record.`}
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRecord ? `Edit ${editingRecord.name}` : `Create ${title}`}
      >
        <form className="space-y-5" onSubmit={handleSave}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, code: event.target.value }))} value={editor.code} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, name: event.target.value }))} value={editor.name} />
            </FormField>
          </div>

          {isPositionPage ? (
            <FormField label="Department">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, departmentId: event.target.value || null }))}
                value={editor.departmentId ?? ''}
              >
                <option value="">No department</option>
                {options?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
          ) : null}

          {isBranchPage ? (
            <FormField label="Address">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, address: event.target.value }))} value={editor.address ?? ''} />
            </FormField>
          ) : null}

          <FormField error={getFieldError(fieldErrors, 'Description', 'description')} label="Description">
            <textarea
              className="shell-textarea"
              onChange={(event) => setEditor((current) => ({ ...current, description: event.target.value }))}
              value={editor.description}
            />
          </FormField>

          <label className="inline-flex items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
            <input checked={editor.isActive} onChange={(event) => setEditor((current) => ({ ...current, isActive: event.target.checked }))} type="checkbox" />
            Active record
          </label>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : editingRecord ? 'Save Changes' : 'Create Record'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Record"
        description={
          deleteTarget
            ? `Delete ${deleteTarget.name}? If it is referenced by employees or dependent setup records, the API will block deletion and you should deactivate it instead.`
            : ''
        }
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteTarget(null)
          }
        }}
        onConfirm={() => void handleDeleteConfirmed()}
        open={Boolean(deleteTarget)}
        title={deleteTarget ? `Delete ${deleteTarget.name}` : 'Delete Record'}
      />
    </div>
  )
}

function FormField({
  label,
  error,
  children,
}: {
  label: string
  error?: string | null
  children: ReactNode
}) {
  return (
    <div>
      <label className="shell-label">{label}</label>
      {children}
      {error ? <p className="mt-2 text-sm text-rose-600">{error}</p> : null}
    </div>
  )
}
