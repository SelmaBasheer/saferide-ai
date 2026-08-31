import { useEffect } from "react"
import { Button } from "@/components/ui/button"

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
    useEffect(() => {
        if (!open) return
        const onKey = (e: KeyboardEvent) => {
            if (e.key === "Escape" && !busy) onCancel()
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