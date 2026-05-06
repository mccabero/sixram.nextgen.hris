/* eslint-disable react-hooks/set-state-in-effect */
import { cloneElement, isValidElement, type ReactElement, type ReactNode, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { ErrorState, LoadingState } from '../components/ContentState'
import { PageSection } from '../components/PageSection'
import type { EmployeeDetail, EmployeeEditorOptions, SaveEmployeeInput } from '../types/models'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const genderOptions = ['Male', 'Female', 'Non-Binary', 'Prefer not to say']
const civilStatusOptions = ['Single', 'Married', 'Separated', 'Widowed']

const emptyForm: SaveEmployeeInput = {
  employeeCode: '',
  firstName: '',
  middleName: '',
  lastName: '',
  suffix: '',
  gender: '',
  birthDate: '',
  civilStatus: '',
  nationality: '',
  mobileNumber: '',
  email: '',
  address: '',
  cityProvince: '',
  postalCode: '',
  emergencyContactName: '',
  emergencyContactRelationship: '',
  emergencyContactPhone: '',
  departmentId: null,
  positionId: null,
  branchId: null,
  employmentTypeId: null,
  employmentStatusId: null,
  managerId: null,
  workSchedule: '',
  dateHired: '',
  dateRegularized: '',
  dateSeparated: '',
  sssNumber: '',
  philHealthNumber: '',
  pagIbigNumber: '',
  tinNumber: '',
  otherGovernmentId: '',
  userId: null,
  isActive: true,
}

type FieldControlProps = {
  id?: string
  'aria-invalid'?: boolean
  'aria-describedby'?: string
}

export function EmployeeFormPage() {
  const { employeeId } = useParams<{ employeeId: string }>()
  const navigate = useNavigate()
  const isEditing = Boolean(employeeId)

  const [form, setForm] = useState<SaveEmployeeInput>(emptyForm)
  const [options, setOptions] = useState<EmployeeEditorOptions | null>(null)
  const [employee, setEmployee] = useState<EmployeeDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  async function loadFormData() {
    setIsLoading(true)

    try {
      const [loadedOptions, loadedEmployee] = await Promise.all([
        sixramApi.getEmployeeOptions(employeeId),
        employeeId ? sixramApi.getEmployeeById(employeeId) : Promise.resolve(null),
      ])

      setOptions(loadedOptions)
      setEmployee(loadedEmployee)
      setForm(loadedEmployee ? mapEmployeeToForm(loadedEmployee) : emptyForm)
      setFieldErrors({})
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadFormData()
  }, [employeeId])

  const filteredPositions = useMemo(() => {
    if (!options) {
      return []
    }

    if (!form.departmentId) {
      return options.positions
    }

    return options.positions.filter((option) => !option.parentId || option.parentId === form.departmentId)
  }, [form.departmentId, options])

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      const saved = isEditing && employeeId
        ? await sixramApi.updateEmployee(employeeId, sanitizeForm(form))
        : await sixramApi.createEmployee(sanitizeForm(form))

      navigate(`/admin/employees/${saved.id}`, { replace: true })
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  function updateField<K extends keyof SaveEmployeeInput>(key: K, value: SaveEmployeeInput[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  if (isLoading) {
    return <LoadingState message="Loading employee form..." />
  }

  if (!options) {
    return <ErrorState message={error ?? 'Unable to load the employee form.'} />
  }

  return (
    <form className="mx-auto max-w-7xl space-y-6 pb-10" onSubmit={handleSubmit}>
      <PageSection
        actions={(
          <>
            <Link className="shell-button-secondary" to={isEditing && employeeId ? `/admin/employees/${employeeId}` : '/admin/employees'}>
              Cancel
            </Link>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Employee'}
            </button>
          </>
        )}
        description="Capture the employee's personal, contact, employment, compliance, and account-linking details in one profile."
        kicker={isEditing ? 'Edit Employee' : 'New Employee'}
        title={isEditing ? employee?.fullName ?? 'Employee Profile' : 'Create Employee Profile'}
      >
        {error ? <ErrorState message={error} /> : null}
      </PageSection>

      <FormSection description="Core employee identity, display name parts, and demographic details." title="Personal Information">
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <FormField error={getFieldError(fieldErrors, 'EmployeeCode', 'employeeCode')} label="Employee Code" required>
            <input className="shell-input" onChange={(event) => updateField('employeeCode', event.target.value)} value={form.employeeCode} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'FirstName', 'firstName')} label="First Name" required>
            <input className="shell-input" onChange={(event) => updateField('firstName', event.target.value)} value={form.firstName} />
          </FormField>
          <FormField label="Middle Name">
            <input className="shell-input" onChange={(event) => updateField('middleName', event.target.value)} value={form.middleName} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'LastName', 'lastName')} label="Last Name" required>
            <input className="shell-input" onChange={(event) => updateField('lastName', event.target.value)} value={form.lastName} />
          </FormField>
          <FormField label="Suffix">
            <input className="shell-input" onChange={(event) => updateField('suffix', event.target.value)} value={form.suffix} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'Gender', 'gender')} label="Gender" required>
            <select className="shell-select" onChange={(event) => updateField('gender', event.target.value)} value={form.gender}>
              <option value="">Select gender</option>
              {genderOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Birthdate">
            <input className="shell-input" onChange={(event) => updateField('birthDate', event.target.value)} type="date" value={form.birthDate ?? ''} />
          </FormField>
          <FormField label="Civil Status">
            <select className="shell-select" onChange={(event) => updateField('civilStatus', event.target.value)} value={form.civilStatus}>
              <option value="">Select civil status</option>
              {civilStatusOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Nationality">
            <input className="shell-input" onChange={(event) => updateField('nationality', event.target.value)} value={form.nationality} />
          </FormField>
        </div>
      </FormSection>

      <FormSection description="Employee contact details and emergency contact information." title="Contact Information">
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <FormField label="Mobile Number">
            <input className="shell-input" onChange={(event) => updateField('mobileNumber', event.target.value)} value={form.mobileNumber} />
          </FormField>
          <FormField
            error={getFieldError(fieldErrors, 'Email', 'email')}
            hint="Use the employee's primary work email where possible."
            label="Email"
            required
          >
            <input className="shell-input" onChange={(event) => updateField('email', event.target.value)} type="email" value={form.email} />
          </FormField>
          <FormField className="xl:col-span-2" label="Address">
            <input className="shell-input" onChange={(event) => updateField('address', event.target.value)} value={form.address} />
          </FormField>
          <FormField label="City / Province">
            <input className="shell-input" onChange={(event) => updateField('cityProvince', event.target.value)} value={form.cityProvince} />
          </FormField>
          <FormField label="ZIP / Postal Code">
            <input className="shell-input" onChange={(event) => updateField('postalCode', event.target.value)} value={form.postalCode} />
          </FormField>
          <FormField label="Emergency Contact Name">
            <input className="shell-input" onChange={(event) => updateField('emergencyContactName', event.target.value)} value={form.emergencyContactName} />
          </FormField>
          <FormField label="Relationship">
            <input className="shell-input" onChange={(event) => updateField('emergencyContactRelationship', event.target.value)} value={form.emergencyContactRelationship} />
          </FormField>
          <FormField label="Emergency Contact Phone">
            <input className="shell-input" onChange={(event) => updateField('emergencyContactPhone', event.target.value)} value={form.emergencyContactPhone} />
          </FormField>
        </div>
      </FormSection>

      <FormSection description="Organization setup references, reporting line, and employment dates." title="Employment Information">
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <FormField error={getFieldError(fieldErrors, 'DepartmentId', 'departmentId')} label="Department" required>
            <select
              className="shell-select"
              onChange={(event) => {
                const nextDepartmentId = event.target.value || null
                updateField('departmentId', nextDepartmentId)

                const canKeepPosition = options.positions.some(
                  (option) =>
                    option.id === form.positionId &&
                    (!nextDepartmentId || !option.parentId || option.parentId === nextDepartmentId),
                )

                if (!canKeepPosition) {
                  updateField('positionId', null)
                }
              }}
              value={form.departmentId ?? ''}
            >
              <option value="">Select department</option>
              {options.departments.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'PositionId', 'positionId')} label="Position / Job Title" required>
            <select className="shell-select" onChange={(event) => updateField('positionId', event.target.value || null)} value={form.positionId ?? ''}>
              <option value="">Select position</option>
              {filteredPositions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'BranchId', 'branchId')} label="Branch / Location" required>
            <select className="shell-select" onChange={(event) => updateField('branchId', event.target.value || null)} value={form.branchId ?? ''}>
              <option value="">Select branch</option>
              {options.branches.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'EmploymentTypeId', 'employmentTypeId')} label="Employment Type" required>
            <select className="shell-select" onChange={(event) => updateField('employmentTypeId', event.target.value || null)} value={form.employmentTypeId ?? ''}>
              <option value="">Select employment type</option>
              {options.employmentTypes.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'EmploymentStatusId', 'employmentStatusId')} label="Employment Status" required>
            <select className="shell-select" onChange={(event) => updateField('employmentStatusId', event.target.value || null)} value={form.employmentStatusId ?? ''}>
              <option value="">Select employment status</option>
              {options.employmentStatuses.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Reporting Manager">
            <select className="shell-select" onChange={(event) => updateField('managerId', event.target.value || null)} value={form.managerId ?? ''}>
              <option value="">No manager assigned</option>
              {options.managers.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.fullName} ({option.employeeCode})
                </option>
              ))}
            </select>
          </FormField>
          <FormField hint="Leave blank when schedules are assigned from the Attendance module." label="Work Schedule">
            <input className="shell-input" onChange={(event) => updateField('workSchedule', event.target.value)} value={form.workSchedule} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'DateHired', 'dateHired')} label="Date Hired" required>
            <input className="shell-input" onChange={(event) => updateField('dateHired', event.target.value)} type="date" value={form.dateHired ?? ''} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'DateRegularized', 'dateRegularized')} label="Date Regularized">
            <input className="shell-input" onChange={(event) => updateField('dateRegularized', event.target.value)} type="date" value={form.dateRegularized ?? ''} />
          </FormField>
          <FormField error={getFieldError(fieldErrors, 'DateSeparated', 'dateSeparated')} label="Date Resigned / Terminated">
            <input className="shell-input" onChange={(event) => updateField('dateSeparated', event.target.value)} type="date" value={form.dateSeparated ?? ''} />
          </FormField>
          <div className="flex items-end xl:col-span-3">
            <label className="inline-flex min-h-[46px] items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
              <input checked={form.isActive} onChange={(event) => updateField('isActive', event.target.checked)} type="checkbox" />
              Active employee profile
            </label>
          </div>
        </div>
      </FormSection>

      <FormSection description="Philippine government and compliance identifiers." title="Government / Compliance IDs">
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <FormField label="SSS">
            <input className="shell-input" onChange={(event) => updateField('sssNumber', event.target.value)} value={form.sssNumber} />
          </FormField>
          <FormField label="PhilHealth">
            <input className="shell-input" onChange={(event) => updateField('philHealthNumber', event.target.value)} value={form.philHealthNumber} />
          </FormField>
          <FormField label="Pag-IBIG">
            <input className="shell-input" onChange={(event) => updateField('pagIbigNumber', event.target.value)} value={form.pagIbigNumber} />
          </FormField>
          <FormField label="TIN">
            <input className="shell-input" onChange={(event) => updateField('tinNumber', event.target.value)} value={form.tinNumber} />
          </FormField>
          <FormField className="xl:col-span-2" label="Other ID">
            <input className="shell-input" onChange={(event) => updateField('otherGovernmentId', event.target.value)} value={form.otherGovernmentId} />
          </FormField>
        </div>
      </FormSection>

      <FormSection description="Optional link to an application login without forcing user creation." title="System / User Link">
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          <FormField className="xl:max-w-md" label="Linked User Account">
            <select className="shell-select" onChange={(event) => updateField('userId', event.target.value || null)} value={form.userId ?? ''}>
              <option value="">No linked user account</option>
              {options.userAccounts.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.displayName} ({option.email})
                </option>
              ))}
            </select>
          </FormField>
        </div>
      </FormSection>
    </form>
  )
}

