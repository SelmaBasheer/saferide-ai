import { useState } from "react"
import { ArrowLeft, Pencil, Plus, X } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { DataTable, type Column } from "@/components/ui/data-table"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { useDebounce } from "@/lib/useDebounce"
import {
    useGetDriversQuery,
    type DriverListItem,
    type DriversQueryArgs,
} from "@/features/drivers/driverApi"
import CreateDriverForm from "@/features/drivers/CreateDriverForm"
import EditDriverForm from "@/features/drivers/EditDriverForm"

const PAGE_SIZE = 10
const EXPIRING_SOON_DAYS = 30

/** Asking for one row returns the real total in the envelope — the pattern the
 *  super admin overview already uses for its headline counts. */
const COUNT_ONLY = { page: 1, pageSize: 1 } as const

type TabKey = "all" | "active" | "expiring" | "expired" | "inactive"

const TABS: { key: TabKey; label: string; filter: Partial<DriversQueryArgs> }[] = [
    { key: "all", label: "All", filter: {} },
    { key: "active", label: "Active", filter: { status: "Active" } },
    { key: "expiring", label: "Licence expiring", filter: { status: "Active", licence: "expiring" } },
    { key: "expired", label: "Licence expired", filter: { status: "Active", licence: "expired" } },
    { key: "inactive", label: "Deactivated", filter: { status: "Inactive" } },
]

function formatDate(iso: string): string {
    const [y, m, d] = iso.split("-").map(Number)
    return new Date(y, m - 1, d).toLocaleDateString()
}

/**
 * Licence validity is arithmetic on a date, never a stored flag, so it can
 * never drift out of step with the date it describes. It is also separate from
 * employment status: a driver can be employed with a lapsed licence, and that
 * is exactly the case the school needs to see.
 */
function licenceState(iso: string): "valid" | "expiring" | "expired" {
    const [y, m, d] = iso.split("-").map(Number)
    const expiry = new Date(y, m - 1, d)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    if (expiry < today) return "expired"

    const days = Math.round((expiry.getTime() - today.getTime()) / 86_400_000)
    return days <= EXPIRING_SOON_DAYS ? "expiring" : "valid"
}

