import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  AttendanceSetupSummary,
  PagedResult,
  SaveShiftInput,
  ShiftListQuery,
  ShiftRecord,
} from '../types/models'
import { formatDateTime, formatMinutes, formatTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const defaultQuery: ShiftListQuery = {
  search: '',
  isActive: null,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'name',
  descending: false,
}

const emptyEditor: SaveShiftInput = {
  code: '',
  name: '',
  startTime: '09:00',
  endTime: '18:00',
  breakStartTime: '12:00',
  breakEndTime: '13:00',
  requiredWorkingMinutes: 480,
  gracePeriodMinutes: 10,
  isOvernight: false,
  isActive: true,
}

export function ShiftsPage() {
  const [summary, setSummary] = useState<AttendanceSetupSummary | null>(null)
  const [result, setResult] = useState<PagedResult<ShiftRecord> | null>(null)
  const [query, setQuery] = useState<ShiftListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<ShiftRecord | null>(null)
  const [editor, setEditor] = useState<SaveShiftInput>(emptyEditor)
  const [deleteTarget, setDeleteTarget] = useState<ShiftRecord | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    void loadSummary()
  }, [])

  useEffect(() => {
    void loadRecords()
  }, [query])

  async function loadSummary() {
    try {
      const response = await sixramApi.getAttendanceSetupSummary()
      setSummary(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRecords() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getShifts(query)
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

  function openEditModal(record: ShiftRecord) {
    setEditingRecord(record)
    setEditor({
      code: record.code,
      name: record.name,
      startTime: record.startTime.slice(0, 5),
      endTime: record.endTime.slice(0, 5),
      breakStartTime: record.breakStartTime ? record.breakStartTime.slice(0, 5) : null,
      breakEndTime: record.breakEndTime ? record.breakEndTime.slice(0, 5) : null,
      requiredWorkingMinutes: record.requiredWorkingMinutes,
      gracePeriodMinutes: record.gracePeriodMinutes,
      isOvernight: record.isOvernight,
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
      if (editingRecord) {
        await sixramApi.updateShift(editingRecord.id, editor)
      } else {
        await sixramApi.createShift(editor)
      }

      setEditorOpen(false)
      await Promise.all([loadSummary(), loadRecords()])
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
      await sixramApi.deleteShift(deleteTarget.id)
      setDeleteTarget(null)
      await Promise.all([loadSummary(), loadRecords()])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Attendance Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Shifts</h3>
            <p className="mt-2 text-sm text-slate-500">
              Maintain shift windows, break times, required work minutes, and overnight handling for assigned employees.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              {summary?.shiftCount ?? result?.totalCount ?? 0} total | {summary?.activeShiftCount ?? 0} active
            </div>
            <button className="shell-button" onClick={openCreateModal} type="button">
              Add Shift
            </button>
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 lg:grid-cols-[2fr_repeat(2,minmax(0,1fr))]">
            <FormField label="Search">
              <input
                className="shell-input"
                onChange={(event) => setDraftSearch(event.target.value)}
                placeholder="Search code or shift name..."
                value={draftSearch}
              />
            </FormField>
            <FormField label="Status">
              <select
                className="shell-select"
                onChange={(event) =>
                  setQuery((current) => ({
                    ...current,
                    isActive: event.target.value === '' ? null : event.target.value === 'true',
                    pageNumber: 1,
                  }))
                }
                value={query.isActive === null || query.isActive === undefined ? '' : String(query.isActive)}
              >
                <option value="">All</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </FormField>
            <FormField label="Sort By">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, sortBy: event.target.value, pageNumber: 1 }))}
                value={query.sortBy ?? 'name'}
              >
                <option value="name">Name</option>
                <option value="code">Code</option>
                <option value="start">Start Time</option>
                <option value="created">Created</option>
              </select>
            </FormField>
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
            <button className="shell-button-secondary" onClick={() => void Promise.all([loadSummary(), loadRecords()])} type="button">
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
                <th>Shift</th>
                <th>Window</th>
                <th>Work Policy</th>
                <th>Status</th>
                <th>Assignments</th>
                <th>Updated</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={7}>
                    Loading shifts...
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
                      <div>
                        {formatTime(record.startTime)} - {formatTime(record.endTime)}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        {record.breakStartTime && record.breakEndTime
                          ? `Break ${formatTime(record.breakStartTime)} - ${formatTime(record.breakEndTime)}`
                          : 'No configured break'}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatMinutes(record.requiredWorkingMinutes)} required</div>
                      <div className="mt-1 text-xs text-slate-500">
                        Grace {formatMinutes(record.gracePeriodMinutes)} | {record.isOvernight ? 'Overnight shift' : 'Same-day shift'}
                      </div>
                    </td>
                    <td>
                      <span className={record.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                        {record.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>
                      <span className={record.assignmentCount > 0 ? 'shell-badge-brand' : 'shell-badge-muted'}>
                        {record.assignmentCount}
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
                  <td className="text-slate-500" colSpan={7}>
                    No shifts found for the current filters.
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
        description={editingRecord ? 'Update the shift window, break times, and overnight behavior.' : 'Create a new reusable shift record.'}
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRecord ? `Edit ${editingRecord.name}` : 'Create Shift'}
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

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'StartTime', 'startTime')} label="Start Time">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, startTime: event.target.value }))}
                type="time"
                value={editor.startTime}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'EndTime', 'endTime')} label="End Time">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, endTime: event.target.value }))}
                type="time"
                value={editor.endTime}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'BreakStartTime', 'breakStartTime')} label="Break Start">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, breakStartTime: event.target.value || null }))}
                type="time"
                value={editor.breakStartTime ?? ''}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'BreakEndTime', 'breakEndTime')} label="Break End">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, breakEndTime: event.target.value || null }))}
                type="time"
                value={editor.breakEndTime ?? ''}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'RequiredWorkingMinutes', 'requiredWorkingMinutes')} label="Required Minutes">
              <input
                className="shell-input"
                min={1}
                onChange={(event) => setEditor((current) => ({ ...current, requiredWorkingMinutes: Number(event.target.value) || 0 }))}
                type="number"
                value={editor.requiredWorkingMinutes}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'GracePeriodMinutes', 'gracePeriodMinutes')} label="Grace Minutes">
              <input
                className="shell-input"
                min={0}
                onChange={(event) => setEditor((current) => ({ ...current, gracePeriodMinutes: Number(event.target.value) || 0 }))}
                type="number"
                value={editor.gracePeriodMinutes}
              />
            </FormField>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <label className="inline-flex items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
              <input checked={editor.isOvernight} onChange={(event) => setEditor((current) => ({ ...current, isOvernight: event.target.checked }))} type="checkbox" />
              Overnight shift
            </label>
            <label className="inline-flex items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
              <input checked={editor.isActive} onChange={(event) => setEditor((current) => ({ ...current, isActive: event.target.checked }))} type="checkbox" />
              Active shift
            </label>
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : editingRecord ? 'Save Changes' : 'Create Shift'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Shift"
        description={
          deleteTarget
            ? `Delete ${deleteTarget.name}? If it is already assigned to employees, the API will block deletion and you should deactivate it instead.`
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
        title={deleteTarget ? `Delete ${deleteTarget.name}` : 'Delete Shift'}
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
