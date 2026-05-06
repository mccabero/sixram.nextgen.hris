type PaginationControlsProps = {
  className?: string
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (pageNumber: number) => void
}

export function PaginationControls({
  className = '',
  pageNumber,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
}: PaginationControlsProps) {
  if (totalCount === 0) {
    return null
  }

  const start = (pageNumber - 1) * pageSize + 1
  const end = Math.min(pageNumber * pageSize, totalCount)

  return (
    <div
      className={[
        'flex flex-col gap-3 border-t border-slate-200 px-5 py-4 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between',
        className,
      ].join(' ').trim()}
    >
      <p className="leading-6">
        Showing {start} to {end} of {totalCount} results
      </p>

      <div className="flex flex-wrap items-center gap-2 sm:justify-end">
        <button
          className="shell-button-secondary px-3 py-2"
          disabled={pageNumber <= 1}
          onClick={() => onPageChange(pageNumber - 1)}
          type="button"
        >
          Previous
        </button>
        <span className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-slate-600">
          Page {pageNumber} of {Math.max(totalPages, 1)}
        </span>
        <button
          className="shell-button-secondary px-3 py-2"
          disabled={pageNumber >= totalPages}
          onClick={() => onPageChange(pageNumber + 1)}
          type="button"
        >
          Next
        </button>
      </div>
    </div>
  )
}
