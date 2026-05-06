import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import { RequestStatusBadge } from '../components/RequestStatusBadge'
import type {
  AttendanceAdjustmentRequest,
  AttendanceRecordListItem,
  AttendanceRecordListQuery,
  SaveAttendanceAdjustmentRequestInput,
  PagedResult,
} from '../types/models'
import { addDaysToDate, formatDate, formatDateTime, formatMinutes, toDateTimeLocalInput } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const today = new Date().toISOString().slice(0, 10)

const defaultQuery: AttendanceRecordListQuery = {
  dateFrom: addDaysToDate(today, -14),
  dateTo: today,
  status: '',
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'date',
  descending: true,
}

const defaultRequestEditor: SaveAttendanceAdjustmentRequestInput = {
  attendanceRecordId: null,
  attendanceDate: today,
  requestType: 'incorrect_time_in_out',
  requestedTimeIn: null,
  requestedTimeOut: null,
  requestedRemarks: '',
  reason: '',
}

export function MyAttendancePage() {
  const [records, setRecords] = useState<PagedResult<AttendanceRecordListItem> | null>(null)
  const [requests, setRequests] = useState<AttendanceAdjustmentRequest[]>([])
  const [query, setQuery] = useState<AttendanceRecordListQuery>(defaultQuery)
  const [editor, setEditor] = useState<SaveAttendanceAdjustmentRequestInput>(defaultRequestEditor)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [editorOpen, setEditorOpen] = useState(false)

  useEffect(() => {
    void Promise.all([loadAttendance(), loadRequests()])
  }, [query.pageNumber, query.status, query.dateFrom, query.dateTo])

  async function loadAttendance() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getMyAttendance(query)
      setRecords(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadRequests() {
    try {
      const response = await sixramApi.getMyAttendanceAdjustments({ pageNumber: 1, pageSize: 6, descending: true })
      setRequests(response.items)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  function openCorrectionModal(record?: AttendanceRecordListItem) {
    setFieldErrors({})
    setEditor({
      attendanceRecordId: record?.attendanceRecordId ?? null,
      attendanceDate: record?.attendanceDate ?? today,
      requestType: 'incorrect_time_in_out',
      requestedTimeIn: record?.actualTimeIn ? toDateTimeLocalInput(record.actualTimeIn) : null,
      requestedTimeOut: record?.actualTimeOut ? toDateTimeLocalInput(record.actualTimeOut) : null,
      requestedRemarks: record?.remarks ?? '',
      reason: '',
    })
    setEditorOpen(true)
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    try {
      await sixramApi.createMyAttendanceAdjustment(editor)
      setEditorOpen(false)
      await Promise.all([loadAttendance(), loadRequests()])
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCancelRequest(requestId: string) {
    try {
      await sixramApi.cancelMyAttendanceAdjustment(requestId, { remarks: 'Cancelled by employee.' })
      await loadRequests()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My attendance</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Attendance history and corrections</h2>
            <p className="mt-3 text-sm text-slate-500">
              Review daily attendance, late and undertime minutes, then send correction requests when a log needs HR or manager review.
            </p>
          </div>

          <button className="shell-button" onClick={() => openCorrectionModal()} type="button">
            Request correction
          </button>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="grid gap-4 md:grid-cols-4">
          <label className="block space-y-2">
            <span className="shell-label mb-0">From</span>
            <input className="shell-input" onChange={(event) => setQuery((current) => ({ ...current, dateFrom: event.target.value, pageNumber: 1 }))} type="date" value={query.dateFrom ?? ''} />
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">To</span>
            <input className="shell-input" onChange={(event) => setQuery((current) => ({ ...current, dateTo: event.target.value, pageNumber: 1 }))} type="date" value={query.dateTo ?? ''} />
          </label>
          <label className="block space-y-2">
            <span className="shell-label mb-0">Status</span>
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))} value={query.status ?? ''}>
              <option value="">All</option>
              <option value="Present">Present</option>
              <option value="Late">Late</option>
              <option value="Undertime">Undertime</option>
              <option value="Incomplete">Incomplete</option>
              <option value="Absent">Absent</option>
              <option value="On Leave">On Leave</option>
            </select>
          </label>
          <div className="flex items-end">
            <button className="shell-button-secondary w-full" onClick={() => setQuery(defaultQuery)} type="button">
              Reset filters
            </button>
          </div>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Status</th>
                <th>Schedule</th>
                <th>Actual time</th>
                <th>Minutes</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading attendance records...
                  </td>
                </tr>
              ) : !records || records.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No attendance records matched the current filters.
                  </td>
                </tr>
              ) : (
                records.items.map((record) => (
                  <tr key={`${record.attendanceDate}-${record.attendanceRecordId ?? 'synthetic'}`}>
                    <td>
                      <div className="font-semibold text-slate-900">{formatDate(record.attendanceDate)}</div>
                      <div className="mt-1 text-slate-500">{record.remarks || 'No remarks'}</div>
                    </td>
                    <td>
                      <AttendanceStatusBadge status={record.status} />
                    </td>
                    <td className="text-slate-500">
                      <div>{record.workScheduleName || 'No schedule'}</div>
                      <div className="mt-1">{record.shiftName || 'No shift'}</div>
                    </td>
                    <td className="text-slate-500">
                      <div>In: {record.actualTimeIn ? formatDateTime(record.actualTimeIn) : '-'}</div>
                      <div className="mt-1">Out: {record.actualTimeOut ? formatDateTime(record.actualTimeOut) : '-'}</div>
                    </td>
                    <td className="text-slate-500">
                      <div>Late: {formatMinutes(record.lateMinutes)}</div>
                      <div className="mt-1">UT: {formatMinutes(record.undertimeMinutes)}</div>
                    </td>
                    <td className="text-right">
                      <button className="shell-button-secondary px-3 py-2" onClick={() => openCorrectionModal(record)} type="button">
                        Request correction
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {records ? (
          <PaginationControls
            className="mt-5"
            pageNumber={records.pageNumber}
            pageSize={records.pageSize}
            totalCount={records.totalCount}
            totalPages={records.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Corrections</p>
          <h3 className="mt-2 text-2xl font-semibold text-slate-950">Recent attendance requests</h3>
        </div>

        <div className="mt-6 space-y-3">
          {requests.length === 0 ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
              No attendance correction requests yet.
            </div>
          ) : (
            requests.map((request) => (
              <div className="rounded-2xl border border-slate-200 px-4 py-4" key={request.id}>
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <div className="font-semibold text-slate-900">{formatDate(request.attendanceDate)}</div>
                    <div className="mt-1 text-sm text-slate-500">{request.reason}</div>
                    <div className="mt-2 text-sm text-slate-500">
                      Requested: {request.requestedTimeIn ? formatDateTime(request.requestedTimeIn) : '-'} / {request.requestedTimeOut ? formatDateTime(request.requestedTimeOut) : '-'}
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <RequestStatusBadge status={request.status} />
                    {request.status.toLowerCase() === 'pending' ? (
                      <button className="shell-button-secondary px-3 py-2" onClick={() => void handleCancelRequest(request.id)} type="button">
                        Cancel
                      </button>
                    ) : null}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </section>

      <Modal
        description="This request will be reviewed by your assigned approver or HR before any attendance record is changed."
        onClose={() => setEditorOpen(false)}
        open={editorOpen}
        title="Attendance correction request"
      >
        <form className="space-y-5" onSubmit={handleSubmit}>
          <div className="grid gap-5 md:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'attendanceDate')} label="Attendance date">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, attendanceDate: event.target.value }))} type="date" value={editor.attendanceDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'requestType')} label="Request type">
              <select className="shell-select" onChange={(event) => setEditor((current) => ({ ...current, requestType: event.target.value }))} value={editor.requestType}>
                <option value="incorrect_time_in_out">Incorrect time in/out</option>
                <option value="missing_time_in">Missing time in</option>
                <option value="missing_time_out">Missing time out</option>
                <option value="remarks_update">Remarks update</option>
              </select>
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'requestedTimeIn')} label="Requested time in">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, requestedTimeIn: event.target.value || null }))} type="datetime-local" value={editor.requestedTimeIn ?? ''} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'requestedTimeOut')} label="Requested time out">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, requestedTimeOut: event.target.value || null }))} type="datetime-local" value={editor.requestedTimeOut ?? ''} />
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'requestedRemarks')} label="Requested remarks">
            <textarea className="shell-textarea" onChange={(event) => setEditor((current) => ({ ...current, requestedRemarks: event.target.value }))} value={editor.requestedRemarks} />
          </FormField>

          <FormField error={getFieldError(fieldErrors, 'reason')} label="Reason">
            <textarea className="shell-textarea" onChange={(event) => setEditor((current) => ({ ...current, reason: event.target.value }))} value={editor.reason} />
          </FormField>

          <div className="flex justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Submitting...' : 'Submit request'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}

function FormField({ children, error, label }: { children: ReactNode; error?: string | null; label: string }) {
  return (
    <label className="block space-y-2">
      <span className="shell-label mb-0">{label}</span>
      {children}
      {error ? <span className="text-sm text-rose-600">{error}</span> : null}
    </label>
  )
}
