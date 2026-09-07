import { useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"

const FOCUSABLE =
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

export default function ConfirmDialog({
    open,
    title,
    description,
    confirmLabel = "Confirm",
    cancelLabel = "Cancel",
    destructive = false,
    busy = false,
    onConfirm,
    onCancel,
}: {
    open: boolean
    title: string
    description?: string
    confirmLabel?: string
    cancelLabel?: string
    destructive?: boolean
    busy?: boolean
    onConfirm: () => void
    onCancel: () => void
}) {
    const dialogRef = useRef<HTMLDivElement>(null)

    // Remember what had focus before the dialog opened, and give it back after.
    // Keyed on `open` alone so a busy-state change doesn't yank focus mid-request.
    useEffect(() => {
        if (!open) return
        const previous = document.activeElement as HTMLElement | null
        return () => previous?.focus()
    }, [open])

    useEffect(() => {
        if (!open) return

        const onKey = (e: KeyboardEvent) => {
            if (e.key === "Escape" && !busy) {
                onCancel()
                return
            }

            if (e.key !== "Tab") return

            const items = Array.from(dialogRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE) ?? [])
            if (items.length === 0) return

            const first = items[0]
            const last = items[items.length - 1]

            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault()
                last.focus()
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault()
                first.focus()
            }
        }

        document.addEventListener("keydown", onKey)
        return () => document.removeEventListener("keydown", onKey)
    }, [open, busy, onCancel])

    if (!open) return null

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div
                className="absolute inset-0 bg-slate-900/40"
                onClick={busy ? undefined : onCancel}
            />
            <div
                ref={dialogRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby="confirm-dialog-title"
                className="relative w-full max-w-md rounded-lg border bg-white p-6 shadow-xl"
            >
                <h2 id="confirm-dialog-title" className="text-lg font-semibold text-slate-800">
                    {title}
                </h2>
                {description && <p className="mt-2 text-sm text-slate-600">{description}</p>}

                <div className="mt-6 flex justify-end gap-3">
                    <Button type="button" variant="outline" disabled={busy} onClick={onCancel}>
                        {cancelLabel}
                    </Button>
                    <Button
                        type="button"
                        autoFocus
                        variant={destructive ? "destructive" : "default"}
                        disabled={busy}
                        onClick={onConfirm}
                    >
                        {busy ? "Working…" : confirmLabel}
                    </Button>
                </div>
            </div>
        </div>
    )
}