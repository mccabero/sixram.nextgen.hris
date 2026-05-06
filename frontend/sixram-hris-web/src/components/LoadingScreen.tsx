export function LoadingScreen({ label = 'Loading Sixram HRIS...' }: { label?: string }) {
  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <div className="shell-card fade-up flex w-full max-w-md flex-col items-center gap-4 px-8 py-10 text-center">
        <div className="h-12 w-12 animate-spin rounded-full border-4 border-slate-200 border-t-[#465fff]" />
        <div className="space-y-1">
          <p className="text-lg font-semibold text-slate-900">{label}</p>
          <p className="text-sm text-slate-500">Please wait a moment while the app catches up.</p>
        </div>
      </div>
    </div>
  )
}
