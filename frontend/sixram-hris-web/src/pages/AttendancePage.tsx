import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '../components/ContentState'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  AttendanceDashboardSummary,
  AttendanceListOptions,
  AttendanceRecordListItem,
  AttendanceRecordListQuery,
  PagedResult,
  SaveAttendanceRecordInput,
} from '../types/models'
import { addDaysToDate, formatDate, formatDateTime, formatMinutes, toDateTimeLocalInput } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

type AttendanceEditorState = SaveAttendanceRecordInput

const emptyAttendanceEditor: AttendanceEditorState = {
  employeeId: null,
  attendanceDate: null,
  actualTimeIn: null,
  actualTimeOut: null,
  breakStartTime: null,
  breakEndTime: null,
  source: 'manual',
  remarks: '',
}

const defaultQuery: AttendanceRecordListQuery = {
  search: '',
  departmentId: '',
  branchId: '',
  status: '',
  source: '',
  dateFrom: '',
  dateTo: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'date',
  descending: true,
}

export function AttendancePage() {
  const [summary, setSummary] = useState<AttendanceDashboardSummary | null>(null)
  const [options, setOptions] = useState<AttendanceListOptions | null>(null)
  const [result, setResult] = useState<PagedResult<AttendanceRecordListItem> | null>(null)
  const [query, setQuery] = useState<AttendanceRecordListQuery>(defaultQuery)
  const [draftSearch, setDraftSearch] = useState('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<AttendanceRecordListItem | null>(null)
  const [editor, setEditor] = useState<AttendanceEditorState>(emptyAttendanceEditor)
  const [deleteTarget, setDeleteTarget] = useState<AttendanceRecordListItem | null>(null)
  const [absentTarget, setAbsentTarget] = useState<AttendanceRecordListItem | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isLoadingRecords, setIsLoadingRecords] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isMarkingAbsent, setIsMarkingAbsent] = useState(false)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!query.dateFrom || !query.dateTo) {
      return
    }

    void loadRecords()
  }, [query])

  const trend = useMemo(() => (summary?.trend ?? []).slice().reverse().slice(0, 7), [summary])

  async function loadInitialData() {
    setIsInitialLoading(true)

    try {
      const [summaryResponse, optionsResponse] = await Promise.all([
        sixramApi.getAttendanceSummary(),
        sixramApi.getAttendanceOptions(),
      ])

      setSummary(summaryResponse)
      setOptions(optionsResponse)
      setError(null)
      setQuery((current) => ({
        ...current,
        dateFrom: addDaysToDate(summaryResponse.attendanceDate, -6),
        dateTo: summaryResponse.attendanceDate,
      }))
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsInitialLoading(false)
    }
  }

  async function loadSummary() {
    try {
      const response = await sixramApi.getAttendanceSummary()
      setSummary(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadRecords() {
    setIsLoadingRecords(true)

    try {
      const response = await sixramApi.getAttendanceRecords(query)
      setResult(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingRecords(false)
    }
  }

  function openCreateModal(record?: AttendanceRecordListItem) {
    setEditingRecord(null)
    setFieldErrors({})
    setEditor({
      employeeId: record?.employeeId ?? options?.employees[0]?.id ?? null,
      attendanceDate: record?.attendanceDate ?? summary?.attendanceDate ?? null,
      actualTimeIn: null,
      actualTimeOut: null,
      breakStartTime: null,
      breakEndTime: null,
      source: 'manual',
      remarks: record?.remarks ?? '',
    })
    setEditorOpen(true)
  }

  function openEditModal(record: AttendanceRecordListItem) {
    setEditingRecord(record)
    setFieldErrors({})
    setEditor({
      employeeId: record.employeeId,
      attendanceDate: record.attendanceDate,
      actualTimeIn: toDateTimeLocalInput(record.actualTimeIn),
      actualTimeOut: toDateTimeLocalInput(record.actualTimeOut),
      breakStartTime: toDateTimeLocalInput(record.breakStartTime),
      breakEndTime: toDateTimeLocalInput(record.breakEndTime),
      source: record.source,
      remarks: record.remarks,
    })
    setEditorOpen(true)
  }

  async function handleSave(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    const payload: SaveAttendanceRecordInput = {
      employeeId: editor.employeeId || null,
      attendanceDate: editor.attendanceDate || null,
      actualTimeIn: editor.actualTimeIn || null,
      actualTimeOut: editor.actualTimeOut || null,
      breakStartTime: editor.breakStartTime || null,
      breakEndTime: editor.breakEndTime || null,
      source: editor.source,
      remarks: editor.remarks,
    }

    try {
      if (editingRecord?.attendanceRecordId) {
        await sixramApi.updateAttendanceRecord(editingRecord.attendanceRecordId, payload)
      } else {
        await sixramApi.createAttendanceRecord(payload)
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
    if (!deleteTarget?.attendanceRecordId) {
      return
    }

    setIsDeleting(true)

    try {
      await sixramApi.deleteAttendanceRecord(deleteTarget.attendanceRecordId)
      setDeleteTarget(null)
      await Promise.all([loadSummary(), loadRecords()])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleMarkAbsentConfirmed() {
    if (!absentTarget) {
      return
    }

    setIsMarkingAbsent(true)

    try {
      await sixramApi.createAttendanceRecord({
        employeeId: absentTarget.employeeId,
        attendanceDate: absentTarget.attendanceDate,
        actualTimeIn: null,
        actualTimeOut: null,
        breakStartTime: null,
        breakEndTime: null,
        source: 'manual',
        remarks: 'Marked absent manually by HR/Admin.',
      })

      setAbsentTarget(null)
      await Promise.all([loadSummary(), loadRecords()])
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsMarkingAbsent(false)
    }
  }

  if (isInitialLoading) {
    return <LoadingState message="Loading attendance workspace..." />
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Attendance & Timekeeping</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Attendance records</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review daily attendance, correct missing logs, monitor incomplete records, and prepare clean timekeeping
              data for the next HR modules.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <button className="shell-button-secondary" onClick={() => void Promise.all([loadSummary(), loadRecords()])} type="button">
              Refresh
            </button>
            <button className="shell-button" onClick={() => openCreateModal()} type="button">
              Add Attendance
            </button>
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Present today" value={summary?.presentCount ?? 0} />
          <SummaryCard label="Late today" value={summary?.lateCount ?? 0} tone="brand" />
          <SummaryCard label="Absent today" value={summary?.absentCount ?? 0} tone="danger" />
          <SummaryCard label="Incomplete logs" value={summary?.incompleteCount ?? 0} tone="warning" />
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Rest day today" value={summary?.restDayCount ?? 0} tone="muted" />
          <SummaryCard label="No schedule today" value={summary?.noScheduleCount ?? 0} tone="muted" />
          <SummaryCard label="Undertime today" value={summary?.undertimeCount ?? 0} tone="brand" />
          <SummaryCard label="Without assignment" value={summary?.employeesWithoutScheduleAssignmentCount ?? 0} tone="warning" />
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-3 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Filters and report view</h4>
            <p className="mt-1 text-sm text-slate-500">
              Use this view as the baseline daily attendance report, incomplete-log monitor, and late or undertime
              review.
            </p>
          </div>
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            {result?.totalCount ?? 0} rows
          </div>
        </div>

        <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="grid gap-4 xl:grid-cols-[1.7fr_repeat(6,minmax(0,1fr))]">
            <div>
              <label className="shell-label" htmlFor="attendance-search">
                Employee search
              </label>
              <input
                className="shell-input"
                id="attendance-search"
                onChange={(event) => setDraftSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    setQuery((current) => ({ ...current, search: draftSearch, pageNumber: 1 }))
                  }
                }}
                placeholder="Search employee code or name"
                value={draftSearch}
              />
            </div>

            <FormControl label="Date From">
              <input
                className="shell-input"
                onChange={(event) => setQuery((current) => ({ ...current, dateFrom: event.target.value, pageNumber: 1 }))}
                type="date"
                value={query.dateFrom ?? ''}
              />
            </FormControl>

            <FormControl label="Date To">
              <input
                className="shell-input"
                onChange={(event) => setQuery((current) => ({ ...current, dateTo: event.target.value, pageNumber: 1 }))}
                type="date"
                value={query.dateTo ?? ''}
              />
            </FormControl>

            <FormControl label="Department">
              <select
                className="shell-select"
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
            </FormControl>

            <FormControl label="Branch">
              <select
                className="shell-select"
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
            </FormControl>

            <FormControl label="Status">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={query.status ?? ''}
              >
                <option value="">All statuses</option>
                {options?.statuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </FormControl>

            <FormControl label="Source">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, source: event.target.value, pageNumber: 1 }))}
                value={query.source ?? ''}
              >
                <option value="">All sources</option>
                {options?.sources.map((source) => (
                  <option key={source} value={source}>
                    {source}
                  </option>
                ))}
              </select>
            </FormControl>
          </div>

          <div className="mt-4 grid gap-4 md:grid-cols-3 xl:grid-cols-5">
            <FormControl label="Sort By">
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, sortBy: event.target.value, pageNumber: 1 }))}
                value={query.sortBy ?? 'date'}
              >
                <option value="date">Date</option>
                <option value="employee">Employee</option>
                <option value="late">Late Minutes</option>
                <option value="undertime">Undertime Minutes</option>
                <option value="overtime">Overtime Minutes</option>
                <option value="worked">Worked Minutes</option>
                <option value="status">Status</option>
              </select>
            </FormControl>

            <FormControl label="Order">
              <select
                className="shell-select"
                onChange={(event) =>
                  setQuery((current) => ({
                    ...current,
                    descending: event.target.value === 'desc',
                    pageNumber: 1,
                  }))
                }
                value={query.descending ? 'desc' : 'asc'}
              >
                <option value="desc">Descending</option>
                <option value="asc">Ascending</option>
              </select>
            </FormControl>

            <div className="md:col-span-3 xl:col-span-3 xl:self-end">
              <div className="flex flex-wrap gap-3">
                <button
                  className="shell-button"
                  onClick={() => setQuery((current) => ({ ...current, search: draftSearch, pageNumber: 1 }))}
                  type="button"
                >
                  Apply Filters
                </button>
                <button
                  className="shell-button-secondary"
                  onClick={() => {
                    setDraftSearch('')
                    setQuery({
                      ...defaultQuery,
                      dateFrom: summary?.attendanceDate ? addDaysToDate(summary.attendanceDate, -6) : '',
                      dateTo: summary?.attendanceDate ?? '',
                    })
                  }}
                  type="button"
                >
                  Reset
                </button>
              </div>
            </div>
          </div>
        </div>

        {error ? <ErrorState className="mt-6" message={error} /> : null}

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Date</th>
                <th>Scheduled</th>
                <th>Actual</th>
                <th>Time Summary</th>
                <th>Status</th>
                <th>Source</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoadingRecords ? (
                <tr>
                  <td className="text-slate-500" colSpan={8}>
                    Loading attendance records...
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((record) => (
                  <tr key={`${record.employeeId}-${record.attendanceDate}`}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                      <div className="mt-1 text-slate-500">
                        {[record.departmentName, record.branchName].filter(Boolean).join(' | ') || 'No organization assignment'}
                      </div>
                    </td>
                    <td className="text-slate-600">{formatDate(record.attendanceDate)}</td>
                    <td>
                      <div className="font-medium text-slate-900">
                        {record.workScheduleName || 'No schedule assigned'}
                        {record.shiftName ? ` | ${record.shiftName}` : ''}
                      </div>
                      <div className="mt-1 text-slate-500">
                        {record.scheduledStartTime && record.scheduledEndTime
                          ? `${formatDateTime(record.scheduledStartTime)} - ${formatDateTime(record.scheduledEndTime)}`
                          : 'No scheduled window'}
                      </div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">
                        {record.actualTimeIn ? formatDateTime(record.actualTimeIn) : 'No time in'}
                      </div>
                      <div className="mt-1 text-slate-500">
                        {record.actualTimeOut ? formatDateTime(record.actualTimeOut) : 'No time out'}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatMinutes(record.totalWorkedMinutes)} worked</div>
                      <div className="mt-1 text-xs text-slate-500">
                        Late {formatMinutes(record.lateMinutes)} | Under {formatMinutes(record.undertimeMinutes)} | OT {formatMinutes(record.overtimeMinutes)}
                      </div>
                    </td>
                    <td>
                      <AttendanceStatusBadge status={record.status} />
                    </td>
                    <td>
                      <div className="font-medium capitalize text-slate-900">{record.source.replace('_', ' ')}</div>
                      <div className="mt-1 text-slate-500">
                        {record.hasBackingRecord ? record.remarks || 'Saved record' : record.remarks}
                      </div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {record.hasBackingRecord && record.attendanceRecordId ? (
                          <>
                            <button className="shell-button-secondary" onClick={() => openEditModal(record)} type="button">
                              Edit
                            </button>
                            <button className="shell-button-danger" onClick={() => setDeleteTarget(record)} type="button">
                              Delete
                            </button>
                          </>
                        ) : (
                          <>
                            <button className="shell-button-secondary" onClick={() => openCreateModal(record)} type="button">
                              Add Record
                            </button>
                            {record.status === 'Absent' ? (
                              <button className="shell-button-secondary" onClick={() => setAbsentTarget(record)} type="button">
                                Mark Absent
                              </button>
                            ) : null}
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={8}>
                    <EmptyState message="No attendance records found for the selected filters." title="No attendance results" />
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

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Seven-day trend</h4>
            <p className="mt-1 text-sm text-slate-500">
              A quick operational view of present, late, absent, and incomplete logs for the most recent days.
            </p>
          </div>
          <span className="shell-badge-muted">Last {trend.length} days</span>
        </div>

        <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {trend.map((point) => (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4" key={point.date}>
              <p className="text-sm font-semibold text-slate-900">{formatDate(point.date)}</p>
              <div className="mt-3 space-y-1 text-sm text-slate-600">
                <p>Present: {point.presentCount}</p>
                <p>Late: {point.lateCount}</p>
                <p>Absent: {point.absentCount}</p>
                <p>Incomplete: {point.incompleteCount}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      <Modal
        description={
          editingRecord
            ? 'Correct the actual logs, source, or remarks for this attendance record.'
            : 'Create a manual attendance entry or correction record.'
        }
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRecord ? `Edit ${editingRecord.employeeFullName}` : 'Add Attendance Record'}
      >
        <form className="space-y-5" onSubmit={handleSave}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormControl error={getFieldError(fieldErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select
                className="shell-select"
                disabled={Boolean(editingRecord)}
                onChange={(event) => setEditor((current) => ({ ...current, employeeId: event.target.value || null }))}
                value={editor.employeeId ?? ''}
              >
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormControl>

            <FormControl error={getFieldError(fieldErrors, 'AttendanceDate', 'attendanceDate')} label="Attendance Date">
              <input
                className="shell-input"
                disabled={Boolean(editingRecord)}
                onChange={(event) => setEditor((current) => ({ ...current, attendanceDate: event.target.value || null }))}
                type="date"
                value={editor.attendanceDate ?? ''}
              />
            </FormControl>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormControl error={getFieldError(fieldErrors, 'ActualTimeIn', 'actualTimeIn')} label="Actual Time In">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, actualTimeIn: event.target.value || null }))}
                type="datetime-local"
                value={editor.actualTimeIn ?? ''}
              />
            </FormControl>
            <FormControl error={getFieldError(fieldErrors, 'ActualTimeOut', 'actualTimeOut')} label="Actual Time Out">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, actualTimeOut: event.target.value || null }))}
                type="datetime-local"
                value={editor.actualTimeOut ?? ''}
              />
            </FormControl>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormControl error={getFieldError(fieldErrors, 'BreakStartTime', 'breakStartTime')} label="Break Start">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, breakStartTime: event.target.value || null }))}
                type="datetime-local"
                value={editor.breakStartTime ?? ''}
              />
            </FormControl>
            <FormControl error={getFieldError(fieldErrors, 'BreakEndTime', 'breakEndTime')} label="Break End">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, breakEndTime: event.target.value || null }))}
                type="datetime-local"
                value={editor.breakEndTime ?? ''}
              />
            </FormControl>
          </div>

          <div className="grid gap-5 sm:grid-cols-[0.9fr_1.1fr]">
            <FormControl error={getFieldError(fieldErrors, 'Source', 'source')} label="Source">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, source: event.target.value }))}
                value={editor.source}
              >
                {options?.sources.filter((source) => source !== 'leave').map((source) => (
                  <option key={source} value={source}>
                    {source}
                  </option>
                ))}
              </select>
            </FormControl>
            <FormControl error={getFieldError(fieldErrors, 'Remarks', 'remarks')} label="Remarks">
              <textarea
                className="shell-textarea"
                onChange={(event) => setEditor((current) => ({ ...current, remarks: event.target.value }))}
                rows={3}
                value={editor.remarks}
              />
            </FormControl>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
            Use the full date and time for overnight shifts. For an absence entry, leave the time fields blank and keep
            the source as manual.
          </div>

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
            ? `Delete the saved attendance record for ${deleteTarget.employeeFullName} on ${formatDate(deleteTarget.attendanceDate)}?`
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
        title="Delete Attendance Record"
      />

      <ConfirmDialog
        confirmLabel="Mark Absent"
        description={
          absentTarget
            ? `Create a manual absence record for ${absentTarget.employeeFullName} on ${formatDate(absentTarget.attendanceDate)}?`
            : ''
        }
        isBusy={isMarkingAbsent}
        onCancel={() => {
          if (!isMarkingAbsent) {
            setAbsentTarget(null)
          }
        }}
        onConfirm={() => void handleMarkAbsentConfirmed()}
        open={Boolean(absentTarget)}
        title="Mark Employee Absent"
      />
    </div>
  )
}

function SummaryCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: number
  tone?: 'default' | 'brand' | 'warning' | 'danger' | 'muted'
}) {
  const className =
    tone === 'danger'
      ? 'border-rose-200 bg-rose-50'
      : tone === 'warning'
        ? 'border-amber-200 bg-amber-50'
        : tone === 'brand'
          ? 'border-[#465fff]/20 bg-[#465fff]/5'
          : tone === 'muted'
            ? 'border-slate-200 bg-slate-50'
            : 'border-slate-200 bg-white'

  return (
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{label}</p>
      <p className="mt-3 text-3xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function FormControl({
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
