import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PaginationControls } from '../components/PaginationControls'
import type {
  CompensationProfileListQuery,
  CompensationProfileRecord,
  EmployeePayrollProfile,
  EmployeeRecurringDeductionRecord,
  EmployeeRecurringEarningRecord,
  PayrollOptions,
  PagedResult,
  RecurringPayrollComponentListQuery,
  SaveCompensationProfileInput,
  SaveEmployeeRecurringDeductionInput,
  SaveEmployeeRecurringEarningInput,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'
import { formatCurrency } from '../utils/money'

type DeleteState = {
  kind: 'profile' | 'earning' | 'deduction'
  id: string
  label: string
}

const defaultProfileQuery: CompensationProfileListQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  isActive: null,
  pageNumber: 1,
  pageSize: 8,
  sortBy: 'effective_start',
  descending: true,
}

const defaultRecurringQuery: RecurringPayrollComponentListQuery = {
  search: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  isActive: null,
  pageNumber: 1,
  pageSize: 8,
  sortBy: 'effective_start',
  descending: true,
}

const emptyCompensationEditor: SaveCompensationProfileInput = {
  employeeId: '',
  payType: 'monthly',
  payFrequency: 'semi_monthly',
  basicSalary: 0,
  dailyRate: null,
  hourlyRate: null,
  currency: 'PHP',
  effectiveStartDate: '',
  effectiveEndDate: '',
  isActive: true,
  remarks: '',
}

const emptyRecurringEarningEditor: SaveEmployeeRecurringEarningInput = {
  employeeId: '',
  earningTypeId: '',
  amount: 0,
  frequency: 'every_payroll',
  effectiveStartDate: '',
  effectiveEndDate: '',
  isActive: true,
  remarks: '',
}

const emptyRecurringDeductionEditor: SaveEmployeeRecurringDeductionInput = {
  employeeId: '',
  deductionTypeId: '',
  amount: 0,
  frequency: 'every_payroll',
  balance: null,
  effectiveStartDate: '',
  effectiveEndDate: '',
  isActive: true,
  remarks: '',
}

