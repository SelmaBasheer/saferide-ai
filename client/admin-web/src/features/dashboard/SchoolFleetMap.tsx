import { useEffect, useMemo } from "react"
import { MapContainer, TileLayer, Marker, Tooltip, useMap } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"
import type { LatLng } from "@/lib/geo"

export interface FleetMarker {
    tripId: string
    routeCode: string
    latitude: number
    longitude: number
    stale: boolean
}

// Kollam, so an empty map still lands somewhere sensible.
const FALLBACK: LatLng = [8.8901, 76.6012]

const busIcon = (routeCode: string, stale: boolean) =>
    L.divIcon({
        className: "",
        html: `<div style="display:flex;align-items:center;justify-content:center;gap:4px;
                 width:100%;height:100%;white-space:nowrap;
                 border-radius:11px;font:600 11px system-ui;
                 background:${stale ? "#94a3b8" : "#0369a1"};color:#fff;
                 opacity:${stale ? 0.7 : 1}">🚌 ${routeCode}</div>`,
        // A real box, so the anchor has something to measure from. Half the
        // width and half the height puts the pill's centre on the bus.
        iconSize: [70, 22],
        iconAnchor: [35, 11],
    })

// Fits the map to the buses once per *set* of buses, not per position update.
// Keying on the trip ids means the map settles and then stops jumping while
// the markers move inside it.
function FitToFleet({ points, fitKey }: { points: LatLng[]; fitKey: string }) {
    const map = useMap()

    useEffect(() => {
        if (points.length === 0) return
        if (points.length === 1) {
            map.setView(points[0], 14)
            return
        }
        map.fitBounds(L.latLngBounds(points), { padding: [40, 40] })
        // points is intentionally not a dependency — fitKey is the signal.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [fitKey, map])

    return null
}

export default function SchoolFleetMap({ markers }: { markers: FleetMarker[] }) {
    const points = useMemo<LatLng[]>(
        () => markers.map((m) => [m.latitude, m.longitude] as LatLng),
        [markers],
    )

    const fitKey = useMemo(
        () => markers.map((m) => m.tripId).sort().join(","),
        [markers],
    )

    if (markers.length === 0) {
        return (
            <div className="flex h-72 items-center justify-center rounded-lg border bg-slate-50">
                <p className="text-sm text-slate-500">No buses on the road right now.</p>
            </div>
        )
    }

    return (
        <div className="h-72 overflow-hidden rounded-lg border">
            <MapContainer center={points[0] ?? FALLBACK} zoom={13} className="h-full w-full">
                <TileLayer
                    url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                />

                {markers.map((m) => (
                    <Marker
                        key={m.tripId}
                        position={[m.latitude, m.longitude]}
                        icon={busIcon(m.routeCode, m.stale)}
                    >
                        <Tooltip direction="top" offset={[0, -12]}>
                            {m.routeCode}
                            {m.stale && " · signal lost"}
                        </Tooltip>
                    </Marker>
                ))}

                <FitToFleet points={points} fitKey={fitKey} />
            </MapContainer>
        </div>
    )
}