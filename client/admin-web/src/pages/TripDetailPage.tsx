import { Link, useParams } from "react-router-dom"
import { ArrowLeft } from "lucide-react"
import { MapContainer, TileLayer, Marker, Polyline, Circle } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import { useGetTripQuery } from "@/features/tracking/trackingApi"
import { formatDate, formatDuration, formatTime } from "@/lib/tripFormat"

const stopIcon = (n: number, reached: boolean) =>
    L.divIcon({
        className: "",
        html: `<div style="width:22px;height:22px;border-radius:50%;background:${reached ? "#22c55e" : "#fff"
            };border:2px solid #444;display:flex;align-items:center;justify-content:center;
        font:600 11px system-ui;color:${reached ? "#fff" : "#222"}">${n}</div>`,
        iconSize: [22, 22],
        iconAnchor: [11, 11],
    })

export default function TripDetailPage() {
    const { id = "" } = useParams()
    const { data: trip, isLoading, isError, refetch } = useGetTripQuery(id, { skip: !id })

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Trips")}>
            <Link
                to={ROUTES.schoolTrips}
                className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
            >
                <ArrowLeft className="h-4 w-4" /> Back to trips
            </Link>

            {isError && (
                <div className="mt-6 rounded-lg border bg-white p-6 text-center">
                    <p className="text-slate-600">Could not load this trip.</p>
                    <Button className="mt-3" variant="outline" onClick={() => refetch()}>
                        Try again
                    </Button>
                </div>
            )}

            {isLoading && <p className="mt-6 text-slate-500">Loading…</p>}

            {trip && (() => {
                const boarded = trip.roster.filter((r) => r.boardingStatus === "Boarded").length
                const absent = trip.roster.filter((r) => r.boardingStatus === "Absent").length
                const unmarked = trip.roster.filter((r) => r.boardingStatus === "Unmarked").length
                const stopIds = new Set(trip.stops.map((s) => s.stopId))
                const orphans = trip.roster.filter((r) => !stopIds.has(r.pickupStopId))
                const path = trip.path.map((p) => [p.latitude, p.longitude] as [number, number])
                const centre = path[0] ?? [8.8901, 76.6012]

                return (
                    <>
                        <div className="mt-4 flex items-start justify-between">
                            <div>
                                <h1 className="text-2xl font-semibold text-slate-800">
                                    {trip.routeCode} — {trip.routeName}
                                </h1>
                                <p className="mt-1 text-sm text-slate-500">
                                    {formatDate(trip.startedAt)} · {formatTime(trip.startedAt)} to{" "}
                                    {formatTime(trip.endedAt)} · {formatDuration(trip.startedAt, trip.endedAt)}
                                </p>
                            </div>
                            <span
                                className={`rounded-full px-3 py-1 text-xs font-medium ${trip.status === "Active"
                                    ? "bg-emerald-50 text-emerald-700"
                                    : trip.status === "Completed"
                                        ? "bg-slate-100 text-slate-600"
                                        : "bg-amber-50 text-amber-700"
                                    }`}
                            >
                                {trip.status}
                            </span>
                        </div>

                        <div className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
                            {[
                                { label: "On the roster", value: trip.roster.length, tone: "text-slate-800" },
                                { label: "Boarded", value: boarded, tone: "text-emerald-700" },
                                { label: "Absent", value: absent, tone: "text-slate-600" },
                                { label: "Unmarked", value: unmarked, tone: unmarked > 0 ? "text-amber-700" : "text-slate-400" },
                            ].map((card) => (
                                <div key={card.label} className="rounded-lg border bg-white p-4">
                                    <div className="text-xs text-slate-400">{card.label}</div>
                                    <div className={`mt-1 text-2xl font-semibold ${card.tone}`}>{card.value}</div>
                                </div>
                            ))}
                        </div>

                        {path.length > 1 && (
                            <div className="mt-6 h-64 w-full overflow-hidden rounded-lg border">
                                <MapContainer center={centre} zoom={13} className="h-full w-full">
                                    <TileLayer
                                        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                                        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                                    />
                                    <Polyline positions={path} pathOptions={{ color: "#2563eb", weight: 4 }} />
                                    {trip.stops.map((s) => (
                                        <Circle
                                            key={`c-${s.stopId}`}
                                            center={[s.latitude, s.longitude]}
                                            radius={100}
                                            pathOptions={{ color: "#9ca3af", weight: 1, fillOpacity: 0.08 }}
                                        />
                                    ))}
                                    {trip.stops.map((s) => (
                                        <Marker
                                            key={`m-${s.stopId}`}
                                            position={[s.latitude, s.longitude]}
                                            icon={stopIcon(s.sequence, !!s.reachedAt)}
                                        />
                                    ))}
                                </MapContainer>
                            </div>
                        )}

                        <section className="mt-6 rounded-lg border bg-white p-6">
                            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                Stops
                            </h2>

                            {trip.stops.map((stop) => {
                                const students = trip.roster.filter((r) => r.pickupStopId === stop.stopId)
                                const late =
                                    stop.reachedAt &&
                                    formatTime(stop.reachedAt) > stop.pickupTime

                                return (
                                    <div key={stop.stopId} className="mb-4 border-b border-slate-100 pb-4 last:mb-0 last:border-0 last:pb-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <span
                                                className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold ${stop.reachedAt ? "bg-emerald-500 text-white" : "bg-slate-200 text-slate-600"
                                                    }`}
                                            >
                                                {stop.sequence}
                                            </span>
                                            <span className="font-medium text-slate-800">{stop.name}</span>
                                            <span className="text-sm text-slate-500">
                                                scheduled {stop.pickupTime}
                                            </span>
                                            {stop.reachedAt ? (
                                                <span className={`text-sm ${late ? "text-amber-700" : "text-emerald-700"}`}>
                                                    · arrived {formatTime(stop.reachedAt)}
                                                </span>
                                            ) : (
                                                <span className="text-sm text-slate-400">· not reached</span>
                                            )}
                                        </div>

                                        {students.length === 0 ? (
                                            <p className="mt-2 pl-8 text-sm text-slate-400">No students at this stop</p>
                                        ) : (
                                            students.map((s) => (
                                                <div
                                                    key={s.studentId}
                                                    className="mt-1 flex items-center gap-2 pl-8 text-sm"
                                                >
                                                    <span className="flex-1 text-slate-700">{s.name}</span>
                                                    {s.markedAt && (
                                                        <span className="text-xs text-slate-400">
                                                            {formatTime(s.markedAt)}
                                                        </span>
                                                    )}
                                                    <span
                                                        className={
                                                            s.boardingStatus === "Boarded"
                                                                ? "text-emerald-600"
                                                                : s.boardingStatus === "Absent"
                                                                    ? "text-slate-500"
                                                                    : "text-amber-600"
                                                        }
                                                    >
                                                        {s.boardingStatus}
                                                    </span>
                                                </div>
                                            ))
                                        )}
                                    </div>
                                )
                            })}
                        </section>

                        {orphans.length > 0 && (
                            <section className="mt-6 rounded-lg border border-red-200 bg-white p-6">
                                <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                    Not linked to a stop on this route
                                </h2>
                                <p className="mb-3 text-sm text-slate-600">
                                    These students were on the roster when the trip started, but their pickup
                                    stop is not on this route. The driver could not mark them.
                                </p>
                                {orphans.map((s) => (
                                    <div key={s.studentId} className="flex items-center justify-between py-1 text-sm">
                                        <span className="text-slate-700">{s.name}</span>
                                        <span className="text-red-700">{s.boardingStatus}</span>
                                    </div>
                                ))}
                            </section>
                        )}
                    </>
                )
            })()}
        </DashboardLayout>
    )
}