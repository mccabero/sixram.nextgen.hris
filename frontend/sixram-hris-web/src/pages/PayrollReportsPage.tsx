/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import type { PayrollOptions, PayrollReportQuery, PayrollReports } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

const defaultQuery: PayrollReportQuery = {
  payPeriodId: '',
  payrollRunId: '',
  employeeId: '',
  departmentId: '',
  branchId: '',
  employmentTypeId: '',
  status: '',
  dateFrom: '',
  dateTo: '',
}

export function PayrollReportsPage() {
  const [options, setOptions] = useState<PayrollOptions | null>(null)
  const [reports, setReports] = useState<PayrollReports | null>(null)
  const [query, setQuery] = useState<PayrollReportQuery>(defaultQuery)
  const [error, setError] = useState<string | null>(null)
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isLoadingReports, setIsLoadingReports] = useState(false)

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

  async function loadReports() {
    setIsLoadingReports(true)

    try {
      const response = await sixramApi.getPayrollReports(query)
      setReports(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoadingReports(false)
    }
  }

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!options) {
      return
    }

    void loadReports()
  }, [options, query])

  if (isInitialLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading payroll reports...</div>
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Payroll Reports</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Payroll register and summary views</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Review payroll totals, register entries, department and branch rollups, component summaries, and adjustments from one reporting view.
            </p>
          </div>

          <button className="shell-button-secondary" onClick={() => void loadReports()} type="button">
            Refresh
          </button>
        </div>

        <div className="mt-5 grid gap-4 lg:grid-cols-4">
          <FormField label="Pay Period">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, payPeriodId: event.target.value }))} value={query.payPeriodId ?? ''}>
              <option value="">All pay periods</option>
              {options?.payPeriods.map((period) => (
                <option key={period.id} value={period.id}>
                  {period.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Payroll Run">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, payrollRunId: event.target.value }))} value={query.payrollRunId ?? ''}>
              <option value="">All payroll runs</option>
            </select>
          </FormField>
          <FormField label="Department">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, departmentId: event.target.value }))} value={query.departmentId ?? ''}>
              <option value="">All departments</option>
              {options?.departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Branch">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, branchId: event.target.value }))} value={query.branchId ?? ''}>
              <option value="">All branches</option>
              {options?.branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </FormField>
        </div>

        <div className="mt-4 grid gap-4 lg:grid-cols-4">
          <FormField label="Employee">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, employeeId: event.target.value }))} value={query.employeeId ?? ''}>
              <option value="">All employees</option>
              {options?.employees.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.employeeCode} | {employee.fullName}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Employment Type">
            <select className="shell-select" onChange={(event) => setQuery((current) => ({ ...current, employmentTypeId: event.target.value }))} value={query.employmentTypeId ?? ''}>
              <option value="">All employment types</option>
              {options?.employmentTypes.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Date From">
            <input className="shell-input" onChange={(event) => setQuery((current) => ({ ...current, dateFrom: event.target.value }))} type="date" value={query.dateFrom ?? ''} />
          </FormField>
          <FormField label="Date To">
            <input className="shell-input" onChange={(event) => setQuery((current) => ({ ...current, dateTo: event.target.value }))} type="date" value={query.dateTo ?? ''} />
          </FormField>
        </div>
      </section>

      {error ? (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
      ) : null}

      <section className="grid gap-6 md:grid-cols-3">
        <SummaryCard label="Gross Pay" value={formatCurrency(reports?.totalGrossPay ?? 0)} />
        <SummaryCard label="Total Deductions" value={formatCurrency(reports?.totalDeductions ?? 0)} />
        <SummaryCard label="Net Pay" value={formatCurrency(reports?.totalNetPay ?? 0)} tone="success" />
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Payroll register</h4>
            <p className="mt-1 text-sm text-slate-500">Employee-level payroll snapshot from the generated payroll items.</p>
          </div>
          <span className="shell-badge-muted">{reports?.register.length ?? 0} rows</span>
        </div>

        <div className="shell-table-wrap mt-5">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Work Summary</th>
                <th>Gross</th>
                <th>Deductions</th>
                <th>Net</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {isLoadingReports ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading payroll register...
                  </td>
                </tr>
              ) : reports?.register.length ? (
                reports.register.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{item.employeeName}</div>
                      <div className="mt-1 text-slate-500">{item.employeeCode}</div>
                      <div className="mt-1 text-slate-500">{[item.departmentName, item.branchName].filter(Boolean).join(' | ')}</div>
                    </td>
                    <td className="text-slate-600">
                      <div>{item.regularWorkedDays} days | {item.regularWorkedHours} hrs</div>
                      <div className="mt-1 text-xs text-slate-500">Late {item.lateMinutes}m | Under {item.undertimeMinutes}m | OT {item.overtimeMinutes}m</div>
                    </td>
                    <td className="text-slate-600">{formatCurrency(item.grossPay, item.currency)}</td>
                    <td className="text-slate-600">{formatCurrency(item.totalDeductions, item.currency)}</td>
                    <td className="font-semibold text-slate-900">{formatCurrency(item.netPay, item.currency)}</td>
                    <td className="text-slate-600">{formatDateTime(item.createdAtUtc)}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No payroll register data available for the selected filters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <ReportGroupCard groups={reports?.byDepartment ?? []} title="Summary by department" />
        <ReportGroupCard groups={reports?.byBranch ?? []} title="Summary by branch" />
      </section>

      <section className="grid gap-6 xl:grid-cols-3">
        <ReportLineCard lines={reports?.earnings ?? []} title="Earnings breakdown" />
        <ReportLineCard lines={reports?.deductions ?? []} title="Deduction breakdown" />
        <ReportLineCard lines={reports?.governmentContributions ?? []} title="Government contributions" />
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-lg font-semibold text-slate-950">Adjustments report</h4>
            <p className="mt-1 text-sm text-slate-500">Review one-time earnings and deductions that flowed into payroll processing.</p>
          </div>
          <span className="shell-badge-muted">{reports?.adjustments.length ?? 0} items</span>
        </div>

        <div className="mt-5 space-y-3">
          {reports?.adjustments.length ? (
            reports.adjustments.map((record) => (
              <div className="rounded-2xl border border-slate-200 bg-white p-4" key={record.id}>
                <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-900">{record.employeeFullName}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      {record.adjustmentType} | {record.earningTypeName || record.deductionTypeName || 'Manual'}
                    </p>
                    <p className="mt-1 text-xs text-slate-500">{record.reason}</p>
                  </div>
                  <div className="text-left xl:text-right">
                    <p className="text-sm font-semibold text-slate-900">{formatCurrency(record.amount)}</p>
                    <p className="mt-1 text-xs text-slate-500">{formatDateTime(record.createdAtUtc)}</p>
                  </div>
                </div>
              </div>
            ))
          ) : (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">
              No adjustments found for the selected filters.
            </div>
          )}
        </div>
      </section>
    </div>
  )
}

