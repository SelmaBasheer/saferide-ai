import { useState } from "react"
import { Link } from "react-router-dom"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { DataTable, type Column } from "@/components/ui/data-table"
import { ROUTES } from "@/routes/paths"
import { useGetTripsQuery, type TripSummary } from "@/features/tracking/trackingApi"
import { formatDate, formatDuration, formatTime } from "@/lib/tripFormat"

const PAGE_SIZE = 10

const statusClass = (s: TripSummary["status"]) =>
    s === "Active"
        ? "bg-emerald-50 text-emerald-700"
        : s === "Completed"
            ? "bg-slate-100 text-slate-600"
            : "bg-amber-50 text-amber-700"

const columns: Column<TripSummary>[] = [
    { header: "Date", cell: (t) => formatDate(t.startedAt) },
    {
        header: "Route",
        cell: (t) => (
            <Link
                to={ROUTES.schoolTripDetail.replace(":id", t.id)}
                className="font-medium text-sky-700 hover:underline"
            >
                {t.routeCode} — {t.routeName}
            </Link>
        ),
    },
    { header: "Started", cell: (t) => formatTime(t.startedAt) },
    { header: "Ended", cell: (t) => formatTime(t.endedAt) },
    { header: "Duration", cell: (t) => formatDuration(t.startedAt, t.endedAt) },
    { header: "Students", cell: (t) => t.studentCount },
    {
        header: "Unmarked",
        cell: (t) =>
            t.unmarkedCount > 0 ? (
                <span className="text-amber-700">{t.unmarkedCount}</span>
            ) : (
                <span className="text-slate-400">0</span>
            ),
    },
    {
        header: "Status",
        cell: (t) => (
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusClass(t.status)}`}>
                {t.status}
            </span>
        ),
    },
]

export default function TripsPage() {
    const [status, setStatus] = useState("")
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)

    const { data, isLoading, isError } = useGetTripsQuery({
        status,
        from,
        to,
        page,
        pageSize: PAGE_SIZE,
    })

    const clearFilters = () => {
        setStatus("")
        setFrom("")
        setTo("")
        setPage(1)
    }

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Trips")}>
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Trips</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Every run made by your fleet, with the stops reached and who boarded.
                </p>
            </div>

            <div className="mt-6 space-y-4">
                <div className="flex flex-wrap items-end gap-3">
                    <div>
                        <label className="text-xs text-slate-400">Status</label>
                        <select
                            value={status}
                            onChange={(e) => {
                                setStatus(e.target.value)
                                setPage(1)
                            }}
                            className="mt-1 block rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                        >
                            <option value="">All trips</option>
                            <option value="Active">In progress</option>
                            <option value="Completed">Completed</option>
                            <option value="Cancelled">Cancelled</option>
                        </select>
                    </div>

                    <div>
                        <label className="text-xs text-slate-400">From</label>
                        <input
                            type="date"
                            value={from}
                            max={to || undefined}
                            onChange={(e) => {
                                setFrom(e.target.value)
                                setPage(1)
                            }}
                            className="mt-1 block rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                        />
                    </div>

                    <div>
                        <label className="text-xs text-slate-400">To</label>
                        <input
                            type="date"
                            value={to}
                            min={from || undefined}
                            onChange={(e) => {
                                setTo(e.target.value)
                                setPage(1)
                            }}
                            className="mt-1 block rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                        />
                    </div>

                    {(status || from || to) && (
                        <button
                            onClick={clearFilters}
                            className="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-600"
                        >
                            Clear
                        </button>
                    )}
                </div>

                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(t) => t.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage="No trips match these filters."
                    page={page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}