import { useState } from "react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import ConfirmDialog from "@/components/ui/confirm-dialog"
import { formatDate, formatTime } from "@/lib/tripFormat"
import {
    useApproveAlertMutation,
    useDismissAlertMutation,
    useGetAlertsQuery,
    type Alert,
} from "@/features/alerts/alertApi"

const PAGE_SIZE = 10

const statusClass = (status: Alert["status"]) =>
    status === "Classified"
        ? "bg-amber-50 text-amber-800"
        : status === "Approved"
            ? "bg-emerald-50 text-emerald-700"
            : status === "Dismissed"
                ? "bg-slate-100 text-slate-600"
                : "bg-sky-50 text-sky-700"

/**
 * Three states, not two. The template classifier produces a real explanation
 * with classifiedByModel false, so "no model" does not mean "no explanation".
 * The third case — no explanation at all — only appears on a Detected alert,
 * which means classification never completed.
 */
const explanationLabel = (alert: Alert) =>
    alert.classifiedByModel
        ? "explained by model"
        : alert.draftMessage
            ? "template explanation"
            : "not yet interpreted"

export default function AlertsPage() {
    const [status, setStatus] = useState("Classified")
    const [page, setPage] = useState(1)
    const [pendingApproval, setPendingApproval] = useState<Alert | null>(null)
    const [actionError, setActionError] = useState<string | null>(null)

    // A set rather than a single id: two dismissals can be in flight at once, and
    // the first one to finish must not re-enable the button for the second.
    const [dismissingIds, setDismissingIds] = useState<ReadonlySet<string>>(new Set())

    const { data, isLoading, isError } = useGetAlertsQuery({ status, page, pageSize: PAGE_SIZE })
    const [approveAlert, { isLoading: approving }] = useApproveAlertMutation()
    const [dismissAlert] = useDismissAlertMutation()

    const alerts = data?.items ?? []
    const lastPage = Math.max(1, Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE))

    const onApprove = async () => {
        if (!pendingApproval) return
        setActionError(null)
        try {
            await approveAlert(pendingApproval.id).unwrap()
            setPendingApproval(null)
        } catch {
            // The alert stays Classified, so its Approve button is still there
            // to retry with. Close the dialog and say what happened.
            setPendingApproval(null)
            setActionError("Could not approve that alert. Nothing was changed — please try again.")
        }
    }

    const onDismiss = async (alert: Alert) => {
        if (dismissingIds.has(alert.id)) return

        setActionError(null)
        setDismissingIds((current) => new Set(current).add(alert.id))

        try {
            await dismissAlert(alert.id).unwrap()
        } catch {
            setActionError("Could not dismiss that alert. Nothing was changed — please try again.")
        } finally {
            setDismissingIds((current) => {
                const next = new Set(current)
                next.delete(alert.id)
                return next
            })
        }
    }

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Alerts")}>
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Alerts</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Unusual events detected during trips. Approving an alert marks its message as
                    ready to go to parents; nothing is delivered without your approval.
                </p>
            </div>

            <div className="mt-6 flex flex-wrap gap-2">
                {[
                    { value: "Classified", label: "Needs review" },
                    { value: "Approved", label: "Approved" },
                    { value: "Dismissed", label: "Dismissed" },
                    { value: "", label: "All" },
                ].map((tab) => (
                    <button
                        key={tab.value}
                        onClick={() => {
                            setStatus(tab.value)
                            setPage(1)
                            setActionError(null)
                        }}
                        className={`rounded-md px-3 py-1.5 text-sm ${status === tab.value ? "bg-sky-700 text-white" : "border border-slate-300"
                            }`}
                    >
                        {tab.label}
                    </button>
                ))}
            </div>

            {actionError && (
                <div
                    role="alert"
                    className="mt-4 flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                >
                    <span>{actionError}</span>
                    <button
                        onClick={() => setActionError(null)}
                        className="text-red-500 hover:text-red-700"
                        aria-label="Dismiss this error"
                    >
                        ✕
                    </button>
                </div>
            )}

            <div className="mt-6 space-y-4">
                {isLoading && <p className="text-slate-500">Loading…</p>}
                {isError && <p className="text-red-600">Could not load alerts.</p>}

                {!isLoading && alerts.length === 0 && (
                    <div className="rounded-lg border bg-white p-8 text-center">
                        <p className="text-slate-600">Nothing here.</p>
                        <p className="mt-1 text-sm text-slate-500">
                            Alerts appear when a trip does something unusual.
                        </p>
                    </div>
                )}

                {alerts.map((alert) => (
                    <section key={alert.id} className="rounded-lg border bg-white p-5">
                        <div className="flex flex-wrap items-start justify-between gap-3">
                            <div>
                                <div className="flex flex-wrap items-center gap-2">
                                    <span className="font-semibold text-slate-800">
                                        {alert.routeCode} — {alert.routeName}
                                    </span>
                                    <span
                                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusClass(alert.status)}`}
                                    >
                                        {alert.status === "Classified" ? "Needs review" : alert.status}
                                    </span>
                                </div>
                                <p className="mt-1 text-sm text-slate-500">
                                    {alert.type} · {formatDate(alert.detectedAtUtc)} at{" "}
                                    {formatTime(alert.detectedAtUtc)}
                                </p>
                            </div>

                            <div className="text-right text-xs">
                                {alert.classification && (
                                    <div className="font-medium text-slate-700">
                                        {alert.classification}
                                        {alert.confidence !== null &&
                                            ` · ${Math.round(alert.confidence * 100)}%`}
                                    </div>
                                )}
                                <div className="text-slate-400">{explanationLabel(alert)}</div>
                            </div>
                        </div>

                        {alert.draftMessage && (
                            <div className="mt-4 rounded-md bg-slate-50 p-3">
                                <div className="text-xs font-medium text-slate-500">
                                    Message for parents
                                </div>
                                <p className="mt-1 text-sm text-slate-800">{alert.draftMessage}</p>
                            </div>
                        )}

                        {alert.reasoning && (
                            <p className="mt-2 text-xs text-slate-500">Why: {alert.reasoning}</p>
                        )}

                        {alert.status === "Classified" && (
                            <div className="mt-4 flex gap-3">
                                <Button
                                    className="bg-sky-700 hover:bg-sky-800"
                                    onClick={() => {
                                        setActionError(null)
                                        setPendingApproval(alert)
                                    }}
                                >
                                    Approve
                                </Button>
                                <Button
                                    variant="outline"
                                    disabled={dismissingIds.has(alert.id)}
                                    onClick={() => onDismiss(alert)}
                                >
                                    {dismissingIds.has(alert.id) ? "Dismissing…" : "Dismiss"}
                                </Button>
                            </div>
                        )}
                    </section>
                ))}

                {lastPage > 1 && (
                    <div className="flex items-center justify-between text-sm">
                        <Button variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                            Previous
                        </Button>
                        <span className="text-slate-500">
                            Page {page} of {lastPage}
                        </span>
                        <Button
                            variant="outline"
                            disabled={page >= lastPage}
                            onClick={() => setPage((p) => p + 1)}
                        >
                            Next
                        </Button>
                    </div>
                )}
            </div>

            <ConfirmDialog
                open={pendingApproval !== null}
                busy={approving}
                title="Approve this message?"
                description={pendingApproval?.draftMessage ?? ""}
                confirmLabel="Approve"
                onConfirm={onApprove}
                onCancel={() => setPendingApproval(null)}
            />
        </DashboardLayout>
    )
}