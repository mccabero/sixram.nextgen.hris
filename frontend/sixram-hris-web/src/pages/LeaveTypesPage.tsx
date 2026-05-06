import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  LeaveManagementOptions,
  LeaveType,
  LeaveTypeInput,
  LeaveTypeListQuery,
  PagedResult,
} from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const defaultQuery: LeaveTypeListQuery = {
  search: '',
  isActive: null,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'name',
  descending: false,
}

const emptyEditor: LeaveTypeInput = {
  code: '',
  name: '',
  description: '',
  isPaid: true,
  requiresAttachment: false,
  requiresReason: true,
  allowHalfDay: true,
  allowNegativeBalance: false,
  defaultAnnualCredits: 0,
  maxDaysPerRequest: null,
  minDaysBeforeFiling: null,
  genderRestriction: '',
  employmentTypeRestrictionIds: [],
  countsRestDays: false,
  countsHolidays: false,
  allowDuringProbationaryPeriod: true,
  isActive: true,
}

export function LeaveTypesPage() {
  const [result, setResult] = useState<PagedResult<LeaveType> | null>(null)
  const [options, setOptions] = useState<LeaveManagementOptions | null>(null)
  const [query, setQuery] = useState<LeaveTypeListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<LeaveType | null>(null)
  const [editor, setEditor] = useState<LeaveTypeInput>(emptyEditor)
  const [deleteTarget, setDeleteTarget] = useState<LeaveType | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    void Promise.all([loadRecords(), loadOptions()])
  }, [query])

  async function loadOptions() {
    try {
      const response = await sixramApi.getLeaveOptions()
      setOptions(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRecords() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getLeaveTypes(query)
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

  function openEditModal(record: LeaveType) {
    setEditingRecord(record)
    setEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      isPaid: record.isPaid,
      requiresAttachment: record.requiresAttachment,
      requiresReason: record.requiresReason,
      allowHalfDay: record.allowHalfDay,
      allowNegativeBalance: record.allowNegativeBalance,
      defaultAnnualCredits: record.defaultAnnualCredits ?? 0,
      maxDaysPerRequest: record.maxDaysPerRequest ?? null,
      minDaysBeforeFiling: record.minDaysBeforeFiling ?? null,
      genderRestriction: record.genderRestriction,
      employmentTypeRestrictionIds: record.employmentTypeRestrictionIds,
      countsRestDays: record.countsRestDays,
      countsHolidays: record.countsHolidays,
      allowDuringProbationaryPeriod: record.allowDuringProbationaryPeriod,
      isActive: record.isActive,
    })
    setFieldErrors({})
    setEditorOpen(true)
  }

  async function handleSave(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    try {
      const payload: LeaveTypeInput = {
        ...editor,
        defaultAnnualCredits: editor.defaultAnnualCredits === null || editor.defaultAnnualCredits === undefined ? null : Number(editor.defaultAnnualCredits),
        maxDaysPerRequest: editor.maxDaysPerRequest === null || editor.maxDaysPerRequest === undefined ? null : Number(editor.maxDaysPerRequest),
        minDaysBeforeFiling: editor.minDaysBeforeFiling === null || editor.minDaysBeforeFiling === undefined ? null : Number(editor.minDaysBeforeFiling),
      }

      if (editingRecord) {
        await sixramApi.updateLeaveType(editingRecord.id, payload)
      } else {
        await sixramApi.createLeaveType(payload)
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
      await sixramApi.deleteLeaveType(deleteTarget.id)
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
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Leave Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Leave types</h3>
            <p className="mt-2 text-sm text-slate-500">
              Maintain leave categories, credit defaults, request rules, and filing restrictions for the leave module.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              {result?.totalCount ?? 0} total | {activeCount} active on page
            </div>
            <button className="shell-button" onClick={openCreateModal} type="button">
              Add Leave Type
            </button>
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 lg:grid-cols-[2fr_repeat(2,minmax(0,1fr))]">
            <div>
              <label className="shell-label" htmlFor="leave-type-search">
                Search
              </label>
              <input
                className="shell-input"
                id="leave-type-search"
                onChange={(event) => setDraftSearch(event.target.value)}
                placeholder="Search code, name, description..."
                value={draftSearch}
              />
            </div>

            <div>
              <label className="shell-label" htmlFor="leave-type-status">
                Status
              </label>
              <select
                className="shell-select"
                id="leave-type-status"
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
              <label className="shell-label" htmlFor="leave-type-sort">
                Sort By
              </label>
              <select
                className="shell-select"
                id="leave-type-sort"
                onChange={(event) => setQuery((current) => ({ ...current, sortBy: event.target.value, pageNumber: 1 }))}
                value={query.sortBy ?? 'name'}
              >
                <option value="name">Name</option>
                <option value="code">Code</option>
                <option value="pending">Pending Requests</option>
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
            <button className="shell-button-secondary" onClick={() => void Promise.all([loadRecords(), loadOptions()])} type="button">
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
                <th>Policy</th>
                <th>Rules</th>
                <th>Status</th>
                <th>Usage</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading leave types...
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.name}</div>
                      <div className="mt-1 text-slate-500">{record.code}</div>
                      <div className="mt-1 text-slate-500">{record.description || 'No description'}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <span className={record.isPaid ? 'shell-badge-success' : 'shell-badge-muted'}>
                          {record.isPaid ? 'Paid' : 'Unpaid'}
                        </span>
                        <span className={record.allowHalfDay ? 'shell-badge-brand' : 'shell-badge-muted'}>
                          {record.allowHalfDay ? 'Half day' : 'Full day only'}
                        </span>
                        <span className={record.allowNegativeBalance ? 'shell-badge-warning' : 'shell-badge-muted'}>
                          {record.allowNegativeBalance ? 'Negative allowed' : 'Balance enforced'}
                        </span>
                      </div>
                      <div className="mt-2 text-slate-500">
                        Default annual credits: {formatNumeric(record.defaultAnnualCredits)}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{record.requiresReason ? 'Reason required' : 'Reason optional'}</div>
                      <div className="mt-1">{record.requiresAttachment ? 'Attachment required' : 'Attachment optional'}</div>
                      <div className="mt-1">{record.allowDuringProbationaryPeriod ? 'Allowed during probation' : 'Blocked during probation'}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <span className={record.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                          {record.isActive ? 'Active' : 'Inactive'}
                        </span>
                        {record.genderRestriction ? <span className="shell-badge-muted">{record.genderRestriction}</span> : null}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{record.employeeCount} employees</div>
                      <div className="mt-1">{record.pendingRequestCount} pending requests</div>
                      <div className="mt-1">{record.updatedAtUtc ? formatDateTime(record.updatedAtUtc) : formatDateTime(record.createdAtUtc)}</div>
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
                    No leave types found for the current filters.
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
        description={
          editingRecord
            ? 'Update the leave-type policy, filing rules, and active state.'
            : 'Create a new leave type for employee leave management.'
        }
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRecord ? `Edit ${editingRecord.name}` : 'Create Leave Type'}
      >
        <form className="space-y-5" onSubmit={handleSave}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'Code', 'code')} label="Code">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, code: event.target.value }))}
                value={editor.code}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'Name', 'name')} label="Name">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, name: event.target.value }))}
                value={editor.name}
              />
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'Description', 'description')} label="Description">
            <textarea
              className="shell-textarea"
              onChange={(event) => setEditor((current) => ({ ...current, description: event.target.value }))}
              value={editor.description}
            />
          </FormField>

          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(fieldErrors, 'DefaultAnnualCredits', 'defaultAnnualCredits')} label="Default Credits">
              <input
                className="shell-input"
                min="0"
                onChange={(event) => setEditor((current) => ({ ...current, defaultAnnualCredits: event.target.value === '' ? null : Number(event.target.value) }))}
                step="0.5"
                type="number"
                value={editor.defaultAnnualCredits ?? ''}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'MaxDaysPerRequest', 'maxDaysPerRequest')} label="Max Days Per Request">
              <input
                className="shell-input"
                min="0"
                onChange={(event) => setEditor((current) => ({ ...current, maxDaysPerRequest: event.target.value === '' ? null : Number(event.target.value) }))}
                step="0.5"
                type="number"
                value={editor.maxDaysPerRequest ?? ''}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'MinDaysBeforeFiling', 'minDaysBeforeFiling')} label="Min Days Before Filing">
              <input
                className="shell-input"
                min="0"
                onChange={(event) => setEditor((current) => ({ ...current, minDaysBeforeFiling: event.target.value === '' ? null : Number(event.target.value) }))}
                type="number"
                value={editor.minDaysBeforeFiling ?? ''}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'GenderRestriction', 'genderRestriction')} label="Gender Restriction">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, genderRestriction: event.target.value }))}
                value={editor.genderRestriction}
              >
                <option value="">None</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'EmploymentTypeRestrictionIds', 'employmentTypeRestrictionIds')} label="Employment Type Restrictions">
              <select
                className="shell-select min-h-[120px]"
                multiple
                onChange={(event) =>
                  setEditor((current) => ({
                    ...current,
                    employmentTypeRestrictionIds: Array.from(event.target.selectedOptions).map((option) => option.value),
                  }))
                }
                value={editor.employmentTypeRestrictionIds}
              >
                {options?.employmentTypes.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <ToggleField checked={editor.isPaid} label="Paid leave" onChange={(checked) => setEditor((current) => ({ ...current, isPaid: checked }))} />
            <ToggleField checked={editor.requiresAttachment} label="Attachment required" onChange={(checked) => setEditor((current) => ({ ...current, requiresAttachment: checked }))} />
            <ToggleField checked={editor.requiresReason} label="Reason required" onChange={(checked) => setEditor((current) => ({ ...current, requiresReason: checked }))} />
            <ToggleField checked={editor.allowHalfDay} label="Allow half day" onChange={(checked) => setEditor((current) => ({ ...current, allowHalfDay: checked }))} />
            <ToggleField checked={editor.allowNegativeBalance} label="Allow negative balance" onChange={(checked) => setEditor((current) => ({ ...current, allowNegativeBalance: checked }))} />
            <ToggleField checked={editor.countsRestDays} label="Count rest days" onChange={(checked) => setEditor((current) => ({ ...current, countsRestDays: checked }))} />
            <ToggleField checked={editor.countsHolidays} label="Count holidays" onChange={(checked) => setEditor((current) => ({ ...current, countsHolidays: checked }))} />
            <ToggleField checked={editor.allowDuringProbationaryPeriod} label="Allow during probation" onChange={(checked) => setEditor((current) => ({ ...current, allowDuringProbationaryPeriod: checked }))} />
            <ToggleField checked={editor.isActive} label="Active record" onChange={(checked) => setEditor((current) => ({ ...current, isActive: checked }))} />
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : editingRecord ? 'Save Changes' : 'Create Leave Type'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Leave Type"
        description={
          deleteTarget
            ? `Delete ${deleteTarget.name}? If it is already used by balances or leave requests, the API will block deletion and you should deactivate it instead.`
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
        title={deleteTarget ? `Delete ${deleteTarget.name}` : 'Delete Leave Type'}
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

function ToggleField({
  label,
  checked,
  onChange,
}: {
  label: string
  checked: boolean
  onChange: (checked: boolean) => void
}) {
  return (
    <label className="inline-flex items-center gap-3 rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700">
      <input checked={checked} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
      {label}
    </label>
  )
}

function formatNumeric(value?: number | null) {
  if (value === null || value === undefined) {
    return '-'
  }

  return Number.isInteger(value) ? `${value}` : value.toFixed(1)
}
