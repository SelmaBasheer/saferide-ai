import { useState } from "react"
import { CalendarOff, X } from "lucide-react"
import ConfirmDialog from "@/components/ui/confirm-dialog"
import { useGetTripsQuery } from "@/features/tracking/trackingApi"
import {
    useGetMyChildrenQuery,
    useMarkLeaveMutation,
    useCancelLeaveMutation,
    type MyChild,
} from "@/features/students/studentApi"

/** Local calendar date. toISOString gives UTC and shifts the day in India. */
function isoDate(d: Date) {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`
}

function prettyDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, {
        weekday: "long",
        day: "numeric",
        month: "long",
    })
}

function shortDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, {
        weekday: "short",
        day: "numeric",
        month: "short",
    })
}

/** What the parent has asked for but not yet confirmed. */
interface PendingChange {
    child: MyChild
    date: string
    action: "mark" | "cancel"
}

function ChildCard({
    child,
    busRunning,
    onRequest,
    busy,
}: {
    child: MyChild
    busRunning: boolean
    onRequest: (change: PendingChange) => void
    busy: boolean
}) {
    const today = isoDate(new Date())

    const tomorrowDate = new Date()
    tomorrowDate.setDate(tomorrowDate.getDate() + 1)
    const tomorrow = isoDate(tomorrowDate)

    const onLeave = (d: string) => child.upcomingLeaves.includes(d)

    const request = (date: string) =>
        onRequest({ child, date, action: onLeave(date) ? "cancel" : "mark" })

    return (
        <div className="rounded-xl border border-slate-200 p-4">
            <div className="flex items-baseline justify-between">
                <div className="text-lg font-semibold">
                    {child.firstName} {child.lastName}
                </div>
                <div className="text-sm text-slate-500">{child.grade}</div>
            </div>

            {!child.routeId && (
                <p className="mt-2 text-sm text-slate-400">Not assigned to a route yet.</p>
            )}

            <div className="mt-3 flex flex-wrap gap-2">
                {busRunning ? (
                    // The roster was taken when the driver pressed start, so a
                    // change now would do nothing. Saying that is better than a
                    // button that silently achieves nothing.
                    <p className="text-sm text-slate-500">Today's bus has already left.</p>
                ) : (
                    <button
                        disabled={busy}
                        onClick={() => request(today)}
                        className={`rounded-full px-4 py-2 text-sm ${onLeave(today)
                                ? "bg-amber-100 text-amber-800"
                                : "border border-slate-300 text-slate-700"
                            }`}
                    >
                        {onLeave(today) ? "Off today ✓" : "Not travelling today"}
                    </button>
                )}

                <button
                    disabled={busy}
                    onClick={() => request(tomorrow)}
                    className={`rounded-full px-4 py-2 text-sm ${onLeave(tomorrow)
                            ? "bg-amber-100 text-amber-800"
                            : "border border-slate-300 text-slate-700"
                        }`}
                >
                    {onLeave(tomorrow) ? "Off tomorrow ✓" : "Not travelling tomorrow"}
                </button>
            </div>

            <label className="mt-3 flex items-center gap-2 text-sm text-slate-500">
                <CalendarOff className="h-4 w-4" />
                Another day
                <input
                    type="date"
                    min={today}
                    disabled={busy}
                    onChange={(e) => {
                        if (e.target.value) request(e.target.value)
                        e.target.value = ""
                    }}
                    className="rounded-md border border-slate-300 px-2 py-1 text-sm"
                />
            </label>

            {child.upcomingLeaves.length > 0 && (
                <div className="mt-3 border-t border-slate-100 pt-3">
                    <p className="text-xs uppercase tracking-wide text-slate-400">Days off</p>
                    <div className="mt-2 flex flex-wrap gap-2">
                        {child.upcomingLeaves.map((d) => (
                            <button
                                key={d}
                                disabled={busy}
                                onClick={() => onRequest({ child, date: d, action: "cancel" })}
                                className="flex items-center gap-1 rounded-full bg-slate-100 px-3 py-1 text-sm text-slate-700"
                            >
                                {shortDate(d)}
                                <X className="h-3.5 w-3.5" />
                            </button>
                        ))}
                    </div>
                </div>
            )}
        </div>
    )
}

export default function ParentChildrenPage() {
    const [pending, setPending] = useState<PendingChange | null>(null)

    const trips = useGetTripsQuery({ status: "Active", page: 1, pageSize: 50 })
    const children = useGetMyChildrenQuery()

    const [markLeave, mark] = useMarkLeaveMutation()
    const [cancelLeave, cancel] = useCancelLeaveMutation()

    const busy = mark.isLoading || cancel.isLoading
    const activeTrips = trips.data?.items ?? []

    const confirm = async () => {
        if (!pending) return

        const args = { studentId: pending.child.id, date: pending.date }

        try {
            if (pending.action === "mark") {
                await markLeave(args).unwrap()
            } else {
                await cancelLeave(args).unwrap()
            }
        } catch {
            // The error banner below covers it; the dialog still closes so the
            // parent is not stuck behind a modal.
        }

        setPending(null)
    }

    if (children.isLoading) {
        return <div className="p-6 text-slate-500">Loading…</div>
    }

    if (children.isError) {
        return (
            <div className="p-6 text-center">
                <p className="text-slate-600">Could not reach the server.</p>
                <button
                    onClick={() => children.refetch()}
                    className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm"
                >
                    Try again
                </button>
            </div>
        )
    }

    const kids = children.data ?? []
    const name = pending ? `${pending.child.firstName} ${pending.child.lastName}` : ""

    return (
        <div className="flex flex-col gap-4 p-4">
            <div>
                <h1 className="text-xl font-semibold">Your children</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Tell the school in advance when your child is not taking the bus, and the driver
                    will see it on their list.
                </p>
            </div>

            {kids.length === 0 ? (
                <p className="text-sm text-slate-500">
                    No children are linked to this account yet. Ask your school to check the email
                    address on your child's record.
                </p>
            ) : (
                // One column on a phone, two once there is room for them.
                <div className="grid gap-4 lg:grid-cols-2">
                    {kids.map((child) => (
                        <ChildCard
                            key={child.id}
                            child={child}
                            // Per child, not per parent — two children can be on
                            // different routes with different buses.
                            busRunning={activeTrips.some((t) => t.routeId === child.routeId)}
                            busy={busy}
                            onRequest={setPending}
                        />
                    ))}
                </div>
            )}

            {(mark.isError || cancel.isError) && (
                <p className="text-sm text-red-600">That didn't save. Please try again.</p>
            )}

            {/* Both directions are confirmed. Marking leave by accident means a
                child is left at the stop; unmarking by accident means the driver
                raises an alarm for a child who was never coming. */}
            <ConfirmDialog
                open={pending !== null}
                busy={busy}
                title={
                    pending?.action === "mark"
                        ? "Not travelling?"
                        : "Travelling as normal?"
                }
                description={
                    pending?.action === "mark"
                        ? `${name} will not take the bus on ${prettyDate(pending.date)}. The driver's list will show them as on leave.`
                        : pending
                            ? `${name} will be expected on the bus on ${prettyDate(pending.date)} as usual.`
                            : ""
                }
                confirmLabel={pending?.action === "mark" ? "Mark as off" : "Yes, travelling"}
                onConfirm={confirm}
                onCancel={() => setPending(null)}
            />
        </div>
    )
}