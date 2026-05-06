/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { PaginationControls } from '../components/PaginationControls'
import type { MyPayslipListQuery, PagedResult, PayslipSummary } from '../types/models'
import { formatDate } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

const currentYear = new Date().getFullYear()

const defaultQuery: MyPayslipListQuery = {
  year: currentYear,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'payroll_date',
  descending: true,
}

export function MyPayslipsPage() {
  const [payslips, setPayslips] = useState<PagedResult<PayslipSummary> | null>(null)
  const [query, setQuery] = useState<MyPayslipListQuery>(defaultQuery)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function loadPayslips(nextQuery: MyPayslipListQuery) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getMyPayslips(nextQuery)
      setPayslips(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadPayslips(query)
  }, [query.pageNumber, query.year])

  const latestPayslip = payslips?.items[0] ?? null
  const yearOptions = useMemo(() => Array.from({ length: 6 }, (_, index) => currentYear - index), [])

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">My payslips</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">Payroll history you can safely access</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Only approved or visible payroll runs are exposed here, following the payroll visibility rules configured by the organization.
            </p>
          </div>

          <label className="block min-w-[160px] space-y-2">
            <span className="shell-label mb-0">Year</span>
            <select
              className="shell-select"
              onChange={(event) => setQuery((current) => ({ ...current, year: Number(event.target.value), pageNumber: 1 }))}
              value={query.year ?? currentYear}
            >
              {yearOptions.map((year) => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </label>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <section className="shell-card fade-up p-6 sm:p-7">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Latest visible payslip</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Most recent payroll snapshot</h3>
          </div>

          {!latestPayslip ? (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
              No visible payslip is available for the selected year yet.
            </div>
          ) : (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-5">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm text-slate-500">{latestPayslip.payPeriodName}</p>
                  <p className="mt-2 text-3xl font-semibold text-slate-950">
                    {formatCurrency(latestPayslip.netPay, latestPayslip.currency)}
                  </p>
                  <p className="mt-2 text-sm text-slate-500">Payroll date {formatDate(latestPayslip.payrollDate)}</p>
                </div>
                <span className="shell-badge-success">{latestPayslip.status.replace(/_/g, ' ')}</span>
              </div>

              <div className="mt-5">
                <Link className="shell-button-secondary" to={`/me/payslips/${latestPayslip.payrollRunItemId}`}>
                  Open payslip
                </Link>
              </div>
            </div>
          )}
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <div className="grid gap-4 md:grid-cols-3">
            <SummaryCard label="Visible payslips" value={String(payslips?.totalCount ?? 0)} />
            <SummaryCard
              label="Gross amount"
              value={latestPayslip ? formatCurrency(latestPayslip.grossPay, latestPayslip.currency) : '-'}
            />
            <SummaryCard
              label="Net amount"
              value={latestPayslip ? formatCurrency(latestPayslip.netPay, latestPayslip.currency) : '-'}
            />
          </div>
        </section>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">History</p>
          <h3 className="mt-2 text-2xl font-semibold text-slate-950">Payslip list</h3>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Pay Period</th>
                <th>Payroll Date</th>
                <th>Gross</th>
                <th>Net</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    Loading payslips...
                  </td>
                </tr>
              ) : !payslips || payslips.items.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={6}>
                    No visible payslips are available for the selected year.
                  </td>
                </tr>
              ) : (
                payslips.items.map((payslip) => (
                  <tr key={payslip.payrollRunItemId}>
                    <td>
                      <div className="font-semibold text-slate-900">{payslip.payPeriodName}</div>
                      <div className="mt-1 text-slate-500">
                        {formatDate(payslip.periodStartDate)} to {formatDate(payslip.periodEndDate)}
                      </div>
                    </td>
                    <td className="text-slate-500">{formatDate(payslip.payrollDate)}</td>
                    <td className="text-slate-500">{formatCurrency(payslip.grossPay, payslip.currency)}</td>
                    <td className="font-semibold text-slate-900">{formatCurrency(payslip.netPay, payslip.currency)}</td>
                    <td>
                      <span className="shell-badge-success">{payslip.status.replace(/_/g, ' ')}</span>
                    </td>
                    <td className="text-right">
                      <Link className="shell-button-secondary px-3 py-2" to={`/me/payslips/${payslip.payrollRunItemId}`}>
                        View
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {payslips ? (
          <PaginationControls
            pageNumber={payslips.pageNumber}
            pageSize={payslips.pageSize}
            totalCount={payslips.totalCount}
            totalPages={payslips.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>
    </div>
  )
}

function SummaryCard({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-3 text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}
