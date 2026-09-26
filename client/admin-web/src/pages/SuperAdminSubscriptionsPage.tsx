import { useState } from "react"
import { Plus, X } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import { DataTable, type Column } from "@/components/ui/data-table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useGetSchoolsQuery } from "@/features/schools/schoolApi"
import {
    useGetPlansQuery,
    useGetSubscriptionsQuery,
    useActivateSubscriptionMutation,
    formatRupees,
    type Subscription,
    type SubscriptionStatus,
} from "@/features/subscriptions/subscriptionApi"

const PAGE_SIZE = 10

const FILTERS: { label: string; value: SubscriptionStatus | "All" }[] = [
    { label: "All", value: "All" },
    { label: "Active", value: "Active" },
    { label: "Grace period", value: "InGrace" },
    { label: "Expired", value: "Expired" },
    { label: "Cancelled", value: "Cancelled" },
]

const badge: Record<SubscriptionStatus, string> = {
    Active: "bg-emerald-50 text-emerald-700",
    InGrace: "bg-amber-50 text-amber-700",
    Expired: "bg-red-50 text-red-700",
    Cancelled: "bg-slate-100 text-slate-500",
}

function errorMessage(e: unknown, fallback: string) {
    const data = (e as { data?: { error?: { message?: string } } })?.data
    return data?.error?.message ?? fallback
}

function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" })
}

function ActivateForm({ onDone }: { onDone: () => void }) {
    const [schoolId, setSchoolId] = useState("")
    const [planId, setPlanId] = useState("")
    const [startsOn, setStartsOn] = useState("")

    const { data: schools } = useGetSchoolsQuery({ status: "Approved", page: 1, pageSize: 200 })
    const { data: plans = [] } = useGetPlansQuery(false)
    const [activate, { isLoading, error }] = useActivateSubscriptionMutation()

    const submit = async (e: React.FormEvent) => {
        e.preventDefault()

        await activate({ schoolId, planId, startsOn: startsOn || null })
            .unwrap()
            .then(() => { setSchoolId(""); setPlanId(""); setStartsOn(""); onDone() })
            .catch(() => { /* shown below */ })
    }

    return (
        <form onSubmit={submit} className="rounded-lg border bg-white p-5">
            <p className="text-sm text-slate-500">
                Records a payment made outside SafeRide — a bank transfer or a cheque. Online
                payment is not built yet.
            </p>

            <div className="mt-4 grid gap-4 sm:grid-cols-3">
                <label className="text-sm">
                    <span className="text-slate-600">School</span>
                    <select required value={schoolId} onChange={(e) => setSchoolId(e.target.value)}
                        className="mt-1 h-9 w-full rounded-md border bg-white px-2 text-sm outline-none focus:ring-2 focus:ring-sky-200">
                        <option value="">Choose a school…</option>
                        {schools?.items.map((s) => (
                            <option key={s.id} value={s.id}>{s.name} — {s.city}</option>
                        ))}
                    </select>
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Plan</span>
                    <select required value={planId} onChange={(e) => setPlanId(e.target.value)}
                        className="mt-1 h-9 w-full rounded-md border bg-white px-2 text-sm outline-none focus:ring-2 focus:ring-sky-200">
                        <option value="">Choose a plan…</option>
                        {plans.map((p) => (
                            <option key={p.id} value={p.id}>
                                {p.name} — {formatRupees(p.priceInPaise)}
                            </option>
                        ))}
                    </select>
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Starts on</span>
                    <Input className="mt-1" type="date" value={startsOn}
                        onChange={(e) => setStartsOn(e.target.value)} />
                    <span className="mt-1 block text-xs text-slate-400">Leave empty for today</span>
                </label>
            </div>

            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {errorMessage(error, "Could not activate the subscription.")}
                </p>
            )}

            <Button type="submit" disabled={isLoading} className="mt-4 bg-sky-700 hover:bg-sky-800">
                {isLoading ? "Activating…" : "Activate"}
            </Button>
        </form>
    )
}

export default function SuperAdminSubscriptionsPage() {
    const [filter, setFilter] = useState<SubscriptionStatus | "All">("All")
    const [page, setPage] = useState(1)
    const [showForm, setShowForm] = useState(false)

    const { data, isLoading, isError } = useGetSubscriptionsQuery({
        status: filter === "All" ? undefined : filter,
        page,
        pageSize: PAGE_SIZE,
    })

    const columns: Column<Subscription>[] = [
        {
            header: "School",
            cell: (s) => <span className="font-medium text-slate-800">{s.schoolName ?? "—"}</span>,
        },
        { header: "Plan", cell: (s) => s.planName },
        { header: "Price", cell: (s) => formatRupees(s.priceInPaise) },
        {
            header: "Buses",
            cell: (s) => (s.busLimit === null ? "Unlimited" : s.busLimit),
        },
        { header: "Ends", cell: (s) => formatDate(s.endsOn) },
        {
            header: "Remaining",
            cell: (s) => (
                <span className={s.daysRemaining <= 30 ? "text-amber-600" : "text-slate-600"}>
                    {s.daysRemaining} days
                </span>
            ),
        },
        {
            header: "Status",
            cell: (s) => (
                <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${badge[s.status]}`}>
                    {s.status === "InGrace" ? "Grace" : s.status}
                </span>
            ),
        },
    ]

    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Subscriptions")}>
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-slate-800">Subscriptions</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Who is paying, for what, and until when.
                    </p>
                </div>
                <Button className="bg-sky-700 hover:bg-sky-800" onClick={() => setShowForm((s) => !s)}>
                    {showForm ? <X className="mr-1 h-4 w-4" /> : <Plus className="mr-1 h-4 w-4" />}
                    {showForm ? "Close" : "Activate"}
                </Button>
            </div>

            {showForm && (
                <div className="mt-6">
                    <ActivateForm onDone={() => setShowForm(false)} />
                </div>
            )}

            <div className="mt-6 flex flex-wrap items-center gap-2">
                {FILTERS.map((f) => (
                    <button key={f.value} onClick={() => { setFilter(f.value); setPage(1) }}
                        className={`rounded-full px-4 py-1.5 text-sm ${filter === f.value ? "bg-sky-700 text-white" : "bg-white text-slate-600 border"}`}>
                        {f.label}
                    </button>
                ))}
            </div>

            <div className="mt-4">
                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(s) => s.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage="No subscriptions match."
                    page={data?.page ?? page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}