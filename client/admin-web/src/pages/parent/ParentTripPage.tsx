import { useEffect, useMemo, useState } from "react"
import { useParams } from "react-router-dom"
import { MapContainer, TileLayer, Marker, Polyline, Circle, useMap } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"

import type { LatLng } from "@/lib/geo"
import { useTrackingHub } from "@/features/tracking/useTrackingHub"
import { useGetTripQuery } from "@/features/tracking/trackingApi"

const busIcon = L.divIcon({
    className: "",
    html: '<div style="font-size:30px;line-height:30px">🚌</div>',
    iconSize: [30, 30],
    iconAnchor: [15, 15],
})

const stopIcon = (n: number, reached: boolean, mine: boolean) =>
    L.divIcon({
        className: "",
        html: `<div style="width:24px;height:24px;border-radius:50%;background:${reached ? "#22c55e" : mine ? "#2563eb" : "#fff"
            };border:2px solid #444;display:flex;align-items:center;justify-content:center;
        font:600 11px system-ui;color:${reached || mine ? "#fff" : "#222"}">${n}</div>`,
        iconSize: [24, 24],
        iconAnchor: [12, 12],
    })

function Recenter({ position }: { position: LatLng | null }) {
    const map = useMap()
    useEffect(() => {
        if (position) map.panTo(position)
    }, [position, map])
    return null
}

export default function ParentTripPage() {
    const { id = "" } = useParams()
    const { data: trip, isLoading, isError, refetch } = useGetTripQuery(id, { skip: !id })

    const [position, setPosition] = useState<LatLng | null>(null)
    const [lastAt, setLastAt] = useState<number | null>(null)
    const [alerts, setAlerts] = useState<string[]>([])
    const [, setTick] = useState(0)

    const push = (msg: string) => setAlerts((a) => [msg, ...a].slice(0, 6))

    const { status, joinTrip, leaveTrip } = useTrackingHub({
        onPosition: (u) => {
            setPosition([u.latitude, u.longitude])
            setLastAt(Date.now())
        },
        onStopReached: (n) => {
            push(`Bus reached ${n.stopName}`)
            void refetch()
        },
        onApproachingStop: (n) => push(`🔔 Bus is ${n.stopsAway} stops from ${n.stopName} — get ready`),
        onStudentBoarded: (n) =>
            push(
                n.status === "Boarded"
                    ? `✅ ${n.studentName} boarded at ${n.stopName}`
                    : `⚠️ ${n.studentName} was marked absent at ${n.stopName}`
            ),
        onTripEnded: () => {
            push("Trip finished")
            void refetch()
        },
    })

    useEffect(() => {
        if (status === "connected" && id) void joinTrip(id)
        return () => {
            void leaveTrip(id)
        }
    }, [status, id, joinTrip, leaveTrip])

    useEffect(() => {
        if (trip?.lastPosition) {
            setPosition([trip.lastPosition.latitude, trip.lastPosition.longitude])
            setLastAt(new Date(trip.lastPosition.recordedAt).getTime())
        }
    }, [trip?.lastPosition])

    // re-render every 10s so the stale check stays current
    useEffect(() => {
        const t = setInterval(() => setTick((n) => n + 1), 10000)
        return () => clearInterval(t)
    }, [])

    const path = useMemo<LatLng[]>(
        () => (trip?.path ?? []).map((p) => [p.latitude, p.longitude] as LatLng),
        [trip?.path]
    )

    if (isError) {
        return (
            <div className="p-6 text-center">
                <p className="text-slate-600">Could not load the bus right now.</p>
                <button
                    onClick={() => refetch()}
                    className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm"
                >
                    Try again
                </button>
            </div>
        )
    }

    if (isLoading || !trip) return <div className="p-6 text-slate-500">Loading…</div>

    const myStopIds = new Set(trip.roster.map((r) => r.pickupStopId))
    const myStops = trip.stops.filter((s) => myStopIds.has(s.stopId))
    const stale = lastAt !== null && Date.now() - lastAt > 60000
    const centre: LatLng = position ?? path[0] ?? [8.8901, 76.6012]

    return (
        <div className="flex min-h-dvh flex-col">
            <header className="border-b border-slate-200 p-3">
                <div className="flex items-center justify-between">
                    <div>
                        <div className="font-semibold">{trip.routeCode}</div>
                        <div className="text-xs text-slate-500">{trip.routeName}</div>
                    </div>
                    <div className="text-right text-xs">
                        {trip.status !== "Active" ? (
                            <span className="text-slate-500">Trip finished</span>
                        ) : status !== "connected" ? (
                            <span className="text-amber-600">● connecting…</span>
                        ) : lastAt === null ? (
                            <span className="text-slate-500">● waiting for the bus</span>
                        ) : stale ? (
                            <span className="text-amber-600">● signal lost</span>
                        ) : (
                            <span className="text-emerald-600">● live</span>
                        )}
                    </div>
                </div>
            </header>

            <div className="flex-1">
                <MapContainer center={centre} zoom={14} className="h-full min-h-64 w-full">
                    <TileLayer
                        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                    />

                    {path.length > 1 && (
                        <Polyline positions={path} pathOptions={{ color: "#2563eb", weight: 4 }} />
                    )}

                    {trip.stops.map((s) => (
                        <Circle
                            key={`c-${s.stopId}`}
                            center={[s.latitude, s.longitude]}
                            radius={100}
                            pathOptions={{ color: "#9ca3af", weight: 1, fillOpacity: 0.06 }}
                        />
                    ))}

                    {trip.stops.map((s) => (
                        <Marker
                            key={`m-${s.stopId}`}
                            position={[s.latitude, s.longitude]}
                            icon={stopIcon(s.sequence, !!s.reachedAt, myStopIds.has(s.stopId))}
                        />
                    ))}

                    {position && (
                        <Marker
                            position={position}
                            icon={busIcon}
                            opacity={stale ? 0.45 : 1}
                            zIndexOffset={1000}
                        />
                    )}

                    <Recenter position={position} />
                </MapContainer>
            </div>

            <div className="border-t border-slate-200 p-3">
                {myStops.map((stop) => (
                    <div key={stop.stopId} className="mb-3 rounded-lg bg-blue-50 p-3">
                        <div className="text-xs text-blue-700">Your stop</div>
                        <div className="font-medium">{stop.name}</div>
                        <div className="text-sm text-slate-600">
                            Scheduled {stop.pickupTime}
                            {stop.reachedAt &&
                                ` · bus arrived ${new Date(stop.reachedAt).toLocaleTimeString([], {
                                    hour: "2-digit",
                                    minute: "2-digit",
                                })}`}
                        </div>
                    </div>
                ))}

                {trip.roster.map((r) => (
                    <div key={r.studentId} className="flex items-center justify-between py-1 text-sm">
                        <span>{r.name}</span>
                        <span
                            className={
                                r.boardingStatus === "Boarded"
                                    ? "text-emerald-600"
                                    : r.boardingStatus === "Absent"
                                        ? "text-slate-500"
                                        : "text-amber-600"
                            }
                        >
                            {r.boardingStatus}
                        </span>
                    </div>
                ))}

                {alerts.length > 0 && (
                    <div className="mt-3 space-y-1 border-t border-slate-100 pt-3">
                        {alerts.map((a, i) => (
                            <div key={i} className="text-sm text-slate-700">
                                {a}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    )
}