import { useEffect, useMemo, useRef, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { MapContainer, TileLayer, Marker, Polyline, Circle, useMap } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"

import { ROUTES } from "@/routes/paths"
import { densify, type LatLng } from "@/lib/geo"
import { useWakeLock } from "@/hooks/useWakeLock"
import { useTrackingHub } from "@/features/tracking/useTrackingHub"
import {
    useEndTripMutation,
    useGetTripQuery,
    useMarkBoardingMutation,
} from "@/features/tracking/trackingApi"

const busIcon = L.divIcon({
    className: "",
    html: '<div style="font-size:26px;line-height:26px">🚌</div>',
    iconSize: [26, 26],
    iconAnchor: [13, 13],
})

const stopIcon = (n: number, reached: boolean) =>
    L.divIcon({
        className: "",
        html: `<div style="width:22px;height:22px;border-radius:50%;background:${reached ? "#22c55e" : "#fff"
            };border:2px solid #444;display:flex;align-items:center;justify-content:center;
        font:600 11px system-ui;color:${reached ? "#fff" : "#222"}">${n}</div>`,
        iconSize: [22, 22],
        iconAnchor: [11, 11],
    })

function Recenter({ position }: { position: LatLng | null }) {
    const map = useMap()
    useEffect(() => {
        if (position) map.panTo(position)
    }, [position, map])
    return null
}

export default function DriverTripPage() {
    const { id = "" } = useParams()
    const navigate = useNavigate()

    const { data: trip, isLoading } = useGetTripQuery(id, { skip: !id })
    const [markBoarding] = useMarkBoardingMutation()
    const [endTrip, { isLoading: ending }] = useEndTripMutation()

    const [position, setPosition] = useState<LatLng | null>(null)
    const [mode, setMode] = useState<"idle" | "gps" | "simulate">("idle")
    const [banner, setBanner] = useState<string | null>(null)
    const [arrivedStopId, setArrivedStopId] = useState<string | null>(null)

    const driveIndex = useRef(0)
    const isActive = trip?.status === "Active"

    useWakeLock(!!isActive && mode !== "idle")

    const { status, joinTrip, sendPosition } = useTrackingHub({
        onPosition: (u) => setPosition([u.latitude, u.longitude]),
        onStopReached: (n) => {
            setBanner(`Arrived at ${n.stopName} — mark students, then continue`)
            setArrivedStopId(n.stopId)
            setMode((m) => (m === "simulate" ? "idle" : m))
        },
        onTripEnded: () => setBanner("Trip ended"),
    })

    useEffect(() => {
        if (status === "connected" && id) void joinTrip(id)
    }, [status, id, joinTrip])

    useEffect(() => {
        if (trip?.lastPosition) {
            setPosition([trip.lastPosition.latitude, trip.lastPosition.longitude])
        }
    }, [trip?.lastPosition])

    const path = useMemo<LatLng[]>(
        () => (trip?.path ?? []).map((p) => [p.latitude, p.longitude] as LatLng),
        [trip?.path]
    )

    const drivePoints = useMemo(() => densify(path, 40), [path])

    // real GPS
    useEffect(() => {
        if (mode !== "gps" || !isActive) return

        if (!("geolocation" in navigator)) {
            setBanner("This device has no GPS")
            setMode("idle")
            return
        }

        const watchId = navigator.geolocation.watchPosition(
            (p) =>
                void sendPosition(
                    id,
                    p.coords.latitude,
                    p.coords.longitude,
                    p.coords.speed !== null ? p.coords.speed * 3.6 : null,
                    "Gps"
                ),
            () => setBanner("Location permission denied"),
            { enableHighAccuracy: true, maximumAge: 2000, timeout: 10000 }
        )

        return () => navigator.geolocation.clearWatch(watchId)
    }, [mode, isActive, id, sendPosition])

    // simulated drive — pauses itself when a stop is reached
    useEffect(() => {
        if (mode !== "simulate" || !isActive || drivePoints.length === 0) return

        const timer = setInterval(() => {
            if (driveIndex.current >= drivePoints.length) {
                setBanner("Reached the end of the route")
                setMode("idle")
                return
            }
            const p = drivePoints[driveIndex.current++]
            void sendPosition(id, p[0], p[1], 30, "Simulated")
        }, 1500)

        return () => clearInterval(timer)
    }, [mode, isActive, drivePoints, id, sendPosition])

    const onMark = async (studentId: string, boardingStatus: "Boarded" | "Absent") => {
        try {
            await markBoarding({ tripId: id, studentId, status: boardingStatus }).unwrap()
        } catch (e) {
            const message =
                (e as { data?: { error?: { message?: string } } })?.data?.error?.message ??
                "Could not record that."
            setBanner(message)
        }
    }

    const onEnd = async () => {
        if (!confirm(`End this trip? ${trip?.unmarkedCount ?? 0} students are still unmarked.`)) return
        try {
            await endTrip(id).unwrap()
            navigate(ROUTES.driver)
        } catch (e) {
            const message =
                (e as { data?: { error?: { message?: string } } })?.data?.error?.message ??
                "Could not end the trip."
            setBanner(message)
        }
    }

    if (isLoading || !trip) return <div className="p-6 text-slate-500">Loading trip…</div>

    const centre: LatLng = position ?? path[0] ?? [8.8901, 76.6012]

    return (
        <div className="flex min-h-dvh flex-col">
            <header className="flex items-center justify-between border-b border-slate-200 p-3">
                <div>
                    <div className="font-semibold">{trip.routeCode}</div>
                    <div className="text-xs text-slate-500">{trip.routeName}</div>
                </div>
                <div className="text-right text-xs">
                    <div className={status === "connected" ? "text-emerald-600" : "text-amber-600"}>
                        ● {status}
                    </div>
                    <div className="text-slate-500">{trip.unmarkedCount} unmarked</div>
                </div>
            </header>

            {banner && (
                <div
                    className="cursor-pointer bg-amber-50 px-3 py-2 text-sm text-amber-800"
                    onClick={() => setBanner(null)}
                >
                    {banner}
                </div>
            )}

            <div className="h-56 w-full">
                <MapContainer center={centre} zoom={14} className="h-full w-full">
                    <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

                    {path.length > 1 && (
                        <Polyline positions={path} pathOptions={{ color: "#2563eb", weight: 4 }} />
                    )}

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

                    {position && <Marker position={position} icon={busIcon} zIndexOffset={1000} />}

                    <Recenter position={position} />
                </MapContainer>
            </div>

            {isActive && (
                <div className="flex gap-2 border-b border-slate-200 p-3">
                    {arrivedStopId && mode === "idle" ? (
                        <button
                            onClick={() => {
                                setArrivedStopId(null)
                                setBanner(null)
                                setMode("simulate")
                            }}
                            className="flex-1 rounded-lg bg-indigo-600 px-3 py-3 text-sm font-medium text-white"
                        >
                            Continue to next stop
                        </button>
                    ) : (
                        <>
                            <button
                                onClick={() => setMode(mode === "gps" ? "idle" : "gps")}
                                className={`flex-1 rounded-lg px-3 py-3 text-sm font-medium ${mode === "gps" ? "bg-emerald-600 text-white" : "border border-slate-300"
                                    }`}
                            >
                                {mode === "gps" ? "Stop GPS" : "Use real GPS"}
                            </button>
                            <button
                                onClick={() => setMode(mode === "simulate" ? "idle" : "simulate")}
                                className={`flex-1 rounded-lg px-3 py-3 text-sm font-medium ${mode === "simulate" ? "bg-indigo-600 text-white" : "border border-slate-300"
                                    }`}
                            >
                                {mode === "simulate" ? "Pause" : "Simulate drive"}
                            </button>
                        </>
                    )}
                </div>
            )}

            <div className="flex-1 overflow-y-auto p-3">
                {trip.stops.map((stop) => {
                    const students = trip.roster.filter((r) => r.pickupStopId === stop.stopId)
                    const isHere = arrivedStopId === stop.stopId

                    return (
                        <section
                            key={stop.stopId}
                            className={`mb-3 rounded-lg p-2 ${isHere ? "bg-amber-50 ring-1 ring-amber-300" : ""
                                }`}
                        >
                            <div className="mb-2 flex items-center gap-2">
                                <span
                                    className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold ${stop.reachedAt ? "bg-emerald-500 text-white" : "bg-slate-200"
                                        }`}
                                >
                                    {stop.sequence}
                                </span>
                                <span className="font-medium">{stop.name}</span>
                                {isHere && (
                                    <span className="rounded bg-amber-200 px-2 py-0.5 text-xs font-medium text-amber-900">
                                        bus is here
                                    </span>
                                )}
                                <span className="ml-auto text-xs text-slate-500">{stop.pickupTime}</span>
                            </div>

                            {students.length === 0 && (
                                <div className="pl-8 text-sm text-slate-400">No students here</div>
                            )}

                            {students.map((s) => (
                                <div key={s.studentId} className="flex items-center gap-2 py-1 pl-8">
                                    <span className="flex-1 text-sm">{s.name}</span>

                                    {s.boardingStatus !== "Unmarked" ? (
                                        <span
                                            className={`text-xs font-medium ${s.boardingStatus === "Boarded"
                                                    ? "text-emerald-600"
                                                    : "text-slate-500"
                                                }`}
                                        >
                                            {s.boardingStatus}
                                        </span>
                                    ) : !stop.reachedAt ? (
                                        <span className="text-xs text-slate-400">waiting</span>
                                    ) : isActive ? (
                                        <>
                                            <button
                                                onClick={() => onMark(s.studentId, "Boarded")}
                                                className="rounded-lg bg-emerald-600 px-3 py-2 text-xs font-medium text-white"
                                            >
                                                Boarded
                                            </button>
                                            <button
                                                onClick={() => onMark(s.studentId, "Absent")}
                                                className="rounded-lg bg-slate-200 px-3 py-2 text-xs font-medium"
                                            >
                                                Absent
                                            </button>
                                        </>
                                    ) : (
                                        <span className="text-xs text-amber-600">Unmarked</span>
                                    )}
                                </div>
                            ))}
                        </section>
                    )
                })}
            </div>

            {isActive && (
                <div className="border-t border-slate-200 p-3">
                    <button
                        onClick={onEnd}
                        disabled={ending}
                        className="w-full rounded-lg bg-red-600 py-4 font-semibold text-white disabled:opacity-50"
                    >
                        End trip
                    </button>
                </div>
            )}
        </div>
    )
}