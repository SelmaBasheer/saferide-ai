import { useState } from "react"
import { Link, useNavigate, useParams } from "react-router-dom"
import { ArrowLeft, Pencil } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import { useGetBusesQuery } from "@/features/buses/busApi"
import {
    useAssignBusToRouteMutation,
    useDeactivateRouteMutation,
    useGetRouteQuery,
} from "@/features/routes/routeApi"
import EditRouteForm from "@/features/routes/EditRouteForm"
import RouteBuilder from "@/features/routes/RouteBuilder"

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function RouteDetailPage() {
    const { id = "" } = useParams()
    const navigate = useNavigate()

    const { data: route, isLoading, isError, refetch } = useGetRouteQuery(id, { skip: !id })
    const { data: buses } = useGetBusesQuery({ page: 1, pageSize: 100 })

    const [assignBus, { isLoading: assigning }] = useAssignBusToRouteMutation()
    const [deactivateRoute, { isLoading: deactivating }] = useDeactivateRouteMutation()

    const [selectedBusId, setSelectedBusId] = useState("")
    const [message, setMessage] = useState<string | null>(null)
    const [editing, setEditing] = useState(false)

    const activeBuses = (buses?.items ?? []).filter((b) => b.status === "ACTIVE")
    const assignedBus = activeBuses.find((b) => b.id === route?.assignedBusId)

    const onAssign = async () => {
        if (!selectedBusId) return
        try {
            await assignBus({ id, busId: selectedBusId }).unwrap()
            setSelectedBusId("")
            setMessage("Bus assigned.")
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not assign the bus.")
        }
    }

    const onDeactivate = async () => {
        if (!route) return
        if (!confirm(`Deactivate ${route.code}? It stays in the records.`)) return
        try {
            await deactivateRoute(id).unwrap()
            navigate(ROUTES.schoolRoutes)
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not deactivate the route.")
        }
    }

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Routes")}>
            <Link
                to={ROUTES.schoolRoutes}
                className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
            >
                <ArrowLeft className="h-4 w-4" /> Back to routes
            </Link>

            {isError && (
                <div className="mt-6 rounded-lg border bg-white p-6 text-center">
                    <p className="text-slate-600">Could not load this route.</p>
                    <Button className="mt-3" variant="outline" onClick={() => refetch()}>
                        Try again
                    </Button>
                </div>
            )}

            {isLoading && <p className="mt-6 text-slate-500">Loading…</p>}

            {route && (
                <>
                    <div className="mt-4 flex items-start justify-between">
                        <div>
                            <h1 className="text-2xl font-semibold text-slate-800">{route.code}</h1>
                            <p className="mt-1 text-sm text-slate-500">{route.name}</p>
                        </div>

                        <div className="flex items-center gap-3">
                            {route.status === "ACTIVE" && (
                                <button
                                    onClick={() => setEditing((v) => !v)}
                                    className="inline-flex items-center gap-1 text-sm text-sky-700 hover:underline"
                                >
                                    <Pencil className="h-4 w-4" /> {editing ? "Cancel" : "Edit"}
                                </button>
                            )}
                            <span
                                className={`rounded-full px-3 py-1 text-xs font-medium
                                ${route.status === "ACTIVE" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}
                            >
                                {route.status === "ACTIVE" ? "Active" : "Inactive"}
                            </span>
                        </div>
                    </div>

                    {message && (
                        <div
                            className="mt-4 cursor-pointer rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800"
                            onClick={() => setMessage(null)}
                        >
                            {message}
                        </div>
                    )}

                    {editing && (
                        <div className="mt-6">
                            <EditRouteForm route={route} onSaved={() => setEditing(false)} />
                        </div>
                    )}

                    <section className="mt-6 rounded-lg border bg-white p-6">
                        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                            Bus
                        </h2>

                        <p className="mb-4 text-sm">
                            {assignedBus ? (
                                <>
                                    Served by{" "}
                                    <span className="font-medium text-slate-800">
                                        {assignedBus.registrationNumber}
                                    </span>{" "}
                                    <span className="text-slate-500">({assignedBus.model})</span>
                                </>
                            ) : route.assignedBusId ? (
                                <span className="text-amber-700">
                                    Assigned to a bus that is no longer active — reassign below.
                                </span>
                            ) : (
                                <span className="text-slate-500">
                                    No bus assigned. A driver cannot start a trip on this route until one is.
                                </span>
                            )}
                        </p>

                        {route.status === "ACTIVE" && (
                            <div className="flex flex-wrap items-center gap-3">
                                <select
                                    value={selectedBusId}
                                    onChange={(e) => setSelectedBusId(e.target.value)}
                                    className="min-w-72 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                                >
                                    <option value="">Choose a bus…</option>
                                    {activeBuses.map((b) => (
                                        <option key={b.id} value={b.id}>
                                            {b.registrationNumber} · {b.model} · {b.capacity} seats
                                        </option>
                                    ))}
                                </select>

                                <Button
                                    className="bg-sky-700 hover:bg-sky-800"
                                    disabled={!selectedBusId || assigning}
                                    onClick={onAssign}
                                >
                                    {assigning ? "Assigning…" : "Assign bus"}
                                </Button>
                            </div>
                        )}

                        {activeBuses.length === 0 && (
                            <p className="mt-3 text-sm text-slate-500">
                                No active buses yet — add one on the Buses page first.
                            </p>
                        )}
                    </section>

                    {route.status === "ACTIVE" ? (
                        <RouteBuilder key={route.stops.map((s) => s.stopId).join("|")} route={route} />
                    ) : (
                        <section className="mt-6 rounded-lg border bg-white p-6">
                            <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                Stops and path
                            </h2>
                            <p className="text-sm text-slate-500">
                                This route is inactive. Stops and the path cannot be edited.
                            </p>
                        </section>
                    )}

                    {route.status === "ACTIVE" && (
                        <section className="mt-6 rounded-lg border border-red-200 bg-white p-6">
                            <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                Danger zone
                            </h2>
                            <p className="mb-4 text-sm text-slate-600">
                                Deactivating keeps the route in the records. Drivers will no longer be able
                                to start a trip on it.
                            </p>
                            <Button variant="destructive" disabled={deactivating} onClick={onDeactivate}>
                                {deactivating ? "Deactivating…" : "Deactivate route"}
                            </Button>
                        </section>
                    )}
                </>
            )}
        </DashboardLayout>
    )
}