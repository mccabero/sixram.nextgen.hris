import { useEffect, useState, type ReactNode } from 'react'
import { sixramApi } from '../api/sixramApi'
import { EmptyState, ErrorState, LoadingState } from '../components/ContentState'
import { Modal } from '../components/Modal'
import { PageSection } from '../components/PageSection'
import { RequestStatusBadge } from '../components/RequestStatusBadge'
import type { EmployeeSelfProfile, ProfileChangeRequest, SaveProfileChangeRequestInput } from '../types/models'
import { formatDate, formatDateTime } from '../utils/date'
import { formatError, getFieldError, getValidationErrors } from '../utils/errors'

const emptyEditor: SaveProfileChangeRequestInput = {
  mobileNumber: '',
  email: '',
  address: '',
  cityProvince: '',
  postalCode: '',
  civilStatus: '',
  nationality: '',
  emergencyContactName: '',
  emergencyContactRelationship: '',
  emergencyContactPhone: '',
  reason: '',
}

export function MyProfilePage() {
  const [profile, setProfile] = useState<EmployeeSelfProfile | null>(null)
  const [requests, setRequests] = useState<ProfileChangeRequest[]>([])
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [editor, setEditor] = useState<SaveProfileChangeRequestInput>(emptyEditor)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [editorOpen, setEditorOpen] = useState(false)

  useEffect(() => {
    void loadPage()
  }, [])

  async function loadPage() {
    setIsLoading(true)

    try {
      const [profileResponse, requestResponse] = await Promise.all([
        sixramApi.getMyProfile(),
        sixramApi.getMyProfileChangeRequests({ pageNumber: 1, pageSize: 10, descending: true }),
      ])

      setProfile(profileResponse)
      setRequests(requestResponse.items)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  function openRequestModal() {
    if (!profile) {
      return
    }

    setFieldErrors({})
    setEditor({
      mobileNumber: profile.mobileNumber,
      email: profile.email,
      address: profile.address,
      cityProvince: profile.cityProvince,
      postalCode: profile.postalCode,
      civilStatus: profile.civilStatus,
      nationality: profile.nationality,
      emergencyContactName: profile.emergencyContactName,
      emergencyContactRelationship: profile.emergencyContactRelationship,
      emergencyContactPhone: profile.emergencyContactPhone,
      reason: '',
    })
    setEditorOpen(true)
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFieldErrors({})
    setError(null)

    try {
      await sixramApi.createMyProfileChangeRequest(editor)
      setEditorOpen(false)
      await loadPage()
    } catch (caughtError) {
      setError(formatError(caughtError))
      setFieldErrors(getValidationErrors(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCancelRequest(requestId: string) {
    try {
      await sixramApi.cancelMyProfileChangeRequest(requestId, { remarks: 'Cancelled by employee.' })
      await loadPage()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  if (isLoading) {
    return <LoadingState message="Loading your profile..." />
  }

  if (error && !profile) {
    return <ErrorState message={error} />
  }

  if (!profile) {
    return <EmptyState message="No employee profile is linked to this account." title="Employee profile unavailable" />
  }

  return (
    <div className="space-y-6">
      <PageSection
        actions={(
          <button className="shell-button" onClick={openRequestModal} type="button">
            Request profile update
          </button>
        )}
        description="Sensitive employment details stay protected. Use a profile change request when you need HR to update personal information."
        kicker="My profile"
        title={profile.fullName}
      />

      {error ? <ErrorState message={error} /> : null}

      <section className="grid gap-6 xl:grid-cols-3">
        <ProfileCard
          title="Personal"
          items={[
            ['Employee code', profile.employeeCode],
            ['Gender', profile.gender],
            ['Birthdate', formatDate(profile.birthDate)],
            ['Civil status', profile.civilStatus],
            ['Nationality', profile.nationality],
          ]}
        />
        <ProfileCard
          title="Contact"
          items={[
            ['Mobile number', profile.mobileNumber],
            ['Email', profile.email],
            ['Address', profile.address],
            ['City / Province', profile.cityProvince],
            ['ZIP / Postal code', profile.postalCode],
          ]}
        />
        <ProfileCard
          title="Employment"
          items={[
            ['Department', profile.departmentName],
            ['Position', profile.positionName],
            ['Branch', profile.branchName],
            ['Employment type', profile.employmentTypeName],
            ['Employment status', profile.employmentStatusName],
            ['Manager', profile.managerName],
            ['Date hired', formatDate(profile.dateHired)],
          ]}
        />
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <ProfileCard
          title="Emergency contact"
          items={[
            ['Name', profile.emergencyContactName],
            ['Relationship', profile.emergencyContactRelationship],
            ['Phone', profile.emergencyContactPhone],
          ]}
        />
        <ProfileCard
          title="Government IDs"
          description="Masked values help protect sensitive identifiers in self-service views."
          items={[
            ['SSS', profile.sssNumberMasked],
            ['PhilHealth', profile.philHealthNumberMasked],
            ['Pag-IBIG', profile.pagIbigNumberMasked],
            ['TIN', profile.tinNumberMasked],
            ['Other ID', profile.otherGovernmentIdMasked],
          ]}
        />
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Requests</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Profile change history</h3>
            <p className="mt-2 text-sm text-slate-500">Track pending, approved, rejected, and cancelled profile updates from one list.</p>
          </div>
        </div>

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>Request</th>
                <th>Fields</th>
                <th>Status</th>
                <th>Updated</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {requests.length === 0 ? (
                <tr>
                  <td colSpan={5}>
                    <EmptyState message="You have not submitted any profile change requests yet." title="No request history yet" />
                  </td>
                </tr>
              ) : (
                requests.map((request) => (
                  <tr key={request.id}>
                    <td>
                      <div className="font-semibold text-slate-900">Personal profile update</div>
                      <div className="mt-1 text-slate-500">{request.reason || 'No reason provided.'}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {request.fieldChanges.map((change) => (
                          <span className="shell-badge-muted" key={`${request.id}-${change.fieldKey}`}>
                            {change.label}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>
                      <RequestStatusBadge status={request.status} />
                    </td>
                    <td className="text-slate-500">{formatDateTime(request.updatedAtUtc ?? request.createdAtUtc)}</td>
                    <td className="text-right">
                      {request.status.toLowerCase() === 'pending' ? (
                        <button className="shell-button-secondary px-3 py-2" onClick={() => void handleCancelRequest(request.id)} type="button">
                          Cancel
                        </button>
                      ) : null}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <Modal
        description="Editable personal and contact fields are reviewed through the self-service approval flow."
        onClose={() => setEditorOpen(false)}
        open={editorOpen}
        title="Request profile change"
      >
        <form className="space-y-5" onSubmit={handleSubmit}>
          <div className="grid gap-5 md:grid-cols-2">
            <FormField error={getFieldError(fieldErrors, 'mobileNumber')} label="Mobile number">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, mobileNumber: event.target.value }))} value={editor.mobileNumber} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'email')} label="Email">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, email: event.target.value }))} type="email" value={editor.email} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'address')} label="Address">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, address: event.target.value }))} value={editor.address} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'cityProvince')} label="City / Province">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, cityProvince: event.target.value }))} value={editor.cityProvince} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'postalCode')} label="ZIP / Postal code">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, postalCode: event.target.value }))} value={editor.postalCode} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'civilStatus')} label="Civil status">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, civilStatus: event.target.value }))} value={editor.civilStatus} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'nationality')} label="Nationality">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, nationality: event.target.value }))} value={editor.nationality} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'emergencyContactName')} label="Emergency contact name">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, emergencyContactName: event.target.value }))} value={editor.emergencyContactName} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'emergencyContactRelationship')} label="Emergency contact relationship">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, emergencyContactRelationship: event.target.value }))} value={editor.emergencyContactRelationship} />
            </FormField>
            <FormField error={getFieldError(fieldErrors, 'emergencyContactPhone')} label="Emergency contact phone">
              <input className="shell-input" onChange={(event) => setEditor((current) => ({ ...current, emergencyContactPhone: event.target.value }))} value={editor.emergencyContactPhone} />
            </FormField>
          </div>

          <FormField error={getFieldError(fieldErrors, 'reason')} label="Reason">
            <textarea className="shell-textarea" onChange={(event) => setEditor((current) => ({ ...current, reason: event.target.value }))} value={editor.reason} />
          </FormField>

          <div className="shell-form-actions">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Submitting...' : 'Submit request'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}

function ProfileCard({
  title,
  description,
  items,
}: {
  title: string
  description?: string
  items: Array<[string, string]>
}) {
  return (
    <section className="shell-card p-6 sm:p-7">
      <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">{title}</p>
      {description ? <p className="mt-2 text-sm text-slate-500">{description}</p> : null}
      <div className="mt-5 space-y-4">
        {items.map(([label, value]) => (
          <div className="shell-detail-row" key={`${title}-${label}`}>
            <div className="shell-detail-label">{label}</div>
            <div className="shell-detail-value">{value || '-'}</div>
          </div>
        ))}
      </div>
    </section>
  )
}

function FormField({ children, error, label }: { children: ReactNode; error?: string | null; label: string }) {
  return (
    <label className="block space-y-2">
      <span className="shell-label mb-0">{label}</span>
      {children}
      {error ? <span className="text-sm text-rose-600">{error}</span> : null}
    </label>
  )
}
