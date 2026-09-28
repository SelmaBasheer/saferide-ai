import { useState } from "react"
import { Download } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { formatRupees } from "@/features/subscriptions/subscriptionApi"
import {
    useGetSuperAdminReportQuery,
    useDownloadSuperAdminReportMutation,
} from "@/features/reports/reportsApi"

function errorMessage(e: unknown, fallback: string) {
    const data = (e as { data?: { error?: { message?: string } } })?.data
    return data?.error?.message ?? fallback
}

/**
 * Local date, not UTC. toISOString() returns yesterday for anyone east of
 * Greenwich once it is past 18:30 UTC — which in Kolkata is most of the evening,
 * so "today" would silently exclude today's payments.
 */
function isoDate(d: Date): string {
    const year = d.getFullYear()
    const month = String(d.getMonth() + 1).padStart(2, "0")
    const day = String(d.getDate()).padStart(2, "0")
    return `${year}-${month}-${day}`
}

function defaultRange() {
    const to = new Date()
    const from = new Date()
    from.setDate(from.getDate() - 30)
    return { from: isoDate(from), to: isoDate(to) }
}

function Stat({ label, value }: { label: string; value: string | number }) {
    return (
        <div className="rounded-lg border bg-white p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
            <p className="mt-1 text-2xl font-semibold text-slate-800">{value}</p>
        </div>
    )
}

