import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  AttendanceListOptions,
  AttendanceSetupSummary,
  EmployeeScheduleAssignmentListQuery,
  EmployeeScheduleAssignmentRecord,
  PagedResult,
  SaveEmployeeScheduleAssignmentInput,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const restDayOptions = [
  { value: 0, label: 'Sun' },
  { value: 1, label: 'Mon' },
  { value: 2, label: 'Tue' },
  { value: 3, label: 'Wed' },
  { value: 4, label: 'Thu' },
  { value: 5, label: 'Fri' },
  { value: 6, label: 'Sat' },
]

const defaultQuery: EmployeeScheduleAssignmentListQuery = {
  search: '',
  departmentId: '',
  branchId: '',
  isActive: true,
  date: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'employee',
  descending: false,
}

const emptyEditor: SaveEmployeeScheduleAssignmentInput = {
  employeeId: null,
  workScheduleId: null,
  shiftId: null,
  effectiveStartDate: null,
  effectiveEndDate: null,
  restDayValues: [0, 6],
  isActive: true,
}

export function ScheduleAssignmentsPage() {
  const [summary, setSummary] = useState<AttendanceSetupSummary | null>(null)
  const [baseOptions, setBaseOptions] = useState<AttendanceListOptions | null>(null)
  const [editorOptions, setEditorOptions] = useState<AttendanceListOptions | null>(null)
  const [result, setResult] = useState<PagedResult<EmployeeScheduleAssignmentRecord> | null>(null)
  const [query, setQuery] = useState<EmployeeScheduleAssignmentListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<EmployeeScheduleAssignmentRecord | null>(null)
  const [editor, setEditor] = useState<SaveEmployeeScheduleAssignmentInput>(emptyEditor)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isEditorLoading, setIsEditorLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    void loadRecords()
  }, [query])

  const selectedWorkSchedule = useMemo(
    () => editorOptions?.workSchedules.find((record) => record.id === editor.workScheduleId) ?? null,
    [editor.workScheduleId, editorOptions],
  )

  const requiresShift =
    selectedWorkSchedule?.scheduleType === 'fixed' || selectedWorkSchedule?.scheduleType === 'shifting'

  async function loadInitialData() {
    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getAttendanceSetupSummary(),
        sixramApi.getAttendanceSetupOptions(),
      ])

      setSummary(summaryResponse)
      setBaseOptions(optionsResponse)
      setEditorOptions(optionsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRecords() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getScheduleAssignments(query)
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
    setFieldErrors({})
    setEditorOptions(baseOptions)
    setEditor({
      ...emptyEditor,
      employeeId: baseOptions?.employees[0]?.id ?? null,
      workScheduleId: baseOptions?.workSchedules[0]?.id ?? null,
    })
    setEditorOpen(true)
  }

  async function openEditModal(record: EmployeeScheduleAssignmentRecord) {
    setIsEditorLoading(true)
    setEditingRecord(record)
    setFieldErrors({})

    try {
      const optionsResponse = await sixramApi.getAttendanceSetupOptions(record.id)
      setEditorOptions(optionsResponse)
      setEditor({
        employeeId: record.employeeId,
        workScheduleId: record.workScheduleId,
        shiftId: record.shiftId ?? null,
        effectiveStartDate: record.effectiveStartDate,
        effectiveEndDate: record.effectiveEndDate ?? null,
        restDayValues: record.restDayValues,
        isActive: record.isActive,
      })
      setEditorOpen(true)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsEditorLoading(false)
    }
  }

  async function handleSave(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    const payload: SaveEmployeeScheduleAssignmentInput = {
      employeeId: editor.employeeId || null,
      workScheduleId: editor.workScheduleId || null,
      shiftId: editor.shiftId || null,
      effectiveStartDate: editor.effectiveStartDate || null,
      effectiveEndDate: editor.effectiveEndDate || null,
      restDayValues: editor.restDayValues,
      isActive: editor.isActive,
    }

    try {
      if (editingRecord) {
        await sixramApi.updateScheduleAssignment(editingRecord.id, payload)
      } else {
        await sixramApi.createScheduleAssignment(payload)
      }

      setEditorOpen(false)
      await Promise.all([loadInitialData(), loadRecords()])
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  function toggleRestDay(dayValue: number) {
    setEditor((current) => ({
      ...current,
      restDayValues: current.restDayValues.includes(dayValue)
        ? current.restDayValues.filter((value) => value !== dayValue)
        : [...current.restDayValues, dayValue].sort((left, right) => left - right),
    }))
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Attendance Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Schedule assignments</h3>
            <p className="mt-2 text-sm text-slate-500">
              Assign schedule policies and optional shifts to employees with effective dates and rest-day rules.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              {summary?.scheduleAssignmentCount ?? result?.totalCount ?? 0} total | {summary?.activeScheduleAssignmentCount ?? 0} active
            </div>
            <button className="shell-button" onClick={openCreateModal} type="button">
              Add Assignment
            </button>
          </div>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 xl:grid-cols-[1.7fr_repeat(4,minmax(0,1fr))]">
            <FormField label="Search">
              <input
                className="shell-input"
                onChange={(event) => setDraftSearch(event.target.value)}
                placeholder="Search employee code or name..."
                value={draftSearch}
              />
            </FormField>
            <FormField label="Department">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))}
                value={query.departmentId ?? ''}
              >
                <option value="">All departments</option>
                {baseOptions?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Branch">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, branchId: event.target.value, pageNumber: 1 }))}
                value={query.branchId ?? ''}
              >
                <option value="">All branches</option>
                {baseOptions?.branches.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
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
            <FormField label="Date Filter">
              <input
                className="shell-input"
                onChange={(event) => setQuery((current) => ({ ...current, date: event.target.value, pageNumber: 1 }))}
                type="date"
                value={query.date ?? ''}
              />
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
            <button className="shell-button-secondary" onClick={() => void Promise.all([loadInitialData(), loadRecords()])} type="button">
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
                <th>Schedule</th>
                <th>Effective Dates</th>
                <th>Rest Days</th>
                <th>Status</th>
                <th>Updated</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={7}>
                    Loading schedule assignments...
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                      <div className="mt-1 text-slate-500">
                        {[record.departmentName, record.branchName].filter(Boolean).join(' | ') || 'No organization assignment'}
                      </div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">{record.workScheduleName}</div>
                      <div className="mt-1 text-slate-500">
                        {record.workScheduleType}
                        {record.shiftName ? ` | ${record.shiftName}` : ' | No shift'}
                      </div>
                      <div className="mt-2 flex flex-wrap gap-2">
                        {!record.workScheduleIsActive ? <span className="shell-badge-muted">Inactive schedule</span> : null}
                        {record.shiftId && !record.shiftIsActive ? <span className="shell-badge-muted">Inactive shift</span> : null}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatDate(record.effectiveStartDate)}</div>
                      <div className="mt-1 text-xs text-slate-500">
                        Ends {record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}
                      </div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {record.restDayLabels.length ? (
                          record.restDayLabels.map((label) => (
                            <span className="shell-badge-muted" key={`${record.id}-${label}`}>
                              {label}
                            </span>
                          ))
                        ) : (
                          <span className="shell-badge-brand">No rest days</span>
                        )}
                      </div>
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
                      <button className="shell-button-secondary" disabled={isEditorLoading} onClick={() => void openEditModal(record)} type="button">
                        Edit
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={7}>
                    No schedule assignments found for the current filters.
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
        description={editingRecord ? 'Update the effective dates, rest days, and active state for this assignment.' : 'Create a new employee schedule assignment.'}
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRecord ? `Edit ${editingRecord.employeeFullName}` : 'Create Schedule Assignment'}
      >
        <form className="space-y-5" onSubmit={handleSave}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select
                className="shell-select"
                disabled={Boolean(editingRecord)}
                onChange={(event) => setEditor((current) => ({ ...current, employeeId: event.target.value || null }))}
                value={editor.employeeId ?? ''}
              >
                <option value="">Select employee</option>
                {editorOptions?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'WorkScheduleId', 'workScheduleId')} label="Work Schedule">
              <select
                className="shell-select"
                onChange={(event) => {
                  const workScheduleId = event.target.value || null
                  const nextWorkSchedule = editorOptions?.workSchedules.find((record) => record.id === workScheduleId) ?? null
                  setEditor((current) => ({
                    ...current,
                    workScheduleId,
                    shiftId:
                      nextWorkSchedule && (nextWorkSchedule.scheduleType === 'fixed' || nextWorkSchedule.scheduleType === 'shifting')
                        ? current.shiftId
                        : null,
                  }))
                }}
                value={editor.workScheduleId ?? ''}
              >
                <option value="">Select work schedule</option>
                {editorOptions?.workSchedules.map((record) => (
                  <option key={record.id} value={record.id}>
                    {record.code} | {record.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'ShiftId', 'shiftId')} label="Shift">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, shiftId: event.target.value || null }))}
                value={editor.shiftId ?? ''}
              >
                <option value="">{requiresShift ? 'Select shift' : 'No shift'}</option>
                {editorOptions?.shifts.map((shift) => (
                  <option key={shift.id} value={shift.id}>
                    {shift.code} | {shift.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Schedule Type">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
                {selectedWorkSchedule ? `${selectedWorkSchedule.scheduleType} schedule` : 'Select a work schedule first'}
              </div>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, effectiveStartDate: event.target.value || null }))}
                type="date"
                value={editor.effectiveStartDate ?? ''}
              />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, effectiveEndDate: event.target.value || null }))}
                type="date"
                value={editor.effectiveEndDate ?? ''}
              />
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'RestDayValues', 'restDayValues')} label="Rest Days">
            <div className="flex flex-wrap gap-2">
              {restDayOptions.map((day) => (
                <label
                  className={`inline-flex items-center gap-2 rounded-full border px-3 py-2 text-sm font-medium ${
                    editor.restDayValues.includes(day.value)
                      ? 'border-[#465fff]/25 bg-[#465fff]/10 text-[#3641f5]'
                      : 'border-slate-300 bg-white text-slate-700'
                  }`}
                  key={day.value}
                >
                  <input
                    checked={editor.restDayValues.includes(day.value)}
                    onChange={() => toggleRestDay(day.value)}
                    type="checkbox"
                  />
                  {day.label}
                </label>
              ))}
            </div>
          </FormField>

          <label className="inline-flex items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
            <input checked={editor.isActive} onChange={(event) => setEditor((current) => ({ ...current, isActive: event.target.checked }))} type="checkbox" />
            Active assignment
          </label>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
            Fixed and shifting schedules should be paired with a shift. If you need to end an assignment, set an end
            date or mark the assignment inactive.
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving || isEditorLoading} type="submit">
              {isSaving ? 'Saving...' : editingRecord ? 'Save Changes' : 'Create Assignment'}
            </button>
          </div>
        </form>
      </Modal>
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
