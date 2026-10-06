import { Suspense } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AppRoutes } from './app/AppRoutes'
import { AuthProvider } from './auth/AuthContext'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Suspense fallback={<RouteFallback />}>
          <AppRoutes />
        </Suspense>
      </AuthProvider>
    </BrowserRouter>
  )
}

function RouteFallback() {
  return (
    <div className="min-h-screen bg-slate-50 px-6 py-10">
      <div className="mx-auto max-w-2xl rounded-2xl border border-slate-200 bg-white px-6 py-8 text-sm text-slate-500 shadow-sm">
        Loading workspace...
      </div>
    </div>
  )
}

export default App
