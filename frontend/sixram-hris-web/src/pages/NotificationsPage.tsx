import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { sixramApi } from '../api/sixramApi'
import { PaginationControls } from '../components/PaginationControls'
import type { NotificationListQuery, NotificationSummary, PagedResult, UserNotification } from '../types/models'
import { formatDateTime } from '../utils/date'
import { formatError } from '../utils/errors'

const defaultQuery: NotificationListQuery = {
  isRead: null,
  pageNumber: 1,
  pageSize: 12,
  sortBy: 'created',
  descending: true,
}

export function NotificationsPage() {
  const navigate = useNavigate()
  const [summary, setSummary] = useState<NotificationSummary | null>(null)
  const [notifications, setNotifications] = useState<PagedResult<UserNotification> | null>(null)
  const [query, setQuery] = useState<NotificationListQuery>(defaultQuery)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [activeNotificationId, setActiveNotificationId] = useState<string | null>(null)

  useEffect(() => {
    void Promise.all([loadSummary(), loadNotifications(defaultQuery)])
  }, [])

  useEffect(() => {
    if (!summary && isLoading) {
      return
    }

    void loadNotifications(query)
  }, [query.pageNumber, query.isRead])

  async function loadSummary() {
    try {
      const response = await sixramApi.getNotificationSummary()
      setSummary(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  async function loadNotifications(nextQuery: NotificationListQuery) {
    setIsLoading(true)

    try {
      const response = await sixramApi.getNotifications(nextQuery)
      setNotifications(response)
      setError(null)
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handleOpen(notification: UserNotification) {
    setActiveNotificationId(notification.id)

    try {
      if (!notification.isRead) {
        await sixramApi.markNotificationRead(notification.id)
      }

      await loadSummary()
      await loadNotifications(query)
      navigate(notification.actionUrl || '/notifications')
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setActiveNotificationId(null)
    }
  }

  async function handleMarkAllRead() {
    try {
      await sixramApi.markAllNotificationsRead()
      await Promise.all([loadSummary(), loadNotifications(query)])
    } catch (caughtError) {
      setError(formatError(caughtError))
    }
  }

  return (
    <div className="space-y-6">
      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <span className="shell-badge-brand">Notifications</span>
            <h2 className="mt-4 text-3xl font-semibold text-slate-950">In-app alerts and workflow updates</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-500">
              Review status changes across leave, attendance, profile updates, approvals, and newly visible payslips from one clean notification list.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <span className="shell-badge-warning">{summary?.unreadCount ?? 0} unread</span>
            <button className="shell-button-secondary" onClick={() => void handleMarkAllRead()} type="button">
              Mark all read
            </button>
          </div>
        </div>
      </section>

      {error ? <div className="shell-card px-5 py-4 text-sm text-rose-600">{error}</div> : null}

      <section className="shell-card fade-up p-6 sm:p-7">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Filters</p>
            <h3 className="mt-2 text-xl font-semibold text-slate-950">Notification history</h3>
          </div>

          <label className="block min-w-[180px] space-y-2">
            <span className="shell-label mb-0">Show</span>
            <select
              className="shell-select"
              onChange={(event) => {
                const value = event.target.value
                setQuery((current) => ({
                  ...current,
                  isRead: value === 'all' ? null : value === 'read',
                  pageNumber: 1,
                }))
              }}
              value={query.isRead === null ? 'all' : query.isRead ? 'read' : 'unread'}
            >
              <option value="all">All notifications</option>
              <option value="unread">Unread only</option>
              <option value="read">Read only</option>
            </select>
          </label>
        </div>

        <div className="mt-6 space-y-4">
          {isLoading ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
              Loading notifications...
            </div>
          ) : !notifications || notifications.items.length === 0 ? (
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
              No notifications matched the current filter.
            </div>
          ) : (
            notifications.items.map((notification) => (
              <div
                className={[
                  'rounded-2xl border px-5 py-4',
                  notification.isRead ? 'border-slate-200 bg-white' : 'border-[#465fff]/20 bg-[#465fff]/5',
                ].join(' ')}
                key={notification.id}
              >
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={notification.isRead ? 'shell-badge-muted' : 'shell-badge-brand'}>
                        {notification.isRead ? 'Read' : 'Unread'}
                      </span>
                      <span className="shell-badge-muted">{notification.type.replace(/_/g, ' ')}</span>
                    </div>
                    <h4 className="mt-3 text-lg font-semibold text-slate-950">{notification.title}</h4>
                    <p className="mt-2 text-sm text-slate-500">{notification.message}</p>
                    <div className="mt-3 text-sm text-slate-400">{formatDateTime(notification.createdAtUtc)}</div>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <button
                      className="shell-button-secondary px-3 py-2"
                      disabled={activeNotificationId === notification.id}
                      onClick={() => void handleOpen(notification)}
                      type="button"
                    >
                      {activeNotificationId === notification.id ? 'Opening...' : 'Open'}
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {notifications ? (
          <PaginationControls
            pageNumber={notifications.pageNumber}
            pageSize={notifications.pageSize}
            totalCount={notifications.totalCount}
            totalPages={notifications.totalPages}
            onPageChange={(pageNumber) => setQuery((current) => ({ ...current, pageNumber }))}
          />
        ) : null}
      </section>
    </div>
  )
}
