import { useMemo, useState } from "react"
import { MapContainer, TileLayer, Marker, Polyline, Circle, useMapEvents } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"
import { ArrowDown, ArrowUp, GripVertical, Trash2, Wand2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
    useGeneratePathMutation,
    useReplacePathMutation,
    useReplaceStopsMutation,
    type RouteGeoPoint,
    type RouteListItem,
    type StopInput,
} from "@/features/routes/routeApi"

type LocalStop = StopInput & { key: string }

const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/

// Tracking marks a stop reached within 100 m, so two stops closer than that
// cannot be told apart — the bus would arrive at both at once.
const MIN_STOP_SEPARATION_METRES = 100

function metresBetween(a: StopInput, b: StopInput) {
    const R = 6_371_000
    const dLat = ((b.latitude - a.latitude) * Math.PI) / 180
    const dLng = ((b.longitude - a.longitude) * Math.PI) / 180
    const lat1 = (a.latitude * Math.PI) / 180
    const lat2 = (b.latitude * Math.PI) / 180
    const h = Math.sin(dLat / 2) ** 2 + Math.sin(dLng / 2) ** 2 * Math.cos(lat1) * Math.cos(lat2)
    return 2 * R * Math.asin(Math.sqrt(h))
}

const stopKey = (s: { stopId: string | null; name: string; latitude: number; longitude: number; pickupTime: string }) =>
    `${s.stopId}|${s.name}|${s.latitude}|${s.longitude}|${s.pickupTime}`

const stopIcon = (n: number) =>
    L.divIcon({
        className: "",
        html: `<div style="width:24px;height:24px;border-radius:50%;background:#fff;border:2px solid #444;
        display:flex;align-items:center;justify-content:center;font:600 12px system-ui;color:#222">${n}</div>`,
        iconSize: [24, 24],
        iconAnchor: [12, 12],
    })

