import { useEffect, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { AttendanceStatusBadge } from '../components/AttendanceStatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { DocumentStatusBadge } from '../components/DocumentStatusBadge'
import { LeaveStatusBadge } from '../components/LeaveStatusBadge'
import { Modal } from '../components/Modal'
import type {
  AttendanceRecordListItem,
  DocumentTypeOption,
  EmployeeDetail,
  EmployeeDocument,
  EmployeeDocumentProfile,
  EmployeeLeaveProfile,
  EmployeePayrollProfile,
  EmployeeScheduleAssignmentRecord,
  PagedResult,
} from '../types/models'
import { addDaysToDate, formatDate, formatDateTime, formatMinutes } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'
import { downloadBlob, formatFileSize, openBlobInNewTab } from '../utils/files'
import { formatCurrency } from '../utils/money'

type DocumentEditorState = {
  documentTypeId: string
  title: string
  issueDate: string
  expiryDate: string
  remarks: string
  file: File | null
}

const emptyDocumentEditor: DocumentEditorState = {
  documentTypeId: '',
  title: '',
  issueDate: '',
  expiryDate: '',
  remarks: '',
  file: null,
}

export function EmployeeProfilePage() {
  const { employeeId } = useParams<{ employeeId: string }>()
  const navigate = useNavigate()
  const [employee, setEmployee] = useState<EmployeeDetail | null>(null)
  const [documentProfile, setDocumentProfile] = useState<EmployeeDocumentProfile | null>(null)
  const [leaveProfile, setLeaveProfile] = useState<EmployeeLeaveProfile | null>(null)
  const [payrollProfile, setPayrollProfile] = useState<EmployeePayrollProfile | null>(null)
  const [recentAttendance, setRecentAttendance] = useState<PagedResult<AttendanceRecordListItem> | null>(null)
  const [scheduleAssignments, setScheduleAssignments] = useState<PagedResult<EmployeeScheduleAssignmentRecord> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [documentsError, setDocumentsError] = useState<string | null>(null)
  const [leaveError, setLeaveError] = useState<string | null>(null)
  const [payrollError, setPayrollError] = useState<string | null>(null)
  const [attendanceError, setAttendanceError] = useState<string | null>(null)
  const [documentFieldErrors, setDocumentFieldErrors] = useState<Record<string, string[]>>({})
  const [replaceFieldErrors, setReplaceFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingDocument, setEditingDocument] = useState<EmployeeDocument | null>(null)
  const [documentEditor, setDocumentEditor] = useState<DocumentEditorState>(emptyDocumentEditor)
  const [isSavingDocument, setIsSavingDocument] = useState(false)
  const [replaceOpen, setReplaceOpen] = useState(false)
  const [replaceTarget, setReplaceTarget] = useState<EmployeeDocument | null>(null)
  const [replacementFile, setReplacementFile] = useState<File | null>(null)
  const [isReplacingDocument, setIsReplacingDocument] = useState(false)
  const [archiveTarget, setArchiveTarget] = useState<EmployeeDocument | null>(null)
  const [archiveTo, setArchiveTo] = useState(false)
  const [isUpdatingArchive, setIsUpdatingArchive] = useState(false)
  const [deleteDocumentTarget, setDeleteDocumentTarget] = useState<EmployeeDocument | null>(null)
  const [isDeletingDocument, setIsDeletingDocument] = useState(false)
  const [activeFileActionId, setActiveFileActionId] = useState<string | null>(null)

  useEffect(() => {
    if (!employeeId) {
      return
    }

    void loadEmployeeProfile(employeeId)
  }, [employeeId])

  async function loadEmployeeProfile(id: string) {
    setIsLoading(true)

    try {
      const employeeResponse = await sixramApi.getEmployeeById(id)
      setEmployee(employeeResponse)
      setError(null)
      await Promise.all([loadDocuments(id), loadAttendanceOverview(id), loadLeaveProfile(id), loadPayrollProfile(id)])
    } catch (caughtError) {
      setEmployee(null)
      setDocumentProfile(null)
      setLeaveProfile(null)
      setPayrollProfile(null)
      setRecentAttendance(null)
      setScheduleAssignments(null)
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function loadDocuments(id: string) {
    try {
      const response = await sixramApi.getEmployeeDocumentProfile(id)
      setDocumentProfile(response)
      setDocumentsError(null)
    } catch (caughtError) {
      setDocumentProfile(null)
      setDocumentsError(formatError(caughtError))
    }
  }

  async function loadAttendanceOverview(id: string) {
    const endDate = new Date().toISOString().slice(0, 10)
    const startDate = addDaysToDate(endDate, -13)

    try {
      const [attendanceResponse, assignmentResponse] = await Promise.all([
        sixramApi.getAttendanceRecords({
          employeeId: id,
          dateFrom: startDate,
          dateTo: endDate,
          pageNumber: 1,
          pageSize: 10,
          sortBy: 'date',
          descending: true,
        }),
        sixramApi.getScheduleAssignments({
          employeeId: id,
          pageNumber: 1,
          pageSize: 5,
          sortBy: 'start',
          descending: true,
        }),
      ])

      setRecentAttendance(attendanceResponse)
      setScheduleAssignments(assignmentResponse)
      setAttendanceError(null)
    } catch (caughtError) {
      setRecentAttendance(null)
      setScheduleAssignments(null)
      setAttendanceError(formatError(caughtError))
    }
  }

  async function loadLeaveProfile(id: string) {
    try {
      const response = await sixramApi.getEmployeeLeaveProfile(id)
      setLeaveProfile(response)
      setLeaveError(null)
    } catch (caughtError) {
      setLeaveProfile(null)
      setLeaveError(formatError(caughtError))
    }
  }

  async function loadPayrollProfile(id: string) {
    try {
      const response = await sixramApi.getEmployeePayrollProfile(id)
      setPayrollProfile(response)
      setPayrollError(null)
    } catch (caughtError) {
      setPayrollProfile(null)
      setPayrollError(formatError(caughtError))
    }
  }

  async function handleDelete() {
    if (!employee) {
      return
    }

    setIsDeleting(true)

    try {
      await sixramApi.deleteEmployee(employee.id)
      navigate('/admin/employees', { replace: true })
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
      setDeleteOpen(false)
    }
  }

  function openCreateDocumentModal() {
    const defaultTypeId = documentProfile?.availableDocumentTypes[0]?.id ?? ''
    setEditingDocument(null)
    setDocumentEditor({
      ...emptyDocumentEditor,
      documentTypeId: defaultTypeId,
    })
    setDocumentFieldErrors({})
    setEditorOpen(true)
  }

  function openEditDocumentModal(document: EmployeeDocument) {
    setEditingDocument(document)
    setDocumentEditor({
      documentTypeId: document.documentTypeId,
      title: document.title,
      issueDate: document.issueDate ?? '',
      expiryDate: document.expiryDate ?? '',
      remarks: document.remarks,
      file: null,
    })
    setDocumentFieldErrors({})
    setEditorOpen(true)
  }

  function openReplaceModal(document: EmployeeDocument) {
    setReplaceTarget(document)
    setReplacementFile(null)
    setReplaceFieldErrors({})
    setReplaceOpen(true)
  }

  async function handleSaveDocument(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!employeeId) {
      return
    }

    setIsSavingDocument(true)
    setDocumentsError(null)
    setDocumentFieldErrors({})

    try {
      if (editingDocument) {
        await sixramApi.updateEmployeeDocument(employeeId, editingDocument.id, {
          documentTypeId: documentEditor.documentTypeId || null,
          title: documentEditor.title,
          issueDate: documentEditor.issueDate || null,
          expiryDate: documentEditor.expiryDate || null,
          remarks: documentEditor.remarks,
        })
      } else {
        const formData = new FormData()
        formData.append('documentTypeId', documentEditor.documentTypeId)
        formData.append('title', documentEditor.title)
        formData.append('remarks', documentEditor.remarks)

        if (documentEditor.issueDate) {
          formData.append('issueDate', documentEditor.issueDate)
        }

        if (documentEditor.expiryDate) {
          formData.append('expiryDate', documentEditor.expiryDate)
        }

        if (documentEditor.file) {
          formData.append('file', documentEditor.file)
        }

        await sixramApi.createEmployeeDocument(employeeId, formData)
      }

      setEditorOpen(false)
      await loadDocuments(employeeId)
    } catch (caughtError) {
      setDocumentsError(formatError(caughtError))
      setDocumentFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingDocument(false)
    }
  }

  async function handleReplaceDocument(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!employeeId || !replaceTarget) {
      return
    }

    const formData = new FormData()
    if (replacementFile) {
      formData.append('file', replacementFile)
    }

    setIsReplacingDocument(true)
    setDocumentsError(null)
    setReplaceFieldErrors({})

    try {
      await sixramApi.replaceEmployeeDocumentFile(employeeId, replaceTarget.id, formData)
      setReplaceOpen(false)
      await loadDocuments(employeeId)
    } catch (caughtError) {
      setDocumentsError(formatError(caughtError))
      setReplaceFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsReplacingDocument(false)
    }
  }

  async function handleArchiveToggle() {
    if (!employeeId || !archiveTarget) {
      return
    }

    setIsUpdatingArchive(true)

    try {
      await sixramApi.setEmployeeDocumentArchiveState(employeeId, archiveTarget.id, {
        isArchived: archiveTo,
      })
      setArchiveTarget(null)
      await loadDocuments(employeeId)
    } catch (caughtError) {
      setDocumentsError(formatError(caughtError))
    } finally {
      setIsUpdatingArchive(false)
    }
  }

  async function handleDeleteDocument() {
    if (!employeeId || !deleteDocumentTarget) {
      return
    }

    setIsDeletingDocument(true)

    try {
      await sixramApi.deleteEmployeeDocument(employeeId, deleteDocumentTarget.id)
      setDeleteDocumentTarget(null)
      await loadDocuments(employeeId)
    } catch (caughtError) {
      setDocumentsError(formatError(caughtError))
    } finally {
      setIsDeletingDocument(false)
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
      setDocumentsError(formatError(caughtError))
    } finally {
      setActiveFileActionId(null)
    }
  }

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading employee profile...</div>
  }

  if (!employee) {
    return (
      <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
        {error ?? 'Employee profile not found.'}
      </div>
    )
  }

  const organizationCards = [
    { label: 'Department', value: employee.departmentName || '-', active: employee.departmentIsActive },
    { label: 'Position', value: employee.positionName || '-', active: employee.positionIsActive },
    { label: 'Branch', value: employee.branchName || '-', active: employee.branchIsActive },
    { label: 'Employment Type', value: employee.employmentTypeName || '-', active: employee.employmentTypeIsActive },
    { label: 'Employment Status', value: employee.employmentStatusName || '-', active: employee.employmentStatusIsActive },
  ]

  const documentSummary = documentProfile?.summary
  const documents = documentProfile?.documents ?? []
  const documentTypeOptions = getDocumentTypeOptions(documentProfile, editingDocument)
  const selectedDocumentType = documentTypeOptions.find((option) => option.id === documentEditor.documentTypeId)
  const activeScheduleAssignment = scheduleAssignments?.items.find((record) => record.isActive) ?? scheduleAssignments?.items[0] ?? null

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-5 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <span className="shell-badge-brand">{employee.employeeCode}</span>
              <span className={employee.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                {employee.isActive ? 'Active Profile' : 'Inactive Profile'}
              </span>
              {documentSummary?.hasIssues ? <span className="shell-badge-danger">Needs attention</span> : null}
            </div>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">{employee.fullName}</h2>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Employee master profile covering identity details, contact data, organization placement, compliance IDs,
              and document readiness.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="shell-button-secondary" to="/admin/employees">
              Back to List
            </Link>
            <Link className="shell-button" to={`/admin/employees/${employee.id}/edit`}>
              Edit Profile
            </Link>
            <button className="shell-button-danger" onClick={() => setDeleteOpen(true)} type="button">
              Delete
            </button>
          </div>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
        ) : null}

        <div className="mt-6 grid gap-4 lg:grid-cols-3">
          {organizationCards.map((card) => (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4" key={card.label}>
              <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{card.label}</p>
              <p className="mt-2 text-sm font-semibold text-slate-900">{card.value}</p>
              <p className="mt-1 text-xs text-slate-500">{card.active ? 'Active reference' : 'Inactive reference'}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <div className="shell-card p-6 sm:p-7">
          <h3 className="text-xl font-semibold text-slate-950">Personal Information</h3>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <DetailItem label="First Name" value={employee.firstName} />
            <DetailItem label="Middle Name" value={employee.middleName || '-'} />
            <DetailItem label="Last Name" value={employee.lastName} />
            <DetailItem label="Suffix" value={employee.suffix || '-'} />
            <DetailItem label="Gender" value={employee.gender} />
            <DetailItem label="Birthdate" value={formatDate(employee.birthDate)} />
            <DetailItem label="Civil Status" value={employee.civilStatus || '-'} />
            <DetailItem label="Nationality" value={employee.nationality || '-'} />
          </dl>
        </div>

        <div className="shell-card p-6 sm:p-7">
          <h3 className="text-xl font-semibold text-slate-950">Contact Information</h3>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <DetailItem label="Mobile Number" value={employee.mobileNumber || '-'} />
            <DetailItem label="Email" value={employee.email || '-'} />
            <DetailItem className="sm:col-span-2" label="Address" value={employee.address || '-'} />
            <DetailItem label="City / Province" value={employee.cityProvince || '-'} />
            <DetailItem label="ZIP / Postal Code" value={employee.postalCode || '-'} />
            <DetailItem label="Emergency Contact" value={employee.emergencyContactName || '-'} />
            <DetailItem label="Relationship" value={employee.emergencyContactRelationship || '-'} />
            <DetailItem label="Emergency Phone" value={employee.emergencyContactPhone || '-'} />
          </dl>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <div className="shell-card p-6 sm:p-7">
          <h3 className="text-xl font-semibold text-slate-950">Employment Information</h3>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <DetailItem label="Reporting Manager" value={employee.managerName || '-'} />
            <DetailItem label="Work Schedule" value={employee.workSchedule || '-'} />
            <DetailItem label="Date Hired" value={formatDate(employee.dateHired)} />
            <DetailItem label="Date Regularized" value={formatDate(employee.dateRegularized)} />
            <DetailItem label="Date Separated" value={formatDate(employee.dateSeparated)} />
          </dl>
        </div>

        <div className="shell-card p-6 sm:p-7">
          <h3 className="text-xl font-semibold text-slate-950">Government / Compliance</h3>
          <dl className="mt-5 grid gap-3 sm:grid-cols-2">
            <DetailItem label="SSS" value={employee.sssNumber || '-'} />
            <DetailItem label="PhilHealth" value={employee.philHealthNumber || '-'} />
            <DetailItem label="Pag-IBIG" value={employee.pagIbigNumber || '-'} />
            <DetailItem label="TIN" value={employee.tinNumber || '-'} />
            <DetailItem className="sm:col-span-2" label="Other ID" value={employee.otherGovernmentId || '-'} />
          </dl>
        </div>
      </section>

      <section className="shell-card p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h3 className="text-xl font-semibold text-slate-950">Attendance Snapshot</h3>
            <p className="mt-2 text-sm text-slate-500">
              Current schedule assignment and the most recent attendance history available for this employee.
            </p>
          </div>
          <Link className="shell-button-secondary" to="/admin/attendance">
            Open Attendance
          </Link>
        </div>

        {attendanceError ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {attendanceError}
          </div>
        ) : null}

        <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">Current Assignment</p>
            {activeScheduleAssignment ? (
              <div className="mt-4 space-y-3">
                <div>
                  <p className="text-lg font-semibold text-slate-950">{activeScheduleAssignment.workScheduleName}</p>
                  <p className="mt-1 text-sm text-slate-500">
                    {activeScheduleAssignment.workScheduleType}
                    {activeScheduleAssignment.shiftName ? ` | ${activeScheduleAssignment.shiftName}` : ' | No shift'}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <span className={activeScheduleAssignment.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                    {activeScheduleAssignment.isActive ? 'Active assignment' : 'Inactive assignment'}
                  </span>
                  {!activeScheduleAssignment.workScheduleIsActive ? <span className="shell-badge-muted">Inactive schedule</span> : null}
                  {activeScheduleAssignment.shiftId && !activeScheduleAssignment.shiftIsActive ? <span className="shell-badge-muted">Inactive shift</span> : null}
                </div>
                <dl className="grid gap-3 sm:grid-cols-2">
                  <DetailItem label="Effective Start" value={formatDate(activeScheduleAssignment.effectiveStartDate)} />
                  <DetailItem label="Effective End" value={formatDate(activeScheduleAssignment.effectiveEndDate)} />
                  <DetailItem label="Rest Days" value={activeScheduleAssignment.restDayLabels.join(', ') || 'None'} />
                  <DetailItem label="Total Assignments" value={String(scheduleAssignments?.totalCount ?? 0)} />
                </dl>
              </div>
            ) : (
              <div className="mt-4 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                No schedule assignment has been configured for this employee yet.
              </div>
            )}
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white">
            <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
              <div>
                <p className="text-sm font-semibold text-slate-900">Recent attendance</p>
                <p className="mt-1 text-sm text-slate-500">Latest 10 records from the last two weeks.</p>
              </div>
              <span className="shell-badge-muted">{recentAttendance?.totalCount ?? 0} rows</span>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full text-left text-sm">
                <thead className="border-b border-slate-200 bg-slate-50 text-slate-500">
                  <tr>
                    <th className="px-5 py-3 font-semibold">Date</th>
                    <th className="px-5 py-3 font-semibold">Status</th>
                    <th className="px-5 py-3 font-semibold">Worked</th>
                    <th className="px-5 py-3 font-semibold">Logs</th>
                  </tr>
                </thead>
                <tbody>
                  {recentAttendance?.items.length ? (
                    recentAttendance.items.map((record) => (
                      <tr className="border-b border-slate-100 last:border-b-0" key={`${record.employeeId}-${record.attendanceDate}`}>
                        <td className="px-5 py-4 text-slate-600">{formatDate(record.attendanceDate)}</td>
                        <td className="px-5 py-4">
                          <AttendanceStatusBadge status={record.status} />
                        </td>
                        <td className="px-5 py-4 text-slate-600">
                          <div>{formatMinutes(record.totalWorkedMinutes)}</div>
                          <div className="mt-1 text-xs text-slate-500">
                            Late {formatMinutes(record.lateMinutes)} | Under {formatMinutes(record.undertimeMinutes)}
                          </div>
                        </td>
                        <td className="px-5 py-4 text-slate-600">
                          <div>{record.actualTimeIn ? formatDateTime(record.actualTimeIn) : 'No time in'}</div>
                          <div className="mt-1 text-xs text-slate-500">
                            {record.actualTimeOut ? formatDateTime(record.actualTimeOut) : 'No time out'}
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td className="px-5 py-4 text-slate-500" colSpan={4}>
                        No recent attendance records are available for this employee yet.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </section>

      <section className="shell-card p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h3 className="text-xl font-semibold text-slate-950">Documents & Compliance</h3>
            <p className="mt-2 text-sm text-slate-500">
              Upload, replace, archive, and review documents tied to this employee profile.
            </p>
          </div>

          <button className="shell-button" onClick={openCreateDocumentModal} type="button">
            Upload Document
          </button>
        </div>

        {documentsError ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {documentsError}
          </div>
        ) : null}

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Total documents" value={String(documentSummary?.totalDocuments ?? 0)} />
          <SummaryCard label="Missing required" value={String(documentSummary?.missingRequiredDocuments ?? 0)} tone="danger" />
          <SummaryCard label="Expired" value={String(documentSummary?.expiredDocuments ?? 0)} tone="danger" />
          <SummaryCard label="Expiring soon" value={String(documentSummary?.expiringSoonDocuments ?? 0)} tone="warning" />
        </div>

        {documentProfile?.missingRequiredDocuments.length ? (
          <div className="mt-6 rounded-2xl border border-amber-200 bg-amber-50 px-5 py-4">
            <p className="text-sm font-semibold text-amber-900">Missing required document types</p>
            <div className="mt-3 flex flex-wrap gap-2">
              {documentProfile.missingRequiredDocuments.map((record) => (
                <span className="inline-flex rounded-full bg-white px-3 py-1 text-xs font-semibold text-amber-800" key={record.documentTypeId}>
                  {record.name}
                </span>
              ))}
            </div>
          </div>
        ) : null}

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Document</th>
                <th>Status</th>
                <th>Dates</th>
                <th>Uploaded</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {!documentProfile ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    Document details are unavailable right now.
                  </td>
                </tr>
              ) : documents.length ? (
                documents.map((document) => {
                  const isFileBusy = activeFileActionId === document.id

                  return (
                    <tr key={document.id}>
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
                          {!document.documentTypeIsActive ? <span className="shell-badge-muted">Inactive type</span> : null}
                        </div>
                      </td>
                      <td className="text-slate-600">
                        <div>Issue: {formatDate(document.issueDate)}</div>
                        <div className="mt-1">Expiry: {formatDate(document.expiryDate)}</div>
                      </td>
                      <td className="text-slate-600">
                        <div>{document.uploadedByDisplayName || document.uploadedByEmail || 'System'}</div>
                        <div className="mt-1">{formatDateTime(document.createdAtUtc)}</div>
                      </td>
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
                          <button className="shell-button-secondary px-3 py-2" onClick={() => openEditDocumentModal(document)} type="button">
                            Edit
                          </button>
                          <button className="shell-button-secondary px-3 py-2" onClick={() => openReplaceModal(document)} type="button">
                            Replace
                          </button>
                          <button
                            className="shell-button-secondary px-3 py-2"
                            onClick={() => {
                              setArchiveTarget(document)
                              setArchiveTo(!document.isArchived)
                            }}
                            type="button"
                          >
                            {document.isArchived ? 'Unarchive' : 'Archive'}
                          </button>
                          <button className="shell-button-danger px-3 py-2" onClick={() => setDeleteDocumentTarget(document)} type="button">
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    No documents uploaded yet for this employee.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="shell-card p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h3 className="text-xl font-semibold text-slate-950">Leave Snapshot</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review current leave balances, pending requests, and recent leave history for this employee.
            </p>
          </div>

          <Link className="shell-button-secondary" to="/admin/leave">
            Open Leave Management
          </Link>
        </div>

        {leaveError ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {leaveError}
          </div>
        ) : null}

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Pending requests" value={String(leaveProfile?.summary.pendingRequestCount ?? 0)} />
          <SummaryCard label="Approved history" value={String(leaveProfile?.summary.approvedRequestCount ?? 0)} />
          <SummaryCard label="Low balances" value={String(leaveProfile?.summary.lowBalanceCount ?? 0)} tone="warning" />
          <SummaryCard label="Negative balances" value={String(leaveProfile?.summary.negativeBalanceCount ?? 0)} tone="danger" />
        </div>

        <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-950">Balances</p>
              <span className="shell-badge-muted">{leaveProfile?.balances.length ?? 0} rows</span>
            </div>
            <div className="mt-4 space-y-3">
              {leaveProfile?.balances.length ? (
                leaveProfile.balances.map((balance) => (
                  <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={balance.id}>
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-900">{balance.leaveTypeName}</p>
                        <p className="mt-1 text-xs text-slate-500">
                          Used {formatLeaveNumber(balance.used)} | Pending {formatLeaveNumber(balance.pending)}
                        </p>
                      </div>
                      <span className={balance.isNegativeBalance ? 'shell-badge-danger' : balance.isLowBalance ? 'shell-badge-warning' : 'shell-badge-success'}>
                        {formatLeaveNumber(balance.availableBalance)}
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                  No leave balances are available for this employee yet.
                </div>
              )}
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-950">Requests & Ledger</p>
              <span className="shell-badge-muted">{(leaveProfile?.pendingRequests.length ?? 0) + (leaveProfile?.history.length ?? 0)} entries</span>
            </div>
            <div className="mt-4 space-y-3">
              {leaveProfile?.pendingRequests.map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.leaveTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        {formatDate(record.startDate)} - {formatDate(record.endDate)} | {formatLeaveDays(record.totalLeaveDays)}
                      </p>
                    </div>
                    <LeaveStatusBadge status={record.status} />
                  </div>
                </div>
              ))}

              {leaveProfile?.history.slice(0, 3).map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.leaveTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        {formatDate(record.startDate)} - {formatDate(record.endDate)} | {formatLeaveDays(record.totalLeaveDays)}
                      </p>
                    </div>
                    <LeaveStatusBadge status={record.status} />
                  </div>
                </div>
              ))}

              {leaveProfile?.ledger.slice(0, 3).map((entry) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={entry.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{entry.transactionType}</p>
                      <p className="mt-1 text-xs text-slate-500">{entry.remarks || 'No remarks provided.'}</p>
                    </div>
                    <div className="text-right text-xs text-slate-500">
                      <p>{formatLeaveNumber(entry.amount)}</p>
                      <p className="mt-1">{formatDateTime(entry.createdAtUtc)}</p>
                    </div>
                  </div>
                </div>
              ))}

              {!leaveProfile?.pendingRequests.length && !leaveProfile?.history.length && !leaveProfile?.ledger.length ? (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                  No leave requests or ledger activity are available for this employee yet.
                </div>
              ) : null}
            </div>
          </div>
        </div>
      </section>

      <section className="shell-card p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h3 className="text-xl font-semibold text-slate-950">Payroll Snapshot</h3>
            <p className="mt-2 text-sm text-slate-500">
              Review compensation history, recurring payroll components, and recent payroll results for this employee.
            </p>
          </div>

          <Link className="shell-button-secondary" to="/admin/payroll/compensation">
            Open Compensation
          </Link>
        </div>

        {payrollError ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {payrollError}
          </div>
        ) : null}

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Profiles" value={String(payrollProfile?.compensationProfiles.length ?? 0)} />
          <SummaryCard label="Recurring earnings" value={String(payrollProfile?.recurringEarnings.length ?? 0)} />
          <SummaryCard label="Recurring deductions" value={String(payrollProfile?.recurringDeductions.length ?? 0)} />
          <SummaryCard label="Payroll history" value={String(payrollProfile?.payrollHistory.length ?? 0)} tone="warning" />
        </div>

        <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-950">Compensation</p>
              <span className="shell-badge-muted">{payrollProfile?.compensationProfiles.length ?? 0} records</span>
            </div>
            <div className="mt-4 space-y-3">
              {payrollProfile?.compensationProfiles.length ? (
                payrollProfile.compensationProfiles.slice(0, 4).map((record) => (
                  <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.basicSalary, record.currency)}</p>
                        <p className="mt-1 text-xs text-slate-500">{record.payType} | {record.payFrequency.replace('_', ' ')}</p>
                      </div>
                      <div className="text-right text-xs text-slate-500">
                        <p>{formatDate(record.effectiveStartDate)}</p>
                        <p className="mt-1">{record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</p>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                  No compensation profiles are available for this employee yet.
                </div>
              )}
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-950">Recurring Components & History</p>
              <span className="shell-badge-muted">
                {(payrollProfile?.recurringEarnings.length ?? 0) + (payrollProfile?.recurringDeductions.length ?? 0)} active items
              </span>
            </div>
            <div className="mt-4 space-y-3">
              {payrollProfile?.recurringEarnings.slice(0, 2).map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.earningTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">Recurring earning</p>
                    </div>
                    <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.amount)}</p>
                  </div>
                </div>
              ))}

              {payrollProfile?.recurringDeductions.slice(0, 2).map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.deductionTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">Recurring deduction</p>
                    </div>
                    <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.amount)}</p>
                  </div>
                </div>
              ))}

              {payrollProfile?.payrollHistory.slice(0, 2).map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3" key={record.id}>
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.status}</p>
                      <p className="mt-1 text-xs text-slate-500">{formatDateTime(record.createdAtUtc)}</p>
                    </div>
                    <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.netPay, record.currency)}</p>
                  </div>
                </div>
              ))}

              {!payrollProfile?.recurringEarnings.length && !payrollProfile?.recurringDeductions.length && !payrollProfile?.payrollHistory.length ? (
                <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                  No payroll activity is available for this employee yet.
                </div>
              ) : null}
            </div>
          </div>
        </div>
      </section>

      <section className="shell-card p-6 sm:p-7">
        <h3 className="text-xl font-semibold text-slate-950">System Link</h3>
        <dl className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <DetailItem label="Linked User Display Name" value={employee.linkedUserDisplayName || '-'} />
          <DetailItem label="Linked User Email" value={employee.linkedUserEmail || '-'} />
          <DetailItem label="Created" value={formatDateTime(employee.createdAtUtc)} />
          <DetailItem label="Updated" value={employee.updatedAtUtc ? formatDateTime(employee.updatedAtUtc) : '-'} />
        </dl>
      </section>

      <Modal
        description={
          editingDocument
            ? 'Update the document type, title, dates, and remarks.'
            : 'Upload a new employee document with compliance metadata.'
        }
        onClose={() => {
          if (!isSavingDocument) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingDocument ? `Edit ${editingDocument.title}` : 'Upload Document'}
      >
        <form className="space-y-5" onSubmit={handleSaveDocument}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(documentFieldErrors, 'DocumentTypeId', 'documentTypeId')} label="Document Type">
              <select
                className="shell-select"
                onChange={(event) => setDocumentEditor((current) => ({ ...current, documentTypeId: event.target.value }))}
                value={documentEditor.documentTypeId}
              >
                <option value="">Select document type</option>
                {documentTypeOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                    {!option.isActive ? ' (Inactive)' : ''}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField error={getFieldError(documentFieldErrors, 'Title', 'title')} label="Document Title">
              <input
                className="shell-input"
                onChange={(event) => setDocumentEditor((current) => ({ ...current, title: event.target.value }))}
                value={documentEditor.title}
              />
            </FormField>
          </div>

          {!editingDocument ? (
            <FormField error={getFieldError(documentFieldErrors, 'File', 'file')} label="Attachment">
              <input
                accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
                className="shell-input file:mr-4 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-700"
                onChange={(event) =>
                  setDocumentEditor((current) => ({
                    ...current,
                    file: event.target.files?.[0] ?? null,
                  }))
                }
                type="file"
              />
            </FormField>
          ) : null}

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(documentFieldErrors, 'IssueDate', 'issueDate')} label="Issue Date">
              <input
                className="shell-input"
                onChange={(event) => setDocumentEditor((current) => ({ ...current, issueDate: event.target.value }))}
                type="date"
                value={documentEditor.issueDate}
              />
            </FormField>

            <FormField error={getFieldError(documentFieldErrors, 'ExpiryDate', 'expiryDate')} label="Expiry Date">
              <input
                className="shell-input"
                onChange={(event) => setDocumentEditor((current) => ({ ...current, expiryDate: event.target.value }))}
                type="date"
                value={documentEditor.expiryDate}
              />
            </FormField>
          </div>

          {selectedDocumentType?.requiresExpiryDate ? (
            <p className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              This document type requires an expiry date.
            </p>
          ) : null}

          <FormField error={getFieldError(documentFieldErrors, 'Remarks', 'remarks')} label="Remarks">
            <textarea
              className="shell-textarea"
              onChange={(event) => setDocumentEditor((current) => ({ ...current, remarks: event.target.value }))}
              value={documentEditor.remarks}
            />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSavingDocument} type="submit">
              {isSavingDocument ? 'Saving...' : editingDocument ? 'Save Changes' : 'Upload Document'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Replace the existing file while keeping the current document metadata."
        onClose={() => {
          if (!isReplacingDocument) {
            setReplaceOpen(false)
          }
        }}
        open={replaceOpen}
        title={replaceTarget ? `Replace ${replaceTarget.title}` : 'Replace Document'}
      >
        <form className="space-y-5" onSubmit={handleReplaceDocument}>
          <FormField error={getFieldError(replaceFieldErrors, 'File', 'file')} label="New Attachment">
            <input
              accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
              className="shell-input file:mr-4 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-700"
              onChange={(event) => setReplacementFile(event.target.files?.[0] ?? null)}
              type="file"
            />
          </FormField>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setReplaceOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isReplacingDocument} type="submit">
              {isReplacingDocument ? 'Replacing...' : 'Replace File'}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel={archiveTo ? 'Archive Document' : 'Unarchive Document'}
        description={
          archiveTarget
            ? archiveTo
              ? `Archive ${archiveTarget.title}? Archived documents stay visible but will no longer count toward compliance.`
              : `Restore ${archiveTarget.title}? It will count toward compliance again.`
            : ''
        }
        isBusy={isUpdatingArchive}
        onCancel={() => {
          if (!isUpdatingArchive) {
            setArchiveTarget(null)
          }
        }}
        onConfirm={() => void handleArchiveToggle()}
        open={Boolean(archiveTarget)}
        title={archiveTo ? 'Archive Document' : 'Unarchive Document'}
      />

      <ConfirmDialog
        confirmLabel="Delete Document"
        description={
          deleteDocumentTarget
            ? `Delete ${deleteDocumentTarget.title}? This removes the file and its metadata from the employee record.`
            : ''
        }
        isBusy={isDeletingDocument}
        onCancel={() => {
          if (!isDeletingDocument) {
            setDeleteDocumentTarget(null)
          }
        }}
        onConfirm={() => void handleDeleteDocument()}
        open={Boolean(deleteDocumentTarget)}
        title={deleteDocumentTarget ? `Delete ${deleteDocumentTarget.title}` : 'Delete Document'}
      />

      <ConfirmDialog
        confirmLabel="Delete Employee"
        description={`Delete employee profile ${employee.fullName} (${employee.employeeCode})? This removes the master profile, linked employee documents, and clears manager links from direct reports.`}
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteOpen(false)
          }
        }}
        onConfirm={() => void handleDelete()}
        open={deleteOpen}
        title={`Delete ${employee.fullName}`}
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

