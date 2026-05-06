import { useEffect, useState, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type {
  GenerateProvidentFundContributionBatchInput,
  PagedResult,
  ProvidentFundAdjustment,
  ProvidentFundAdjustmentInput,
  ProvidentFundBalance,
  ProvidentFundBalanceReportRow,
  ProvidentFundContributionBatch,
  ProvidentFundContributionBatchDetail,
  ProvidentFundContributionReportRow,
  ProvidentFundDashboard,
  ProvidentFundEnrollment,
  ProvidentFundEnrollmentInput,
  ProvidentFundLedgerTransaction,
  ProvidentFundOptions,
  ProvidentFundPolicy,
  ProvidentFundPolicyInput,
  ProvidentFundVestingRule,
  ProvidentFundVestingRuleInput,
  ProvidentFundWithdrawalInput,
  ProvidentFundWithdrawalReportRow,
  ProvidentFundWithdrawalRequest,
} from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

type SectionKey =
  | 'dashboard'
  | 'policies'
  | 'vesting'
  | 'enrollments'
  | 'contributions'
  | 'balances'
  | 'ledger'
  | 'withdrawals'
  | 'adjustments'
  | 'reports'

const sections: { key: SectionKey; label: string; path: string }[] = [
  { key: 'dashboard', label: 'Dashboard', path: '/admin/provident-fund' },
  { key: 'policies', label: 'Fund Policies', path: '/admin/provident-fund/policies' },
  { key: 'vesting', label: 'Vesting Rules', path: '/admin/provident-fund/vesting' },
  { key: 'enrollments', label: 'Employee Enrollment', path: '/admin/provident-fund/enrollments' },
  { key: 'contributions', label: 'Monthly Contributions', path: '/admin/provident-fund/contributions' },
  { key: 'balances', label: 'Fund Balances', path: '/admin/provident-fund/balances' },
  { key: 'ledger', label: 'Fund Ledger', path: '/admin/provident-fund/ledger' },
  { key: 'withdrawals', label: 'Withdrawals', path: '/admin/provident-fund/withdrawals' },
  { key: 'adjustments', label: 'Adjustments', path: '/admin/provident-fund/adjustments' },
  { key: 'reports', label: 'Reports', path: '/admin/provident-fund/reports' },
]

const today = new Date().toISOString().slice(0, 10)
const currentMonth = new Date().getMonth() + 1
const currentYear = new Date().getFullYear()

const emptyPolicy: ProvidentFundPolicyInput = {
  policyName: '',
  description: '',
  eligibilityRules: '',
  employeeContributionType: 'percentage',
  employeeContributionValue: 5,
  employerContributionType: 'percentage',
  employerContributionValue: 5,
  contributionFrequency: 'monthly',
  effectiveDate: today,
  status: 'active',
  allowVoluntaryContribution: true,
  allowWithdrawal: true,
  allowLoan: false,
  remarks: '',
}

const emptyVesting: ProvidentFundVestingRuleInput = {
  policyId: '',
  yearsOfService: 0,
  vestedPercentage: 0,
  remarks: '',
}

const emptyEnrollment: ProvidentFundEnrollmentInput = {
  employeeId: '',
  policyId: '',
  enrollmentDate: today,
  vestingStartDate: today,
  employeeContributionOverrideType: '',
  employeeContributionOverrideValue: null,
  employerContributionOverrideType: '',
  employerContributionOverrideValue: null,
  status: 'active',
  remarks: '',
}

const emptyBatch: GenerateProvidentFundContributionBatchInput = {
  month: currentMonth,
  year: currentYear,
  policyId: '',
  isSupplemental: false,
  batchNumber: '',
  remarks: '',
  manualLines: [],
}

const emptyWithdrawal: ProvidentFundWithdrawalInput = {
  employeeId: '',
  enrollmentId: '',
  requestDate: today,
  withdrawalType: 'partial',
  requestedAmount: 0,
  reason: '',
  attachmentPath: '',
  remarks: '',
}

const emptyAdjustment: ProvidentFundAdjustmentInput = {
  employeeId: '',
  enrollmentId: '',
  adjustmentType: 'credit',
  adjustmentDate: today,
  amount: 0,
  shareAffected: 'employee',
  reason: '',
  attachmentPath: '',
}

export function AdminProvidentFundPage() {
  const location = useLocation()
  const activeSection = resolveSection(location.pathname)
  const [options, setOptions] = useState<ProvidentFundOptions | null>(null)
  const [dashboard, setDashboard] = useState<ProvidentFundDashboard | null>(null)
  const [policies, setPolicies] = useState<PagedResult<ProvidentFundPolicy> | null>(null)
  const [vestingRules, setVestingRules] = useState<PagedResult<ProvidentFundVestingRule> | null>(null)
  const [enrollments, setEnrollments] = useState<PagedResult<ProvidentFundEnrollment> | null>(null)
  const [batches, setBatches] = useState<PagedResult<ProvidentFundContributionBatch> | null>(null)
  const [selectedBatch, setSelectedBatch] = useState<ProvidentFundContributionBatchDetail | null>(null)
  const [ledger, setLedger] = useState<PagedResult<ProvidentFundLedgerTransaction> | null>(null)
  const [balance, setBalance] = useState<ProvidentFundBalance | null>(null)
  const [withdrawals, setWithdrawals] = useState<PagedResult<ProvidentFundWithdrawalRequest> | null>(null)
  const [adjustments, setAdjustments] = useState<PagedResult<ProvidentFundAdjustment> | null>(null)
  const [contributionReport, setContributionReport] = useState<ProvidentFundContributionReportRow[]>([])
  const [balanceReport, setBalanceReport] = useState<ProvidentFundBalanceReportRow[]>([])
  const [withdrawalReport, setWithdrawalReport] = useState<ProvidentFundWithdrawalReportRow[]>([])
  const [ledgerReport, setLedgerReport] = useState<ProvidentFundLedgerTransaction[]>([])
  const [policyForm, setPolicyForm] = useState<ProvidentFundPolicyInput>(emptyPolicy)
  const [editingPolicyId, setEditingPolicyId] = useState<string | null>(null)
  const [vestingForm, setVestingForm] = useState<ProvidentFundVestingRuleInput>(emptyVesting)
  const [editingVestingId, setEditingVestingId] = useState<string | null>(null)
  const [enrollmentForm, setEnrollmentForm] = useState<ProvidentFundEnrollmentInput>(emptyEnrollment)
  const [editingEnrollmentId, setEditingEnrollmentId] = useState<string | null>(null)
  const [batchForm, setBatchForm] = useState<GenerateProvidentFundContributionBatchInput>(emptyBatch)
  const [withdrawalForm, setWithdrawalForm] = useState<ProvidentFundWithdrawalInput>(emptyWithdrawal)
  const [adjustmentForm, setAdjustmentForm] = useState<ProvidentFundAdjustmentInput>(emptyAdjustment)
  const [selectedEmployeeId, setSelectedEmployeeId] = useState('')
  const [filter, setFilter] = useState({ policyId: '', employeeId: '', departmentId: '', status: '', transactionType: '', dateFrom: '', dateTo: '' })
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  const employees = options?.employees ?? []

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadInitialData()
    }, 0)

    return () => window.clearTimeout(timer)
  }, [])

  async function loadInitialData() {
    setIsLoading(true)

    try {
      const [
        optionsResponse,
        dashboardResponse,
        policyResponse,
        vestingResponse,
        enrollmentResponse,
        batchResponse,
        ledgerResponse,
        withdrawalResponse,
        adjustmentResponse,
      ] = await Promise.all([
        sixramApi.getProvidentFundOptions(),
        sixramApi.getProvidentFundDashboard(),
        sixramApi.getProvidentFundPolicies({ pageSize: 20 }),
        sixramApi.getProvidentFundVestingRules({ pageSize: 50 }),
        sixramApi.getProvidentFundEnrollments({ pageSize: 20 }),
        sixramApi.getProvidentFundContributionBatches({ pageSize: 20 }),
        sixramApi.getProvidentFundLedger({ pageSize: 20 }),
        sixramApi.getProvidentFundWithdrawals({ pageSize: 20 }),
        sixramApi.getProvidentFundAdjustments({ pageSize: 20 }),
      ])

      setOptions(optionsResponse)
      setDashboard(dashboardResponse)
      setPolicies(policyResponse)
      setVestingRules(vestingResponse)
      setEnrollments(enrollmentResponse)
      setBatches(batchResponse)
      setLedger(ledgerResponse)
      setWithdrawals(withdrawalResponse)
      setAdjustments(adjustmentResponse)

      const defaultPolicyId = optionsResponse.policies.find((policy) => policy.status === 'active')?.id
      if (defaultPolicyId) {
        setVestingForm((current) => current.policyId ? current : { ...current, policyId: defaultPolicyId })
        setEnrollmentForm((current) => current.policyId ? current : { ...current, policyId: defaultPolicyId })
        setBatchForm((current) => current.policyId ? current : { ...current, policyId: defaultPolicyId })
      }

      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function refreshCore() {
    const [dashboardResponse, policyResponse, vestingResponse, enrollmentResponse, batchResponse, ledgerResponse, withdrawalResponse, adjustmentResponse] =
      await Promise.all([
        sixramApi.getProvidentFundDashboard(),
        sixramApi.getProvidentFundPolicies({ pageSize: 20 }),
        sixramApi.getProvidentFundVestingRules({ pageSize: 50, policyId: filter.policyId || undefined }),
        sixramApi.getProvidentFundEnrollments({ pageSize: 20, policyId: filter.policyId || undefined, employeeId: filter.employeeId || undefined }),
        sixramApi.getProvidentFundContributionBatches({ pageSize: 20, policyId: filter.policyId || undefined }),
        sixramApi.getProvidentFundLedger({
          pageSize: 20,
          policyId: filter.policyId || undefined,
          employeeId: filter.employeeId || undefined,
          transactionType: filter.transactionType || undefined,
          dateFrom: filter.dateFrom || undefined,
          dateTo: filter.dateTo || undefined,
        }),
        sixramApi.getProvidentFundWithdrawals({ pageSize: 20, employeeId: filter.employeeId || undefined, status: filter.status || undefined }),
        sixramApi.getProvidentFundAdjustments({ pageSize: 20, employeeId: filter.employeeId || undefined, status: filter.status || undefined }),
      ])

    setDashboard(dashboardResponse)
    setPolicies(policyResponse)
    setVestingRules(vestingResponse)
    setEnrollments(enrollmentResponse)
    setBatches(batchResponse)
    setLedger(ledgerResponse)
    setWithdrawals(withdrawalResponse)
    setAdjustments(adjustmentResponse)
  }

  async function savePolicy() {
    setIsSaving(true)
    try {
      if (editingPolicyId) {
        await sixramApi.updateProvidentFundPolicy(editingPolicyId, policyForm)
      } else {
        await sixramApi.createProvidentFundPolicy(policyForm)
      }
      setPolicyForm(emptyPolicy)
      setEditingPolicyId(null)
      await loadInitialData()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function saveVestingRule() {
    setIsSaving(true)
    try {
      if (editingVestingId) {
        await sixramApi.updateProvidentFundVestingRule(editingVestingId, vestingForm)
      } else {
        await sixramApi.createProvidentFundVestingRule(vestingForm)
      }
      setVestingForm({ ...emptyVesting, policyId: vestingForm.policyId })
      setEditingVestingId(null)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function saveEnrollment() {
    setIsSaving(true)
    try {
      if (editingEnrollmentId) {
        await sixramApi.updateProvidentFundEnrollment(editingEnrollmentId, enrollmentForm)
      } else {
        await sixramApi.createProvidentFundEnrollment(enrollmentForm)
      }
      setEnrollmentForm({ ...emptyEnrollment, policyId: enrollmentForm.policyId })
      setEditingEnrollmentId(null)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function generateBatch() {
    setIsSaving(true)
    try {
      const detail = await sixramApi.generateProvidentFundContributionBatch({ ...batchForm, policyId: batchForm.policyId || null })
      setSelectedBatch(detail)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function runBatchAction(action: 'review' | 'post' | 'cancel', batchId: string) {
    const labels = { review: 'mark this batch as reviewed', post: 'post this contribution batch to the ledger', cancel: 'cancel this draft batch' }
    if (!window.confirm(`Confirm that you want to ${labels[action]}?`)) {
      return
    }

    setIsSaving(true)
    try {
      const detail =
        action === 'review'
          ? await sixramApi.reviewProvidentFundContributionBatch(batchId)
          : action === 'post'
            ? await sixramApi.postProvidentFundContributionBatch(batchId)
            : await sixramApi.cancelProvidentFundContributionBatch(batchId)
      setSelectedBatch(detail)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function loadBalance(employeeId = selectedEmployeeId) {
    if (!employeeId) {
      setBalance(null)
      return
    }

    try {
      setBalance(await sixramApi.getProvidentFundBalance(employeeId))
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function saveWithdrawal() {
    setIsSaving(true)
    try {
      await sixramApi.createProvidentFundWithdrawal(withdrawalForm)
      setWithdrawalForm(emptyWithdrawal)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function runWithdrawalAction(action: 'submit' | 'approve' | 'reject' | 'paid', request: ProvidentFundWithdrawalRequest) {
    if (!window.confirm(`Confirm ${action.replace('_', ' ')} for ${request.requestNumber}?`)) {
      return
    }

    setIsSaving(true)
    try {
      if (action === 'submit') {
        await sixramApi.submitProvidentFundWithdrawal(request.id)
      } else if (action === 'approve') {
        await sixramApi.approveProvidentFundWithdrawal(request.id, request.requestedAmount)
      } else if (action === 'reject') {
        await sixramApi.rejectProvidentFundWithdrawal(request.id)
      } else {
        await sixramApi.markProvidentFundWithdrawalPaid(request.id, request.approvedAmount || request.requestedAmount, '', request.withdrawalType === 'full')
      }
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function saveAdjustment() {
    setIsSaving(true)
    try {
      await sixramApi.createProvidentFundAdjustment(adjustmentForm)
      setAdjustmentForm(emptyAdjustment)
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function runAdjustmentAction(action: 'approve' | 'reject' | 'post', adjustment: ProvidentFundAdjustment) {
    if (!window.confirm(`Confirm ${action} for this adjustment?`)) {
      return
    }

    setIsSaving(true)
    try {
      if (action === 'approve') {
        await sixramApi.approveProvidentFundAdjustment(adjustment.id)
      } else if (action === 'reject') {
        await sixramApi.rejectProvidentFundAdjustment(adjustment.id)
      } else {
        await sixramApi.postProvidentFundAdjustment(adjustment.id)
      }
      await refreshCore()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function loadReports() {
    setIsSaving(true)
    try {
      const query = {
        month: batchForm.month,
        year: batchForm.year,
        policyId: filter.policyId || undefined,
        employeeId: filter.employeeId || undefined,
        departmentId: filter.departmentId || undefined,
        transactionType: filter.transactionType || undefined,
        dateFrom: filter.dateFrom || undefined,
        dateTo: filter.dateTo || undefined,
      }
      const [contributions, balances, withdrawalRows, ledgerRows] = await Promise.all([
        sixramApi.getProvidentFundContributionReport(query),
        sixramApi.getProvidentFundBalanceReport(query),
        sixramApi.getProvidentFundWithdrawalReport(query),
        sixramApi.getProvidentFundLedgerReport(query),
      ])
      setContributionReport(contributions)
      setBalanceReport(balances)
      setWithdrawalReport(withdrawalRows)
      setLedgerReport(ledgerRows)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading provident fund workspace...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card p-4 sm:p-5">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div>
            <p className="shell-kicker">Provident Fund Management</p>
            <h2 className="mt-2 text-xl font-semibold text-slate-950">Ledger-backed fund administration</h2>
            <p className="mt-1 max-w-4xl text-sm leading-6 text-slate-500">
              Configure policies, enroll employees, process monthly contributions, post withdrawals and adjustments, and review balances computed from ledger transactions.
            </p>
          </div>
          <button className="shell-button-secondary" disabled={isSaving} onClick={() => void loadInitialData()} type="button">
            Refresh
          </button>
        </div>
        <div className="mt-5 flex gap-2 overflow-x-auto pb-1">
          {sections.map((section) => (
            <NavLink
              className={({ isActive }) =>
                [
                  'whitespace-nowrap rounded-lg px-3 py-2 text-sm font-semibold transition',
                  isActive || activeSection === section.key ? 'bg-[#465fff] text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200',
                ].join(' ')
              }
              end={section.key === 'dashboard'}
              key={section.key}
              to={section.path}
            >
              {section.label}
            </NavLink>
          ))}
        </div>
      </section>

      {error ? <div className="shell-state-error">{error}</div> : null}

      <FilterBar
        filter={filter}
        options={options}
        onChange={setFilter}
        onRefresh={() => void refreshCore()}
      />

      {activeSection === 'dashboard' ? <DashboardSection dashboard={dashboard} /> : null}
      {activeSection === 'policies' ? (
        <PolicySection
          form={policyForm}
          isSaving={isSaving}
          policies={policies?.items ?? []}
          onCancel={() => {
            setPolicyForm(emptyPolicy)
            setEditingPolicyId(null)
          }}
          onEdit={(policy) => {
            setEditingPolicyId(policy.id)
            setPolicyForm(toPolicyInput(policy))
          }}
          onFormChange={setPolicyForm}
          onSave={() => void savePolicy()}
        />
      ) : null}
      {activeSection === 'vesting' ? (
        <VestingSection
          form={vestingForm}
          isSaving={isSaving}
          options={options}
          rules={vestingRules?.items ?? []}
          onDelete={(rule) => {
            if (window.confirm('Delete this vesting rule?')) {
              void sixramApi.deleteProvidentFundVestingRule(rule.id).then(refreshCore).catch((caughtError) => setError(formatError(caughtError)))
            }
          }}
          onEdit={(rule) => {
            setEditingVestingId(rule.id)
            setVestingForm({ policyId: rule.policyId, yearsOfService: rule.yearsOfService, vestedPercentage: rule.vestedPercentage, remarks: rule.remarks })
          }}
          onFormChange={setVestingForm}
          onSave={() => void saveVestingRule()}
        />
      ) : null}
      {activeSection === 'enrollments' ? (
        <EnrollmentSection
          enrollments={enrollments?.items ?? []}
          form={enrollmentForm}
          isSaving={isSaving}
          options={options}
          onEdit={(record) => {
            setEditingEnrollmentId(record.id)
            setEnrollmentForm(toEnrollmentInput(record))
          }}
          onFormChange={setEnrollmentForm}
          onSave={() => void saveEnrollment()}
        />
      ) : null}
      {activeSection === 'contributions' ? (
        <ContributionSection
          batches={batches?.items ?? []}
          form={batchForm}
          isSaving={isSaving}
          options={options}
          selectedBatch={selectedBatch}
          onBatchAction={(action, batchId) => void runBatchAction(action, batchId)}
          onFormChange={setBatchForm}
          onGenerate={() => void generateBatch()}
          onSelectBatch={(batchId) => void sixramApi.getProvidentFundContributionBatch(batchId).then(setSelectedBatch).catch((caughtError) => setError(formatError(caughtError)))}
        />
      ) : null}
      {activeSection === 'balances' ? (
        <BalanceSection
          balance={balance}
          employeeId={selectedEmployeeId}
          employees={employees}
          onEmployeeChange={(employeeId) => {
            setSelectedEmployeeId(employeeId)
            void loadBalance(employeeId)
          }}
        />
      ) : null}
      {activeSection === 'ledger' ? (
        <LedgerSection
          ledger={ledger?.items ?? []}
          onReverse={(record) => {
            if (window.confirm(`Reverse ${record.transactionNumber}?`)) {
              void sixramApi.reverseProvidentFundLedger(record.id).then(refreshCore).catch((caughtError) => setError(formatError(caughtError)))
            }
          }}
        />
      ) : null}
      {activeSection === 'withdrawals' ? (
        <WithdrawalSection
          form={withdrawalForm}
          isSaving={isSaving}
          options={options}
          requests={withdrawals?.items ?? []}
          onAction={(action, request) => void runWithdrawalAction(action, request)}
          onFormChange={setWithdrawalForm}
          onSave={() => void saveWithdrawal()}
        />
      ) : null}
      {activeSection === 'adjustments' ? (
        <AdjustmentSection
          adjustments={adjustments?.items ?? []}
          form={adjustmentForm}
          isSaving={isSaving}
          options={options}
          onAction={(action, adjustment) => void runAdjustmentAction(action, adjustment)}
          onFormChange={setAdjustmentForm}
          onSave={() => void saveAdjustment()}
        />
      ) : null}
      {activeSection === 'reports' ? (
        <ReportsSection
          balanceRows={balanceReport}
          contributionRows={contributionReport}
          ledgerRows={ledgerReport}
          onLoad={() => void loadReports()}
          withdrawalRows={withdrawalReport}
        />
      ) : null}

      <section className="rounded-xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm leading-6 text-amber-800">
        Compliance note: actual provident fund rules, tax treatment, eligibility, vesting, withdrawal rules, and final settlement rules must be validated by HR, Finance, Legal, and local labor compliance before production use.
      </section>
    </div>
  )
}

function resolveSection(pathname: string): SectionKey {
  if (pathname.endsWith('/policies')) return 'policies'
  if (pathname.endsWith('/vesting')) return 'vesting'
  if (pathname.endsWith('/enrollments')) return 'enrollments'
  if (pathname.endsWith('/contributions')) return 'contributions'
  if (pathname.endsWith('/balances')) return 'balances'
  if (pathname.endsWith('/ledger')) return 'ledger'
  if (pathname.endsWith('/withdrawals')) return 'withdrawals'
  if (pathname.endsWith('/adjustments')) return 'adjustments'
  if (pathname.endsWith('/reports')) return 'reports'
  return 'dashboard'
}

function FilterBar({
  filter,
  options,
  onChange,
  onRefresh,
}: {
  filter: { policyId: string; employeeId: string; departmentId: string; status: string; transactionType: string; dateFrom: string; dateTo: string }
  options: ProvidentFundOptions | null
  onChange: (filter: { policyId: string; employeeId: string; departmentId: string; status: string; transactionType: string; dateFrom: string; dateTo: string }) => void
  onRefresh: () => void
}) {
  return (
    <section className="shell-toolbar">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-7">
        <FormField label="Policy">
          <select className="shell-select" onChange={(event) => onChange({ ...filter, policyId: event.target.value })} value={filter.policyId}>
            <option value="">All policies</option>
            {options?.policies.map((policy) => (
              <option key={policy.id} value={policy.id}>{policy.policyName}</option>
            ))}
          </select>
        </FormField>
        <FormField label="Employee">
          <select className="shell-select" onChange={(event) => onChange({ ...filter, employeeId: event.target.value })} value={filter.employeeId}>
            <option value="">All employees</option>
            {options?.employees.map((employee) => (
              <option key={employee.id} value={employee.id}>{employee.employeeCode} | {employee.fullName}</option>
            ))}
          </select>
        </FormField>
        <FormField label="Department">
          <select className="shell-select" onChange={(event) => onChange({ ...filter, departmentId: event.target.value })} value={filter.departmentId}>
            <option value="">All departments</option>
            {options?.departments.map((department) => (
              <option key={department.id} value={department.id}>{department.name}</option>
            ))}
          </select>
        </FormField>
        <FormField label="Status">
          <input className="shell-input" onChange={(event) => onChange({ ...filter, status: event.target.value })} placeholder="status" value={filter.status} />
        </FormField>
        <FormField label="Txn Type">
          <select className="shell-select" onChange={(event) => onChange({ ...filter, transactionType: event.target.value })} value={filter.transactionType}>
            <option value="">All types</option>
            {options?.ledgerTransactionTypes.map((type) => <option key={type} value={type}>{toTitle(type)}</option>)}
          </select>
        </FormField>
        <FormField label="From">
          <input className="shell-input" onChange={(event) => onChange({ ...filter, dateFrom: event.target.value })} type="date" value={filter.dateFrom} />
        </FormField>
        <FormField label="To">
          <input className="shell-input" onChange={(event) => onChange({ ...filter, dateTo: event.target.value })} type="date" value={filter.dateTo} />
        </FormField>
      </div>
      <div className="mt-4 flex justify-end">
        <button className="shell-button-secondary" onClick={onRefresh} type="button">Apply Filters</button>
      </div>
    </section>
  )
}

function DashboardSection({ dashboard }: { dashboard: ProvidentFundDashboard | null }) {
  return (
    <div className="space-y-6">
      <section className="shell-summary-grid">
        <SummaryCard label="Total Fund Value" value={formatCurrency(dashboard?.totalFundValue ?? 0)} />
        <SummaryCard label="Employee Contributions" value={formatCurrency(dashboard?.totalEmployeeContributions ?? 0)} />
        <SummaryCard label="Employer Contributions" value={formatCurrency(dashboard?.totalEmployerContributions ?? 0)} />
        <SummaryCard label="Pending Withdrawals" value={`${dashboard?.pendingWithdrawalRequestCount ?? 0}`} tone="warning" />
        <SummaryCard label="Current Month Batch" value={toTitle(dashboard?.currentMonthContributionStatus ?? 'not_started')} />
        <SummaryCard label="Employees Enrolled" value={`${dashboard?.employeesEnrolled ?? 0}`} />
        <SummaryCard label="Employees Not Enrolled" value={`${dashboard?.employeesNotEnrolled ?? 0}`} />
        <SummaryCard label="Withdrawals This Month" value={formatCurrency(dashboard?.totalWithdrawalsThisMonth ?? 0)} tone="danger" />
      </section>
      <section className="shell-card p-6">
        <h3 className="text-lg font-semibold text-slate-950">Fund balance trend</h3>
        <div className="mt-5 grid gap-3 md:grid-cols-6">
          {dashboard?.fundBalanceTrend.map((point) => (
            <div className="rounded-xl border border-slate-200 bg-slate-50 p-4" key={point.period}>
              <p className="text-xs font-semibold text-slate-500">{point.period}</p>
              <p className="mt-2 text-sm font-semibold text-slate-900">{formatCurrency(point.balance)}</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}

function PolicySection({
  form,
  isSaving,
  policies,
  onCancel,
  onEdit,
  onFormChange,
  onSave,
}: {
  form: ProvidentFundPolicyInput
  isSaving: boolean
  policies: ProvidentFundPolicy[]
  onCancel: () => void
  onEdit: (policy: ProvidentFundPolicy) => void
  onFormChange: (form: ProvidentFundPolicyInput) => void
  onSave: () => void
}) {
  return (
    <GridWithEditor
      editor={
        <div className="space-y-4">
          <FormField label="Policy Name"><input className="shell-input" onChange={(event) => onFormChange({ ...form, policyName: event.target.value })} value={form.policyName} /></FormField>
          <div className="grid gap-4 md:grid-cols-2">
            <FormField label="Employee Type"><ContributionTypeSelect value={form.employeeContributionType} onChange={(value) => onFormChange({ ...form, employeeContributionType: value })} /></FormField>
            <FormField label="Employee Value"><NumberInput value={form.employeeContributionValue} onChange={(value) => onFormChange({ ...form, employeeContributionValue: value })} /></FormField>
            <FormField label="Employer Type"><ContributionTypeSelect value={form.employerContributionType} onChange={(value) => onFormChange({ ...form, employerContributionType: value })} /></FormField>
            <FormField label="Employer Value"><NumberInput value={form.employerContributionValue} onChange={(value) => onFormChange({ ...form, employerContributionValue: value })} /></FormField>
            <FormField label="Effective Date"><input className="shell-input" onChange={(event) => onFormChange({ ...form, effectiveDate: event.target.value })} type="date" value={form.effectiveDate} /></FormField>
            <FormField label="Status"><StatusSelect statuses={['active', 'inactive']} value={form.status} onChange={(value) => onFormChange({ ...form, status: value })} /></FormField>
          </div>
          <FormField label="Eligibility Rules"><textarea className="shell-textarea" onChange={(event) => onFormChange({ ...form, eligibilityRules: event.target.value })} value={form.eligibilityRules} /></FormField>
          <FormField label="Description"><textarea className="shell-textarea" onChange={(event) => onFormChange({ ...form, description: event.target.value })} value={form.description} /></FormField>
          <div className="grid gap-3 md:grid-cols-3">
            <Checkbox label="Allow voluntary" checked={form.allowVoluntaryContribution} onChange={(checked) => onFormChange({ ...form, allowVoluntaryContribution: checked })} />
            <Checkbox label="Allow withdrawal" checked={form.allowWithdrawal} onChange={(checked) => onFormChange({ ...form, allowWithdrawal: checked })} />
            <Checkbox label="Allow loan later" checked={form.allowLoan} onChange={(checked) => onFormChange({ ...form, allowLoan: checked })} />
          </div>
          <FormField label="Remarks"><textarea className="shell-textarea" onChange={(event) => onFormChange({ ...form, remarks: event.target.value })} value={form.remarks} /></FormField>
          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={onCancel} type="button">Reset</button>
            <button className="shell-button" disabled={isSaving} onClick={onSave} type="button">{isSaving ? 'Saving...' : 'Save Policy'}</button>
          </div>
        </div>
      }
      title="Policy setup"
    >
      <Table headers={['Policy', 'Contributions', 'Flags', 'Status', 'Actions']}>
        {policies.map((policy) => (
          <tr key={policy.id}>
            <td><b>{policy.policyName}</b><div className="mt-1 text-slate-500">{policy.eligibilityRules || policy.description}</div></td>
            <td>{toTitle(policy.employeeContributionType)} {policy.employeeContributionValue}% / {toTitle(policy.employerContributionType)} {policy.employerContributionValue}%</td>
            <td>{policy.allowVoluntaryContribution ? 'Voluntary ' : ''}{policy.allowWithdrawal ? 'Withdrawals ' : ''}{policy.allowLoan ? 'Loans ' : ''}</td>
            <td><StatusBadge status={policy.status} /></td>
            <td><button className="shell-button-secondary px-3 py-2" onClick={() => onEdit(policy)} type="button">Edit</button></td>
          </tr>
        ))}
      </Table>
    </GridWithEditor>
  )
}

function VestingSection(props: {
  form: ProvidentFundVestingRuleInput
  isSaving: boolean
  options: ProvidentFundOptions | null
  rules: ProvidentFundVestingRule[]
  onDelete: (rule: ProvidentFundVestingRule) => void
  onEdit: (rule: ProvidentFundVestingRule) => void
  onFormChange: (form: ProvidentFundVestingRuleInput) => void
  onSave: () => void
}) {
  return (
    <GridWithEditor title="Vesting rules" editor={
      <div className="space-y-4">
        <PolicySelect options={props.options} value={props.form.policyId} onChange={(policyId) => props.onFormChange({ ...props.form, policyId })} />
        <div className="grid gap-4 md:grid-cols-2">
          <FormField label="Years Threshold"><NumberInput value={props.form.yearsOfService} onChange={(value) => props.onFormChange({ ...props.form, yearsOfService: value })} /></FormField>
          <FormField label="Vested %"><NumberInput value={props.form.vestedPercentage} onChange={(value) => props.onFormChange({ ...props.form, vestedPercentage: value })} /></FormField>
        </div>
        <FormField label="Remarks"><textarea className="shell-textarea" onChange={(event) => props.onFormChange({ ...props.form, remarks: event.target.value })} value={props.form.remarks} /></FormField>
        <button className="shell-button w-full" disabled={props.isSaving} onClick={props.onSave} type="button">Save Vesting Rule</button>
      </div>
    }>
      <Table headers={['Policy', 'Years', 'Vested %', 'Actions']}>
        {props.rules.map((rule) => (
          <tr key={rule.id}>
            <td>{rule.policyName}</td>
            <td>{rule.yearsOfService}</td>
            <td>{rule.vestedPercentage}%</td>
            <td className="space-x-2">
              <button className="shell-button-secondary px-3 py-2" onClick={() => props.onEdit(rule)} type="button">Edit</button>
              <button className="shell-button-danger px-3 py-2" onClick={() => props.onDelete(rule)} type="button">Delete</button>
            </td>
          </tr>
        ))}
      </Table>
    </GridWithEditor>
  )
}

function EnrollmentSection(props: {
  enrollments: ProvidentFundEnrollment[]
  form: ProvidentFundEnrollmentInput
  isSaving: boolean
  options: ProvidentFundOptions | null
  onEdit: (record: ProvidentFundEnrollment) => void
  onFormChange: (form: ProvidentFundEnrollmentInput) => void
  onSave: () => void
}) {
  return (
    <GridWithEditor title="Employee enrollment" editor={
      <div className="space-y-4">
        <EmployeeSelect employees={props.options?.employees ?? []} value={props.form.employeeId} onChange={(employeeId) => props.onFormChange({ ...props.form, employeeId })} />
        <PolicySelect options={props.options} value={props.form.policyId} onChange={(policyId) => props.onFormChange({ ...props.form, policyId })} />
        <div className="grid gap-4 md:grid-cols-2">
          <FormField label="Enrollment Date"><input className="shell-input" onChange={(event) => props.onFormChange({ ...props.form, enrollmentDate: event.target.value })} type="date" value={props.form.enrollmentDate} /></FormField>
          <FormField label="Vesting Start"><input className="shell-input" onChange={(event) => props.onFormChange({ ...props.form, vestingStartDate: event.target.value })} type="date" value={props.form.vestingStartDate} /></FormField>
          <FormField label="Status"><StatusSelect statuses={['active', 'suspended', 'closed']} value={props.form.status} onChange={(status) => props.onFormChange({ ...props.form, status })} /></FormField>
        </div>
        <FormField label="Remarks"><textarea className="shell-textarea" onChange={(event) => props.onFormChange({ ...props.form, remarks: event.target.value })} value={props.form.remarks} /></FormField>
        <button className="shell-button w-full" disabled={props.isSaving} onClick={props.onSave} type="button">Save Enrollment</button>
      </div>
    }>
      <Table headers={['Employee', 'Policy', 'Dates', 'Balances', 'Status', 'Actions']}>
        {props.enrollments.map((record) => (
          <tr key={record.id}>
            <td><b>{record.employeeFullName}</b><div className="mt-1 text-slate-500">{record.employeeCode} | {record.departmentName}</div></td>
            <td>{record.policyName}</td>
            <td>{formatDate(record.enrollmentDate)}<div className="mt-1 text-slate-500">Vesting {formatDate(record.vestingStartDate)}</div></td>
            <td>{formatCurrency(record.grossBalance)}<div className="mt-1 text-slate-500">Withdrawable {formatCurrency(record.withdrawableBalance)}</div></td>
            <td><StatusBadge status={record.status} /></td>
            <td><button className="shell-button-secondary px-3 py-2" onClick={() => props.onEdit(record)} type="button">Edit</button></td>
          </tr>
        ))}
      </Table>
    </GridWithEditor>
  )
}

function ContributionSection(props: {
  batches: ProvidentFundContributionBatch[]
  form: GenerateProvidentFundContributionBatchInput
  isSaving: boolean
  options: ProvidentFundOptions | null
  selectedBatch: ProvidentFundContributionBatchDetail | null
  onBatchAction: (action: 'review' | 'post' | 'cancel', batchId: string) => void
  onFormChange: (form: GenerateProvidentFundContributionBatchInput) => void
  onGenerate: () => void
  onSelectBatch: (batchId: string) => void
}) {
  return (
    <div className="space-y-6">
      <GridWithEditor title="Contribution processing" editor={
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <FormField label="Month"><NumberInput value={props.form.month} onChange={(month) => props.onFormChange({ ...props.form, month })} /></FormField>
            <FormField label="Year"><NumberInput value={props.form.year} onChange={(year) => props.onFormChange({ ...props.form, year })} /></FormField>
          </div>
          <PolicySelect options={props.options} value={props.form.policyId ?? ''} onChange={(policyId) => props.onFormChange({ ...props.form, policyId })} />
          <Checkbox label="Supplemental batch" checked={props.form.isSupplemental} onChange={(isSupplemental) => props.onFormChange({ ...props.form, isSupplemental })} />
          <FormField label="Batch Number"><input className="shell-input" onChange={(event) => props.onFormChange({ ...props.form, batchNumber: event.target.value })} placeholder="Auto when blank" value={props.form.batchNumber} /></FormField>
          <FormField label="Remarks"><textarea className="shell-textarea" onChange={(event) => props.onFormChange({ ...props.form, remarks: event.target.value })} value={props.form.remarks} /></FormField>
          <button className="shell-button w-full" disabled={props.isSaving} onClick={props.onGenerate} type="button">Generate Preview</button>
        </div>
      }>
        <Table headers={['Batch', 'Period', 'Totals', 'Status', 'Actions']}>
          {props.batches.map((batch) => (
            <tr key={batch.id}>
              <td><b>{batch.batchNumber}</b><div className="mt-1 text-slate-500">{batch.policyName || 'All policies'}</div></td>
              <td>{batch.month}/{batch.year}</td>
              <td>{formatCurrency(batch.totalContribution)}<div className="mt-1 text-slate-500">{batch.lineCount} lines</div></td>
              <td><StatusBadge status={batch.status} /></td>
              <td className="flex flex-wrap gap-2">
                <button className="shell-button-secondary px-3 py-2" onClick={() => props.onSelectBatch(batch.id)} type="button">Preview</button>
                {batch.status === 'draft' ? <button className="shell-button-secondary px-3 py-2" onClick={() => props.onBatchAction('review', batch.id)} type="button">Review</button> : null}
                {batch.status === 'reviewed' ? <button className="shell-button px-3 py-2" onClick={() => props.onBatchAction('post', batch.id)} type="button">Post</button> : null}
                {batch.status !== 'posted' && batch.status !== 'cancelled' ? <button className="shell-button-danger px-3 py-2" onClick={() => props.onBatchAction('cancel', batch.id)} type="button">Cancel</button> : null}
              </td>
            </tr>
          ))}
        </Table>
      </GridWithEditor>
      {props.selectedBatch ? (
        <section className="shell-card p-6">
          <h3 className="text-lg font-semibold text-slate-950">Batch preview: {props.selectedBatch.batch.batchNumber}</h3>
          <div className="mt-5">
            <Table headers={['Employee', 'Salary', 'Employee', 'Employer', 'Voluntary', 'Total', 'Status']}>
              {props.selectedBatch.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.employeeFullName}<div className="text-slate-500">{line.employeeCode}</div></td>
                  <td>{formatCurrency(line.basicSalary)}</td>
                  <td>{formatCurrency(line.employeeContribution)}</td>
                  <td>{formatCurrency(line.employerContribution)}</td>
                  <td>{formatCurrency(line.voluntaryContribution)}</td>
                  <td>{formatCurrency(line.totalContribution)}</td>
                  <td><StatusBadge status={line.status} /></td>
                </tr>
              ))}
            </Table>
          </div>
        </section>
      ) : null}
    </div>
  )
}

function BalanceSection({ balance, employeeId, employees, onEmployeeChange }: { balance: ProvidentFundBalance | null; employeeId: string; employees: { id: string; employeeCode: string; fullName: string }[]; onEmployeeChange: (employeeId: string) => void }) {
  return (
    <section className="shell-card p-6">
      <div className="max-w-lg">
        <EmployeeSelect employees={employees} value={employeeId} onChange={onEmployeeChange} />
      </div>
      {balance ? (
        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Gross Balance" value={formatCurrency(balance.grossFundBalance)} />
          <SummaryCard label="Withdrawable" value={formatCurrency(balance.withdrawableBalance)} tone="success" />
          <SummaryCard label="Vested Employer" value={formatCurrency(balance.vestedEmployerBalance)} />
          <SummaryCard label="Non-Vested Employer" value={formatCurrency(balance.nonVestedEmployerBalance)} tone="warning" />
          <SummaryCard label="Employee Share" value={formatCurrency(balance.totalEmployeeContribution)} />
          <SummaryCard label="Employer Share" value={formatCurrency(balance.totalEmployerContribution)} />
          <SummaryCard label="Voluntary + Interest" value={formatCurrency(balance.totalVoluntaryContribution + balance.totalInterest)} />
          <SummaryCard label="Withdrawals" value={formatCurrency(balance.totalWithdrawals)} tone="danger" />
        </div>
      ) : <div className="shell-state-empty mt-5">Select an employee to view computed provident fund balance.</div>}
    </section>
  )
}

function LedgerSection({ ledger, onReverse }: { ledger: ProvidentFundLedgerTransaction[]; onReverse: (record: ProvidentFundLedgerTransaction) => void }) {
  return (
    <section className="shell-card p-6">
      <Table headers={['Date', 'Transaction', 'Employee', 'Type', 'Shares', 'Debit', 'Credit', 'Actions']}>
        {ledger.map((record) => (
          <tr key={record.id}>
            <td>{formatDate(record.transactionDate)}</td>
            <td><b>{record.transactionNumber}</b><div className="text-slate-500">{record.remarks}</div></td>
            <td>{record.employeeFullName}<div className="text-slate-500">{record.employeeCode}</div></td>
            <td>{toTitle(record.transactionType)}</td>
            <td>Emp {formatCurrency(record.employeeShareAmount)}<div className="text-slate-500">Er {formatCurrency(record.employerShareAmount)}</div></td>
            <td>{formatCurrency(record.debitAmount)}</td>
            <td>{formatCurrency(record.creditAmount)}</td>
            <td>{record.isReversed ? <StatusBadge status="reversed" /> : <button className="shell-button-danger px-3 py-2" onClick={() => onReverse(record)} type="button">Reverse</button>}</td>
          </tr>
        ))}
      </Table>
    </section>
  )
}

function WithdrawalSection(props: {
  form: ProvidentFundWithdrawalInput
  isSaving: boolean
  options: ProvidentFundOptions | null
  requests: ProvidentFundWithdrawalRequest[]
  onAction: (action: 'submit' | 'approve' | 'reject' | 'paid', request: ProvidentFundWithdrawalRequest) => void
  onFormChange: (form: ProvidentFundWithdrawalInput) => void
  onSave: () => void
}) {
  return (
    <GridWithEditor title="Withdrawal requests" editor={
      <div className="space-y-4">
        <EmployeeSelect employees={props.options?.employees ?? []} value={props.form.employeeId ?? ''} onChange={(employeeId) => props.onFormChange({ ...props.form, employeeId })} />
        <FormField label="Type"><StatusSelect statuses={props.options?.withdrawalTypes ?? ['partial', 'full']} value={props.form.withdrawalType} onChange={(withdrawalType) => props.onFormChange({ ...props.form, withdrawalType })} /></FormField>
        <FormField label="Request Date"><input className="shell-input" onChange={(event) => props.onFormChange({ ...props.form, requestDate: event.target.value })} type="date" value={props.form.requestDate} /></FormField>
        <FormField label="Amount"><NumberInput value={props.form.requestedAmount} onChange={(requestedAmount) => props.onFormChange({ ...props.form, requestedAmount })} /></FormField>
        <FormField label="Reason"><textarea className="shell-textarea" onChange={(event) => props.onFormChange({ ...props.form, reason: event.target.value })} value={props.form.reason} /></FormField>
        <button className="shell-button w-full" disabled={props.isSaving} onClick={props.onSave} type="button">Create Withdrawal</button>
      </div>
    }>
      <Table headers={['Request', 'Employee', 'Amounts', 'Status', 'Payment', 'Actions']}>
        {props.requests.map((request) => (
          <tr key={request.id}>
            <td><b>{request.requestNumber}</b><div className="text-slate-500">{toTitle(request.withdrawalType)} | {formatDate(request.requestDate)}</div></td>
            <td>{request.employeeFullName}<div className="text-slate-500">{request.departmentName}</div></td>
            <td>Req {formatCurrency(request.requestedAmount)}<div className="text-slate-500">Appr {formatCurrency(request.approvedAmount)}</div></td>
            <td><StatusBadge status={request.status} /></td>
            <td>{request.paymentDate ? formatDateTime(request.paymentDate) : 'Not paid'}</td>
            <td className="flex flex-wrap gap-2">
              {request.status === 'draft' ? <button className="shell-button-secondary px-3 py-2" onClick={() => props.onAction('submit', request)} type="button">Submit</button> : null}
              {['submitted', 'hr_reviewed', 'finance_reviewed'].includes(request.status) ? <button className="shell-button px-3 py-2" onClick={() => props.onAction('approve', request)} type="button">Approve</button> : null}
              {['submitted', 'hr_reviewed', 'finance_reviewed'].includes(request.status) ? <button className="shell-button-danger px-3 py-2" onClick={() => props.onAction('reject', request)} type="button">Reject</button> : null}
              {request.status === 'approved' ? <button className="shell-button px-3 py-2" onClick={() => props.onAction('paid', request)} type="button">Mark Paid</button> : null}
            </td>
          </tr>
        ))}
      </Table>
    </GridWithEditor>
  )
}

function AdjustmentSection(props: {
  adjustments: ProvidentFundAdjustment[]
  form: ProvidentFundAdjustmentInput
  isSaving: boolean
  options: ProvidentFundOptions | null
  onAction: (action: 'approve' | 'reject' | 'post', adjustment: ProvidentFundAdjustment) => void
  onFormChange: (form: ProvidentFundAdjustmentInput) => void
  onSave: () => void
}) {
  return (
    <GridWithEditor title="Manual adjustments" editor={
      <div className="space-y-4">
        <EmployeeSelect employees={props.options?.employees ?? []} value={props.form.employeeId} onChange={(employeeId) => props.onFormChange({ ...props.form, employeeId })} />
        <FormField label="Adjustment Type"><StatusSelect statuses={props.options?.adjustmentTypes ?? ['credit', 'debit']} value={props.form.adjustmentType} onChange={(adjustmentType) => props.onFormChange({ ...props.form, adjustmentType })} /></FormField>
        <FormField label="Share Affected"><StatusSelect statuses={props.options?.shareTypes ?? ['employee', 'employer', 'voluntary', 'interest']} value={props.form.shareAffected} onChange={(shareAffected) => props.onFormChange({ ...props.form, shareAffected })} /></FormField>
        <FormField label="Date"><input className="shell-input" onChange={(event) => props.onFormChange({ ...props.form, adjustmentDate: event.target.value })} type="date" value={props.form.adjustmentDate} /></FormField>
        <FormField label="Amount"><NumberInput value={props.form.amount} onChange={(amount) => props.onFormChange({ ...props.form, amount })} /></FormField>
        <FormField label="Reason"><textarea className="shell-textarea" onChange={(event) => props.onFormChange({ ...props.form, reason: event.target.value })} value={props.form.reason} /></FormField>
        <button className="shell-button w-full" disabled={props.isSaving} onClick={props.onSave} type="button">Create Adjustment</button>
      </div>
    }>
      <Table headers={['Employee', 'Adjustment', 'Amount', 'Status', 'Actions']}>
        {props.adjustments.map((adjustment) => (
          <tr key={adjustment.id}>
            <td>{adjustment.employeeFullName}<div className="text-slate-500">{adjustment.employeeCode}</div></td>
            <td>{toTitle(adjustment.adjustmentType)} {toTitle(adjustment.shareAffected)}<div className="text-slate-500">{adjustment.reason}</div></td>
            <td>{formatCurrency(adjustment.amount)}</td>
            <td><StatusBadge status={adjustment.status} /></td>
            <td className="flex flex-wrap gap-2">
              {adjustment.status === 'draft' ? <button className="shell-button px-3 py-2" onClick={() => props.onAction('approve', adjustment)} type="button">Approve</button> : null}
              {adjustment.status === 'draft' ? <button className="shell-button-danger px-3 py-2" onClick={() => props.onAction('reject', adjustment)} type="button">Reject</button> : null}
              {adjustment.status === 'approved' ? <button className="shell-button px-3 py-2" onClick={() => props.onAction('post', adjustment)} type="button">Post</button> : null}
            </td>
          </tr>
        ))}
      </Table>
    </GridWithEditor>
  )
}

function ReportsSection(props: {
  balanceRows: ProvidentFundBalanceReportRow[]
  contributionRows: ProvidentFundContributionReportRow[]
  ledgerRows: ProvidentFundLedgerTransaction[]
  onLoad: () => void
  withdrawalRows: ProvidentFundWithdrawalReportRow[]
}) {
  return (
    <div className="space-y-6">
      <section className="shell-card p-6">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-950">Provident fund reports</h3>
            <p className="mt-1 text-sm text-slate-500">Report tables use the active filters above.</p>
          </div>
          <button className="shell-button" onClick={props.onLoad} type="button">Run Reports</button>
        </div>
      </section>
      <ReportBlock
        title="Monthly Contribution Report"
        count={props.contributionRows.length}
        onExport={() => exportCsv(
          'provident-fund-monthly-contributions.csv',
          ['Employee Number', 'Employee Name', 'Department', 'Basic Salary', 'Employee Contribution', 'Employer Contribution', 'Voluntary Contribution', 'Total Contribution', 'Batch Status'],
          props.contributionRows.map((row) => [
            row.employeeNumber,
            row.employeeName,
            row.department,
            row.basicSalary,
            row.employeeContribution,
            row.employerContribution,
            row.voluntaryContribution,
            row.totalContribution,
            row.batchStatus,
          ]),
        )}
      >
        <Table headers={['Employee', 'Department', 'Salary', 'Employee', 'Employer', 'Voluntary', 'Total', 'Status']}>
          {props.contributionRows.map((row, index) => (
            <tr key={`${row.employeeNumber}-${index}`}>
              <td>{row.employeeName}<div className="text-slate-500">{row.employeeNumber}</div></td>
              <td>{row.department}</td>
              <td>{formatCurrency(row.basicSalary)}</td>
              <td>{formatCurrency(row.employeeContribution)}</td>
              <td>{formatCurrency(row.employerContribution)}</td>
              <td>{formatCurrency(row.voluntaryContribution)}</td>
              <td>{formatCurrency(row.totalContribution)}</td>
              <td>{toTitle(row.batchStatus)}</td>
            </tr>
          ))}
        </Table>
      </ReportBlock>
      <ReportBlock
        title="Employee Fund Balance Report"
        count={props.balanceRows.length}
        onExport={() => exportCsv(
          'provident-fund-balances.csv',
          ['Employee Number', 'Employee Name', 'Employee Share', 'Employer Share', 'Vested Employer Share', 'Non-Vested Employer Share', 'Interest', 'Withdrawals', 'Current Balance', 'Withdrawable Balance'],
          props.balanceRows.map((row) => [
            row.employeeNumber,
            row.employeeName,
            row.totalEmployeeShare,
            row.totalEmployerShare,
            row.vestedEmployerShare,
            row.nonVestedEmployerShare,
            row.interest,
            row.withdrawals,
            row.currentBalance,
            row.withdrawableBalance,
          ]),
        )}
      >
        <Table headers={['Employee', 'Employee Share', 'Employer Share', 'Vested', 'Non-Vested', 'Withdrawals', 'Balance', 'Withdrawable']}>
          {props.balanceRows.map((row, index) => (
            <tr key={`${row.employeeNumber}-${index}`}>
              <td>{row.employeeName}<div className="text-slate-500">{row.employeeNumber}</div></td>
              <td>{formatCurrency(row.totalEmployeeShare)}</td>
              <td>{formatCurrency(row.totalEmployerShare)}</td>
              <td>{formatCurrency(row.vestedEmployerShare)}</td>
              <td>{formatCurrency(row.nonVestedEmployerShare)}</td>
              <td>{formatCurrency(row.withdrawals)}</td>
              <td>{formatCurrency(row.currentBalance)}</td>
              <td>{formatCurrency(row.withdrawableBalance)}</td>
            </tr>
          ))}
        </Table>
      </ReportBlock>
      <ReportBlock
        title="Withdrawal Report"
        count={props.withdrawalRows.length}
        onExport={() => exportCsv(
          'provident-fund-withdrawals.csv',
          ['Request Number', 'Employee', 'Request Date', 'Withdrawal Type', 'Requested Amount', 'Approved Amount', 'Status', 'Payment Date'],
          props.withdrawalRows.map((row) => [
            row.requestNumber,
            row.employee,
            row.requestDate,
            row.withdrawalType,
            row.requestedAmount,
            row.approvedAmount,
            row.status,
            row.paymentDate ?? '',
          ]),
        )}
      >
        <Table headers={['Request', 'Employee', 'Date', 'Type', 'Requested', 'Approved', 'Status', 'Paid']}>
          {props.withdrawalRows.map((row) => (
            <tr key={row.requestNumber}>
              <td>{row.requestNumber}</td>
              <td>{row.employee}</td>
              <td>{formatDate(row.requestDate)}</td>
              <td>{toTitle(row.withdrawalType)}</td>
              <td>{formatCurrency(row.requestedAmount)}</td>
              <td>{formatCurrency(row.approvedAmount)}</td>
              <td>{toTitle(row.status)}</td>
              <td>{row.paymentDate ? formatDateTime(row.paymentDate) : ''}</td>
            </tr>
          ))}
        </Table>
      </ReportBlock>
      <ReportBlock
        title="Ledger Transaction Report"
        count={props.ledgerRows.length}
        onExport={() => exportCsv(
          'provident-fund-ledger.csv',
          ['Transaction Date', 'Transaction Number', 'Employee', 'Transaction Type', 'Employee Share', 'Employer Share', 'Voluntary Share', 'Interest', 'Debit', 'Credit', 'Running Balance', 'Remarks'],
          props.ledgerRows.map((row) => [
            row.transactionDate,
            row.transactionNumber,
            row.employeeFullName,
            row.transactionType,
            row.employeeShareAmount,
            row.employerShareAmount,
            row.voluntaryShareAmount,
            row.interestAmount,
            row.debitAmount,
            row.creditAmount,
            row.runningBalance ?? '',
            row.remarks,
          ]),
        )}
      >
        <Table headers={['Date', 'Transaction', 'Employee', 'Type', 'Debit', 'Credit', 'Remarks']}>
          {props.ledgerRows.map((row) => (
            <tr key={row.id}>
              <td>{formatDate(row.transactionDate)}</td>
              <td>{row.transactionNumber}</td>
              <td>{row.employeeFullName}</td>
              <td>{toTitle(row.transactionType)}</td>
              <td>{formatCurrency(row.debitAmount)}</td>
              <td>{formatCurrency(row.creditAmount)}</td>
              <td>{row.remarks}</td>
            </tr>
          ))}
        </Table>
      </ReportBlock>
    </div>
  )
}

function GridWithEditor({ children, editor, title }: { children: ReactNode; editor: ReactNode; title: string }) {
  return (
    <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
      <div className="min-w-0 shell-card p-5 sm:p-6">{children}</div>
      <aside className="shell-card p-5 sm:p-6">
        <h3 className="text-lg font-semibold text-slate-950">{title}</h3>
        <div className="mt-5">{editor}</div>
      </aside>
    </section>
  )
}

function ReportBlock({ children, count, onExport, title }: { children: ReactNode; count: number; onExport?: () => void; title: string }) {
  return (
    <section className="shell-card p-6">
      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-lg font-semibold text-slate-950">{title}</h3>
        <div className="flex items-center gap-2">
          {onExport ? <button className="shell-button-secondary px-3 py-2 text-xs" onClick={onExport} type="button">Export CSV</button> : null}
          <span className="shell-badge-muted">{count} rows</span>
        </div>
      </div>
      {children}
    </section>
  )
}

function Table({ children, headers }: { children: ReactNode; headers: string[] }) {
  return (
    <div className="shell-table-wrap">
      <table className="shell-table">
        <thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead>
        <tbody>
          {children}
          {Array.isArray(children) && children.length === 0 ? <tr><td className="text-slate-500" colSpan={headers.length}>No records found.</td></tr> : null}
        </tbody>
      </table>
    </div>
  )
}

function SummaryCard({ label, tone = 'default', value }: { label: string; tone?: 'default' | 'success' | 'warning' | 'danger'; value: string }) {
  const toneClass = tone === 'success' ? 'text-emerald-700' : tone === 'warning' ? 'text-amber-700' : tone === 'danger' ? 'text-rose-700' : 'text-slate-950'
  return (
    <div className="shell-summary-card">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">{label}</p>
      <p className={`mt-3 text-xl font-semibold ${toneClass}`}>{value}</p>
    </div>
  )
}

function StatusBadge({ status }: { status: string }) {
  const className =
    ['active', 'posted', 'approved', 'paid', 'reviewed'].includes(status) ? 'shell-badge-success' :
      ['draft', 'submitted', 'pending', 'hr_reviewed', 'finance_reviewed'].includes(status) ? 'shell-badge-warning' :
        ['rejected', 'cancelled', 'closed', 'reversed'].includes(status) ? 'shell-badge-danger' : 'shell-badge-muted'
  return <span className={className}>{toTitle(status)}</span>
}

function FormField({ children, label }: { children: ReactNode; label: string }) {
  return <label className="block"><span className="shell-label">{label}</span>{children}</label>
}

function EmployeeSelect({ employees, onChange, value }: { employees: { id: string; employeeCode: string; fullName: string }[]; onChange: (value: string) => void; value: string }) {
  return (
    <FormField label="Employee">
      <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">Select employee</option>
        {employees.map((employee) => <option key={employee.id} value={employee.id}>{employee.employeeCode} | {employee.fullName}</option>)}
      </select>
    </FormField>
  )
}

function PolicySelect({ onChange, options, value }: { onChange: (value: string) => void; options: ProvidentFundOptions | null; value: string }) {
  return (
    <FormField label="Policy">
      <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">Select policy</option>
        {options?.policies.map((policy) => <option key={policy.id} value={policy.id}>{policy.policyName}</option>)}
      </select>
    </FormField>
  )
}

function ContributionTypeSelect({ onChange, value }: { onChange: (value: string) => void; value: string }) {
  return <StatusSelect statuses={['percentage', 'fixed_amount']} value={value} onChange={onChange} />
}

function StatusSelect({ onChange, statuses, value }: { onChange: (value: string) => void; statuses: string[]; value: string }) {
  return (
    <select className="shell-select" onChange={(event) => onChange(event.target.value)} value={value}>
      {statuses.map((status) => <option key={status} value={status}>{toTitle(status)}</option>)}
    </select>
  )
}

function NumberInput({ onChange, value }: { onChange: (value: number) => void; value?: number | null }) {
  return <input className="shell-input" onChange={(event) => onChange(Number(event.target.value))} step="0.01" type="number" value={value ?? 0} />
}

function Checkbox({ checked, label, onChange }: { checked: boolean; label: string; onChange: (checked: boolean) => void }) {
  return (
    <label className="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-medium text-slate-700">
      <input checked={checked} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
      {label}
    </label>
  )
}

function exportCsv(fileName: string, headers: string[], rows: (string | number | null | undefined)[][]) {
  const lines = [
    headers.map(formatCsvCell).join(','),
    ...rows.map((row) => row.map(formatCsvCell).join(',')),
  ]
  const blob = new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' })
  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  window.URL.revokeObjectURL(url)
}

function formatCsvCell(value: string | number | null | undefined) {
  return `"${String(value ?? '').replace(/"/g, '""')}"`
}

function toTitle(value: string) {
  return value.replace(/_/g, ' ').replace(/\b\w/g, (match) => match.toUpperCase())
}

function toPolicyInput(policy: ProvidentFundPolicy): ProvidentFundPolicyInput {
  return {
    policyName: policy.policyName,
    description: policy.description,
    eligibilityRules: policy.eligibilityRules,
    employeeContributionType: policy.employeeContributionType,
    employeeContributionValue: policy.employeeContributionValue,
    employerContributionType: policy.employerContributionType,
    employerContributionValue: policy.employerContributionValue,
    contributionFrequency: policy.contributionFrequency,
    effectiveDate: policy.effectiveDate,
    status: policy.status,
    allowVoluntaryContribution: policy.allowVoluntaryContribution,
    allowWithdrawal: policy.allowWithdrawal,
    allowLoan: policy.allowLoan,
    remarks: policy.remarks,
  }
}

function toEnrollmentInput(record: ProvidentFundEnrollment): ProvidentFundEnrollmentInput {
  return {
    employeeId: record.employeeId,
    policyId: record.policyId,
    enrollmentDate: record.enrollmentDate,
    vestingStartDate: record.vestingStartDate,
    employeeContributionOverrideType: record.employeeContributionOverrideType,
    employeeContributionOverrideValue: record.employeeContributionOverrideValue,
    employerContributionOverrideType: record.employerContributionOverrideType,
    employerContributionOverrideValue: record.employerContributionOverrideValue,
    status: record.status,
    remarks: record.remarks,
  }
}