function mapEmployeeToForm(employee: EmployeeDetail): SaveEmployeeInput {
  return {
    employeeCode: employee.employeeCode,
    firstName: employee.firstName,
    middleName: employee.middleName,
    lastName: employee.lastName,
    suffix: employee.suffix,
    gender: employee.gender,
    birthDate: employee.birthDate ?? '',
    civilStatus: employee.civilStatus,
    nationality: employee.nationality,
    mobileNumber: employee.mobileNumber,
    email: employee.email,
    address: employee.address,
    cityProvince: employee.cityProvince,
    postalCode: employee.postalCode,
    emergencyContactName: employee.emergencyContactName,
    emergencyContactRelationship: employee.emergencyContactRelationship,
    emergencyContactPhone: employee.emergencyContactPhone,
    departmentId: employee.departmentId ?? null,
    positionId: employee.positionId ?? null,
    branchId: employee.branchId ?? null,
    employmentTypeId: employee.employmentTypeId ?? null,
    employmentStatusId: employee.employmentStatusId ?? null,
    managerId: employee.managerId ?? null,
    workSchedule: employee.workSchedule,
    dateHired: employee.dateHired ?? '',
    dateRegularized: employee.dateRegularized ?? '',
    dateSeparated: employee.dateSeparated ?? '',
    sssNumber: employee.sssNumber,
    philHealthNumber: employee.philHealthNumber,
    pagIbigNumber: employee.pagIbigNumber,
    tinNumber: employee.tinNumber,
    otherGovernmentId: employee.otherGovernmentId,
    userId: employee.userId || null,
    isActive: employee.isActive,
  }
}