export function PayrollCompensationPage() {
  const [options, setOptions] = useState<PayrollOptions | null>(null)
  const [profiles, setProfiles] = useState<PagedResult<CompensationProfileRecord> | null>(null)
  const [recurringEarnings, setRecurringEarnings] = useState<PagedResult<EmployeeRecurringEarningRecord> | null>(null)
  const [recurringDeductions, setRecurringDeductions] = useState<PagedResult<EmployeeRecurringDeductionRecord> | null>(null)
  const [profileQuery, setProfileQuery] = useState<CompensationProfileListQuery>(defaultProfileQuery)
  const [earningQuery, setEarningQuery] = useState<RecurringPayrollComponentListQuery>(defaultRecurringQuery)
  const [deductionQuery, setDeductionQuery] = useState<RecurringPayrollComponentListQuery>(defaultRecurringQuery)
  const [previewProfile, setPreviewProfile] = useState<EmployeePayrollProfile | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [compensationErrors, setCompensationErrors] = useState<Record<string, string[]>>({})
  const [earningErrors, setEarningErrors] = useState<Record<string, string[]>>({})
  const [deductionErrors, setDeductionErrors] = useState<Record<string, string[]>>({})
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isLoadingProfiles, setIsLoadingProfiles] = useState(false)
  const [isLoadingEarnings, setIsLoadingEarnings] = useState(false)
  const [isLoadingDeductions, setIsLoadingDeductions] = useState(false)
  const [compensationModalOpen, setCompensationModalOpen] = useState(false)
  const [editingCompensation, setEditingCompensation] = useState<CompensationProfileRecord | null>(null)
  const [compensationEditor, setCompensationEditor] = useState<SaveCompensationProfileInput>(emptyCompensationEditor)
  const [earningModalOpen, setEarningModalOpen] = useState(false)
  const [editingRecurringEarning, setEditingRecurringEarning] = useState<EmployeeRecurringEarningRecord | null>(null)
  const [recurringEarningEditor, setRecurringEarningEditor] = useState<SaveEmployeeRecurringEarningInput>(emptyRecurringEarningEditor)
  const [deductionModalOpen, setDeductionModalOpen] = useState(false)
  const [editingRecurringDeduction, setEditingRecurringDeduction] = useState<EmployeeRecurringDeductionRecord | null>(null)
  const [recurringDeductionEditor, setRecurringDeductionEditor] = useState<SaveEmployeeRecurringDeductionInput>(emptyRecurringDeductionEditor)
  const [deleteState, setDeleteState] = useState<DeleteState | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadProfiles()
  }, [options, profileQuery])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadRecurringEarnings()
  }, [options, earningQuery])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadRecurringDeductions()
  }, [options, deductionQuery])

  async function loadInitialData() {
    setIsInitialLoading(true)

    try {
      const optionsResponse = await sixramApi.getPayrollOptions()
      setOptions(optionsResponse)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsInitialLoading(false)
    }
  }

  async function loadProfiles() {
    setIsLoadingProfiles(true)

    try {
      const response = await sixramApi.getCompensationProfiles(profileQuery)
      setProfiles(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingProfiles(false)
    }
  }

  async function loadRecurringEarnings() {
    setIsLoadingEarnings(true)

    try {
      const response = await sixramApi.getRecurringEarnings(earningQuery)
      setRecurringEarnings(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingEarnings(false)
    }
  }

  async function loadRecurringDeductions() {
    setIsLoadingDeductions(true)

    try {
      const response = await sixramApi.getRecurringDeductions(deductionQuery)
      setRecurringDeductions(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingDeductions(false)
    }
  }

  async function refreshAll() {
    await Promise.all([loadProfiles(), loadRecurringEarnings(), loadRecurringDeductions()])
  }

  function openCreateCompensationModal() {
    setEditingCompensation(null)
    setCompensationErrors({})
    setCompensationEditor({
      ...emptyCompensationEditor,
      employeeId: options?.employees[0]?.id ?? '',
      payType: options?.payTypes[0] ?? 'monthly',
      payFrequency: options?.payFrequencies[0] ?? 'semi_monthly',
      effectiveStartDate: new Date().toISOString().slice(0, 10),
    })
    setCompensationModalOpen(true)
  }

  function openEditCompensationModal(record: CompensationProfileRecord) {
    setEditingCompensation(record)
    setCompensationErrors({})
    setCompensationEditor({
      employeeId: record.employeeId,
      payType: record.payType,
      payFrequency: record.payFrequency,
      basicSalary: record.basicSalary,
      dailyRate: record.dailyRate ?? null,
      hourlyRate: record.hourlyRate ?? null,
      currency: record.currency,
      effectiveStartDate: record.effectiveStartDate,
      effectiveEndDate: record.effectiveEndDate ?? '',
      isActive: record.isActive,
      remarks: record.remarks,
    })
    setCompensationModalOpen(true)
  }

  function openCreateEarningModal() {
    setEditingRecurringEarning(null)
    setEarningErrors({})
    setRecurringEarningEditor({
      ...emptyRecurringEarningEditor,
      employeeId: options?.employees[0]?.id ?? '',
      earningTypeId: options?.earningTypes[0]?.id ?? '',
      effectiveStartDate: new Date().toISOString().slice(0, 10),
    })
    setEarningModalOpen(true)
  }

  function openEditEarningModal(record: EmployeeRecurringEarningRecord) {
    setEditingRecurringEarning(record)
    setEarningErrors({})
    setRecurringEarningEditor({
      employeeId: record.employeeId,
      earningTypeId: record.earningTypeId,
      amount: record.amount,
      frequency: record.frequency,
      effectiveStartDate: record.effectiveStartDate,
      effectiveEndDate: record.effectiveEndDate ?? '',
      isActive: record.isActive,
      remarks: record.remarks,
    })
    setEarningModalOpen(true)
  }

  function openCreateDeductionModal() {
    setEditingRecurringDeduction(null)
    setDeductionErrors({})
    setRecurringDeductionEditor({
      ...emptyRecurringDeductionEditor,
      employeeId: options?.employees[0]?.id ?? '',
      deductionTypeId: options?.deductionTypes[0]?.id ?? '',
      effectiveStartDate: new Date().toISOString().slice(0, 10),
    })
    setDeductionModalOpen(true)
  }

  function openEditDeductionModal(record: EmployeeRecurringDeductionRecord) {
    setEditingRecurringDeduction(record)
    setDeductionErrors({})
    setRecurringDeductionEditor({
      employeeId: record.employeeId,
      deductionTypeId: record.deductionTypeId,
      amount: record.amount,
      frequency: record.frequency,
      balance: record.balance ?? null,
      effectiveStartDate: record.effectiveStartDate,
      effectiveEndDate: record.effectiveEndDate ?? '',
      isActive: record.isActive,
      remarks: record.remarks,
    })
    setDeductionModalOpen(true)
  }

  async function handleSaveCompensation(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setCompensationErrors({})
    setError(null)

    try {
      if (editingCompensation) {
        await sixramApi.updateCompensationProfile(editingCompensation.id, compensationEditor)
      } else {
        await sixramApi.createCompensationProfile(compensationEditor)
      }

      setCompensationModalOpen(false)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setCompensationErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleSaveRecurringEarning(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setEarningErrors({})
    setError(null)

    try {
      if (editingRecurringEarning) {
        await sixramApi.updateRecurringEarning(editingRecurringEarning.id, recurringEarningEditor)
      } else {
        await sixramApi.createRecurringEarning(recurringEarningEditor)
      }

      setEarningModalOpen(false)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setEarningErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleSaveRecurringDeduction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setDeductionErrors({})
    setError(null)

    try {
      if (editingRecurringDeduction) {
        await sixramApi.updateRecurringDeduction(editingRecurringDeduction.id, recurringDeductionEditor)
      } else {
        await sixramApi.createRecurringDeduction(recurringDeductionEditor)
      }

      setDeductionModalOpen(false)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setDeductionErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteConfirmed() {
    if (!deleteState) {
      return
    }

    setIsDeleting(true)
    setError(null)

    try {
      if (deleteState.kind === 'profile') {
        await sixramApi.deleteCompensationProfile(deleteState.id)
      } else if (deleteState.kind === 'earning') {
        await sixramApi.deleteRecurringEarning(deleteState.id)
      } else {
        await sixramApi.deleteRecurringDeduction(deleteState.id)
      }

      setDeleteState(null)
      await refreshAll()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  async function handlePreview(employeeId: string) {
    setPreviewLoading(true)
    setError(null)

    try {
      const response = await sixramApi.getEmployeePayrollProfile(employeeId)
      setPreviewProfile(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setPreviewLoading(false)
    }
  }

  if (isInitialLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading compensation workspace...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Compensation Management</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Compensation profiles and recurring components</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Keep salary history, recurring earnings, and recurring deductions aligned with each employee before payroll is generated.
            </p>
          </div>

          <button className="shell-button-secondary" onClick={() => void refreshAll()} type="button">
            Refresh
          </button>
        </div>
      </section>

      {error ? (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Compensation profiles</h4>
            <p className="mt-1 text-sm text-slate-500">Historical salary profiles determine which pay rates apply to a pay period.</p>
          </div>
          <button className="shell-button-secondary" onClick={openCreateCompensationModal} type="button">
            Add Profile
          </button>
        </div>

        <div className="mt-5 grid gap-4 lg:grid-cols-4">
          <FormField label="Search">
            <input className="shell-input" onChange={(event) => setProfileQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))} placeholder="Employee or code" value={profileQuery.search ?? ''} />
          </FormField>
          <FormField label="Employee">
            <select className="shell-select" onChange={(event) => setProfileQuery((current) => ({ ...current, employeeId: event.target.value, pageNumber: 1 }))} value={profileQuery.employeeId ?? ''}>
              <option value="">All employees</option>
              {options?.employees.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.employeeCode} | {employee.fullName}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Department">
            <select className="shell-select" onChange={(event) => setProfileQuery((current) => ({ ...current, departmentId: event.target.value, pageNumber: 1 }))} value={profileQuery.departmentId ?? ''}>
              <option value="">All departments</option>
              {options?.departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Branch">
            <select className="shell-select" onChange={(event) => setProfileQuery((current) => ({ ...current, branchId: event.target.value, pageNumber: 1 }))} value={profileQuery.branchId ?? ''}>
              <option value="">All branches</option>
              {options?.branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </FormField>
        </div>

        <div className="shell-table-wrap mt-5">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Compensation</th>
                <th>Effective</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoadingProfiles ? (
                <tr>
                  <td className="text-slate-500" colSpan={4}>
                    Loading compensation profiles...
                  </td>
                </tr>
              ) : profiles?.items.length ? (
                profiles.items.map((record) => (
                  <tr key={record.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{record.employeeFullName}</div>
                      <div className="mt-1 text-slate-500">{record.employeeCode}</div>
                      <div className="mt-1 text-slate-500">{[record.departmentName, record.branchName].filter(Boolean).join(' | ')}</div>
                    </td>
                    <td>
                      <div className="font-semibold text-slate-900">{formatCurrency(record.basicSalary, record.currency)}</div>
                      <div className="mt-1 text-slate-500">{record.payType} | {record.payFrequency.replace('_', ' ')}</div>
                      <div className="mt-1 text-xs text-slate-500">
                        Daily {record.dailyRate ? formatCurrency(record.dailyRate, record.currency) : '-'} | Hourly {record.hourlyRate ? formatCurrency(record.hourlyRate, record.currency) : '-'}
                      </div>
                    </td>
                    <td className="text-slate-600">
                      <div>{formatDate(record.effectiveStartDate)}</div>
                      <div className="mt-1 text-xs text-slate-500">{record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary px-3 py-2" onClick={() => void handlePreview(record.employeeId)} type="button">
                          Preview
                        </button>
                        <button className="shell-button-secondary px-3 py-2" onClick={() => openEditCompensationModal(record)} type="button">
                          Edit
                        </button>
                        <button className="shell-button-danger px-3 py-2" onClick={() => setDeleteState({ kind: 'profile', id: record.id, label: `${record.employeeFullName} compensation profile` })} type="button">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={4}>
                    No compensation profiles found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          <PaginationControls
            onPageChange={(pageNumber) => setProfileQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={profiles?.pageNumber ?? 1}
            pageSize={profiles?.pageSize ?? profileQuery.pageSize ?? 8}
            totalCount={profiles?.totalCount ?? 0}
            totalPages={profiles?.totalPages ?? 0}
          />
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Recurring earnings</h4>
              <p className="mt-1 text-sm text-slate-500">Apply recurring allowance or earning lines when the period is effective.</p>
            </div>
            <button className="shell-button-secondary" onClick={openCreateEarningModal} type="button">
              Add Recurring Earning
            </button>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <FormField label="Search">
              <input className="shell-input" onChange={(event) => setEarningQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))} placeholder="Employee or earning type" value={earningQuery.search ?? ''} />
            </FormField>
            <FormField label="Employee">
              <select className="shell-select" onChange={(event) => setEarningQuery((current) => ({ ...current, employeeId: event.target.value, pageNumber: 1 }))} value={earningQuery.employeeId ?? ''}>
                <option value="">All employees</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="mt-5 space-y-3">
            {isLoadingEarnings ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">Loading recurring earnings...</div>
            ) : recurringEarnings?.items.length ? (
              recurringEarnings.items.map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white p-4" key={record.id}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.employeeFullName}</p>
                      <p className="mt-1 text-sm text-slate-500">{record.earningTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">{formatDate(record.effectiveStartDate)} to {record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</p>
                    </div>
                    <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.amount)}</p>
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <button className="shell-button-secondary px-3 py-2" onClick={() => openEditEarningModal(record)} type="button">
                      Edit
                    </button>
                    <button className="shell-button-danger px-3 py-2" onClick={() => setDeleteState({ kind: 'earning', id: record.id, label: `${record.employeeFullName} recurring earning` })} type="button">
                      Delete
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No recurring earnings found.</div>
            )}
          </div>

          <PaginationControls
            onPageChange={(pageNumber) => setEarningQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={recurringEarnings?.pageNumber ?? 1}
            pageSize={recurringEarnings?.pageSize ?? earningQuery.pageSize ?? 8}
            totalCount={recurringEarnings?.totalCount ?? 0}
            totalPages={recurringEarnings?.totalPages ?? 0}
          />
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
            <div>
              <h4 className="text-lg font-semibold text-slate-950">Recurring deductions</h4>
              <p className="mt-1 text-sm text-slate-500">Maintain recurring deductions such as loans, cash advances, and scheduled other deductions.</p>
            </div>
            <button className="shell-button-secondary" onClick={openCreateDeductionModal} type="button">
              Add Recurring Deduction
            </button>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <FormField label="Search">
              <input className="shell-input" onChange={(event) => setDeductionQuery((current) => ({ ...current, search: event.target.value, pageNumber: 1 }))} placeholder="Employee or deduction type" value={deductionQuery.search ?? ''} />
            </FormField>
            <FormField label="Employee">
              <select className="shell-select" onChange={(event) => setDeductionQuery((current) => ({ ...current, employeeId: event.target.value, pageNumber: 1 }))} value={deductionQuery.employeeId ?? ''}>
                <option value="">All employees</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="mt-5 space-y-3">
            {isLoadingDeductions ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">Loading recurring deductions...</div>
            ) : recurringDeductions?.items.length ? (
              recurringDeductions.items.map((record) => (
                <div className="rounded-2xl border border-slate-200 bg-white p-4" key={record.id}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{record.employeeFullName}</p>
                      <p className="mt-1 text-sm text-slate-500">{record.deductionTypeName}</p>
                      <p className="mt-1 text-xs text-slate-500">{formatDate(record.effectiveStartDate)} to {record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.amount)}</p>
                      <p className="mt-1 text-xs text-slate-500">Balance {record.balance ? formatCurrency(record.balance) : '-'}</p>
                    </div>
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <button className="shell-button-secondary px-3 py-2" onClick={() => openEditDeductionModal(record)} type="button">
                      Edit
                    </button>
                    <button className="shell-button-danger px-3 py-2" onClick={() => setDeleteState({ kind: 'deduction', id: record.id, label: `${record.employeeFullName} recurring deduction` })} type="button">
                      Delete
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No recurring deductions found.</div>
            )}
          </div>

          <PaginationControls
            onPageChange={(pageNumber) => setDeductionQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={recurringDeductions?.pageNumber ?? 1}
            pageSize={recurringDeductions?.pageSize ?? deductionQuery.pageSize ?? 8}
            totalCount={recurringDeductions?.totalCount ?? 0}
            totalPages={recurringDeductions?.totalPages ?? 0}
          />
        </div>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Employee payroll preview</h4>
            <p className="mt-1 text-sm text-slate-500">Use preview to review the linked compensation history, recurring items, and recent payroll snapshots for one employee.</p>
          </div>
          {previewProfile ? <span className="shell-badge-brand">{previewProfile.employeeCode}</span> : null}
        </div>

        <div className="mt-5">
          {previewLoading ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">Loading employee payroll preview...</div>
          ) : previewProfile ? (
            <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
                <p className="text-lg font-semibold text-slate-950">{previewProfile.employeeFullName}</p>
                <p className="mt-1 text-sm text-slate-500">{previewProfile.employeeCode}</p>
                <div className="mt-5 space-y-3">
                  <InfoLine label="Compensation profiles" value={String(previewProfile.compensationProfiles.length)} />
                  <InfoLine label="Recurring earnings" value={String(previewProfile.recurringEarnings.length)} />
                  <InfoLine label="Recurring deductions" value={String(previewProfile.recurringDeductions.length)} />
                  <InfoLine label="Payroll history" value={String(previewProfile.payrollHistory.length)} />
                </div>
              </div>

              <div className="space-y-4">
                <div className="rounded-2xl border border-slate-200 bg-white p-5">
                  <p className="text-sm font-semibold text-slate-900">Latest compensation</p>
                  {previewProfile.compensationProfiles[0] ? (
                    <div className="mt-3">
                      <p className="text-lg font-semibold text-slate-950">
                        {formatCurrency(previewProfile.compensationProfiles[0].basicSalary, previewProfile.compensationProfiles[0].currency)}
                      </p>
                      <p className="mt-1 text-sm text-slate-500">
                        {previewProfile.compensationProfiles[0].payType} | {previewProfile.compensationProfiles[0].payFrequency.replace('_', ' ')}
                      </p>
                    </div>
                  ) : (
                    <p className="mt-3 text-sm text-slate-500">No compensation profile found for this employee.</p>
                  )}
                </div>

                <div className="rounded-2xl border border-slate-200 bg-white p-5">
                  <p className="text-sm font-semibold text-slate-900">Recent payroll history</p>
                  <div className="mt-3 space-y-2">
                    {previewProfile.payrollHistory.slice(0, 4).map((item) => (
                      <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3" key={item.id}>
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-slate-900">{item.employeeName}</p>
                            <p className="mt-1 text-xs text-slate-500">{item.status}</p>
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-semibold text-slate-900">{formatCurrency(item.netPay, item.currency)}</p>
                            <p className="mt-1 text-xs text-slate-500">{formatDateTime(item.createdAtUtc)}</p>
                          </div>
                        </div>
                      </div>
                    ))}
                    {!previewProfile.payrollHistory.length ? (
                      <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
                        No payroll history yet for this employee.
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
              Select Preview from a compensation row to inspect the employee payroll setup.
            </div>
          )}
        </div>
      </section>

      <Modal
        description="Maintain historical compensation profiles. Only one active profile should cover a date range."
        onClose={() => {
          if (!isSaving) {
            setCompensationModalOpen(false)
          }
        }}
        open={compensationModalOpen}
        title={editingCompensation ? `Edit ${editingCompensation.employeeFullName}` : 'Add Compensation Profile'}
      >
        <form className="space-y-5" onSubmit={handleSaveCompensation}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(compensationErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select className="shell-select" onChange={(event) => setCompensationEditor((current) => ({ ...current, employeeId: event.target.value }))} value={compensationEditor.employeeId ?? ''}>
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(compensationErrors, 'Currency', 'currency')} label="Currency">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, currency: event.target.value.toUpperCase() }))} value={compensationEditor.currency} />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(compensationErrors, 'PayType', 'payType')} label="Pay Type">
              <select className="shell-select" onChange={(event) => setCompensationEditor((current) => ({ ...current, payType: event.target.value }))} value={compensationEditor.payType}>
                {options?.payTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(compensationErrors, 'PayFrequency', 'payFrequency')} label="Pay Frequency">
              <select className="shell-select" onChange={(event) => setCompensationEditor((current) => ({ ...current, payFrequency: event.target.value }))} value={compensationEditor.payFrequency}>
                {options?.payFrequencies.map((type) => (
                  <option key={type} value={type}>
                    {type.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(compensationErrors, 'BasicSalary', 'basicSalary')} label="Basic Salary">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, basicSalary: Number(event.target.value) }))} step="0.01" type="number" value={compensationEditor.basicSalary} />
            </FormField>
            <FormField error={getFieldError(compensationErrors, 'DailyRate', 'dailyRate')} label="Daily Rate">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, dailyRate: event.target.value ? Number(event.target.value) : null }))} step="0.01" type="number" value={compensationEditor.dailyRate ?? ''} />
            </FormField>
            <FormField error={getFieldError(compensationErrors, 'HourlyRate', 'hourlyRate')} label="Hourly Rate">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, hourlyRate: event.target.value ? Number(event.target.value) : null }))} step="0.01" type="number" value={compensationEditor.hourlyRate ?? ''} />
            </FormField>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(compensationErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, effectiveStartDate: event.target.value }))} type="date" value={compensationEditor.effectiveStartDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(compensationErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input className="shell-input" onChange={(event) => setCompensationEditor((current) => ({ ...current, effectiveEndDate: event.target.value }))} type="date" value={compensationEditor.effectiveEndDate ?? ''} />
            </FormField>
          </div>

          <FormField error={getFieldError(compensationErrors, 'Remarks', 'remarks')} label="Remarks">
            <textarea className="shell-textarea" onChange={(event) => setCompensationEditor((current) => ({ ...current, remarks: event.target.value }))} rows={3} value={compensationEditor.remarks} />
          </FormField>

          <ToggleField checked={compensationEditor.isActive} label="Compensation profile is active" onChange={(checked) => setCompensationEditor((current) => ({ ...current, isActive: checked }))} />

          <ModalActions busy={isSaving} close={() => setCompensationModalOpen(false)} saveLabel={editingCompensation ? 'Save Changes' : 'Create Profile'} />
        </form>
      </Modal>

      <Modal
        description="Recurring earnings are applied during payroll generation when their effective dates cover the pay period."
        onClose={() => {
          if (!isSaving) {
            setEarningModalOpen(false)
          }
        }}
        open={earningModalOpen}
        title={editingRecurringEarning ? `Edit ${editingRecurringEarning.employeeFullName}` : 'Add Recurring Earning'}
      >
        <form className="space-y-5" onSubmit={handleSaveRecurringEarning}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(earningErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select className="shell-select" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, employeeId: event.target.value }))} value={recurringEarningEditor.employeeId ?? ''}>
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(earningErrors, 'EarningTypeId', 'earningTypeId')} label="Earning Type">
              <select className="shell-select" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, earningTypeId: event.target.value }))} value={recurringEarningEditor.earningTypeId ?? ''}>
                <option value="">Select earning type</option>
                {options?.earningTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(earningErrors, 'Amount', 'amount')} label="Amount">
              <input className="shell-input" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, amount: Number(event.target.value) }))} step="0.01" type="number" value={recurringEarningEditor.amount} />
            </FormField>
            <FormField error={getFieldError(earningErrors, 'Frequency', 'frequency')} label="Frequency">
              <input className="shell-input" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, frequency: event.target.value }))} value={recurringEarningEditor.frequency} />
            </FormField>
            <FormField error={getFieldError(earningErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input className="shell-input" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, effectiveStartDate: event.target.value }))} type="date" value={recurringEarningEditor.effectiveStartDate ?? ''} />
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(earningErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input className="shell-input" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, effectiveEndDate: event.target.value }))} type="date" value={recurringEarningEditor.effectiveEndDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(earningErrors, 'Remarks', 'remarks')} label="Remarks">
              <textarea className="shell-textarea" onChange={(event) => setRecurringEarningEditor((current) => ({ ...current, remarks: event.target.value }))} rows={3} value={recurringEarningEditor.remarks} />
            </FormField>
          </div>
          <ToggleField checked={recurringEarningEditor.isActive} label="Recurring earning is active" onChange={(checked) => setRecurringEarningEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSaving} close={() => setEarningModalOpen(false)} saveLabel={editingRecurringEarning ? 'Save Changes' : 'Create Recurring Earning'} />
        </form>
      </Modal>

      <Modal
        description="Recurring deductions are applied during payroll generation when their effective dates cover the pay period."
        onClose={() => {
          if (!isSaving) {
            setDeductionModalOpen(false)
          }
        }}
        open={deductionModalOpen}
        title={editingRecurringDeduction ? `Edit ${editingRecurringDeduction.employeeFullName}` : 'Add Recurring Deduction'}
      >
        <form className="space-y-5" onSubmit={handleSaveRecurringDeduction}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(deductionErrors, 'EmployeeId', 'employeeId')} label="Employee">
              <select className="shell-select" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, employeeId: event.target.value }))} value={recurringDeductionEditor.employeeId ?? ''}>
                <option value="">Select employee</option>
                {options?.employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} | {employee.fullName}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'DeductionTypeId', 'deductionTypeId')} label="Deduction Type">
              <select className="shell-select" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, deductionTypeId: event.target.value }))} value={recurringDeductionEditor.deductionTypeId ?? ''}>
                <option value="">Select deduction type</option>
                {options?.deductionTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-4">
            <FormField error={getFieldError(deductionErrors, 'Amount', 'amount')} label="Amount">
              <input className="shell-input" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, amount: Number(event.target.value) }))} step="0.01" type="number" value={recurringDeductionEditor.amount} />
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'Frequency', 'frequency')} label="Frequency">
              <input className="shell-input" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, frequency: event.target.value }))} value={recurringDeductionEditor.frequency} />
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'Balance', 'balance')} label="Balance">
              <input className="shell-input" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, balance: event.target.value ? Number(event.target.value) : null }))} step="0.01" type="number" value={recurringDeductionEditor.balance ?? ''} />
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input className="shell-input" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, effectiveStartDate: event.target.value }))} type="date" value={recurringDeductionEditor.effectiveStartDate ?? ''} />
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(deductionErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input className="shell-input" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, effectiveEndDate: event.target.value }))} type="date" value={recurringDeductionEditor.effectiveEndDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'Remarks', 'remarks')} label="Remarks">
              <textarea className="shell-textarea" onChange={(event) => setRecurringDeductionEditor((current) => ({ ...current, remarks: event.target.value }))} rows={3} value={recurringDeductionEditor.remarks} />
            </FormField>
          </div>
          <ToggleField checked={recurringDeductionEditor.isActive} label="Recurring deduction is active" onChange={(checked) => setRecurringDeductionEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSaving} close={() => setDeductionModalOpen(false)} saveLabel={editingRecurringDeduction ? 'Save Changes' : 'Create Recurring Deduction'} />
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Payroll Record"
        description={deleteState ? `Delete ${deleteState.label}?` : ''}
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteState(null)
          }
        }}
        onConfirm={() => void handleDeleteConfirmed()}
        open={Boolean(deleteState)}
        title="Delete Payroll Record"
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
    <label className="inline-flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
      <input checked={checked} className="h-4 w-4 rounded border-slate-300 text-[#465fff] focus:ring-[#465fff]" onChange={(event) => onChange(event.target.checked)} type="checkbox" />
      {label}
    </label>
  )
}

function ModalActions({
  busy,
  close,
  saveLabel,
}: {
  busy: boolean
  close: () => void
  saveLabel: string
}) {
  return (
    <div className="flex flex-wrap justify-end gap-3">
      <button className="shell-button-secondary" onClick={close} type="button">
        Cancel
      </button>
      <button className="shell-button" disabled={busy} type="submit">
        {busy ? 'Saving...' : saveLabel}
      </button>
    </div>
  )
}

function InfoLine({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  )
}
