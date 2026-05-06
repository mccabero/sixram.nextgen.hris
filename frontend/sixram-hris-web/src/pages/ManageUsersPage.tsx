import { useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import type { Role, UserSummary } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

type UserEditorState = {
  email: string
  displayName: string
  password: string
  isEnabled: boolean
  roleNames: string[]
}

const emptyUserEditor: UserEditorState = {
  email: '',
  displayName: '',
  password: '',
  isEnabled: true,
  roleNames: ['User'],
}

export function ManageUsersPage() {
  const [users, setUsers] = useState<UserSummary[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [editorOpen, setEditorOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null)
  const [editorState, setEditorState] = useState<UserEditorState>(emptyUserEditor)
  const [isSaving, setIsSaving] = useState(false)

  const [passwordTarget, setPasswordTarget] = useState<UserSummary | null>(null)
  const [passwordValue, setPasswordValue] = useState('')
  const [isUpdatingPassword, setIsUpdatingPassword] = useState(false)

  useEffect(() => {
    void loadData()
  }, [])

  async function loadData() {
    setIsLoading(true)

    try {
      const [loadedUsers, loadedRoles] = await Promise.all([sixramApi.getUsers(), sixramApi.getRoles()])
      setUsers(loadedUsers)
      setRoles(loadedRoles)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateModal() {
    setEditingUser(null)
    setEditorState({
      ...emptyUserEditor,
      roleNames: roles.some((role) => role.name === 'User') ? ['User'] : [],
    })
    setEditorOpen(true)
  }

  function openEditModal(user: UserSummary) {
    setEditingUser(user)
    setEditorState({
      email: user.email,
      displayName: user.displayName,
      password: '',
      isEnabled: user.isEnabled,
      roleNames: user.roles,
    })
    setEditorOpen(true)
  }

  function closeEditor() {
    if (isSaving) {
      return
    }

    setEditorOpen(false)
    setEditingUser(null)
  }

  function toggleRole(roleName: string) {
    setEditorState((current) => {
      const alreadyAssigned = current.roleNames.includes(roleName)
      return {
        ...current,
        roleNames: alreadyAssigned
          ? current.roleNames.filter((role) => role !== roleName)
          : [...current.roleNames, roleName],
      }
    })
  }

  async function handleSaveUser(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)

    try {
      if (editingUser) {
        await sixramApi.updateUser(editingUser.id, {
          email: editorState.email,
          displayName: editorState.displayName,
        })

        await sixramApi.setUserStatus(editingUser.id, {
          isEnabled: editorState.isEnabled,
        })

        await sixramApi.setUserRoles(editingUser.id, {
          roleNames: editorState.roleNames,
        })
      } else {
        await sixramApi.createUser({
          email: editorState.email,
          displayName: editorState.displayName,
          password: editorState.password,
          isEnabled: editorState.isEnabled,
          roleNames: editorState.roleNames,
        })
      }

      await loadData()
      setEditorOpen(false)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleStatusToggle(user: UserSummary) {
    try {
      await sixramApi.setUserStatus(user.id, { isEnabled: !user.isEnabled })
      await loadData()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handleDelete(user: UserSummary) {
    const confirmed = window.confirm(`Delete ${user.email}? This permanently removes the account.`)
    if (!confirmed) {
      return
    }

    try {
      await sixramApi.deleteUser(user.id)
      await loadData()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function handlePasswordReset(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!passwordTarget) {
      return
    }

    setIsUpdatingPassword(true)

    try {
      await sixramApi.resetUserPassword(passwordTarget.id, { newPassword: passwordValue })
      setPasswordTarget(null)
      setPasswordValue('')
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsUpdatingPassword(false)
    }
  }

  const totalUsers = users.length
  const enabledUsers = users.filter((user) => user.isEnabled).length
  const disabledUsers = totalUsers - enabledUsers

  const summaryCards = [
    { label: 'Total users', value: totalUsers, tone: 'text-slate-950', description: 'Identity records available.' },
    { label: 'Enabled', value: enabledUsers, tone: 'text-emerald-700', description: 'Accounts allowed to authenticate.' },
    { label: 'Disabled', value: disabledUsers, tone: 'text-rose-700', description: 'Accounts blocked from sign-in.' },
    { label: 'Roles', value: roles.length, tone: 'text-[#3641f5]', description: 'Assignable role definitions.' },
  ]

  return (
    <div className="space-y-6">
      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {summaryCards.map((card) => (
          <div className="shell-card p-5" key={card.label}>
            <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">{card.label}</p>
            <p className={['mt-3 text-3xl font-semibold', card.tone].join(' ')}>{card.value}</p>
            <p className="mt-2 text-sm text-slate-500">{card.description}</p>
          </div>
        ))}
      </section>

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Directory</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">User directory</h3>
            <p className="mt-2 text-sm text-slate-500">
              Create accounts, update profile details, toggle status, assign roles, and rotate passwords.
            </p>
          </div>

          <button className="shell-button" onClick={openCreateModal} type="button">
            Create User
          </button>
        </div>

        {error ? (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {error}
          </div>
        ) : null}

        <div className="shell-table-wrap mt-6">
          <table className="shell-table">
            <thead>
              <tr>
                <th>User</th>
                <th>Roles</th>
                <th>Created</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    Loading users...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={5}>
                    No users found.
                  </td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{user.displayName}</div>
                      <div className="mt-1 text-slate-500">{user.email}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {user.roles.length === 0 ? (
                          <span className="shell-badge-muted">No roles</span>
                        ) : (
                          user.roles.map((role) => (
                            <span className="shell-badge-brand" key={role}>
                              {role}
                            </span>
                          ))
                        )}
                      </div>
                    </td>
                    <td className="text-slate-500">{formatDateTime(user.createdAtUtc)}</td>
                    <td>
                      <span className={user.isEnabled ? 'shell-badge-success' : 'shell-badge-danger'}>
                        {user.isEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary" onClick={() => openEditModal(user)} type="button">
                          Edit
                        </button>
                        <button className="shell-button-secondary" onClick={() => void handleStatusToggle(user)} type="button">
                          {user.isEnabled ? 'Disable' : 'Enable'}
                        </button>
                        <button
                          className="shell-button-secondary"
                          onClick={() => {
                            setPasswordTarget(user)
                            setPasswordValue('')
                          }}
                          type="button"
                        >
                          Password
                        </button>
                        <button className="shell-button-danger" onClick={() => void handleDelete(user)} type="button">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <Modal
        description={
          editingUser
            ? 'Update the user profile and adjust the assigned roles.'
            : 'Create a new account and set the initial role mapping.'
        }
        onClose={closeEditor}
        open={editorOpen}
        title={editingUser ? `Edit ${editingUser.email}` : 'Create User'}
      >
        <form className="space-y-5" onSubmit={handleSaveUser}>
          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label className="shell-label" htmlFor="user-email">
                Email
              </label>
              <input
                className="shell-input"
                id="user-email"
                onChange={(event) => setEditorState((current) => ({ ...current, email: event.target.value }))}
                type="email"
                value={editorState.email}
              />
            </div>

            <div>
              <label className="shell-label" htmlFor="user-display-name">
                Display name
              </label>
              <input
                className="shell-input"
                id="user-display-name"
                onChange={(event) => setEditorState((current) => ({ ...current, displayName: event.target.value }))}
                type="text"
                value={editorState.displayName}
              />
            </div>
          </div>

          {!editingUser ? (
            <div>
              <label className="shell-label" htmlFor="user-password">
                Initial password
              </label>
              <input
                className="shell-input"
                id="user-password"
                onChange={(event) => setEditorState((current) => ({ ...current, password: event.target.value }))}
                type="password"
                value={editorState.password}
              />
              <p className="mt-2 text-sm text-slate-500">Enter a temporary password that meets the Identity policy shown in the API validation rules.</p>
            </div>
          ) : null}

          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="text-sm font-semibold text-slate-900">Role assignments</p>
                <p className="mt-1 text-sm text-slate-500">Toggle the roles this user should have after save.</p>
              </div>
              <label className="inline-flex items-center gap-3 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700">
                <input
                  checked={editorState.isEnabled}
                  onChange={(event) => setEditorState((current) => ({ ...current, isEnabled: event.target.checked }))}
                  type="checkbox"
                />
                Enabled
              </label>
            </div>

            <div className="mt-5 grid gap-3 sm:grid-cols-2">
              {roles.map((role) => (
                <label
                  className="flex items-start gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm"
                  key={role.id}
                >
                  <input
                    checked={editorState.roleNames.includes(role.name)}
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
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={closeEditor} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving || (!editingUser && !editorState.password.trim())} type="submit">
              {isSaving ? 'Saving...' : editingUser ? 'Save Changes' : 'Create User'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Set a new password for the selected user. The backend uses ASP.NET Core Identity password hashing."
        onClose={() => {
          if (!isUpdatingPassword) {
            setPasswordTarget(null)
          }
        }}
        open={Boolean(passwordTarget)}
        title={passwordTarget ? `Reset Password for ${passwordTarget.email}` : 'Reset Password'}
      >
        <form className="space-y-5" onSubmit={handlePasswordReset}>
          <div>
            <label className="shell-label" htmlFor="reset-password">
              New password
            </label>
            <input
              className="shell-input"
              id="reset-password"
              onChange={(event) => setPasswordValue(event.target.value)}
              type="password"
              value={passwordValue}
            />
            <p className="mt-2 text-sm text-slate-500">Use a new temporary password. Do not rely on shared default passwords for live accounts.</p>
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setPasswordTarget(null)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isUpdatingPassword || !passwordValue.trim()} type="submit">
              {isUpdatingPassword ? 'Updating...' : 'Reset Password'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
