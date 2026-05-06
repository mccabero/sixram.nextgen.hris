import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Modal } from '../components/Modal'
import { PayrollStatusBadge } from '../components/PayrollStatusBadge'
import type {
  ContributionTypeRecord,
  DeductionTypeRecord,
  EarningTypeRecord,
  GovernmentContributionBracket,
  GovernmentContributionTableRecord,
  PayPeriodTemplateRecord,
  PayrollOptions,
  PayrollSettings,
  PayrollSetupSummary,
  SaveContributionTypeInput,
  SaveDeductionTypeInput,
  SaveEarningTypeInput,
  SaveGovernmentContributionTableInput,
  SavePayPeriodTemplateInput,
  SaveTaxTableInput,
  TaxBracket,
  TaxTableRecord,
} from '../types/models'
import { formatDate } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

type DeleteState = {
  kind: 'template' | 'earning' | 'deduction' | 'contribution' | 'contributionTable' | 'taxTable'
  id: string
  label: string
}

const emptySettings: PayrollSettings = {
  defaultPayFrequency: 'semi_monthly',
  defaultWorkingDaysPerMonth: 22,
  defaultWorkingHoursPerDay: 8,
  lateUndertimeDeductionPolicy: 'minute_based',
  absenceDeductionPolicy: 'day_based',
  overtimeCalculationPolicy: 'preliminary_only',
  roundingRule: 'round_2',
  payrollTimeZoneId: 'Singapore Standard Time',
  payslipVisibilityRule: 'approved_or_paid',
  allowNegativeNetPay: false,
  defaultCurrency: 'PHP',
}

const emptyTemplateEditor: SavePayPeriodTemplateInput = {
  code: '',
  name: '',
  description: '',
  payFrequency: 'semi_monthly',
  periodLengthDays: 15,
  payrollOffsetDays: 5,
  isActive: true,
}

const emptyEarningTypeEditor: SaveEarningTypeInput = {
  code: '',
  name: '',
  description: '',
  category: 'basic',
  taxable: true,
  recurring: false,
  affectsThirteenthMonth: false,
  isActive: true,
}

const emptyDeductionTypeEditor: SaveDeductionTypeInput = {
  code: '',
  name: '',
  description: '',
  category: 'other',
  preTax: false,
  recurring: false,
  isActive: true,
}

const emptyContributionTypeEditor: SaveContributionTypeInput = {
  code: '',
  name: '',
  description: '',
  employeeShareApplicable: true,
  employerShareApplicable: true,
  isActive: true,
}

const emptyContributionTableEditor: SaveGovernmentContributionTableInput = {
  contributionTypeId: '',
  name: '',
  effectiveStartDate: '',
  effectiveEndDate: '',
  isActive: true,
  brackets: [buildEmptyContributionBracket()],
}

const emptyTaxTableEditor: SaveTaxTableInput = {
  code: '',
  name: '',
  payFrequency: 'semi_monthly',
  effectiveStartDate: '',
  effectiveEndDate: '',
  isActive: true,
  brackets: [buildEmptyTaxBracket()],
}

