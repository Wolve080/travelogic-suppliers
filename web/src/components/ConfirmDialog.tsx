import { useEffect, useRef, type ReactNode } from 'react'
import { Button } from './ui'

interface ConfirmDialogProps {
  open: boolean
  title: string
  children: ReactNode
  confirmLabel: string
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/** Uses the native <dialog> element, which gives focus trapping and Escape handling for free. */
export function ConfirmDialog({ open, title, children, confirmLabel, loading, onConfirm, onCancel }: ConfirmDialogProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal?.()
    if (!open && dialog.open) dialog.close?.()
  }, [open])

  return (
    <dialog
      ref={ref}
      onCancel={(e) => {
        e.preventDefault()
        onCancel()
      }}
      aria-labelledby="confirm-title"
      className="m-auto w-full max-w-md rounded-xl p-0 shadow-xl backdrop:bg-stone-900/40"
    >
      {open && (
        <div className="p-6">
          <h2 id="confirm-title" className="text-lg font-semibold text-stone-900">
            {title}
          </h2>
          <div className="mt-2 text-sm text-stone-600">{children}</div>
          <div className="mt-6 flex justify-end gap-2">
            <Button variant="secondary" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
            <Button variant="danger" onClick={onConfirm} loading={loading}>
              {confirmLabel}
            </Button>
          </div>
        </div>
      )}
    </dialog>
  )
}
