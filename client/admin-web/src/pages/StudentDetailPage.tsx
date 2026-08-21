import { useEffect, useMemo, useState } from "react"
import { Link, useParams } from "react-router-dom"
import { ArrowLeft } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import { useAssignStudentRouteMutation, useGetStudentQuery } from "@/features/students/studentApi"
import { useGetRouteQuery, useGetRoutesQuery } from "@/features/routes/routeApi"

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function StudentDetailPage() {
    const { id = "" } = useParams()

    const { data: student, isLoading, isError, refetch } = useGetStudentQuery(id, { skip: !id })
    const { data: routes } = useGetRoutesQuery({ page: 1, pageSize: 50, includeInactive: false })
    const [routeId, setRouteId] = useState("")
    const { data: selectedRoute } = useGetRouteQuery(routeId, { skip: !routeId })
    const [assignRoute, { isLoading: saving }] = useAssignStudentRouteMutation()

    const [pickupStopId, setPickupStopId] = useState("")
    const [dropStopId, setDropStopId] = useState("")
    const [message, setMessage] = useState<string | null>(null)

    useEffect(() => {
        if (!student) return
        setRouteId(student.routeId ?? "")
        setPickupStopId(student.pickupStopId ?? "")
        setDropStopId(student.dropStopId ?? "")
    }, [student])

    const stops = useMemo(
        () => [...(selectedRoute?.stops ?? [])].sort((a, b) => a.sequence - b.sequence),
        [selectedRoute],
    )

    const activeRoutes = routes?.items ?? []
    const known = (stopId: string) => stops.some((s) => s.stopId === stopId)
    const pickupValue = known(pickupStopId) ? pickupStopId : ""
    const dropValue = known(dropStopId) ? dropStopId : ""

    const assignedRouteIsActive = activeRoutes.some((r) => r.id === student?.routeId)
    const viewingAssignedRoute = Boolean(student?.routeId) && student?.routeId === routeId
    const assignedStopsExist = Boolean(
        student?.pickupStopId &&
        student?.dropStopId &&
        known(student.pickupStopId) &&
        known(student.dropStopId),
    )
    const orphaned = Boolean(selectedRoute && viewingAssignedRoute && !assignedStopsExist)

    const dirty =
        routeId !== (student?.routeId ?? "") ||
        pickupValue !== (student?.pickupStopId ?? "") ||
        dropValue !== (student?.dropStopId ?? "")

    const onRouteChange = (value: string) => {
        setRouteId(value)
        setPickupStopId("")
        setDropStopId("")
        setMessage(null)
    }

    const onPickupChange = (value: string) => {
        setPickupStopId(value)
        if (!dropValue) setDropStopId(value)
        setMessage(null)
    }

    const onSave = async () => {
        if (!routeId || !pickupValue || !dropValue) return
        try {
            await assignRoute({ id, routeId, pickupStopId: pickupValue, dropStopId: dropValue }).unwrap()
            setMessage("Assignment saved.")
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not save the assignment.")
        }
    }

    const stopLabel = (stopId: string | null) => {
        const stop = stopId ? stops.find((s) => s.stopId === stopId) : undefined
        return stop ? `${stop.sequence} · ${stop.name} · ${stop.pickupTime}` : "—"
    }

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Students")}>
            <Link
                to={ROUTES.schoolStudents}
                className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
            >
                <ArrowLeft className="h-4 w-4" /> Back to students
            </Link>

            {isError && (
                <div className="mt-6 rounded-lg border bg-white p-6 text-center">
                    <p className="text-slate-600">Could not load this student.</p>
                    <Button className="mt-3" variant="outline" onClick={() => refetch()}>
                        Try again
                    </Button>
                </div>
            )}

            {isLoading && <p className="mt-6 text-slate-500">Loading…</p>}

            {student && (
                <>
                    <div className="mt-4 flex items-start justify-between">
                        <div>
                            <h1 className="text-2xl font-semibold text-slate-800">
                                {student.firstName} {student.lastName}
                            </h1>
                            <p className="mt-1 text-sm text-slate-500">
                                Admission {student.admissionNumber} · Grade {student.grade}
                            </p>
                        </div>
                        <span
                            className={`rounded-full px-3 py-1 text-xs font-medium
                            ${student.status === "ACTIVE" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}
                        >
                            {student.status === "ACTIVE" ? "Active" : "Inactive"}
                        </span>
                    </div>

                    {message && (
                        <div
                            className="mt-4 cursor-pointer rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800"
                            onClick={() => setMessage(null)}
                        >
                            {message}
                        </div>
                    )}

                    <section className="mt-6 rounded-lg border bg-white p-6">
                        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                            Parent
                        </h2>
                        <dl className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-3">
                            <div>
                                <dt className="text-xs text-slate-400">Name</dt>
                                <dd className="mt-0.5 text-slate-800">
                                    {student.parentFirstName} {student.parentLastName}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-xs text-slate-400">Email</dt>
                                <dd className="mt-0.5 break-all text-slate-800">{student.parentEmail}</dd>
                            </div>
                            <div>
                                <dt className="text-xs text-slate-400">Phone</dt>
                                <dd className="mt-0.5 text-slate-800">{student.parentPhone}</dd>
                            </div>
                        </dl>
                    </section>

                    <section className="mt-6 rounded-lg border bg-white p-6">
                        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                            Route and stops
                        </h2>

                        <p className="mb-4 text-sm">
                            {viewingAssignedRoute && assignedStopsExist && assignedRouteIsActive ? (
                                <>
                                    Picks up at{" "}
                                    <span className="font-medium text-slate-800">
                                        {stopLabel(student.pickupStopId)}
                                    </span>
                                    , drops at{" "}
                                    <span className="font-medium text-slate-800">
                                        {stopLabel(student.dropStopId)}
                                    </span>
                                </>
                            ) : student.routeId ? (
                                <span className="text-amber-700">
                                    This assignment is incomplete — the route or one of its stops is no longer
                                    available. Reassign below.
                                </span>
                            ) : (
                                <span className="text-slate-500">
                                    No route assigned yet. Until this is set, the parent sees nothing and the
                                    driver has no one to mark at a stop.
                                </span>
                            )}
                        </p>

                        {orphaned && (
                            <p className="mb-4 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
                                The stop this student was assigned to no longer exists on the route — it was most
                                likely removed in the route builder. Pick a new stop below.
                            </p>
                        )}

                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            <div>
                                <label className="text-xs text-slate-400">Route</label>
                                <select
                                    value={routeId}
                                    onChange={(e) => onRouteChange(e.target.value)}
                                    className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                                >
                                    <option value="">Not assigned</option>
                                    {student.routeId && !assignedRouteIsActive && (
                                        <option value={student.routeId}>
                                            {selectedRoute
                                                ? `${selectedRoute.code} — ${selectedRoute.name}`
                                                : "Current route"}{" "}
                                            · no longer active
                                        </option>
                                    )}
                                    {activeRoutes.map((r) => (
                                        <option key={r.id} value={r.id}>
                                            {r.code} — {r.name}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label className="text-xs text-slate-400">Pickup stop</label>
                                <select
                                    value={pickupValue}
                                    onChange={(e) => onPickupChange(e.target.value)}
                                    disabled={!routeId || stops.length === 0}
                                    className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm disabled:bg-slate-50 disabled:text-slate-400"
                                >
                                    <option value="">Choose a stop…</option>
                                    {stops.map((s) => (
                                        <option key={s.stopId} value={s.stopId}>
                                            {s.sequence} · {s.name} · {s.pickupTime}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label className="text-xs text-slate-400">Drop stop</label>
                                <select
                                    value={dropValue}
                                    onChange={(e) => {
                                        setDropStopId(e.target.value)
                                        setMessage(null)
                                    }}
                                    disabled={!routeId || stops.length === 0}
                                    className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm disabled:bg-slate-50 disabled:text-slate-400"
                                >
                                    <option value="">Choose a stop…</option>
                                    {stops.map((s) => (
                                        <option key={s.stopId} value={s.stopId}>
                                            {s.sequence} · {s.name} · {s.pickupTime}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        {routeId && stops.length === 0 && (
                            <p className="mt-3 text-sm text-slate-500">
                                This route has no stops yet —{" "}
                                <Link
                                    to={ROUTES.schoolRouteDetail.replace(":id", routeId)}
                                    className="font-medium text-sky-700 hover:underline"
                                >
                                    add them in the route builder
                                </Link>{" "}
                                first.
                            </p>
                        )}

                        {activeRoutes.length === 0 && (
                            <p className="mt-3 text-sm text-slate-500">
                                No active routes yet — create one on the Routes page first.
                            </p>
                        )}

                        <div className="mt-5">
                            <Button
                                className="bg-sky-700 hover:bg-sky-800"
                                disabled={!routeId || !pickupValue || !dropValue || !dirty || saving}
                                onClick={onSave}
                            >
                                {saving ? "Saving…" : "Save assignment"}
                            </Button>
                        </div>
                    </section>
                </>
            )}
        </DashboardLayout>
    )
}