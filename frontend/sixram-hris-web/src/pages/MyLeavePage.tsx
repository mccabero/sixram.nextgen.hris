import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  EmployeeLeaveProfile,
  LeaveManagementOptions,
  LeaveRequest,
  LeaveRequestListQuery,
  PagedResult,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'
import { downloadBlob, formatFileSize } from '../utils/files'

type LeaveEditorState = {
  employeeId: string
  leaveTypeId: string
  startDate: string
  endDate: string
  startDayType: string
  endDayType: string
  reason: string
  attachment: File | null
}

const defaultQuery: LeaveRequestListQuery = {
  status: '',
  dateFrom: '',
  dateTo: '',
  pageNumber: 1,
  pageSize: 8,
  sortBy: 'submitted',
  descending: true,
}

const emptyEditor: LeaveEditorState = {
  employeeId: '',
  leaveTypeId: '',
  startDate: '',
  endDate: '',
  startDayType: 'full_day',
  endDayType: 'full_day',
  reason: '',
  attachment: null,
}

export function MyLeavePage() {
  const [profile, setProfile] = useState<EmployeeLeaveProfile | null>(null)
  const [options, setOptions] = useState<LeaveManagementOptions | null>(null)
  const [requests, setRequests] = useState<PagedResult<LeaveRequest> | null>(null)
  const [query, setQuery] = useState<LeaveRequestListQuery>(defaultQuery)
  const [selectedYear, setSelectedYear] = useState<number>(new Date().getFullYear())
  const [editor, setEditor] = useState<LeaveEditorState>(emptyEditor)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [editorOpen, setEditorOpen] = useState(false)
  const [activeDownloadId, setActiveDownloadId] = useState<string | null>(null)

  useEffect(() => {
    void loadBootstrap()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadProfileAndRequests(selectedYear, query)
  }, [options, selectedYear, query.pageNumber, query.status, query.dateFrom, query.dateTo])

  async function loadBootstrap() {
    setIsLoading(true)

    try {
      const optionsResponse = await sixramApi.getMyLeaveOptions()
      const initialYear =
        optionsResponse.periodYears.includes(selectedYear)
          ? selectedYear
          : optionsResponse.periodYears[optionsResponse.periodYears.length - 1] ?? new Date().getFullYear()

      setOptions(optionsResponse)
      setSelectedYear(initialYear)
      setEditor((current) => ({
        ...current,
        employeeId: optionsResponse.employees[0]?.id ?? '',
        leaveTypeId: optionsResponse.leaveTypes[0]?.id ?? '',
        startDate: current.startDate || new Date().toISOString().slice(0, 10),
        endDate: current.endDate || new Date().toISOString().slice(0, 10),
      }))

      await loadProfileAndRequests(initialYear, defaultQuery)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadProfileAndRequests(year: number, nextQuery: LeaveRequestListQuery) {
    try {
      const [profileResponse, requestResponse] = await Promise.all([
        sixramApi.getMyLeaveProfile(year),
        sixramApi.getMyLeaveRequests(nextQuery),
      ])

      setProfile(profileResponse)
      setRequests(requestResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  function openRequestModal() {
    const defaultDate = new Date().toISOString().slice(0, 10)
    setFieldErrors({})
    setEditor({
      employeeId: options?.employees[0]?.id ?? '',
      leaveTypeId: options?.leaveTypes[0]?.id ?? '',
      startDate: defaultDate,
      endDate: defaultDate,
      startDayType: 'full_day',
      endDayType: 'full_day',
      reason: '',
      attachment: null,
    })
    setEditorOpen(true)
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    try {
      const formData = new FormData()
      formData.append('employeeId', editor.employeeId)
      formData.append('leaveTypeId', editor.leaveTypeId)
      formData.append('startDate', editor.startDate)
      formData.append('endDate', editor.endDate)
      formData.append('startDayType', editor.startDayType)
      formData.append('endDayType', editor.endDayType)
      formData.append('reason', editor.reason)

      if (editor.attachment) {
        formData.append('attachment', editor.attachment)
      }

      await sixramApi.createMyLeaveRequest(formData)
      setEditorOpen(false)
      await loadProfileAndRequests(selectedYear, query)
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCancelRequest(requestId: string) {
    try {
      await sixramApi.cancelMyLeaveRequest(requestId, { remarks: 'Cancelled by employee.' })
      await loadProfileAndRequests(selectedYear, query)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handleDownloadAttachment(request: LeaveRequest) {
    setActiveDownloadId(request.id)

    try {
      const file = await sixramApi.downloadMyLeaveAttachment(request.id)
      downloadBlob(file.blob, file.fileName)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveDownloadId(null)
    }
  }

  const selectedLeaveType = useMemo(
    () => options?.leaveTypes.find((record) => record.id === editor.leaveTypeId) ?? null,
    [editor.leaveTypeId, options?.leaveTypes],
  )

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading your leave workspace...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My leave</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Balances, requests, and leave history</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Review current balances, submit new leave requests, and track approval outcomes without leaving the employee portal.
            </p>
          </div>

          <button className="shell-button" onClick={openRequestModal} type="button">
            File leave request
          </button>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="grid gap-6 xl:grid-cols-[1fr_1fr_0.9fr]">
        <section className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Summary</p>
              <h3 className="mt-2 text-xl font-semibold text-slate-950">Request overview</h3>
            </div>
            <select
              className="shell-select max-w-[130px]"
              onChange={(event) => setSelectedYear(Number(event.target.value))}
              value={selectedYear}
            >
              {(options?.periodYears ?? []).map((year) => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </div>

          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            <SummaryCard label="Pending" value={String(profile?.summary.pendingRequestCount ?? 0)} />
            <SummaryCard label="Approved" value={String(profile?.summary.approvedRequestCount ?? 0)} />
            <SummaryCard label="Low balances" value={String(profile?.summary.lowBalanceCount ?? 0)} />
            <SummaryCard label="Negative balances" value={String(profile?.summary.negativeBalanceCount ?? 0)} />
          </div>
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Balances</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Available leave credits</h3>
          </div>

          <div className="mt-6 space-y-3">
            {profile?.balances.length ? (
              profile.balances.map((balance) => (
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3" key={balance.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{balance.leaveTypeName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        Used {formatLeaveNumber(balance.used)} | Pending {formatLeaveNumber(balance.pending)}
                      </div>
                    </div>
                    <span className={balance.isNegativeBalance ? 'shell-badge-danger' : balance.isLowBalance ? 'shell-badge-warning' : 'shell-badge-success'}>
                      {formatLeaveNumber(balance.availableBalance)}
                    </span>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                No leave balances are available for the selected year.
              </div>
            )}
          </div>
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Recent history</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Latest approved leave</h3>
          </div>

          <div className="mt-6 space-y-3">
            {profile?.history.length ? (
              profile.history.slice(0, 4).map((record) => (
                <div className="rounded-2xl border border-slate-200 px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{record.leaveTypeName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {formatDate(record.startDate)} to {formatDate(record.endDate)}
                      </div>
                    </div>
                    <LeaveStatusBadge status={record.status} />
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                No leave history is available yet.
              </div>
            )}
          </div>
        </section>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Requests</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">My leave requests</h3>
          </div>

          <div className="grid gap-4 md:grid-cols-4">
            <label className="block space-y-2">
              <span className="shell-label mb-0">From</span>
              <input
                className="shell-input"
                onChange={(event) => setQuery((current) => ({ ...current, dateFrom: event.target.value, pageNumber: 1 }))}
                type="date"
                value={query.dateFrom ?? ''}
              />
            </label>
            <label className="block space-y-2">
              <span className="shell-label mb-0">To</span>
              <input
                className="shell-input"
                onChange={(event) => setQuery((current) => ({ ...current, dateTo: event.target.value, pageNumber: 1 }))}
                type="date"
                value={query.dateTo ?? ''}
              />
            </label>
            <label className="block space-y-2">
              <span className="shell-label mb-0">Status</span>
              <select
                className="shell-select"
                onChange={(event) => setQuery((current) => ({ ...current, status: event.target.value, pageNumber: 1 }))}
                value={query.status ?? ''}
              >
                <option value="">All</option>
                {(options?.statuses ?? []).map((status) => (
                  <option key={status} value={status}>
                    {toTitleLabel(status)}
                  </option>
                ))}
              </select>
            </label>
            <div className="flex items-end">
              <button className="shell-button-secondary w-full" onClick={() => setQuery(defaultQuery)} type="button">
                Reset filters
              </button>
            </div>
          </div>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Leave</th>
                <th>Dates</th>
                <th>Status</th>
                <th>Submitted</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {!requests || requests.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    No leave requests matched the current filters.
                  </td>
                </tr>
              ) : (
                requests.items.map((request) => (
                  <tr key={request.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{request.leaveTypeName}</div>
                      <div className="mt-1 text-slate-500">{request.reason || 'No reason provided.'}</div>
                      {request.decisionRemarks ? <div className="mt-1 text-slate-400">Decision: {request.decisionRemarks}</div> : null}
                    </td>
                    <td className="text-slate-500">
                      <div>
                        {formatDate(request.startDate)} to {formatDate(request.endDate)}
                      </div>
                      <div className="mt-1">{formatLeaveDays(request.totalLeaveDays)}</div>
                    </td>
                    <td>
                      <LeaveStatusBadge status={request.status} />
                    </td>
                    <td className="text-slate-500">{formatDateTime(request.submittedAtUtc ?? request.createdAtUtc)}</td>
                    <td className="text-right">
                      <div className="flex justify-end gap-2">
                        {request.hasAttachment ? (
                          <button
                            className="shell-button-secondary px-3 py-2"
                            disabled={activeDownloadId === request.id}
                            onClick={() => void handleDownloadAttachment(request)}
                            type="button"
                          >
                            {activeDownloadId === request.id ? 'Opening...' : `Attachment${request.attachmentFileSize ? ` (${formatFileSize(request.attachmentFileSize)})` : ''}`}
                          </button>
                        ) : null}
                        {request.status.toLowerCase() === 'pending' ? (
                          <button className="shell-button-secondary px-3 py-2" onClick={() => void handleCancelRequest(request.id)} type="button">
                            Cancel
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {requests ? (
          <PaginationControls
            pageNumber={requests.pageNumber}
            pageSize={requests.pageSize}
            totalCount={requests.totalCount}
            totalPages={requests.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Quick links</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Related self-service areas</h3>
          </div>
          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/me/attendance">
              My attendance
            </Link>
            <Link className="shell-button-secondary" to="/me/requests">
              My requests
            </Link>
          </div>
        </div>
      </section>

      <Modal
        description="Leave requests go through the existing balance, overlap, and attachment validation rules before they are submitted."
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title="File leave request"
      >
        <form className="space-y-5" onSubmit={handleSubmit}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'LeaveTypeId', 'leaveTypeId')} label="Leave type">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, leaveTypeId: event.target.value }))}
                value={editor.leaveTypeId}
              >
                <option value="">Select leave type</option>
                {(options?.leaveTypes ?? []).map((leaveType) => (
                  <option key={leaveType.id} value={leaveType.id}>
                    {leaveType.name}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField label="Employee">
              <input
                className="shell-input"
                disabled
                value={options?.employees[0] ? `${options.employees[0].employeeCode} | ${options.employees[0].fullName}` : ''}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'StartDate', 'startDate')} label="Start date">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, startDate: event.target.value }))}
                type="date"
                value={editor.startDate}
              />
            </FormField>

            <FormField error={getFieldError(fieldErrors, 'EndDate', 'endDate')} label="End date">
              <input
                className="shell-input"
                onChange={(event) => setEditor((current) => ({ ...current, endDate: event.target.value }))}
                type="date"
                value={editor.endDate}
              />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'StartDayType', 'startDayType')} label="Start day type">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, startDayType: event.target.value }))}
                value={editor.startDayType}
              >
                <option value="full_day">Full day</option>
                <option disabled={!selectedLeaveType?.allowHalfDay} value="first_half">
                  First half
                </option>
                <option disabled={!selectedLeaveType?.allowHalfDay} value="second_half">
                  Second half
                </option>
              </select>
            </FormField>

            <FormField error={getFieldError(fieldErrors, 'EndDayType', 'endDayType')} label="End day type">
              <select
                className="shell-select"
                onChange={(event) => setEditor((current) => ({ ...current, endDayType: event.target.value }))}
                value={editor.endDayType}
              >
                <option value="full_day">Full day</option>
                <option disabled={!selectedLeaveType?.allowHalfDay} value="first_half">
                  First half
                </option>
                <option disabled={!selectedLeaveType?.allowHalfDay} value="second_half">
                  Second half
                </option>
              </select>
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'Reason', 'reason')} label="Reason">
            <textarea
              className="shell-textarea"
              onChange={(event) => setEditor((current) => ({ ...current, reason: event.target.value }))}
              rows={4}
              value={editor.reason}
            />
          </FormField>

          <FormField error={getFieldError(fieldErrors, 'Attachment', 'attachment')} label="Attachment">
            <input
              accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
              className="shell-input file:mr-4 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-700"
              onChange={(event) => setEditor((current) => ({ ...current, attachment: event.target.files?.[0] ?? null }))}
              type="file"
            />
          </FormField>

          {selectedLeaveType?.requiresAttachment ? (
            <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              This leave type requires an attachment before submission.
            </div>
          ) : null}

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

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}

function FormField({
  children,
  error,
  label,
}: {
  children: ReactNode
  error?: string | null
  label: string
}) {
  return (
    <label className="block space-y-2">
      <span className="shell-label mb-0">{label}</span>
      {children}
      {error ? <span className="text-sm text-rose-600">{error}</span> : null}
    </label>
  )
}

function formatLeaveDays(value: number) {
  const normalized = Number.isInteger(value) ? String(value) : value.toFixed(1)
  return `${normalized} day${value === 1 ? '' : 's'}`
}

function formatLeaveNumber(value: number) {
  return Number.isInteger(value) ? `${value}` : value.toFixed(1)
}

function toTitleLabel(value: string) {
  return value
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}