function DetailItem({
  label,
  value,
  className = '',
}: {
  label: string
  value: string
  className?: string
}) {
  return (
    <div className={['rounded-xl border border-slate-200 bg-white p-4', className].join(' ')}>
      <dt className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</dt>
      <dd className="mt-2 text-sm font-medium text-slate-900">{value}</dd>
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

function getDocumentTypeOptions(
  documentProfile: EmployeeDocumentProfile | null,
  editingDocument: EmployeeDocument | null,
): DocumentTypeOption[] {
  const options = [...(documentProfile?.availableDocumentTypes ?? [])]

  if (
    editingDocument &&
    !options.some((option) => option.id === editingDocument.documentTypeId)
  ) {
    options.push({
      id: editingDocument.documentTypeId,
      code: editingDocument.documentTypeCode,
      name: editingDocument.documentTypeName,
      requiresExpiryDate: editingDocument.documentTypeRequiresExpiryDate,
      isRequired: editingDocument.documentTypeIsRequired,
      isActive: editingDocument.documentTypeIsActive,
    })
  }

  return options.sort((left, right) => left.name.localeCompare(right.name))
}

function formatLeaveDays(value: number) {
  const normalized = Number.isInteger(value) ? String(value) : value.toFixed(1)
  return `${normalized} day${value === 1 ? '' : 's'}`
}

function formatLeaveNumber(value: number) {
  return Number.isInteger(value) ? `${value}` : value.toFixed(1)
}
