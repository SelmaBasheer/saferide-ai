import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"
import {
    Bus as BusIcon, Users, Route as RouteIcon, MapPin,
    ClipboardList, AlertTriangle, CreditCard, CheckCircle2,
} from "lucide-react"
import { ROUTES } from "@/routes/paths"
import type { SchoolDetail } from "@/features/schools/schoolApi"
import { useTrackingHub } from "@/features/tracking/useTrackingHub"
import { useSchoolOverview } from "@/features/dashboard/useSchoolOverview"
import SchoolFleetMap, { type FleetMarker } from "@/features/dashboard/SchoolFleetMap"

const STALE_MS = 3 * 60 * 1000

interface LivePosition {
    latitude: number
    longitude: number
    routeCode: string
    recordedAt: string
}

function StatCard({ icon, label, value, sub, to, tone = "slate" }: {
    icon: React.ReactNode
    label: string
    value: React.ReactNode
    sub?: string
    to?: string
    tone?: "slate" | "amber"
}) {
    const body = (
        <div className={`rounded-lg border p-5 ${tone === "amber" ? "border-amber-200 bg-amber-50" : "bg-white"}
            ${to ? "transition hover:border-sky-300" : ""}`}>
            <div className={`flex items-center gap-2 text-sm ${tone === "amber" ? "text-amber-700" : "text-slate-500"}`}>
                {icon}
                {label}
            </div>
            <p className={`mt-1 text-2xl font-semibold ${tone === "amber" ? "text-amber-800" : "text-slate-800"}`}>
                {value}
            </p>
            {sub && (
                <p className={`mt-0.5 text-xs ${tone === "amber" ? "text-amber-600" : "text-slate-400"}`}>{sub}</p>
            )}
        </div>
    )

    return to ? <Link to={to}>{body}</Link> : body
}

