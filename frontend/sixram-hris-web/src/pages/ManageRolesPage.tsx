import { useEffect, useState } from 'react'
import { sixramApi } from '../api/sixramApi'
import { Modal } from '../components/Modal'
import type { Role } from '../types/models'
import { formatError } from '../utils/errors'

type RoleEditorState = {
  name: string
  description: string
}

const emptyRoleEditor: RoleEditorState = {
  name: '',
  description: '',
}

export function ManageRolesPage() {
  const [roles, setRoles] = useState<Role[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [editorOpen, setEditorOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<Role | null>(null)
  const [editorState, setEditorState] = useState<RoleEditorState>(emptyRoleEditor)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    void loadRoles()
  }, [])

  async function loadRoles() {
    setIsLoading(true)

    try {
      const response = await sixramApi.getRoles()
      setRoles(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateModal() {
    setEditingRole(null)
    setEditorState(emptyRoleEditor)
    setEditorOpen(true)
  }

  function openEditModal(role: Role) {
    setEditingRole(role)
    setEditorState({
      name: role.name,
      description: role.description,
    })
    setEditorOpen(true)
  }

  async function handleSaveRole(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)

    try {
      if (editingRole) {
        await sixramApi.updateRole(editingRole.id, editorState)
      } else {
        await sixramApi.createRole(editorState)
      }

      await loadRoles()
      setEditorOpen(false)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteRole(role: Role) {
    const confirmed = window.confirm(`Delete the ${role.name} role? This only succeeds if it is not assigned.`)
    if (!confirmed) {
      return
    }

    try {
      await sixramApi.deleteRole(role.id)
      await loadRoles()
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  const totalAssignedUsers = roles.reduce((sum, role) => sum + role.userCount, 0)
  const inUseRoles = roles.filter((role) => role.userCount > 0).length

  const summaryCards = [
    { label: 'Total roles', value: roles.length, tone: 'text-slate-950', description: 'Available authorization roles.' },
    { label: 'In use', value: inUseRoles, tone: 'text-[#3641f5]', description: 'Roles currently assigned to users.' },
    { label: 'Assignments', value: totalAssignedUsers, tone: 'text-emerald-700', description: 'Total user-role memberships.' },
  ]

  return (
    <div className="space-y-6">
      <section className="grid gap-4 md:grid-cols-3">
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
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Catalog</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-950">Role catalog</h3>
            <p className="mt-2 text-sm text-slate-500">
              Maintain the roles used by API authorization attributes and the RBAC assignment screens.
            </p>
          </div>

          <button className="shell-button" onClick={openCreateModal} type="button">
            Add Role
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
                <th>Role</th>
                <th>Description</th>
                <th>Assigned Users</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="text-slate-500" colSpan={4}>
                    Loading roles...
                  </td>
                </tr>
              ) : roles.length === 0 ? (
                <tr>
                  <td className="text-slate-500" colSpan={4}>
                    No roles found.
                  </td>
                </tr>
              ) : (
                roles.map((role) => (
                  <tr key={role.id}>
                    <td>
                      <div className="font-semibold text-slate-900">{role.name}</div>
                    </td>
                    <td className="text-slate-500">{role.description || 'No description provided.'}</td>
                    <td>
                      <span className={role.userCount > 0 ? 'shell-badge-brand' : 'shell-badge-muted'}>
                        {role.userCount}
                      </span>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button className="shell-button-secondary" onClick={() => openEditModal(role)} type="button">
                          Edit
                        </button>
                        <button className="shell-button-danger" onClick={() => void handleDeleteRole(role)} type="button">
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
        description="Role names are used directly by backend authorization policies and administrator-only endpoints."
        onClose={() => {
          if (!isSaving) {
            setEditorOpen(false)
          }
        }}
        open={editorOpen}
        title={editingRole ? `Edit ${editingRole.name}` : 'Create Role'}
      >
        <form className="space-y-5" onSubmit={handleSaveRole}>
          <div>
            <label className="shell-label" htmlFor="role-name">
              Role name
            </label>
            <input
              className="shell-input"
              id="role-name"
              onChange={(event) => setEditorState((current) => ({ ...current, name: event.target.value }))}
              type="text"
              value={editorState.name}
            />
          </div>

          <div>
            <label className="shell-label" htmlFor="role-description">
              Description
            </label>
            <textarea
              className="shell-textarea"
              id="role-description"
              onChange={(event) => setEditorState((current) => ({ ...current, description: event.target.value }))}
              value={editorState.description}
            />
          </div>

          <div className="flex flex-wrap justify-end gap-3">
            <button className="shell-button-secondary" onClick={() => setEditorOpen(false)} type="button">
              Cancel
            </button>
            <button className="shell-button" disabled={isSaving} type="submit">
              {isSaving ? 'Saving...' : editingRole ? 'Save Changes' : 'Create Role'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
