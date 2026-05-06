/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import type { OrganizationSummary } from '../types/models'
import { formatError } from '../utils/errors'

export function OrganizationSetupPage() {
  const [summary, setSummary] = useState<OrganizationSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function loadSummary() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getOrganizationSummary()
      setSummary(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadSummary()
  }, [])

  const sections = [
    {
      title: 'Departments',
      description: 'Business unit groupings used across employee records and position assignments.',
      link: '/admin/organization/departments',
      total: summary?.departmentCount ?? 0,
      active: summary?.activeDepartmentCount ?? 0,
    },
    {
      title: 'Positions',
      description: 'Job titles and role definitions, optionally tied to departments.',
      link: '/admin/organization/positions',
      total: summary?.positionCount ?? 0,
      active: summary?.activePositionCount ?? 0,
    },
    {
      title: 'Branches',
      description: 'Office or site locations assigned to employees.',
      link: '/admin/organization/branches',
      total: summary?.branchCount ?? 0,
      active: summary?.activeBranchCount ?? 0,
    },
    {
      title: 'Employment Types',
      description: 'Employment arrangements such as regular, probationary, or contractual.',
      link: '/admin/organization/employment-types',
      total: summary?.employmentTypeCount ?? 0,
      active: summary?.activeEmploymentTypeCount ?? 0,
    },
    {
      title: 'Employment Statuses',
      description: 'Operational employment states such as active, regularized, resigned, or terminated.',
      link: '/admin/organization/employment-statuses',
      total: summary?.employmentStatusCount ?? 0,
      active: summary?.activeEmploymentStatusCount ?? 0,
    },
  ]

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Organization Setup</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Foundation tables for the HR modules</h3>
            <p className="mt-2 max-w-3xl text-sm text-slate-500">
              Maintain the reusable master data referenced by employee profiles today and by attendance, leave,
              payroll, and reporting modules later.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            Employees: {summary?.employeeCount ?? 0} | Active Profiles: {summary?.activeEmployeeCount ?? 0}
          </div>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">{error}</div>
        ) : null}
      </section>

      {isLoading ? (
        <div className="shell-card px-5 py-4 text-sm text-slate-500">Loading organization setup summary...</div>
      ) : null}

      <section className="grid gap-4 lg:grid-cols-2 xl:grid-cols-3">
        {sections.map((section) => (
          <Link className="shell-card block p-5 transition hover:border-[#465fff]/30 hover:bg-[#465fff]/5" key={section.link} to={section.link}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <h4 className="text-lg font-semibold text-slate-950">{section.title}</h4>
                <p className="mt-2 text-sm leading-6 text-slate-500">{section.description}</p>
              </div>
              <span className="shell-badge-brand">{section.active} active</span>
            </div>

            <div className="mt-5 text-sm text-slate-600">
              {section.total} total records
            </div>
          </Link>
        ))}
      </section>
    </div>
  )
}