export default function DriversPage() {
    const [tab, setTab] = useState<TabKey>("all")
    const [search, setSearch] = useState("")
    const [page, setPage] = useState(1)
    const [showForm, setShowForm] = useState(false)
    const [editing, setEditing] = useState<DriverListItem | null>(null)
    const [message, setMessage] = useState<string | null>(null)

    const debouncedSearch = useDebounce(search, 400)
    const activeFilter = TABS.find((t) => t.key === tab)!.filter

    const { data, isLoading, isError } = useGetDriversQuery({
        ...activeFilter,
        search: debouncedSearch,
        page,
        pageSize: PAGE_SIZE,
    })

    // Counted across the whole school, not the current page — the figure in the
    // banner has to mean the same thing as the tab it points at.
    const expired = useGetDriversQuery({ ...COUNT_ONLY, status: "Active", licence: "expired" })
    const expiring = useGetDriversQuery({ ...COUNT_ONLY, status: "Active", licence: "expiring" })

    const expiredCount = expired.data?.totalCount ?? 0
    const expiringCount = expiring.data?.totalCount ?? 0

    const goToTab = (key: TabKey) => {
        setTab(key)
        setPage(1)
        setEditing(null)
    }

    // ---------- Editing takes over the page ----------
    // Showing the full list underneath an edit panel invites editing one driver
    // while looking at another. One screen, one subject.
    if (editing) {
        return (
            <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Drivers")}>
                <button
                    onClick={() => setEditing(null)}
                    className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
                >
                    <ArrowLeft className="h-4 w-4" /> Back to drivers
                </button>

                <div className="mt-4">
                    <EditDriverForm
                        key={editing.id}
                        driver={editing}
                        onSaved={() => {
                            setEditing(null)
                            setMessage("Driver updated.")
                        }}
                        onCancel={() => setEditing(null)}
                        onStatusChanged={(m) => {
                            setEditing(null)
                            setMessage(m)
                        }}
                    />
                </div>
            </DashboardLayout>
        )
    }

    const columns: Column<DriverListItem>[] = [
        {
            header: "Name",
            cell: (d) => (
                <span className="font-medium text-slate-800">
                    {d.firstName} {d.lastName}
                </span>
            ),
        },
        { header: "Email", cell: (d) => d.email },
        { header: "Phone", cell: (d) => d.phone, hideOnMobile: true },
        { header: "Licence", cell: (d) => d.licenseNumber, hideOnMobile: true },
        {
            header: "Expiry",
            cell: (d) => {
                const state = licenceState(d.licenseExpiryDate)
                return (
                    <div className="flex flex-wrap items-center justify-end gap-1.5 md:justify-start">
                        <span className={state === "expired" ? "text-red-700" : "text-slate-700"}>
                            {formatDate(d.licenseExpiryDate)}
                        </span>
                        {state === "expired" && (
                            <span className="rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-700">
                                expired
                            </span>
                        )}
                        {state === "expiring" && (
                            <span className="rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-800">
                                expiring
                            </span>
                        )}
                    </div>
                )
            },
        },
        {
            header: "Status",
            cell: (d) => (
                <span className={`rounded-full px-2 py-0.5 text-xs font-medium
                    ${d.status === "Active" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>
                    {d.status}
                </span>
            ),
        },
        {
            header: "",
            cell: (d) => (
                <div className="flex justify-end md:justify-start">
                    <button
                        onClick={() => {
                            setShowForm(false)
                            setMessage(null)
                            setEditing(d)
                        }}
                        className="inline-flex items-center gap-1 text-sm text-sky-700 hover:underline"
                    >
                        <Pencil className="h-3.5 w-3.5" /> Edit
                    </button>
                </div>
            ),
        },
    ]

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Drivers")}>
            <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                    <h1 className="text-xl font-semibold text-slate-800 sm:text-2xl">Drivers</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Drivers you add here receive an email invitation to set their own password.
                    </p>
                </div>
                <Button
                    className="bg-sky-700 hover:bg-sky-800"
                    onClick={() => setShowForm((s) => !s)}
                >
                    {showForm ? <X className="mr-1 h-4 w-4" /> : <Plus className="mr-1 h-4 w-4" />}
                    {showForm ? "Close" : "Add driver"}
                </Button>
            </div>

            {(expiredCount > 0 || expiringCount > 0) && (
                <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                    {expiredCount > 0 && (
                        <button onClick={() => goToTab("expired")} className="font-medium underline">
                            {expiredCount} expired
                        </button>
                    )}
                    {expiredCount > 0 && expiringCount > 0 && " · "}
                    {expiringCount > 0 && (
                        <button onClick={() => goToTab("expiring")} className="font-medium underline">
                            {expiringCount} expiring within {EXPIRING_SOON_DAYS} days
                        </button>
                    )}
                    {" "}— a driver cannot be assigned to a bus once the licence lapses.
                </div>
            )}

            {message && (
                <div
                    className="mt-4 cursor-pointer rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-700"
                    onClick={() => setMessage(null)}
                >
                    {message}
                </div>
            )}

            {showForm && (
                <div className="mt-6">
                    <CreateDriverForm onCreated={() => setShowForm(false)} />
                </div>
            )}

            <div className="mt-6 flex flex-wrap gap-2">
                {TABS.map((t) => (
                    <button
                        key={t.key}
                        onClick={() => goToTab(t.key)}
                        className={`rounded-md px-3 py-1.5 text-sm ${tab === t.key ? "bg-sky-700 text-white" : "border border-slate-300"
                            }`}
                    >
                        {t.label}
                    </button>
                ))}
            </div>

            <div className="mt-4 space-y-4">
                <Input placeholder="Search by name, email or licence…" className="max-w-sm bg-white"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(d) => d.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage={
                        tab === "all"
                            ? "No drivers yet — add your first driver."
                            : "No drivers match this filter."
                    }
                    page={page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}