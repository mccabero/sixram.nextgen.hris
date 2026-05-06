import { useEffect, type ReactNode } from 'react'

type ModalProps = {
  open: boolean
  title: string
  description?: string
  children: ReactNode
  onClose: () => void
}

export function Modal({ open, title, description, children, onClose }: ModalProps) {
  useEffect(() => {
    if (!open) {
      return
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [onClose, open])

  if (!open) {
    return null
  }

  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/55 p-3 sm:items-center sm:p-4"
      role="dialog"
      onClick={onClose}
    >
      <div
        className="shell-card shell-modal-scroll fade-up max-h-[92vh] w-full max-w-2xl overflow-y-auto p-5 sm:max-h-[90vh] sm:p-7"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="mb-6 flex items-start justify-between gap-4 border-b border-slate-200 pb-5">
          <div className="space-y-1">
            <p className="shell-kicker">Modal</p>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h2>
            {description ? <p className="max-w-2xl text-sm leading-6 text-slate-500">{description}</p> : null}
          </div>
          <button aria-label="Close dialog" className="shell-button-secondary px-3 py-2" onClick={onClose} type="button">
            Close
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}