export default function SuperAdminReportsPage() {
    const initial = defaultRange()

    // Two pieces of state: what the inputs show, and what has been asked for.
    // Without the split, every keystroke in a date field fires a new query.
    const [draft, setDraft] = useState(initial)
    const [range, setRange] = useState(initial)

    const { data, isFetching, isError, error } = useGetSuperAdminReportQuery(range)
    const [download, { isLoading: isDownloading, error: downloadError, reset }] =
        useDownloadSuperAdminReportMutation()

    const save = async () => {
        try {
            const blob = await download({ ...range, format: "csv" }).unwrap()

            const url = URL.createObjectURL(blob)
            const link = document.createElement("a")
            link.href = url
            link.download = `saferide-platform-${range.from}-to-${range.to}.csv`
            link.click()

            // Revoke, or the blob stays in memory until the tab closes.
            URL.revokeObjectURL(url)

            // The file is on disk; nothing should still be holding it in the
            // RTK Query cache either.
            reset()
        } catch {
            // Shown below.
        }
    }

    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Reports")}>
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Reports</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Schools, subscriptions and revenue across the platform. Read from the analytics
                    store, which is updated from events — so figures may lag the live screens by a
                    moment.
                </p>
            </div>

            <form
                className="mt-6 flex flex-wrap items-end gap-3 rounded-lg border bg-white p-4"
                onSubmit={(e) => {
                    e.preventDefault()
                    setRange(draft)
                }}
            >
                <label className="text-sm">
                    <span className="text-slate-600">From</span>
                    <Input
                        type="date"
                        className="mt-1"
                        value={draft.from}
                        max={draft.to}
                        onChange={(e) => setDraft({ ...draft, from: e.target.value })}
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">To</span>
                    <Input
                        type="date"
                        className="mt-1"
                        value={draft.to}
                        min={draft.from}
                        onChange={(e) => setDraft({ ...draft, to: e.target.value })}
                    />
                </label>

                <Button type="submit" className="bg-sky-700 hover:bg-sky-800">
                    Apply
                </Button>

                <Button
                    type="button"
                    variant="outline"
                    disabled={!data || isDownloading}
                    onClick={save}
                >
                    <Download className="mr-1 h-4 w-4" />
                    {isDownloading ? "Preparing…" : "Download CSV"}
                </Button>
            </form>

            {isError && (
                <p className="mt-4 text-sm text-red-600">
                    {errorMessage(error, "Could not load the report.")}
                </p>
            )}

            {downloadError && (
                <p className="mt-4 text-sm text-red-600">
                    {errorMessage(downloadError, "Could not download the report.")}
                </p>
            )}

            {isFetching && <p className="mt-4 text-sm text-slate-500">Loading…</p>}

            {data && (
                <>
                    <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        <Stat label="Schools" value={data.summary.totalSchools} />
                        <Stat label="Approved" value={data.summary.approvedSchools} />
                        <Stat label="Suspended" value={data.summary.suspendedSchools} />
                        <Stat label="Revenue" value={formatRupees(data.summary.revenuePaise)} />
                    </div>

                    <div className="mt-6 overflow-hidden rounded-lg border bg-white">
                        <h2 className="border-b bg-slate-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                            Schools
                        </h2>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead className="border-b text-left text-xs uppercase tracking-wide text-slate-400">
                                    <tr>
                                        <th className="px-4 py-3">Name</th>
                                        <th className="px-4 py-3">City</th>
                                        <th className="px-4 py-3">Status</th>
                                        <th className="px-4 py-3">Plan</th>
                                        <th className="px-4 py-3">Subscription</th>
                                        <th className="px-4 py-3">Ends</th>
                                        <th className="px-4 py-3">Buses</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y">
                                    {data.schools.length === 0 ? (
                                        <tr>
                                            <td colSpan={7} className="px-4 py-6 text-slate-500">
                                                No schools yet.
                                            </td>
                                        </tr>
                                    ) : (
                                        data.schools.map((s) => (
                                            <tr key={s.name}>
                                                <td className="px-4 py-3 font-medium text-slate-800">
                                                    {s.name}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.city}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.status}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.planName ?? "—"}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.subscriptionStatus}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.subscriptionEndsOn ?? "—"}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {s.busesInUse} / {s.busLimit ?? "Unlimited"}
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div className="mt-6 overflow-hidden rounded-lg border bg-white">
                        <h2 className="border-b bg-slate-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                            Revenue
                        </h2>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead className="border-b text-left text-xs uppercase tracking-wide text-slate-400">
                                    <tr>
                                        <th className="px-4 py-3">Date</th>
                                        <th className="px-4 py-3">School</th>
                                        <th className="px-4 py-3">Plan</th>
                                        <th className="px-4 py-3">Amount</th>
                                        <th className="px-4 py-3">Status</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y">
                                    {data.revenue.length === 0 ? (
                                        <tr>
                                            <td colSpan={5} className="px-4 py-6 text-slate-500">
                                                No payments in this range.
                                            </td>
                                        </tr>
                                    ) : (
                                        data.revenue.map((r, i) => (
                                            <tr key={`${r.paymentDate}-${r.schoolName}-${i}`}>
                                                <td className="px-4 py-3 text-slate-800">
                                                    {r.paymentDate}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {r.schoolName}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {r.planName}
                                                </td>
                                                <td className="px-4 py-3 font-medium text-slate-800">
                                                    {formatRupees(r.amountPaise)}
                                                </td>
                                                <td className="px-4 py-3 text-slate-500">
                                                    {r.status}
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div className="mt-6 overflow-hidden rounded-lg border bg-white">
                        <h2 className="border-b bg-slate-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                            By plan
                        </h2>
                        <div className="divide-y">
                            {data.plans.length === 0 ? (
                                <p className="px-4 py-6 text-sm text-slate-500">Nothing sold yet.</p>
                            ) : (
                                data.plans.map((p) => (
                                    <div
                                        key={p.planName}
                                        className="flex items-baseline justify-between px-4 py-3 text-sm"
                                    >
                                        <span className="font-medium text-slate-800">
                                            {p.planName}
                                        </span>
                                        <span className="text-slate-500">
                                            {p.paymentCount} paid · {formatRupees(p.revenuePaise)}
                                        </span>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>

                    <p className="mt-3 text-sm text-slate-400">
                        Student and trip data is deliberately absent — a platform administrator has
                        no need to see a child's record.
                    </p>
                </>
            )}
        </DashboardLayout>
    )
}