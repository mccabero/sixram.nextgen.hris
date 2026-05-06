/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type { Payslip } from '../types/models'
import { formatDate, formatDateTime, formatMinutes } from '../utils/date'
import { formatError } from '../utils/errors'
import { formatCurrency } from '../utils/money'

export function MyPayslipDetailPage() {
  const { payrollRunItemId } = useParams<{ payrollRunItemId: string }>()
  const [payslip, setPayslip] = useState<Payslip | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function loadPayslip(id: string) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getMyPayslip(id)
      setPayslip(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    if (!payrollRunItemId) {
      return
    }

    void loadPayslip(payrollRunItemId)
  }, [payrollRunItemId])

  if (isLoading) {
    return <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading payslip...</div>
  }

  if (!payslip) {
    return (
      <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
        {error ?? 'Payslip not found.'}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7 print:shadow-none">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">My payslip</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">{payslip.employeeName}</h3>
            <p className="mt-2 text-sm text-slate-500">
              {payslip.companyName} | {payslip.payrollRunReferenceNumber}
            </p>
          </div>

          <div className="flex flex-wrap gap-3 print:hidden">
            <Link className="shell-button-secondary" to="/me/payslips">
              Back to Payslips
            </Link>
            <button className="shell-button" onClick={() => window.print()} type="button">
              Print Payslip
            </button>
          </div>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <InfoCard label="Pay Period" value={payslip.payPeriodName} />
          <InfoCard label="Payroll Date" value={formatDate(payslip.payrollDate)} />
          <InfoCard label="Employee Code" value={payslip.employeeCode} />
          <InfoCard label="Generated" value={formatDateTime(payslip.generatedAtUtc)} />
        </div>

        <div className="mt-6 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <p className="text-sm font-semibold text-slate-900">Employee information</p>
            <div className="mt-4 space-y-3 text-sm text-slate-600">
              <p><span className="font-medium text-slate-900">Department:</span> {payslip.departmentName || '-'}</p>
              <p><span className="font-medium text-slate-900">Position:</span> {payslip.positionName || '-'}</p>
              <p><span className="font-medium text-slate-900">Branch:</span> {payslip.branchName || '-'}</p>
              <p><span className="font-medium text-slate-900">Period:</span> {formatDate(payslip.periodStartDate)} - {formatDate(payslip.periodEndDate)}</p>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <SummaryCard label="Gross Pay" value={formatCurrency(payslip.grossPay, payslip.currency)} />
            <SummaryCard label="Deductions" value={formatCurrency(payslip.totalDeductions, payslip.currency)} />
            <SummaryCard label="Net Pay" tone="success" value={formatCurrency(payslip.netPay, payslip.currency)} />
          </div>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <section className="shell-card fade-up p-6 sm:p-7">
          <h4 className="text-lg font-semibold text-slate-950">Earnings</h4>
          <div className="mt-5 space-y-3">
            {payslip.earnings.map((line) => (
              <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3" key={line.id}>
                <div>
                  <p className="text-sm font-semibold text-slate-900">{line.earningTypeName || line.description}</p>
                  <p className="mt-1 text-xs text-slate-500">{line.description}</p>
                </div>
                <p className="text-sm font-semibold text-slate-900">{formatCurrency(line.amount, payslip.currency)}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="shell-card fade-up p-6 sm:p-7">
          <h4 className="text-lg font-semibold text-slate-950">Deductions</h4>
          <div className="mt-5 space-y-3">
            {payslip.deductions.map((line) => (
              <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3" key={line.id}>
                <div>
                  <p className="text-sm font-semibold text-slate-900">{line.deductionTypeName || line.description}</p>
                  <p className="mt-1 text-xs text-slate-500">{line.description}</p>
                </div>
                <p className="text-sm font-semibold text-slate-900">{formatCurrency(line.amount, payslip.currency)}</p>
              </div>
            ))}
          </div>
        </section>
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <h4 className="text-lg font-semibold text-slate-950">Attendance and leave summary</h4>
        <div className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <InfoCard label="Worked Days" value={`${payslip.regularWorkedDays}`} />
          <InfoCard label="Worked Hours" value={`${payslip.regularWorkedHours}`} />
          <InfoCard label="Paid Leave Days" value={`${payslip.paidLeaveDays}`} />
          <InfoCard label="Unpaid Leave Days" value={`${payslip.unpaidLeaveDays}`} />
          <InfoCard label="Absent Days" value={`${payslip.absentDays}`} />
          <InfoCard label="Late Minutes" value={formatMinutes(payslip.lateMinutes)} />
          <InfoCard label="Undertime Minutes" value={formatMinutes(payslip.undertimeMinutes)} />
          <InfoCard label="Overtime Minutes" value={formatMinutes(payslip.overtimeMinutes)} />
        </div>
      </section>

      {payslip.issues.length || payslip.remarks ? (
        <section className="shell-card fade-up p-6 sm:p-7">
          <h4 className="text-lg font-semibold text-slate-950">Issues and remarks</h4>
          <div className="mt-5 space-y-3">
            {payslip.issues.map((issue) => (
              <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800" key={issue}>
                {issue}
              </div>
            ))}
            {payslip.remarks ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
                {payslip.remarks}
              </div>
            ) : null}
          </div>
        </section>
      ) : null}
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
    <div className={`rounded-2xl border p-5 ${className}`}>
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-3 text-2xl font-semibold text-slate-950">{value}</p>
    </div>
  )
}

function InfoCard({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  )
}