function ClickHandler({ onClick }: { onClick: (lat: number, lng: number) => void }) {
    useMapEvents({ click: (e) => onClick(e.latlng.lat, e.latlng.lng) })
    return null
}

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function RouteBuilder({ route }: { route: RouteListItem }) {
    const [mode, setMode] = useState<"stops" | "path">("stops")
    const [message, setMessage] = useState<string | null>(null)

    const [stops, setStops] = useState<LocalStop[]>(
        route.stops.map((s) => ({
            key: s.stopId,
            stopId: s.stopId,
            name: s.name,
            latitude: s.latitude,
            longitude: s.longitude,
            pickupTime: s.pickupTime,
        }))
    )

    const [points, setPoints] = useState<RouteGeoPoint[]>(route.path ?? [])

    const [dragIndex, setDragIndex] = useState<number | null>(null)
    const [overIndex, setOverIndex] = useState<number | null>(null)
    const [handleHeld, setHandleHeld] = useState(false)

    const [replaceStops, { isLoading: savingStops }] = useReplaceStopsMutation()
    const [replacePath, { isLoading: savingPath }] = useReplacePathMutation()
    const [generatePath, { isLoading: generating }] = useGeneratePathMutation()

    const onMapClick = (latitude: number, longitude: number) => {
        if (mode === "stops") {
            setStops((s) => [
                ...s,
                { key: crypto.randomUUID(), stopId: null, name: "", latitude, longitude, pickupTime: "" },
            ])
        } else {
            setPoints((p) => [...p, { latitude, longitude }])
        }
    }

    const editStop = (index: number, patch: Partial<LocalStop>) =>
        setStops((s) => s.map((stop, i) => (i === index ? { ...stop, ...patch } : stop)))

    const moveStop = (from: number, to: number) =>
        setStops((s) => {
            if (to < 0 || to >= s.length || from === to) return s
            const next = [...s]
            const [moved] = next.splice(from, 1)
            next.splice(to, 0, moved)
            return next
        })

    const removeStop = (index: number) => setStops((s) => s.filter((_, i) => i !== index))

    const resetDrag = () => {
        setDragIndex(null)
        setOverIndex(null)
        setHandleHeld(false)
    }

    const outOfOrder = (i: number) =>
        i > 0 &&
        TIME_PATTERN.test(stops[i].pickupTime) &&
        TIME_PATTERN.test(stops[i - 1].pickupTime) &&
        stops[i].pickupTime <= stops[i - 1].pickupTime

    const anyOutOfOrder = stops.some((_, i) => outOfOrder(i))

    const tooClose = useMemo(() => {
        const flagged = new Set<number>()
        for (let i = 0; i < stops.length; i++) {
            for (let j = i + 1; j < stops.length; j++) {
                if (metresBetween(stops[i], stops[j]) < MIN_STOP_SEPARATION_METRES) {
                    flagged.add(i)
                    flagged.add(j)
                }
            }
        }
        return flagged
    }, [stops])

    const stopsDirty = useMemo(
        () => route.stops.map(stopKey).join(";") !== stops.map(stopKey).join(";"),
        [route.stops, stops]
    )

    const pathBusy = generating || savingPath

    const onSaveStops = async () => {
        if (stops.some((s) => !s.name.trim())) {
            setMessage("Every stop needs a name.")
            return
        }
        if (stops.some((s) => !TIME_PATTERN.test(s.pickupTime))) {
            setMessage("Every stop needs a pickup time in HH:mm, for example 07:15.")
            return
        }
        for (let i = 1; i < stops.length; i++) {
            if (stops[i].pickupTime <= stops[i - 1].pickupTime) {
                setMessage(
                    `Stop ${i + 1} is picked up at ${stops[i].pickupTime}, which is not after stop ${i} at ${stops[i - 1].pickupTime}.`
                )
                return
            }
        }
        for (let i = 0; i < stops.length; i++) {
            for (let j = i + 1; j < stops.length; j++) {
                const metres = metresBetween(stops[i], stops[j])
                if (metres < MIN_STOP_SEPARATION_METRES) {
                    setMessage(
                        `Stops ${i + 1} and ${j + 1} are only ${Math.round(metres)} m apart. The bus counts as arrived within ${MIN_STOP_SEPARATION_METRES} m of a stop, so it could not tell them apart. Move one of them.`
                    )
                    return
                }
            }
        }
        try {
            await replaceStops({
                id: route.id,
                stops: stops.map((s) => ({
                    stopId: s.stopId,
                    name: s.name,
                    latitude: s.latitude,
                    longitude: s.longitude,
                    pickupTime: s.pickupTime,
                })),
            }).unwrap()
            setMessage("Stops saved.")
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not save the stops.")
        }
    }

    const onGenerate = async () => {
        setMessage(null)
        try {
            const updated = await generatePath(route.id).unwrap()
            setPoints(updated.path ?? [])
            setMessage(`Path generated — ${updated.path?.length ?? 0} points following the road.`)
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not generate the path.")
        }
    }

    const onSavePath = async () => {
        if (points.length < 2) {
            setMessage("A path needs at least two points.")
            return
        }
        try {
            await replacePath({ id: route.id, points }).unwrap()
            setMessage("Path saved.")
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not save the path.")
        }
    }

    const centre: [number, number] = stops.length
        ? [stops[0].latitude, stops[0].longitude]
        : points.length
            ? [points[0].latitude, points[0].longitude]
            : [8.8901, 76.6012]

    return (
        <section className="mt-6 rounded-lg border bg-white p-6">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                    Stops and path
                </h2>
                <div className="flex gap-2">
                    <button
                        onClick={() => setMode("stops")}
                        className={`rounded-md px-3 py-1.5 text-sm ${mode === "stops" ? "bg-sky-700 text-white" : "border border-slate-300"
                            }`}
                    >
                        Place stops
                    </button>
                    <button
                        onClick={() => setMode("path")}
                        className={`rounded-md px-3 py-1.5 text-sm ${mode === "path" ? "bg-sky-700 text-white" : "border border-slate-300"
                            }`}
                    >
                        Path
                    </button>
                </div>
            </div>

            <p className="mb-3 text-sm text-slate-500">
                {mode === "stops"
                    ? "Click the map to add a stop, then give it a name and pickup time. Drag by the handle to reorder — times must still increase down the list."
                    : "Generate the path from your stops so it follows real roads. Drawing by hand is a fallback for roads the map doesn't know."}
            </p>

            {message && (
                <div
                    className="mb-3 cursor-pointer rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800"
                    onClick={() => setMessage(null)}
                >
                    {message}
                </div>
            )}

            <div className="h-96 w-full overflow-hidden rounded-lg border">
                <MapContainer center={centre} zoom={14} className="h-full w-full">
                    <TileLayer
                        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                    />
                    <ClickHandler onClick={onMapClick} />

                    {points.length > 1 && (
                        <Polyline
                            positions={points.map((p) => [p.latitude, p.longitude])}
                            pathOptions={{ color: "#2563eb", weight: 4 }}
                        />
                    )}

                    {stops.map((s) => (
                        <Circle
                            key={`c-${s.key}`}
                            center={[s.latitude, s.longitude]}
                            radius={100}
                            pathOptions={{ color: "#9ca3af", weight: 1, fillOpacity: 0.08 }}
                        />
                    ))}

                    {stops.map((s, i) => (
                        <Marker
                            key={`m-${s.key}`}
                            position={[s.latitude, s.longitude]}
                            icon={stopIcon(i + 1)}
                        />
                    ))}
                </MapContainer>
            </div>

            {mode === "stops" ? (
                <>
                    {anyOutOfOrder && (
                        <p className="mt-4 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
                            Some pickup times are no longer in order after reordering. Fix the highlighted
                            rows before saving.
                        </p>
                    )}

                    {tooClose.size > 0 && (
                        <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
                            Two stops are within {MIN_STOP_SEPARATION_METRES} m of each other. The bus counts
                            as arrived within that distance, so it could not tell them apart.
                        </p>
                    )}

                    <div className="mt-4 space-y-2">
                        {stops.length === 0 && (
                            <p className="text-sm text-slate-500">No stops yet — click the map to add one.</p>
                        )}

                        {stops.map((s, i) => (
                            <div
                                key={s.key}
                                draggable={handleHeld}
                                onDragStart={(e) => {
                                    setDragIndex(i)
                                    e.dataTransfer.effectAllowed = "move"
                                    e.dataTransfer.setData("text/plain", String(i))
                                }}
                                onDragOver={(e) => {
                                    e.preventDefault()
                                    e.dataTransfer.dropEffect = "move"
                                    if (overIndex !== i) setOverIndex(i)
                                }}
                                onDrop={(e) => {
                                    e.preventDefault()
                                    if (dragIndex !== null) moveStop(dragIndex, i)
                                    resetDrag()
                                }}
                                onDragEnd={resetDrag}
                                className={`flex flex-wrap items-center gap-2 rounded-md border p-2 transition
                                    ${dragIndex === i ? "opacity-40" : ""}
                                    ${overIndex === i && dragIndex !== i ? "border-sky-400 ring-2 ring-sky-200" : ""}
                                    ${outOfOrder(i) ? "border-amber-300 bg-amber-50" : ""}
                                    ${tooClose.has(i) ? "border-red-300 bg-red-50" : ""}`}
                            >
                                <span
                                    onPointerDown={() => setHandleHeld(true)}
                                    onPointerUp={() => setHandleHeld(false)}
                                    title="Drag to reorder"
                                    className="cursor-grab touch-none p-1 text-slate-400 active:cursor-grabbing"
                                >
                                    <GripVertical className="h-4 w-4" />
                                </span>

                                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-slate-200 text-xs font-semibold">
                                    {i + 1}
                                </span>

                                <input
                                    value={s.name}
                                    onChange={(e) => editStop(i, { name: e.target.value })}
                                    placeholder="Stop name"
                                    className="min-w-48 flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm"
                                />

                                <input
                                    type="time"
                                    value={s.pickupTime}
                                    onChange={(e) => editStop(i, { pickupTime: e.target.value })}
                                    className={`w-32 rounded-md border px-2 py-1.5 text-sm ${outOfOrder(i) ? "border-amber-400" : "border-slate-300"
                                        }`}
                                />

                                <span className="text-xs text-slate-400">
                                    {s.latitude.toFixed(4)}, {s.longitude.toFixed(4)}
                                </span>

                                {s.stopId === null && (
                                    <span className="rounded bg-emerald-50 px-2 py-0.5 text-xs text-emerald-700">
                                        new
                                    </span>
                                )}

                                <div className="ml-auto flex gap-1">
                                    <button
                                        onClick={() => moveStop(i, i - 1)}
                                        disabled={i === 0}
                                        title="Move up"
                                        className="p-1 text-slate-500 disabled:opacity-30"
                                    >
                                        <ArrowUp className="h-4 w-4" />
                                    </button>
                                    <button
                                        onClick={() => moveStop(i, i + 1)}
                                        disabled={i === stops.length - 1}
                                        title="Move down"
                                        className="p-1 text-slate-500 disabled:opacity-30"
                                    >
                                        <ArrowDown className="h-4 w-4" />
                                    </button>
                                    <button
                                        onClick={() => removeStop(i)}
                                        title="Remove stop"
                                        className="p-1 text-red-500"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>

                    <Button
                        className="mt-4 bg-sky-700 hover:bg-sky-800"
                        disabled={savingStops}
                        onClick={onSaveStops}
                    >
                        {savingStops ? "Saving…" : "Save stops"}
                    </Button>
                </>
            ) : (
                <>
                    <p className="mt-4 text-sm text-slate-600">
                        {points.length} point{points.length === 1 ? "" : "s"} on the path.
                    </p>

                    <div className="mt-3 flex flex-wrap gap-2">
                        <Button
                            className="bg-sky-700 hover:bg-sky-800"
                            disabled={pathBusy || stopsDirty || stops.length < 2}
                            onClick={onGenerate}
                        >
                            <Wand2 className="mr-1 h-4 w-4" />
                            {generating ? "Generating…" : "Generate path from stops"}
                        </Button>
                        <Button variant="outline" disabled={pathBusy} onClick={onSavePath}>
                            {savingPath ? "Saving…" : "Save drawn path"}
                        </Button>
                        <Button variant="outline" onClick={() => setPoints((p) => p.slice(0, -1))}>
                            Undo last point
                        </Button>
                        <Button variant="outline" onClick={() => setPoints([])}>
                            Clear
                        </Button>
                    </div>

                    {stopsDirty && (
                        <p className="mt-3 text-sm text-amber-700">
                            Save your stop changes first — generation uses the stops stored on the server, not
                            the ones on screen.
                        </p>
                    )}

                    {!stopsDirty && stops.length < 2 && (
                        <p className="mt-3 text-sm text-slate-500">
                            Add at least two stops before generating a path.
                        </p>
                    )}
                </>
            )}
        </section>
    )
}