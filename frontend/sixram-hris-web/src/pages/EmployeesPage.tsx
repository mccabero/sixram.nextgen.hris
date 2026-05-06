import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { ErrorState, LoadingState, EmptyState } from '../components/ContentState'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { MetricCard } from '../components/MetricCard'
import { PageSection } from '../components/PageSection'
import { PaginationControls } from '../components/PaginationControls'
import type { EmployeeListQuery, EmployeeSummary, OrganizationOptions, PagedResult } from '../types/models'
import { formatDate } from '../utils/date'
import { formatError } from '../utils/errors'

const initialQuery: EmployeeListQuery = {
  search: '',
  departmentId: '',
  branchId: '',
  employmentStatusId: '',
  isActive: true,
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'name',
  descending: false,
}

export function EmployeesPage() {
  const [result, setResult] = useState<PagedResult<EmployeeSummary> | null>(null)
  const [options, setOptions] = useState<OrganizationOptions | null>(null)
  const [query, setQuery] = useState<EmployeeListQuery>(initialQuery)
  const [draftSearch, setDraftSearch] = useState(initialQuery.search ?? '')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<EmployeeSummary | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    void loadOptions()
  }, [])

  useEffect(() => {
    void loadEmployees()
  }, [query])

  async function loadOptions() {
    try {
      const response = await sixramApi.getOrganizationOptions()
      setOptions(response)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadEmployees() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getEmployees(query)
      setResult(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handleDeleteConfirmed() {
    if (!deleteTarget) {
      return
    }

    setIsDeleting(true)

    try {
      await sixramApi.deleteEmployee(deleteTarget.id)
      setDeleteTarget(null)
      await loadEmployees()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsDeleting(false)
    }
  }

  const activeCount = result?.items.filter((item) => item.isActive).length ?? 0
  const inactiveCount = (result?.items.length ?? 0) - activeCount

  return (
    <div className="space-y-6">
      <PageSection
        actions={
          <>
            <Link className="shell-button-secondary" to="/admin/production-readiness">
              Go-live checklist
            </Link>
            <Link className="shell-button" to="/admin/employees/new">
              Add Employee
            </Link>
          </>
        }
        description="Maintain employee master records, organization assignments, compliance IDs, and linked user accounts with a responsive review workspace."
        kicker="Employee Master"
        title="Employee profiles"
      >
        <div className="shell-summary-grid">
          <MetricCard detail="Employees currently returned by your active filter set." label="Filtered total" tone="brand" value={String(result?.totalCount ?? 0)} />
          <MetricCard detail="Active employees visible in the current page result." label="Active on page" tone="success" value={String(activeCount)} />
          <MetricCard detail="Inactive employees visible in the current page result." label="Inactive on page" tone="warning" value={String(Math.max(inactiveCount, 0))} />
          <MetricCard detail="Use the production-readiness workspace for bulk employee import." label="Operational note" value="CSV import ready" />
        </div>

        <div className="shell-toolbar mt-6">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-[1.8fr_repeat(3,minmax(0,1fr))]">
            <div>
              <label className="shell-label shell-required" htmlFor="employee-search">
                Search
              </label>
              <input
                className="shell-input"
                id="employee-search"
                onChange={(event) => setDraftSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    setQuery((current) => ({
                      ...current,
                      search: draftSearch,
                      pageNumber: 1,
                    }))
                  }
                }}
                placeholder="Search code, name, email..."
                value={draftSearch}
              />
              <p className="shell-helper">Search by employee code, full name, email, or mobile number.</p>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-department-filter">
                Department
              </label>
              <select
                className="shell-select"
                id="employee-department-filter"
                onChange={(event) =>
                  setQuery((current) => ({
                    ...current,
                    departmentId: event.target.value,
                    pageNumber: 1,
                  }))
                }
                value={query.departmentId ?? ''}
              >
                <option value="">All departments</option>
                {options?.departments.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-branch-filter">
                Branch
              </label>
              <select
                className="shell-select"
                id="employee-branch-filter"
                onChange={(event) =>
                  setQuery((current) => ({
                    ...current,
                    branchId: event.target.value,
                    pageNumber: 1,
                  }))
                }
                value={query.branchId ?? ''}
              >
                <option value="">All branches</option>
                {options?.branches.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="shell-label" htmlFor="employee-active-filter">
                Status
              </label>
              <select
                className="shell-select"
                id="employee-active-filter"
                onChange={(event) => {
                  const value = event.target.value
                  setQuery((current) => ({
                    ...current,
                    isActive: value === '' ? null : value === 'true',
                    pageNumber: 1,
                  }))
                }}
                value={query.isActive === null || query.isActive === undefined ? '' : String(query.isActive)}
              >
                <option value="">All</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
          </div>

          <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
            <button
              className="shell-button"
              onClick={() =>
                setQuery((current) => ({
                  ...current,
                  search: draftSearch,
                  pageNumber: 1,
                }))
              }
              type="button"
            >
              Apply Filters
            </button>
            <button
              className="shell-button-secondary"
              onClick={() => {
                setDraftSearch('')
                setQuery(initialQuery)
              }}
              type="button"
            >
              Reset
            </button>
            <button className="shell-button-secondary" onClick={() => void loadEmployees()} type="button">
              Refresh
            </button>
          </div>
        </div>

        {error ? <ErrorState className="mt-6" description={error} title="Employee workspace unavailable" /> : null}

        <div className="shell-table-wrap mt-6">
          <div className="border-b border-slate-200 px-5 py-4 text-sm text-slate-500">
            Swipe horizontally on smaller screens to review all employee fields and actions.
          </div>
          <table className="shell-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Role</th>
                <th>Status</th>
                <th>Date Hired</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={5}>
                    <LoadingState className="border-0 bg-transparent px-0 py-0 shadow-none" description="Loading employee profiles..." />
                  </td>
                </tr>
              ) : result?.items.length ? (
                result.items.map((employee) => (
                  <tr key={employee.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{employee.fullName}</div>
                      <div className="mt-1 text-slate-500">{employee.employeeCode}</div>
                      <div className="mt-1 text-slate-500">{employee.email || employee.mobileNumber || 'No contact details'}</div>
                    </td>
                    <td>
                      <div className="font-medium text-slate-900">{employee.positionName || '-'}</div>
                      <div className="mt-1 text-slate-500">
                        {[employee.departmentName, employee.branchName].filter(Boolean).join(' | ') || 'No assignment'}
                      </div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <span className="shell-badge-muted">{employee.employmentStatusName || 'Unassigned'}</span>
                        <span className={employee.isActive ? 'shell-badge-success' : 'shell-badge-danger'}>
                          {employee.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                    </td>
                    <td className="text-slate-600">{formatDate(employee.dateHired)}</td>
                    <td>
                      <div className="shell-table-actions">
                        <Link className="shell-button-secondary" to={`/admin/employees/${employee.id}`}>
                          View
                        </Link>
                        <Link className="shell-button-secondary" to={`/admin/employees/${employee.id}/edit`}>
                          Edit
                        </Link>
                        <button className="shell-button-danger" onClick={() => setDeleteTarget(employee)} type="button">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5}>
                    <EmptyState
                      className="border-0 bg-transparent px-0 py-0"
                      description="Try widening the search scope, clearing a filter, or creating a new employee profile."
                      title="No employee profiles matched the current filters"
                    />
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <PaginationControls
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
            pageNumber={result?.pageNumber ?? 1}
            pageSize={result?.pageSize ?? query.pageSize ?? 10}
            totalCount={result?.totalCount ?? 0}
            totalPages={result?.totalPages ?? 0}
          />
        </div>
      </PageSection>

      <ConfirmDialog
        confirmLabel="Delete Employee"
        description={
          deleteTarget
            ? `Delete employee profile ${deleteTarget.fullName} (${deleteTarget.employeeCode})? This will remove the master profile and clear manager links from direct reports.`
            : ''
        }
        isBusy={isDeleting}
        onCancel={() => {
          if (!isDeleting) {
            setDeleteTarget(null)
          }
        }}
        onConfirm={() => void handleDeleteConfirmed()}
        open={Boolean(deleteTarget)}
        title={deleteTarget ? `Delete ${deleteTarget.fullName}` : 'Delete Employee'}
      />
    </div>
  )
}
