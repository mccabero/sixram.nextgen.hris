import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import loginBackground from '../assets/hris-login-background.png'
import { useAuth } from '../auth/AuthContext'
import { formatError } from '../utils/errors'

const workspaceHighlights = [
  {
    label: 'Workforce',
    value: 'Employee records, roles, documents',
  },
  {
    label: 'Operations',
    value: 'Attendance, leave, approvals',
  },
  {
    label: 'Payroll',
    value: 'Compensation, runs, payslips',
  },
]

export function LoginPage() {
  const { isAuthenticated, isReady, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const nextPath = typeof location.state?.from === 'string' ? location.state.from : '/'

  if (isReady && isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await login(email, password)
      navigate(nextPath, { replace: true })
    } catch (caughtError) {
      setError(formatError(caughtError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="relative min-h-screen overflow-hidden bg-[#0f172a] px-4 py-6 text-slate-900 sm:px-6 lg:px-8">
      <img
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover"
        src={loginBackground}
      />
      <div aria-hidden="true" className="absolute inset-0 bg-[#0f172a]/50" />
      <div aria-hidden="true" className="absolute inset-0 bg-[linear-gradient(90deg,rgba(15,23,42,0.88)_0%,rgba(15,23,42,0.62)_46%,rgba(15,23,42,0.38)_100%)]" />
      <div aria-hidden="true" className="absolute inset-0 bg-[radial-gradient(circle_at_24%_18%,rgba(34,197,94,0.18),transparent_30%)]" />

      <div className="relative z-10 mx-auto flex min-h-[calc(100vh-3rem)] w-full max-w-6xl flex-col justify-center">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-lg border border-white/15 bg-white/10 text-sm font-semibold text-white shadow-sm">
              HR
            </div>
            <div>
              <p className="text-base font-semibold text-white">Sixram HRIS</p>
              <p className="text-sm text-slate-300">Company workforce operations</p>
            </div>
          </div>
          <div className="rounded-lg border border-white/15 bg-white/10 px-4 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-100 shadow-sm">
            Secure Access
          </div>
        </div>

        <div className="grid overflow-hidden rounded-2xl border border-white/15 bg-white shadow-2xl lg:grid-cols-[1.05fr_0.95fr]">
          <section className="relative flex min-h-[560px] flex-col justify-between overflow-hidden bg-[#101828]/92 px-7 py-8 text-white sm:px-10 sm:py-10 lg:px-12">
            <div aria-hidden="true" className="absolute inset-x-0 bottom-0 h-1 bg-[#22c55e]" />

            <div className="relative z-10">
              <div className="inline-flex items-center gap-3 rounded-lg border border-white/12 bg-white/8 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-200">
                <span className="h-2 w-2 rounded-full bg-emerald-400" />
                HRIS Portal
              </div>

              <h1 className="mt-8 max-w-xl text-4xl font-semibold leading-tight text-white sm:text-[46px]">
                A focused workspace for people, payroll, and HR operations.
              </h1>
              <p className="mt-5 max-w-xl text-base leading-7 text-slate-300">
                Access employee records, attendance, leave, compliance, and payroll workflows from one governed company application.
              </p>
            </div>

            <div className="relative z-10 mt-10 grid gap-3">
              {workspaceHighlights.map((item) => (
                <div className="flex items-start gap-4 border-t border-white/10 pt-4" key={item.label}>
                  <div className="mt-1 h-2.5 w-2.5 rounded-full bg-[#38bdf8]" />
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">{item.label}</p>
                    <p className="mt-1 text-sm font-medium text-slate-100">{item.value}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="relative z-10 mt-10 rounded-xl border border-white/10 bg-white/8 p-5">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Session Protection</p>
              <p className="mt-2 text-sm leading-6 text-slate-300">
                Accounts, roles, and session renewal are controlled by the HRIS identity service.
              </p>
            </div>
          </section>

          <section className="flex items-center bg-white px-6 py-8 sm:px-10 sm:py-12 lg:px-12">
            <div className="mx-auto w-full max-w-[420px]">
              <div className="mb-8 border-b border-slate-200 pb-8">
                <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-400">Sign in</p>
                <h2 className="mt-3 text-3xl font-semibold text-slate-950">Welcome back</h2>
                <p className="mt-3 text-sm leading-6 text-slate-500">
                  Use the account assigned by your HR or system administrator.
                </p>
              </div>

              <form className="space-y-5" onSubmit={handleSubmit}>
                <div>
                  <label className="shell-label" htmlFor="email">
                    Email address
                  </label>
                  <input
                    autoComplete="username"
                    className="shell-input rounded-lg border-slate-300 bg-slate-50/70 px-4 py-3.5 focus:border-[#465fff] focus:bg-white"
                    id="email"
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="you@example.com"
                    type="email"
                    value={email}
                  />
                </div>

                <div>
                  <div className="flex items-center justify-between gap-3">
                    <label className="shell-label mb-2" htmlFor="password">
                      Password
                    </label>
                    <span className="mb-2 text-xs font-medium text-slate-400">Protected session</span>
                  </div>
                  <input
                    autoComplete="current-password"
                    className="shell-input rounded-lg border-slate-300 bg-slate-50/70 px-4 py-3.5 focus:border-[#465fff] focus:bg-white"
                    id="password"
                    onChange={(event) => setPassword(event.target.value)}
                    placeholder="Enter your password"
                    type="password"
                    value={password}
                  />
                </div>

                {error ? (
                  <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm leading-6 text-rose-700">
                    {error}
                  </div>
                ) : null}

                <button className="shell-button w-full rounded-lg py-3.5 text-base" disabled={isSubmitting} type="submit">
                  {isSubmitting ? 'Signing in...' : 'Sign In to Sixram HRIS'}
                </button>
              </form>

              <div className="mt-8 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm leading-6 text-slate-500">
                Access is monitored according to company security and HR data policies.
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
