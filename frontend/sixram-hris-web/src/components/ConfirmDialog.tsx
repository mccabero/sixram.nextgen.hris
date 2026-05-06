import { Modal } from './Modal'

type ConfirmDialogProps = {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  confirmTone?: 'danger' | 'primary'
  isBusy?: boolean
  onCancel: () => void
  onConfirm: () => void
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  confirmTone = 'danger',
  isBusy = false,
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <Modal description={description} onClose={onCancel} open={open} title={title}>
      <div className="flex flex-wrap justify-end gap-3">
        <button className="shell-button-secondary" disabled={isBusy} onClick={onCancel} type="button">
          {cancelLabel}
        </button>
        <button
          className={confirmTone === 'danger' ? 'shell-button-danger' : 'shell-button'}
          disabled={isBusy}
          onClick={onConfirm}
          type="button"
        >
          {isBusy ? 'Working...' : confirmLabel}
        </button>
      </div>
    </Modal>
  )
}
