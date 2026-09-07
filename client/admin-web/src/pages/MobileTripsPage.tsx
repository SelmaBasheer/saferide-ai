import { useState } from "react"
import { Link } from "react-router-dom"
import { ArrowLeft } from "lucide-react"
import { useGetTripsQuery } from "@/features/tracking/trackingApi"
import { formatDate, formatDuration, formatTime } from "@/lib/tripFormat"

const PAGE_SIZE = 10

export default function MobileTripsPage({
    title,
    homePath,
    detailPath,
}: {
    title: string
    homePath: string
    detailPath: string
}) {
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)

    const { data, isLoading, isError, refetch } = useGetTripsQuery({
        from,
        to,
        page,
        pageSize: PAGE_SIZE,
    })

    const trips = data?.items ?? []
    const total = data?.totalCount ?? 0
    const lastPage = Math.max(1, Math.ceil(total / PAGE_SIZE))

    return (
        <div className="flex flex-col gap-3 p-4">
            <Link
                to={homePath}
                className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
            >
                <ArrowLeft className="h-4 w-4" /> Back
            </Link>

            <h1 className="text-xl font-semibold">{title}</h1>

            <div className="flex items-end gap-2">
                <div className="flex-1">
                    <label className="text-xs text-slate-500">From</label>
                    <input
                        type="date"
                        value={from}
                        max={to || undefined}
                        onChange={(e) => {
                            setFrom(e.target.value)
                            setPage(1)
                        }}
                        className="mt-1 w-full rounded-lg border border-slate-300 px-2 py-2 text-sm"
                    />
                </div>
                <div className="flex-1">
                    <label className="text-xs text-slate-500">To</label>
                    <input
                        type="date"
                        value={to}
                        min={from || undefined}
                        onChange={(e) => {
                            setTo(e.target.value)
                            setPage(1)
                        }}
                        className="mt-1 w-full rounded-lg border border-slate-300 px-2 py-2 text-sm"
                    />
                </div>
                {(from || to) && (
                    <button
                        onClick={() => {
                            setFrom("")
                            setTo("")
                            setPage(1)
                        }}
                        className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-600"
                    >
                        Clear
                    </button>
                )}
            </div>

            {isError && (
                <div className="rounded-lg border border-slate-200 p-4 text-center">
                    <p className="text-slate-600">Could not load your trips.</p>
                    <button
                        onClick={() => refetch()}
                        className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm"
                    >
                        Try again
                    </button>
                </div>
            )}

            {isLoading && <p className="text-slate-500">Loading…</p>}

            {!isLoading && !isError && trips.length === 0 && (
                <p className="text-sm text-slate-500">
                    {from || to ? "No trips in this date range." : "No trips yet."}
                </p>
            )}

            {trips.map((t) => (
                <Link
                    key={t.id}
                    to={detailPath.replace(":id", t.id)}
                    className="rounded-xl border border-slate-200 p-4"
                >
                    <div className="flex items-start justify-between gap-3">
                        <div>
                            <div className="font-semibold">{t.routeCode}</div>
                            <div className="text-sm text-slate-600">{t.routeName}</div>
                        </div>
                        <span
                            className={`rounded-full px-2 py-0.5 text-xs font-medium ${t.status === "Active"
                                    ? "bg-emerald-50 text-emerald-700"
                                    : t.status === "Completed"
                                        ? "bg-slate-100 text-slate-600"
                                        : "bg-amber-50 text-amber-700"
                                }`}
                        >
                            {t.status}
                        </span>
                    </div>
                    <div className="mt-2 text-sm text-slate-500">
                        {formatDate(t.startedAt)} · {formatTime(t.startedAt)} to {formatTime(t.endedAt)} ·{" "}
                        {formatDuration(t.startedAt, t.endedAt)}
                    </div>
                </Link>
            ))}

            {total > PAGE_SIZE && (
                <div className="mt-2 flex items-center justify-between text-sm">
                    <button
                        disabled={page <= 1}
                        onClick={() => setPage((p) => p - 1)}
                        className="rounded-lg border border-slate-300 px-4 py-2 disabled:opacity-40"
                    >
                        Previous
                    </button>
                    <span className="text-slate-500">
                        Page {page} of {lastPage}
                    </span>
                    <button
                        disabled={page >= lastPage}
                        onClick={() => setPage((p) => p + 1)}
                        className="rounded-lg border border-slate-300 px-4 py-2 disabled:opacity-40"
                    >
                        Next
                    </button>
                </div>
            )}
        </div>
    )
}