function sanitizeForm(form: SaveEmployeeInput): SaveEmployeeInput {
  return {
    ...form,
    birthDate: form.birthDate || null,
    dateHired: form.dateHired || null,
    dateRegularized: form.dateRegularized || null,
    dateSeparated: form.dateSeparated || null,
    managerId: form.managerId || null,
    userId: form.userId || null,
  }
}

function FormSection({
  title,
  description,
  children,
}: {
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <section className="shell-form-section">
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-slate-950">{title}</h3>
        <p className="mt-2 text-sm text-slate-500">{description}</p>
      </div>
      {children}
    </section>
  )
}

function FormField({
  label,
  error,
  hint,
  required = false,
  inputId,
  className = '',
  children,
}: {
  label: string
  error?: string | null
  hint?: string
  required?: boolean
  inputId?: string
  className?: string
  children: ReactNode
}) {
  const generatedId = inputId ?? `field-${label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`
  const existingId = isValidElement(children) ? (children.props as FieldControlProps).id : undefined
  const controlId = existingId ?? generatedId
  const describedBy = [hint ? `${controlId}-hint` : null, error ? `${controlId}-error` : null].filter(Boolean).join(' ') || undefined
  const control = isValidElement(children)
    ? cloneElement(children as ReactElement<FieldControlProps>, {
      id: controlId,
      'aria-invalid': error ? true : undefined,
      'aria-describedby': describedBy,
    })
    : children

  return (
    <div className={`space-y-2 ${className}`}>
      <label className="shell-label mb-0" htmlFor={controlId}>
        {label}
        {required ? <span className="shell-required">Required</span> : null}
      </label>
      {control}
      {hint && !error ? (
        <p className="shell-helper" id={`${controlId}-hint`}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p className="text-sm text-rose-600" id={`${controlId}-error`}>
          {error}
        </p>
      ) : null}
    </div>
  )
}