function FormField({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div>
      <label className="shell-label">{label}</label>
      {children}
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
  tone?: 'default' | 'success'
}) {
  const className = tone === 'success' ? 'border-emerald-200 bg-emerald-50' : 'border-slate-200 bg-white'

  return (
    <div className={`shell-card p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function ReportGroupCard({
  title,
  groups,
}: {
  title: string
  groups: PayrollReports['byDepartment']
}) {
  return (
    <section className="shell-card fade-up p-6 sm:p-7">
      <h4 className="text-lg font-semibold text-slate-950">{title}</h4>
      <div className="mt-5 space-y-3">
        {groups.length ? (
          groups.map((group) => (
            <div className="rounded-2xl border border-slate-200 bg-white p-4" key={group.label}>
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-900">{group.label}</p>
                  <p className="mt-1 text-xs text-slate-500">{group.count} items</p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-semibold text-slate-900">{formatCurrency(group.netPay)}</p>
                  <p className="mt-1 text-xs text-slate-500">
                    Gross {formatCurrency(group.grossPay)} | Deduct {formatCurrency(group.deductions)}
                  </p>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No grouped data available.</div>
        )}
      </div>
    </section>
  )
}

function ReportLineCard({
  title,
  lines,
}: {
  title: string
  lines: PayrollReports['earnings']
}) {
  return (
    <section className="shell-card fade-up p-6 sm:p-7">
      <h4 className="text-lg font-semibold text-slate-950">{title}</h4>
      <div className="mt-5 space-y-3">
        {lines.length ? (
          lines.map((line) => (
            <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3" key={line.label}>
              <p className="text-sm font-medium text-slate-700">{line.label}</p>
              <p className="text-sm font-semibold text-slate-950">{formatCurrency(line.amount)}</p>
            </div>
          ))
        ) : (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-500">No line summary available.</div>
        )}
      </div>
    </section>
  )
}