export default function SchoolOverview({ school }: { school: SchoolDetail }) {
    const o = useSchoolOverview()

    // Positions arriving over the hub, keyed by trip. The server's
    // lastPosition is the starting point; these overwrite it as they land.
    const [live, setLive] = useState<Record<string, LivePosition>>({})

    // Deviations seen since this page opened. Deliberately not a total —
    // there is no "deviations today" endpoint, and inventing one from a
    // partial stream would be a number nobody could trust.
    const [deviationsSeen, setDeviationsSeen] = useState(0)

    const { status, joinFleet } = useTrackingHub({
        onPosition: (u) =>
            setLive((p) => ({
                ...p,
                [u.tripId]: {
                    latitude: u.latitude,
                    longitude: u.longitude,
                    routeCode: u.routeCode,
                    recordedAt: u.recordedAt,
                },
            })),
        onRouteDeviation: () => setDeviationsSeen((n) => n + 1),
        onTripStarted: () => void o.refetchActiveTrips(),
        onTripEnded: () => void o.refetchActiveTrips(),
    })

    useEffect(() => {
        if (status === "connected") void joinFleet()
    }, [status, joinFleet])

    const markers = useMemo<FleetMarker[]>(() => {
        return o.activeTrips
            .map((t) => {
                const hub = live[t.id]
                const latitude = hub?.latitude ?? t.lastPosition?.latitude
                const longitude = hub?.longitude ?? t.lastPosition?.longitude
                if (latitude === undefined || longitude === undefined) return null

                const at = hub?.recordedAt ?? t.lastPosition?.recordedAt
                const stale = !at || Date.now() - new Date(at).getTime() > STALE_MS

                return {
                    tripId: t.id,
                    routeCode: hub?.routeCode ?? t.routeCode,
                    latitude,
                    longitude,
                    stale,
                }
            })
            .filter((m): m is FleetMarker => m !== null)
    }, [o.activeTrips, live])

    const maxTrips = Math.max(1, ...o.tripsPerDay.map((d) => d.count))

    return (
        <>
            <div className="flex items-center gap-2">
                <h1 className="text-2xl font-semibold text-slate-800">{school.name}</h1>
                <CheckCircle2 className="h-5 w-5 text-emerald-600" />
            </div>
            <p className="mt-1 text-sm text-slate-500">
                {new Date().toLocaleDateString(undefined, {
                    weekday: "long", day: "numeric", month: "long", year: "numeric",
                })}
            </p>

            {o.isError && (
                <p className="mt-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                    Some figures could not be loaded. The numbers below may be incomplete.
                </p>
            )}

            {/* ---------- Summary cards ---------- */}

            <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <StatCard
                    icon={<BusIcon className="h-4 w-4" />} label="Buses"
                    value={o.isLoading ? "—" : o.buses.total}
                    sub={`${o.buses.active} active`}
                    to={ROUTES.schoolBuses}
                />
                <StatCard
                    icon={<Users className="h-4 w-4" />} label="Students"
                    value={o.isLoading ? "—" : o.studentCount}
                    to={ROUTES.schoolStudents}
                />
                <StatCard
                    icon={<RouteIcon className="h-4 w-4" />} label="Routes"
                    value={o.isLoading ? "—" : o.routeCount}
                    to={ROUTES.schoolRoutes}
                />
                <StatCard
                    icon={<MapPin className="h-4 w-4" />} label="Stops"
                    value={o.isLoading ? "—" : o.stopCount}
                    sub={o.stopCountPartial ? "across the first 100 routes" : "across all active routes"}
                />
                <StatCard
                    icon={<ClipboardList className="h-4 w-4" />} label="Drivers"
                    value={o.isLoading ? "—" : o.driverCount}
                    to={ROUTES.schoolDrivers}
                />
                <StatCard
                    icon={<AlertTriangle className="h-4 w-4" />} label="Open anomalies"
                    value={o.isLoading ? "—" : o.openAlertCount}
                    sub="waiting for your review"
                    to={ROUTES.schoolAlerts}
                    tone={o.openAlertCount > 0 ? "amber" : "slate"}
                />
            </div>

            {/* Shown, not hidden, so the gap is visible rather than forgotten. */}
            <div className="mt-4 flex items-center gap-3 rounded-lg border border-dashed p-4 text-slate-400">
                <CreditCard className="h-4 w-4" />
                <span className="text-sm">Subscription and billing — not built yet.</span>
            </div>

            {/* ---------- Live fleet ---------- */}

            <div className="mt-8 rounded-lg border bg-white p-5">
                <div className="flex flex-wrap items-center justify-between gap-2">
                    <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                        Live bus tracking
                    </h2>
                    <span className="text-xs">
                        {status === "connected" ? (
                            <span className="text-emerald-600">● live</span>
                        ) : status === "reconnecting" ? (
                            <span className="text-amber-600">● reconnecting…</span>
                        ) : status === "connecting" ? (
                            <span className="text-slate-500">● connecting…</span>
                        ) : (
                            <span className="text-slate-400">● not connected</span>
                        )}
                    </span>
                </div>

                <div className="mt-3 flex flex-wrap gap-6">
                    <div>
                        <span className="text-xl font-semibold text-slate-800">{o.activeTrips.length}</span>
                        <span className="ml-1 text-sm text-slate-500">on trip</span>
                    </div>
                    <div>
                        <span className="text-xl font-semibold text-amber-600">{o.silentCount}</span>
                        <span className="ml-1 text-sm text-slate-500">signal lost</span>
                    </div>
                    <div>
                        <span className="text-xl font-semibold text-red-600">{deviationsSeen}</span>
                        <span className="ml-1 text-sm text-slate-500">deviations since you opened this page</span>
                    </div>
                </div>

                <div className="mt-4">
                    <SchoolFleetMap markers={markers} />
                </div>
            </div>

            {/* ---------- Recent anomalies ---------- */}

            <div className="mt-6 rounded-lg border bg-white p-5">
                <div className="flex items-center justify-between">
                    <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                        Recent anomalies
                    </h2>
                    <Link to={ROUTES.schoolAlerts} className="text-sm text-sky-700 hover:underline">
                        View all
                    </Link>
                </div>

                {o.recentAlerts.length === 0 ? (
                    <p className="mt-3 text-sm text-slate-500">Nothing flagged recently.</p>
                ) : (
                    <div className="mt-2 divide-y">
                        {o.recentAlerts.map((a) => (
                            <div key={a.id} className="py-3">
                                <div className="flex flex-wrap items-center gap-2">
                                    <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                                        {a.type}
                                    </span>
                                    <span className="text-sm text-slate-700">{a.routeCode} · {a.routeName}</span>
                                    <span className="ml-auto text-xs text-slate-400">
                                        {new Date(a.detectedAtUtc).toLocaleString()}
                                    </span>
                                </div>

                                <p className="mt-1 text-sm text-slate-600">
                                    {a.classification ? (
                                        <>
                                            <span className="font-medium text-slate-800">{a.classification}</span>
                                            {a.confidence !== null && (
                                                <> · confidence {a.confidence.toFixed(2)}</>
                                            )}
                                            {!a.classifiedByModel && (
                                                <span className="text-slate-400"> · template wording</span>
                                            )}
                                            {a.reasoning && <> — {a.reasoning}</>}
                                        </>
                                    ) : (
                                        <span className="text-slate-400">Not yet classified.</span>
                                    )}
                                </p>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* ---------- Trips per day ---------- */}

            <div className="mt-6 rounded-lg border bg-white p-5">
                <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                    Trips completed, last 7 days
                </h2>

                <div className="mt-6 flex h-32 items-end gap-3">
                    {o.tripsPerDay.map((d) => (
                        <div key={d.date} className="flex flex-1 flex-col items-center justify-end">
                            <span className="mb-1 text-xs text-slate-500">{d.count}</span>
                            <div
                                className={`w-full rounded-t ${d.count === 0 ? "bg-slate-200" : "bg-sky-600"}`}
                                style={{ height: `${Math.max(2, Math.round((d.count / maxTrips) * 100))}%` }}
                            />
                            <span className="mt-1 text-xs text-slate-400">{d.label}</span>
                        </div>
                    ))}
                </div>

                {o.tripHistoryPartial && (
                    <p className="mt-3 text-xs text-slate-400">
                        More trips than this page holds — the chart is showing a sample.
                    </p>
                )}
            </div>
        </>
    )
}