export function PayrollSetupPage() {
  const [summary, setSummary] = useState<PayrollSetupSummary | null>(null)
  const [settings, setSettings] = useState<PayrollSettings>(emptySettings)
  const [options, setOptions] = useState<PayrollOptions | null>(null)
  const [templates, setTemplates] = useState<PayPeriodTemplateRecord[]>([])
  const [earningTypes, setEarningTypes] = useState<EarningTypeRecord[]>([])
  const [deductionTypes, setDeductionTypes] = useState<DeductionTypeRecord[]>([])
  const [contributionTypes, setContributionTypes] = useState<ContributionTypeRecord[]>([])
  const [contributionTables, setContributionTables] = useState<GovernmentContributionTableRecord[]>([])
  const [taxTables, setTaxTables] = useState<TaxTableRecord[]>([])
  const [error, setError] = useState<string | null>(null)
  const [settingsErrors, setSettingsErrors] = useState<Record<string, string[]>>({})
  const [templateErrors, setTemplateErrors] = useState<Record<string, string[]>>({})
  const [earningErrors, setEarningErrors] = useState<Record<string, string[]>>({})
  const [deductionErrors, setDeductionErrors] = useState<Record<string, string[]>>({})
  const [contributionErrors, setContributionErrors] = useState<Record<string, string[]>>({})
  const [contributionTableErrors, setContributionTableErrors] = useState<Record<string, string[]>>({})
  const [taxTableErrors, setTaxTableErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSavingSettings, setIsSavingSettings] = useState(false)
  const [isSavingModal, setIsSavingModal] = useState(false)
  const [deleteState, setDeleteState] = useState<DeleteState | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [templateModalOpen, setTemplateModalOpen] = useState(false)
  const [editingTemplate, setEditingTemplate] = useState<PayPeriodTemplateRecord | null>(null)
  const [templateEditor, setTemplateEditor] = useState<SavePayPeriodTemplateInput>(emptyTemplateEditor)
  const [earningModalOpen, setEarningModalOpen] = useState(false)
  const [editingEarning, setEditingEarning] = useState<EarningTypeRecord | null>(null)
  const [earningEditor, setEarningEditor] = useState<SaveEarningTypeInput>(emptyEarningTypeEditor)
  const [deductionModalOpen, setDeductionModalOpen] = useState(false)
  const [editingDeduction, setEditingDeduction] = useState<DeductionTypeRecord | null>(null)
  const [deductionEditor, setDeductionEditor] = useState<SaveDeductionTypeInput>(emptyDeductionTypeEditor)
  const [contributionModalOpen, setContributionModalOpen] = useState(false)
  const [editingContribution, setEditingContribution] = useState<ContributionTypeRecord | null>(null)
  const [contributionEditor, setContributionEditor] = useState<SaveContributionTypeInput>(emptyContributionTypeEditor)
  const [contributionTableModalOpen, setContributionTableModalOpen] = useState(false)
  const [editingContributionTable, setEditingContributionTable] = useState<GovernmentContributionTableRecord | null>(null)
  const [contributionTableEditor, setContributionTableEditor] = useState<SaveGovernmentContributionTableInput>(emptyContributionTableEditor)
  const [taxTableModalOpen, setTaxTableModalOpen] = useState(false)
  const [editingTaxTable, setEditingTaxTable] = useState<TaxTableRecord | null>(null)
  const [taxTableEditor, setTaxTableEditor] = useState<SaveTaxTableInput>(emptyTaxTableEditor)

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    setIsLoading(true)

    try {
      const [
        summaryResponse,
        settingsResponse,
        optionsResponse,
        templateResponse,
        earningTypeResponse,
        deductionTypeResponse,
        contributionTypeResponse,
        contributionTableResponse,
        taxTableResponse,
      ] = await Promise.all([
        sixramApi.getPayrollSetupSummary(),
        sixramApi.getPayrollSettings(),
        sixramApi.getPayrollOptions(),
        sixramApi.getPayPeriodTemplates({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
        sixramApi.getEarningTypes({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
        sixramApi.getDeductionTypes({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
        sixramApi.getContributionTypes({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
        sixramApi.getGovernmentContributionTables({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
        sixramApi.getTaxTables({ pageNumber: 1, pageSize: 100, sortBy: 'name', descending: false }),
      ])

      setSummary(summaryResponse)
      setSettings(settingsResponse)
      setOptions(optionsResponse)
      setTemplates(templateResponse.items)
      setEarningTypes(earningTypeResponse.items)
      setDeductionTypes(deductionTypeResponse.items)
      setContributionTypes(contributionTypeResponse.items)
      setContributionTables(contributionTableResponse.items)
      setTaxTables(taxTableResponse.items)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function reloadSetupLists() {
    await loadData()
  }

  async function handleSaveSettings(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingSettings(true)
    setSettingsErrors({})
    setError(null)

    try {
      const response = await sixramApi.updatePayrollSettings(settings)
      setSettings(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
      setSettingsErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingSettings(false)
    }
  }

  function openCreateTemplateModal() {
    setEditingTemplate(null)
    setTemplateErrors({})
    setTemplateEditor({
      ...emptyTemplateEditor,
      payFrequency: options?.payFrequencies[0] ?? 'semi_monthly',
    })
    setTemplateModalOpen(true)
  }

  function openEditTemplateModal(record: PayPeriodTemplateRecord) {
    setEditingTemplate(record)
    setTemplateErrors({})
    setTemplateEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      payFrequency: record.payFrequency,
      periodLengthDays: record.periodLengthDays,
      payrollOffsetDays: record.payrollOffsetDays,
      isActive: record.isActive,
    })
    setTemplateModalOpen(true)
  }

  function openCreateEarningModal() {
    setEditingEarning(null)
    setEarningErrors({})
    setEarningEditor(emptyEarningTypeEditor)
    setEarningModalOpen(true)
  }

  function openEditEarningModal(record: EarningTypeRecord) {
    setEditingEarning(record)
    setEarningErrors({})
    setEarningEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      category: record.category,
      taxable: record.taxable,
      recurring: record.recurring,
      affectsThirteenthMonth: record.affectsThirteenthMonth,
      isActive: record.isActive,
    })
    setEarningModalOpen(true)
  }

  function openCreateDeductionModal() {
    setEditingDeduction(null)
    setDeductionErrors({})
    setDeductionEditor(emptyDeductionTypeEditor)
    setDeductionModalOpen(true)
  }

  function openEditDeductionModal(record: DeductionTypeRecord) {
    setEditingDeduction(record)
    setDeductionErrors({})
    setDeductionEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      category: record.category,
      preTax: record.preTax,
      recurring: record.recurring,
      isActive: record.isActive,
    })
    setDeductionModalOpen(true)
  }

  function openCreateContributionModal() {
    setEditingContribution(null)
    setContributionErrors({})
    setContributionEditor(emptyContributionTypeEditor)
    setContributionModalOpen(true)
  }

  function openEditContributionModal(record: ContributionTypeRecord) {
    setEditingContribution(record)
    setContributionErrors({})
    setContributionEditor({
      code: record.code,
      name: record.name,
      description: record.description,
      employeeShareApplicable: record.employeeShareApplicable,
      employerShareApplicable: record.employerShareApplicable,
      isActive: record.isActive,
    })
    setContributionModalOpen(true)
  }

  function openCreateContributionTableModal() {
    setEditingContributionTable(null)
    setContributionTableErrors({})
    setContributionTableEditor({
      ...emptyContributionTableEditor,
      contributionTypeId: options?.contributionTypes[0]?.id ?? '',
      effectiveStartDate: new Date().toISOString().slice(0, 10),
      brackets: [buildEmptyContributionBracket()],
    })
    setContributionTableModalOpen(true)
  }

  function openEditContributionTableModal(record: GovernmentContributionTableRecord) {
    setEditingContributionTable(record)
    setContributionTableErrors({})
    setContributionTableEditor({
      contributionTypeId: record.contributionTypeId,
      name: record.name,
      effectiveStartDate: record.effectiveStartDate,
      effectiveEndDate: record.effectiveEndDate ?? '',
      isActive: record.isActive,
      brackets: record.brackets.map((bracket) => ({ ...bracket })),
    })
    setContributionTableModalOpen(true)
  }

  function openCreateTaxTableModal() {
    setEditingTaxTable(null)
    setTaxTableErrors({})
    setTaxTableEditor({
      ...emptyTaxTableEditor,
      payFrequency: options?.payFrequencies[0] ?? 'semi_monthly',
      effectiveStartDate: new Date().toISOString().slice(0, 10),
      brackets: [buildEmptyTaxBracket()],
    })
    setTaxTableModalOpen(true)
  }

  function openEditTaxTableModal(record: TaxTableRecord) {
    setEditingTaxTable(record)
    setTaxTableErrors({})
    setTaxTableEditor({
      code: record.code,
      name: record.name,
      payFrequency: record.payFrequency,
      effectiveStartDate: record.effectiveStartDate,
      effectiveEndDate: record.effectiveEndDate ?? '',
      isActive: record.isActive,
      brackets: record.brackets.map((bracket) => ({ ...bracket })),
    })
    setTaxTableModalOpen(true)
  }

  async function handleSaveTemplate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setTemplateErrors({})
    setError(null)

    try {
      if (editingTemplate) {
        await sixramApi.updatePayPeriodTemplate(editingTemplate.id, templateEditor)
      } else {
        await sixramApi.createPayPeriodTemplate(templateEditor)
      }

      setTemplateModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setTemplateErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleSaveEarning(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setEarningErrors({})
    setError(null)

    try {
      if (editingEarning) {
        await sixramApi.updateEarningType(editingEarning.id, earningEditor)
      } else {
        await sixramApi.createEarningType(earningEditor)
      }

      setEarningModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setEarningErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleSaveDeduction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setDeductionErrors({})
    setError(null)

    try {
      if (editingDeduction) {
        await sixramApi.updateDeductionType(editingDeduction.id, deductionEditor)
      } else {
        await sixramApi.createDeductionType(deductionEditor)
      }

      setDeductionModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setDeductionErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleSaveContribution(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setContributionErrors({})
    setError(null)

    try {
      if (editingContribution) {
        await sixramApi.updateContributionType(editingContribution.id, contributionEditor)
      } else {
        await sixramApi.createContributionType(contributionEditor)
      }

      setContributionModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setContributionErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleSaveContributionTable(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setContributionTableErrors({})
    setError(null)

    try {
      if (editingContributionTable) {
        await sixramApi.updateGovernmentContributionTable(editingContributionTable.id, contributionTableEditor)
      } else {
        await sixramApi.createGovernmentContributionTable(contributionTableEditor)
      }

      setContributionTableModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setContributionTableErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleSaveTaxTable(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSavingModal(true)
    setTaxTableErrors({})
    setError(null)

    try {
      if (editingTaxTable) {
        await sixramApi.updateTaxTable(editingTaxTable.id, taxTableEditor)
      } else {
        await sixramApi.createTaxTable(taxTableEditor)
      }

      setTaxTableModalOpen(false)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setTaxTableErrors(getValidationErrors(caughtError))
    } finally {
      setIsSavingModal(false)
    }
  }

  async function handleDeleteConfirmed() {
    if (!deleteState) {
      return
    }

    setIsDeleting(true)
    setError(null)

    try {
      if (deleteState.kind === 'template') {
        await sixramApi.deletePayPeriodTemplate(deleteState.id)
      } else if (deleteState.kind === 'earning') {
        await sixramApi.deleteEarningType(deleteState.id)
      } else if (deleteState.kind === 'deduction') {
        await sixramApi.deleteDeductionType(deleteState.id)
      } else if (deleteState.kind === 'contribution') {
        await sixramApi.deleteContributionType(deleteState.id)
      } else if (deleteState.kind === 'contributionTable') {
        await sixramApi.deleteGovernmentContributionTable(deleteState.id)
      } else {
        await sixramApi.deleteTaxTable(deleteState.id)
      }

      setDeleteState(null)
      await reloadSetupLists()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading payroll setup...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Payroll Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Policies, earnings, deductions, and tables</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Keep payroll rules configurable by maintaining templates, component types, contribution tables, and tax tables in one workspace.
            </p>
          </div>

          <button className="shell-button-secondary" onClick={() => void loadData()} type="button">
            Refresh
          </button>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Templates" value={String(summary?.payPeriodTemplateCount ?? 0)} />
          <SummaryCard label="Earning types" value={String(summary?.earningTypeCount ?? 0)} />
          <SummaryCard label="Deduction types" value={String(summary?.deductionTypeCount ?? 0)} />
          <SummaryCard label="Contribution types" value={String(summary?.contributionTypeCount ?? 0)} />
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Contribution tables" value={String(summary?.governmentContributionTableCount ?? 0)} tone="brand" />
          <SummaryCard label="Tax tables" value={String(summary?.taxTableCount ?? 0)} tone="brand" />
          <SummaryCard label="Active templates" value={String(summary?.activePayPeriodTemplateCount ?? 0)} tone="success" />
          <SummaryCard label="Active earning types" value={String(summary?.activeEarningTypeCount ?? 0)} tone="success" />
        </div>
      </section>

      {error ? (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div>
          <h4 className="text-lg font-semibold text-slate-950">Payroll settings</h4>
          <p className="mt-1 text-sm text-slate-500">These defaults guide payroll generation and payroll calculation behavior.</p>
        </div>

        <form className="mt-5 space-y-5" onSubmit={handleSaveSettings}>
          <div className="grid gap-5 lg:grid-cols-3">
            <FormField error={getFieldError(settingsErrors, 'DefaultPayFrequency', 'defaultPayFrequency')} label="Default Pay Frequency">
              <select className="shell-select" onChange={(event) => setSettings((current) => ({ ...current, defaultPayFrequency: event.target.value }))} value={settings.defaultPayFrequency}>
                {options?.payFrequencies.map((item) => (
                  <option key={item} value={item}>
                    {item.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'DefaultWorkingDaysPerMonth', 'defaultWorkingDaysPerMonth')} label="Working Days / Month">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, defaultWorkingDaysPerMonth: Number(event.target.value) }))} step="0.5" type="number" value={settings.defaultWorkingDaysPerMonth} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'DefaultWorkingHoursPerDay', 'defaultWorkingHoursPerDay')} label="Working Hours / Day">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, defaultWorkingHoursPerDay: Number(event.target.value) }))} step="0.5" type="number" value={settings.defaultWorkingHoursPerDay} />
            </FormField>
          </div>

          <div className="grid gap-5 lg:grid-cols-3">
            <FormField error={getFieldError(settingsErrors, 'LateUndertimeDeductionPolicy', 'lateUndertimeDeductionPolicy')} label="Late / Undertime Policy">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, lateUndertimeDeductionPolicy: event.target.value }))} value={settings.lateUndertimeDeductionPolicy} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'AbsenceDeductionPolicy', 'absenceDeductionPolicy')} label="Absence Policy">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, absenceDeductionPolicy: event.target.value }))} value={settings.absenceDeductionPolicy} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'OvertimeCalculationPolicy', 'overtimeCalculationPolicy')} label="Overtime Policy">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, overtimeCalculationPolicy: event.target.value }))} value={settings.overtimeCalculationPolicy} />
            </FormField>
          </div>

          <div className="grid gap-5 lg:grid-cols-4">
            <FormField error={getFieldError(settingsErrors, 'RoundingRule', 'roundingRule')} label="Rounding Rule">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, roundingRule: event.target.value }))} value={settings.roundingRule} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'PayrollTimeZoneId', 'payrollTimeZoneId')} label="Payroll Time Zone">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, payrollTimeZoneId: event.target.value }))} value={settings.payrollTimeZoneId} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'PayslipVisibilityRule', 'payslipVisibilityRule')} label="Payslip Visibility">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, payslipVisibilityRule: event.target.value }))} value={settings.payslipVisibilityRule} />
            </FormField>
            <FormField error={getFieldError(settingsErrors, 'DefaultCurrency', 'defaultCurrency')} label="Default Currency">
              <input className="shell-input" onChange={(event) => setSettings((current) => ({ ...current, defaultCurrency: event.target.value.toUpperCase() }))} value={settings.defaultCurrency} />
            </FormField>
          </div>

          <label className="inline-flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
            <input checked={settings.allowNegativeNetPay} className="h-4 w-4 rounded border-slate-300 text-[#465fff] focus:ring-[#465fff]" onChange={(event) => setSettings((current) => ({ ...current, allowNegativeNetPay: event.target.checked }))} type="checkbox" />
            Allow negative net pay while generating payroll runs.
          </label>

          <div className="flex justify-end">
            <button className="shell-button" disabled={isSavingSettings} type="submit">
              {isSavingSettings ? 'Saving...' : 'Save Settings'}
            </button>
          </div>
        </form>
      </section>

      <CrudSection
        actionLabel="Add Template"
        description="Reusable template definitions for weekly, semi-monthly, monthly, or custom payroll periods."
        onAction={openCreateTemplateModal}
        title="Pay period templates"
      >
        <SimpleTable
          columns={['Template', 'Frequency', 'Activity', 'Actions']}
          emptyLabel="No pay period templates are configured."
          rows={templates.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.code}</div>
                <div className="mt-1 text-slate-500">{record.periodLengthDays} days | Payroll +{record.payrollOffsetDays}</div>
              </td>
              <td className="text-slate-600">{record.payFrequency.replace('_', ' ')}</td>
              <td>
                <PayrollStatusBadge status={record.isActive ? 'approved' : 'cancelled'} />
              </td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'template', id: record.id, label: record.name })}
                  onEdit={() => openEditTemplateModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <CrudSection
        actionLabel="Add Earning Type"
        description="Configurable payroll earning categories such as base pay, allowance, overtime, and bonuses."
        onAction={openCreateEarningModal}
        title="Earning types"
      >
        <SimpleTable
          columns={['Earning', 'Category', 'Rules', 'Actions']}
          emptyLabel="No earning types are configured."
          rows={earningTypes.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.code}</div>
              </td>
              <td className="text-slate-600">{record.category.replace('_', ' ')}</td>
              <td className="text-slate-600">
                <div>{record.taxable ? 'Taxable' : 'Non-taxable'}</div>
                <div className="mt-1 text-xs text-slate-500">
                  {record.recurring ? 'Recurring enabled' : 'One-time only'} | {record.isActive ? 'Active' : 'Inactive'}
                </div>
              </td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'earning', id: record.id, label: record.name })}
                  onEdit={() => openEditEarningModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <CrudSection
        actionLabel="Add Deduction Type"
        description="Configurable deduction categories such as tax, government contributions, loans, and absence deductions."
        onAction={openCreateDeductionModal}
        title="Deduction types"
      >
        <SimpleTable
          columns={['Deduction', 'Category', 'Rules', 'Actions']}
          emptyLabel="No deduction types are configured."
          rows={deductionTypes.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.code}</div>
              </td>
              <td className="text-slate-600">{record.category.replace('_', ' ')}</td>
              <td className="text-slate-600">
                <div>{record.preTax ? 'Pre-tax' : 'Post-tax'}</div>
                <div className="mt-1 text-xs text-slate-500">
                  {record.recurring ? 'Recurring enabled' : 'One-time only'} | {record.isActive ? 'Active' : 'Inactive'}
                </div>
              </td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'deduction', id: record.id, label: record.name })}
                  onEdit={() => openEditDeductionModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <CrudSection
        actionLabel="Add Contribution Type"
        description="Contribution types define the government or company-funded contribution groups used by payroll."
        onAction={openCreateContributionModal}
        title="Contribution types"
      >
        <SimpleTable
          columns={['Contribution', 'Shares', 'Tables', 'Actions']}
          emptyLabel="No contribution types are configured."
          rows={contributionTypes.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.code}</div>
              </td>
              <td className="text-slate-600">
                <div>{record.employeeShareApplicable ? 'Employee share enabled' : 'No employee share'}</div>
                <div className="mt-1 text-xs text-slate-500">{record.employerShareApplicable ? 'Employer share enabled' : 'No employer share'}</div>
              </td>
              <td className="text-slate-600">{record.tableCount}</td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'contribution', id: record.id, label: record.name })}
                  onEdit={() => openEditContributionModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <CrudSection
        actionLabel="Add Contribution Table"
        description="Set effective contribution brackets for employee and employer shares without changing code."
        onAction={openCreateContributionTableModal}
        title="Government contribution tables"
      >
        <SimpleTable
          columns={['Table', 'Effective', 'Brackets', 'Actions']}
          emptyLabel="No contribution tables are configured."
          rows={contributionTables.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.contributionTypeName}</div>
              </td>
              <td className="text-slate-600">
                <div>{formatDate(record.effectiveStartDate)}</div>
                <div className="mt-1 text-xs text-slate-500">{record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</div>
              </td>
              <td className="text-slate-600">{record.bracketCount}</td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'contributionTable', id: record.id, label: record.name })}
                  onEdit={() => openEditContributionTableModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <CrudSection
        actionLabel="Add Tax Table"
        description="Maintain configurable tax brackets by payroll frequency and effectivity date."
        onAction={openCreateTaxTableModal}
        title="Tax tables"
      >
        <SimpleTable
          columns={['Table', 'Frequency', 'Effective', 'Actions']}
          emptyLabel="No tax tables are configured."
          rows={taxTables.map((record) => (
            <tr key={record.id}>
              <td>
                <div className="font-semibold text-slate-900">{record.name}</div>
                <div className="mt-1 text-slate-500">{record.code}</div>
              </td>
              <td className="text-slate-600">{record.payFrequency.replace('_', ' ')}</td>
              <td className="text-slate-600">
                <div>{formatDate(record.effectiveStartDate)}</div>
                <div className="mt-1 text-xs text-slate-500">{record.effectiveEndDate ? formatDate(record.effectiveEndDate) : 'Open-ended'}</div>
              </td>
              <td>
                <ActionButtons
                  onDelete={() => setDeleteState({ kind: 'taxTable', id: record.id, label: record.name })}
                  onEdit={() => openEditTaxTableModal(record)}
                />
              </td>
            </tr>
          ))}
        />
      </CrudSection>

      <Modal
        description="Maintain reusable pay-period templates for faster pay-period setup."
        onClose={() => {
          if (!isSavingModal) {
            setTemplateModalOpen(false)
          }
        }}
        open={templateModalOpen}
        title={editingTemplate ? `Edit ${editingTemplate.name}` : 'Add Pay Period Template'}
      >
        <form className="space-y-5" onSubmit={handleSaveTemplate}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(templateErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setTemplateEditor((current) => ({ ...current, code: event.target.value }))} value={templateEditor.code} />
            </FormField>
            <FormField error={getFieldError(templateErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setTemplateEditor((current) => ({ ...current, name: event.target.value }))} value={templateEditor.name} />
            </FormField>
          </div>
          <FormField error={getFieldError(templateErrors, 'Description', 'description')} label="Description">
            <textarea className="shell-textarea" onChange={(event) => setTemplateEditor((current) => ({ ...current, description: event.target.value }))} rows={3} value={templateEditor.description} />
          </FormField>
          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(templateErrors, 'PayFrequency', 'payFrequency')} label="Pay Frequency">
              <select className="shell-select" onChange={(event) => setTemplateEditor((current) => ({ ...current, payFrequency: event.target.value }))} value={templateEditor.payFrequency}>
                {options?.payFrequencies.map((item) => (
                  <option key={item} value={item}>
                    {item.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(templateErrors, 'PeriodLengthDays', 'periodLengthDays')} label="Period Length">
              <input className="shell-input" onChange={(event) => setTemplateEditor((current) => ({ ...current, periodLengthDays: Number(event.target.value) }))} type="number" value={templateEditor.periodLengthDays} />
            </FormField>
            <FormField error={getFieldError(templateErrors, 'PayrollOffsetDays', 'payrollOffsetDays')} label="Payroll Offset">
              <input className="shell-input" onChange={(event) => setTemplateEditor((current) => ({ ...current, payrollOffsetDays: Number(event.target.value) }))} type="number" value={templateEditor.payrollOffsetDays} />
            </FormField>
          </div>
          <ToggleField checked={templateEditor.isActive} label="Template is active" onChange={(checked) => setTemplateEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSavingModal} close={() => setTemplateModalOpen(false)} saveLabel={editingTemplate ? 'Save Changes' : 'Create Template'} />
        </form>
      </Modal>

      <Modal
        description="Earning types determine how payroll earning lines are categorized and reported."
        onClose={() => {
          if (!isSavingModal) {
            setEarningModalOpen(false)
          }
        }}
        open={earningModalOpen}
        title={editingEarning ? `Edit ${editingEarning.name}` : 'Add Earning Type'}
      >
        <form className="space-y-5" onSubmit={handleSaveEarning}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(earningErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setEarningEditor((current) => ({ ...current, code: event.target.value }))} value={earningEditor.code} />
            </FormField>
            <FormField error={getFieldError(earningErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setEarningEditor((current) => ({ ...current, name: event.target.value }))} value={earningEditor.name} />
            </FormField>
          </div>
          <FormField error={getFieldError(earningErrors, 'Description', 'description')} label="Description">
            <textarea className="shell-textarea" onChange={(event) => setEarningEditor((current) => ({ ...current, description: event.target.value }))} rows={3} value={earningEditor.description} />
          </FormField>
          <FormField error={getFieldError(earningErrors, 'Category', 'category')} label="Category">
            <select className="shell-select" onChange={(event) => setEarningEditor((current) => ({ ...current, category: event.target.value }))} value={earningEditor.category}>
              {['basic', 'allowance', 'overtime', 'holiday_pay', 'bonus', 'commission', 'reimbursement', 'other'].map((item) => (
                <option key={item} value={item}>
                  {item.replace('_', ' ')}
                </option>
              ))}
            </select>
          </FormField>
          <div className="grid gap-4 sm:grid-cols-3">
            <ToggleField checked={earningEditor.taxable} label="Taxable" onChange={(checked) => setEarningEditor((current) => ({ ...current, taxable: checked }))} />
            <ToggleField checked={earningEditor.recurring} label="Recurring" onChange={(checked) => setEarningEditor((current) => ({ ...current, recurring: checked }))} />
            <ToggleField checked={earningEditor.affectsThirteenthMonth} label="Affects 13th month" onChange={(checked) => setEarningEditor((current) => ({ ...current, affectsThirteenthMonth: checked }))} />
          </div>
          <ToggleField checked={earningEditor.isActive} label="Earning type is active" onChange={(checked) => setEarningEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSavingModal} close={() => setEarningModalOpen(false)} saveLabel={editingEarning ? 'Save Changes' : 'Create Earning Type'} />
        </form>
      </Modal>

      <Modal
        description="Deduction types determine how payroll deduction lines are categorized and reported."
        onClose={() => {
          if (!isSavingModal) {
            setDeductionModalOpen(false)
          }
        }}
        open={deductionModalOpen}
        title={editingDeduction ? `Edit ${editingDeduction.name}` : 'Add Deduction Type'}
      >
        <form className="space-y-5" onSubmit={handleSaveDeduction}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(deductionErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setDeductionEditor((current) => ({ ...current, code: event.target.value }))} value={deductionEditor.code} />
            </FormField>
            <FormField error={getFieldError(deductionErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setDeductionEditor((current) => ({ ...current, name: event.target.value }))} value={deductionEditor.name} />
            </FormField>
          </div>
          <FormField error={getFieldError(deductionErrors, 'Description', 'description')} label="Description">
            <textarea className="shell-textarea" onChange={(event) => setDeductionEditor((current) => ({ ...current, description: event.target.value }))} rows={3} value={deductionEditor.description} />
          </FormField>
          <FormField error={getFieldError(deductionErrors, 'Category', 'category')} label="Category">
            <select className="shell-select" onChange={(event) => setDeductionEditor((current) => ({ ...current, category: event.target.value }))} value={deductionEditor.category}>
              {['government', 'loan', 'cash_advance', 'absence', 'late', 'undertime', 'tax', 'other'].map((item) => (
                <option key={item} value={item}>
                  {item.replace('_', ' ')}
                </option>
              ))}
            </select>
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <ToggleField checked={deductionEditor.preTax} label="Pre-tax" onChange={(checked) => setDeductionEditor((current) => ({ ...current, preTax: checked }))} />
            <ToggleField checked={deductionEditor.recurring} label="Recurring" onChange={(checked) => setDeductionEditor((current) => ({ ...current, recurring: checked }))} />
          </div>
          <ToggleField checked={deductionEditor.isActive} label="Deduction type is active" onChange={(checked) => setDeductionEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSavingModal} close={() => setDeductionModalOpen(false)} saveLabel={editingDeduction ? 'Save Changes' : 'Create Deduction Type'} />
        </form>
      </Modal>

      <Modal
        description="Contribution types define the configurable contribution group before brackets are attached."
        onClose={() => {
          if (!isSavingModal) {
            setContributionModalOpen(false)
          }
        }}
        open={contributionModalOpen}
        title={editingContribution ? `Edit ${editingContribution.name}` : 'Add Contribution Type'}
      >
        <form className="space-y-5" onSubmit={handleSaveContribution}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(contributionErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setContributionEditor((current) => ({ ...current, code: event.target.value }))} value={contributionEditor.code} />
            </FormField>
            <FormField error={getFieldError(contributionErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setContributionEditor((current) => ({ ...current, name: event.target.value }))} value={contributionEditor.name} />
            </FormField>
          </div>
          <FormField error={getFieldError(contributionErrors, 'Description', 'description')} label="Description">
            <textarea className="shell-textarea" onChange={(event) => setContributionEditor((current) => ({ ...current, description: event.target.value }))} rows={3} value={contributionEditor.description} />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <ToggleField checked={contributionEditor.employeeShareApplicable} label="Employee share applicable" onChange={(checked) => setContributionEditor((current) => ({ ...current, employeeShareApplicable: checked }))} />
            <ToggleField checked={contributionEditor.employerShareApplicable} label="Employer share applicable" onChange={(checked) => setContributionEditor((current) => ({ ...current, employerShareApplicable: checked }))} />
          </div>
          <ToggleField checked={contributionEditor.isActive} label="Contribution type is active" onChange={(checked) => setContributionEditor((current) => ({ ...current, isActive: checked }))} />
          <ModalActions busy={isSavingModal} close={() => setContributionModalOpen(false)} saveLabel={editingContribution ? 'Save Changes' : 'Create Contribution Type'} />
        </form>
      </Modal>

      <Modal
        description="Maintain effective contribution brackets. Use amount or rate fields depending on your policy."
        onClose={() => {
          if (!isSavingModal) {
            setContributionTableModalOpen(false)
          }
        }}
        open={contributionTableModalOpen}
        title={editingContributionTable ? `Edit ${editingContributionTable.name}` : 'Add Contribution Table'}
      >
        <form className="space-y-5" onSubmit={handleSaveContributionTable}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(contributionTableErrors, 'ContributionTypeId', 'contributionTypeId')} label="Contribution Type">
              <select className="shell-select" onChange={(event) => setContributionTableEditor((current) => ({ ...current, contributionTypeId: event.target.value }))} value={contributionTableEditor.contributionTypeId ?? ''}>
                <option value="">Select contribution type</option>
                {options?.contributionTypes.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(contributionTableErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setContributionTableEditor((current) => ({ ...current, name: event.target.value }))} value={contributionTableEditor.name} />
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(contributionTableErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input className="shell-input" onChange={(event) => setContributionTableEditor((current) => ({ ...current, effectiveStartDate: event.target.value }))} type="date" value={contributionTableEditor.effectiveStartDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(contributionTableErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input className="shell-input" onChange={(event) => setContributionTableEditor((current) => ({ ...current, effectiveEndDate: event.target.value }))} type="date" value={contributionTableEditor.effectiveEndDate ?? ''} />
            </FormField>
          </div>
          <ToggleField checked={contributionTableEditor.isActive} label="Contribution table is active" onChange={(checked) => setContributionTableEditor((current) => ({ ...current, isActive: checked }))} />

          <BracketSection
            actionLabel="Add Bracket"
            onAdd={() =>
              setContributionTableEditor((current) => ({
                ...current,
                brackets: [...current.brackets, buildEmptyContributionBracket()],
              }))
            }
            title="Brackets"
          >
            {contributionTableEditor.brackets.map((bracket, index) => (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4" key={bracket.id ?? `bracket-${index}`}>
                <div className="grid gap-4 lg:grid-cols-3">
                  <FormField label="Min Compensation">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'minCompensation', Number(event.target.value), setContributionTableEditor)} step="0.01" type="number" value={bracket.minCompensation} />
                  </FormField>
                  <FormField label="Max Compensation">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'maxCompensation', event.target.value ? Number(event.target.value) : null, setContributionTableEditor)} step="0.01" type="number" value={bracket.maxCompensation ?? ''} />
                  </FormField>
                  <FormField label="Remarks">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'remarks', event.target.value, setContributionTableEditor)} value={bracket.remarks} />
                  </FormField>
                </div>
                <div className="mt-4 grid gap-4 lg:grid-cols-4">
                  <FormField label="Employee Amount">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'employeeShareAmount', event.target.value ? Number(event.target.value) : null, setContributionTableEditor)} step="0.01" type="number" value={bracket.employeeShareAmount ?? ''} />
                  </FormField>
                  <FormField label="Employee Rate">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'employeeShareRate', event.target.value ? Number(event.target.value) : null, setContributionTableEditor)} step="0.0001" type="number" value={bracket.employeeShareRate ?? ''} />
                  </FormField>
                  <FormField label="Employer Amount">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'employerShareAmount', event.target.value ? Number(event.target.value) : null, setContributionTableEditor)} step="0.01" type="number" value={bracket.employerShareAmount ?? ''} />
                  </FormField>
                  <FormField label="Employer Rate">
                    <input className="shell-input" onChange={(event) => updateContributionBracket(index, 'employerShareRate', event.target.value ? Number(event.target.value) : null, setContributionTableEditor)} step="0.0001" type="number" value={bracket.employerShareRate ?? ''} />
                  </FormField>
                </div>
                <div className="mt-4 flex justify-end">
                  <button className="shell-button-danger px-3 py-2" onClick={() => removeContributionBracket(index, contributionTableEditor, setContributionTableEditor)} type="button">
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </BracketSection>

          <ModalActions busy={isSavingModal} close={() => setContributionTableModalOpen(false)} saveLabel={editingContributionTable ? 'Save Changes' : 'Create Contribution Table'} />
        </form>
      </Modal>

      <Modal
        description="Maintain tax brackets by frequency and effectivity. These values are snapshots when payroll is generated."
        onClose={() => {
          if (!isSavingModal) {
            setTaxTableModalOpen(false)
          }
        }}
        open={taxTableModalOpen}
        title={editingTaxTable ? `Edit ${editingTaxTable.name}` : 'Add Tax Table'}
      >
        <form className="space-y-5" onSubmit={handleSaveTaxTable}>
          <div className="grid gap-5 sm:grid-cols-2">
            <FormField error={getFieldError(taxTableErrors, 'Code', 'code')} label="Code">
              <input className="shell-input" onChange={(event) => setTaxTableEditor((current) => ({ ...current, code: event.target.value }))} value={taxTableEditor.code} />
            </FormField>
            <FormField error={getFieldError(taxTableErrors, 'Name', 'name')} label="Name">
              <input className="shell-input" onChange={(event) => setTaxTableEditor((current) => ({ ...current, name: event.target.value }))} value={taxTableEditor.name} />
            </FormField>
          </div>
          <div className="grid gap-5 sm:grid-cols-3">
            <FormField error={getFieldError(taxTableErrors, 'PayFrequency', 'payFrequency')} label="Pay Frequency">
              <select className="shell-select" onChange={(event) => setTaxTableEditor((current) => ({ ...current, payFrequency: event.target.value }))} value={taxTableEditor.payFrequency}>
                {options?.payFrequencies.map((item) => (
                  <option key={item} value={item}>
                    {item.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField error={getFieldError(taxTableErrors, 'EffectiveStartDate', 'effectiveStartDate')} label="Effective Start">
              <input className="shell-input" onChange={(event) => setTaxTableEditor((current) => ({ ...current, effectiveStartDate: event.target.value }))} type="date" value={taxTableEditor.effectiveStartDate ?? ''} />
            </FormField>
            <FormField error={getFieldError(taxTableErrors, 'EffectiveEndDate', 'effectiveEndDate')} label="Effective End">
              <input className="shell-input" onChange={(event) => setTaxTableEditor((current) => ({ ...current, effectiveEndDate: event.target.value }))} type="date" value={taxTableEditor.effectiveEndDate ?? ''} />
            </FormField>
          </div>
          <ToggleField checked={taxTableEditor.isActive} label="Tax table is active" onChange={(checked) => setTaxTableEditor((current) => ({ ...current, isActive: checked }))} />

          <BracketSection
            actionLabel="Add Bracket"
            onAdd={() =>
              setTaxTableEditor((current) => ({
                ...current,
                brackets: [...current.brackets, buildEmptyTaxBracket()],
              }))
            }
            title="Tax Brackets"
          >
            {taxTableEditor.brackets.map((bracket, index) => (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4" key={bracket.id ?? `tax-bracket-${index}`}>
                <div className="grid gap-4 lg:grid-cols-5">
                  <FormField label="Min Income">
                    <input className="shell-input" onChange={(event) => updateTaxBracket(index, 'minTaxableIncome', Number(event.target.value), setTaxTableEditor)} step="0.01" type="number" value={bracket.minTaxableIncome} />
                  </FormField>
                  <FormField label="Max Income">
                    <input className="shell-input" onChange={(event) => updateTaxBracket(index, 'maxTaxableIncome', event.target.value ? Number(event.target.value) : null, setTaxTableEditor)} step="0.01" type="number" value={bracket.maxTaxableIncome ?? ''} />
                  </FormField>
                  <FormField label="Base Tax">
                    <input className="shell-input" onChange={(event) => updateTaxBracket(index, 'baseTax', Number(event.target.value), setTaxTableEditor)} step="0.01" type="number" value={bracket.baseTax} />
                  </FormField>
                  <FormField label="Tax Rate">
                    <input className="shell-input" onChange={(event) => updateTaxBracket(index, 'taxRate', Number(event.target.value), setTaxTableEditor)} step="0.0001" type="number" value={bracket.taxRate} />
                  </FormField>
                  <FormField label="Excess Over">
                    <input className="shell-input" onChange={(event) => updateTaxBracket(index, 'excessOver', Number(event.target.value), setTaxTableEditor)} step="0.01" type="number" value={bracket.excessOver} />
                  </FormField>
                </div>
                <div className="mt-4 flex justify-end">
                  <button className="shell-button-danger px-3 py-2" onClick={() => removeTaxBracket(index, taxTableEditor, setTaxTableEditor)} type="button">
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </BracketSection>

          <ModalActions busy={isSavingModal} close={() => setTaxTableModalOpen(false)} saveLabel={editingTaxTable ? 'Save Changes' : 'Create Tax Table'} />
        </form>
      </Modal>

      <ConfirmDialog
        confirmLabel="Delete Setup Record"
        description={deleteState ? `Delete ${deleteState.label}? Historical payroll snapshots will remain, but this setup record will be removed if no dependencies block it.` : ''}
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteState(null)
          }
        }}
        onConfirm={() => void handleDeleteConfirmed()}
        open={Boolean(deleteState)}
        title="Delete Payroll Setup Record"
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
  tone?: 'default' | 'success' | 'brand'
}) {
  const className =
    tone === 'success'
      ? 'border-emerald-200 bg-emerald-50'
      : tone === 'brand'
        ? 'border-[#465fff]/20 bg-[#465fff]/5'
        : 'border-slate-200 bg-white'

  return (
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function CrudSection({
  title,
  description,
  actionLabel,
  onAction,
  children,
}: {
  title: string
  description: string
  actionLabel: string
  onAction: () => void
  children: ReactNode
}) {
  return (
    <section className="shell-card fade-up p-6 sm:p-7">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <h4 className="text-lg font-semibold text-slate-950">{title}</h4>
          <p className="mt-1 text-sm text-slate-500">{description}</p>
        </div>
        <button className="shell-button-secondary" onClick={onAction} type="button">
          {actionLabel}
        </button>
      </div>
      <div className="mt-5">{children}</div>
    </section>
  )
}

function SimpleTable({
  columns,
  rows,
  emptyLabel,
}: {
  columns: string[]
  rows: ReactNode[]
  emptyLabel: string
}) {
  return (
    <div className="shell-table-wrap">
      <table className="shell-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column}>{column}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length ? (
            rows
          ) : (
            <tr>
              <td className="text-slate-500" colSpan={columns.length}>
                {emptyLabel}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function ActionButtons({
  onEdit,
  onDelete,
}: {
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <div className="flex flex-wrap gap-2">
      <button className="shell-button-secondary px-3 py-2" onClick={onEdit} type="button">
        Edit
      </button>
      <button className="shell-button-danger px-3 py-2" onClick={onDelete} type="button">
        Delete
      </button>
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

function BracketSection({
  title,
  actionLabel,
  onAdd,
  children,
}: {
  title: string
  actionLabel: string
  onAdd: () => void
  children: ReactNode
}) {
  return (
    <div>
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-semibold text-slate-900">{title}</p>
        <button className="shell-button-secondary px-3 py-2" onClick={onAdd} type="button">
          {actionLabel}
        </button>
      </div>
      <div className="mt-4 space-y-3">{children}</div>
    </div>
  )
}

function buildEmptyContributionBracket(): GovernmentContributionBracket {
  return {
    minCompensation: 0,
    maxCompensation: null,
    employeeShareAmount: null,
    employeeShareRate: null,
    employerShareAmount: null,
    employerShareRate: null,
    remarks: '',
  }
}

function buildEmptyTaxBracket(): TaxBracket {
  return {
    minTaxableIncome: 0,
    maxTaxableIncome: null,
    baseTax: 0,
    taxRate: 0,
    excessOver: 0,
  }
}

function updateContributionBracket<TKey extends keyof GovernmentContributionBracket>(
  index: number,
  key: TKey,
  value: GovernmentContributionBracket[TKey],
  setEditor: React.Dispatch<React.SetStateAction<SaveGovernmentContributionTableInput>>,
) {
  setEditor((current) => ({
    ...current,
    brackets: current.brackets.map((bracket, bracketIndex) =>
      bracketIndex === index ? { ...bracket, [key]: value } : bracket,
    ),
  }))
}

function removeContributionBracket(
  index: number,
  editor: SaveGovernmentContributionTableInput,
  setEditor: React.Dispatch<React.SetStateAction<SaveGovernmentContributionTableInput>>,
) {
  if (editor.brackets.length === 1) {
    setEditor((current) => ({
      ...current,
      brackets: [buildEmptyContributionBracket()],
    }))
    return
  }

  setEditor((current) => ({
    ...current,
    brackets: current.brackets.filter((_, bracketIndex) => bracketIndex !== index),
  }))
}

function updateTaxBracket<TKey extends keyof TaxBracket>(
  index: number,
  key: TKey,
  value: TaxBracket[TKey],
  setEditor: React.Dispatch<React.SetStateAction<SaveTaxTableInput>>,
) {
  setEditor((current) => ({
    ...current,
    brackets: current.brackets.map((bracket, bracketIndex) =>
      bracketIndex === index ? { ...bracket, [key]: value } : bracket,
    ),
  }))
}

function removeTaxBracket(
  index: number,
  editor: SaveTaxTableInput,
  setEditor: React.Dispatch<React.SetStateAction<SaveTaxTableInput>>,
) {
  if (editor.brackets.length === 1) {
    setEditor((current) => ({
      ...current,
      brackets: [buildEmptyTaxBracket()],
    }))
    return
  }

  setEditor((current) => ({
    ...current,
    brackets: current.brackets.filter((_, bracketIndex) => bracketIndex !== index),
  }))
}
