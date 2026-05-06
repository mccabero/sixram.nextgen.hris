/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import type { RbacSummary } from '../types/models'
import { formatError } from '../utils/errors'

export function RbacManagementPage() {
  const [summary, setSummary] = useState<RbacSummary | null>(null)
  const [selectedUserId, setSelectedUserId] = useState<string>('')
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function loadSummary() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getRbacSummary()
      const nextUserId =
        response.users.some((user) => user.id === selectedUserId)
          ? selectedUserId
          : (response.users[0]?.id ?? '')
      const mappedUser = response.users.find((user) => user.id === nextUserId)

      setSummary(response)
      setSelectedUserId(nextUserId)
      setSelectedRoles(mappedUser?.roles ?? [])
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

  function toggleRole(roleName: string) {
    setSelectedRoles((current) =>
      current.includes(roleName) ? current.filter((role) => role !== roleName) : [...current, roleName],
    )
  }

  async function handleSaveMapping() {
    const selectedUser = summary?.users.find((user) => user.id === selectedUserId)
    if (!selectedUser) {
      return
    }

    setIsSaving(true)

    try {
      await sixramApi.setRbacUserRoles(selectedUser.id, {
        roleNames: selectedRoles,
      })
      await loadSummary()
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  const summaryCards = [
    { label: 'Users', value: summary?.users.length ?? 0, tone: 'text-slate-950' },
    { label: 'Roles', value: summary?.roles.length ?? 0, tone: 'text-[#3641f5]' },
    { label: 'Assignments', value: summary?.assignments.length ?? 0, tone: 'text-emerald-700' },
  ]

  return (
    <div className="space-y-6">
      <section className="grid gap-4 md:grid-cols-3">
        {summaryCards.map((card) => (
          <div className="shell-card p-5" key={card.label}>
            <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{card.label}</p>
            <p className={['mt-3 text-3xl font-semibold', card.tone].join(' ')}>{card.value}</p>
            <p className="mt-2 text-sm text-slate-500">Live data loaded from the RBAC summary endpoint.</p>
          </div>
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Editor</p>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Role assignment editor</h3>
              <p className="mt-2 text-sm text-slate-500">
                Select a user, then save the exact role set the backend should persist.
              </p>
            </div>

            <span className="shell-badge-brand">Exact mapping</span>
          </div>

          {error ? (
            <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
              {error}
            </div>
          ) : null}

          <div className="mt-6">
            <label className="shell-label" htmlFor="rbac-user">
              User
            </label>
            <select
              className="shell-select"
              id="rbac-user"
              onChange={(event) => {
                const nextUserId = event.target.value
                setSelectedUserId(nextUserId)
                const nextUser = summary?.users.find((user) => user.id === nextUserId)
                setSelectedRoles(nextUser?.roles ?? [])
              }}
              value={selectedUserId}
            >
              {summary?.users.map((user) => (
                <option key={user.id} value={user.id}>
                  {user.displayName} ({user.email})
                </option>
              ))}
            </select>
          </div>

          <div className="mt-6 grid gap-3">
            {summary?.roles.map((role) => (
              <label
                className="flex items-start gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm"
                key={role.id}
              >
                <input
                  checked={selectedRoles.includes(role.name)}
                  onChange={() => toggleRole(role.name)}
                  type="checkbox"
                />
                <span>
                  <span className="block font-semibold text-slate-900">{role.name}</span>
                  <span className="mt-1 block text-slate-500">{role.description || 'No description provided.'}</span>
                </span>
              </label>
            ))}
          </div>

          <div className="mt-6 flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => void loadSummary()} type="button">
              Refresh
            </button>
            <button className="shell-button" disabled={!selectedUserId || isSaving} onClick={() => void handleSaveMapping()} type="button">
              {isSaving ? 'Saving...' : 'Save Mapping'}
            </button>
          </div>
        </div>

        <div className="shell-card fade-up p-6 sm:p-7">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Matrix</p>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Current user-role map</h3>
              <p className="mt-2 text-sm text-slate-500">Live assignment view backed by the RBAC summary endpoint.</p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              Users: {summary?.users.length ?? 0} | Roles: {summary?.roles.length ?? 0} | Assignments:{' '}
              {summary?.assignments.length ?? 0}
            </div>
          </div>

          <div className="shell-table-wrap mt-6">
            <table className="shell-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Status</th>
                  <th>Roles</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr>
                    <td className="text-slate-500" colSpan={3}>
                      Loading RBAC summary...
                    </td>
                  </tr>
                ) : summary?.users.length ? (
                  summary.users.map((user) => (
                    <tr key={user.id}>
                      <td>
                        <div className="font-semibold text-slate-900">{user.displayName}</div>
                        <div className="mt-1 text-slate-500">{user.email}</div>
                      </td>
                      <td>
                        <span className={user.isEnabled ? 'shell-badge-success' : 'shell-badge-danger'}>
                          {user.isEnabled ? 'Enabled' : 'Disabled'}
                        </span>
                      </td>
                      <td>
                        <div className="flex flex-wrap gap-2">
                          {user.roles.length === 0 ? (
                            <span className="shell-badge-muted">No roles</span>
                          ) : (
                            user.roles.map((role) => (
                              <span className="shell-badge-brand" key={`${user.id}-${role}`}>
                                {role}
                              </span>
                            ))
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td className="text-slate-500" colSpan={3}>
                      No RBAC data found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </section>
    </div>
  )
}
