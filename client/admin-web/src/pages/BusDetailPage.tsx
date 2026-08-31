import { useState } from "react"
import { Link, useNavigate, useParams } from "react-router-dom"
import { ArrowLeft, Pencil } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import { useGetDriversQuery } from "@/features/drivers/driverApi"
import EditBusForm from "@/features/buses/EditBusForm"
import {
    useAssignDriverMutation,
    useDeactivateBusMutation,
    useGetBusQuery,
} from "@/features/buses/busApi"
import ConfirmDialog from "@/components/ui/confirm-dialog"

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function BusDetailPage() {
    const { id = "" } = useParams()
    const navigate = useNavigate()

    const { data: bus, isLoading, isError, refetch } = useGetBusQuery(id, { skip: !id })
    const { data: drivers } = useGetDriversQuery({ page: 1, pageSize: 50 })

    const [assignDriver, { isLoading: assigning }] = useAssignDriverMutation()
    const [deactivateBus, { isLoading: deactivating }] = useDeactivateBusMutation()

    const [selectedDriverId, setSelectedDriverId] = useState("")
    const [message, setMessage] = useState<string | null>(null)
    const [editing, setEditing] = useState(false)

    const activeDrivers = (drivers?.items ?? []).filter((d) => d.status === "Active")
    const assignedDriver = activeDrivers.find((d) => d.id === bus?.assignedDriverId)

    const today = new Date().toISOString().slice(0, 10)

    const [confirmOpen, setConfirmOpen] = useState(false)

    const onAssign = async () => {
        if (!selectedDriverId) return
        try {
            await assignDriver({ id, driverId: selectedDriverId }).unwrap()
            setSelectedDriverId("")
            setMessage("Driver assigned.")
        } catch (e) {
            setMessage(apiErrorMessage(e) ?? "Could not assign the driver.")
        }
    }

    const onDeactivate = async () => {
        try {
            await deactivateBus(id).unwrap()
            navigate(ROUTES.schoolBuses)
        } catch (e) {
            setConfirmOpen(false)
            setMessage(apiErrorMessage(e) ?? "Could not deactivate the bus.")
        }
    }

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Buses")}>
            <Link
                to={ROUTES.schoolBuses}
                className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700"
            >
                <ArrowLeft className="h-4 w-4" /> Back to buses
            </Link>

            {isError && (
                <div className="mt-6 rounded-lg border bg-white p-6 text-center">
                    <p className="text-slate-600">Could not load this bus.</p>
                    <Button className="mt-3" variant="outline" onClick={() => refetch()}>
                        Try again
                    </Button>
                </div>
            )}

            {isLoading && <p className="mt-6 text-slate-500">Loading…</p>}

            {bus && (
                <>
                    <div className="mt-4 flex items-start justify-between">
                        <div>
                            <h1 className="text-2xl font-semibold text-slate-800">
                                {bus.registrationNumber}
                            </h1>
                            <p className="mt-1 text-sm text-slate-500">
                                {bus.model} · {bus.capacity} seats
                            </p>
                        </div>
                        <div className="flex items-center gap-3">
                            {bus.status === "ACTIVE" && !editing && (
                                <Button variant="outline" onClick={() => setEditing(true)}>
                                    <Pencil className="mr-1 h-4 w-4" /> Edit
                                </Button>
                            )}
                            <span
                                className={`rounded-full px-3 py-1 text-xs font-medium
                                ${bus.status === "ACTIVE" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}
                            >
                                {bus.status === "ACTIVE" ? "Active" : "Inactive"}
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
                            <EditBusForm
                                bus={bus}
                                onSaved={() => {
                                    setEditing(false)
                                    setMessage("Bus details updated.")
                                }}
                                onCancel={() => setEditing(false)}
                            />
                        </div>
                    )}

                    <section className="mt-6 rounded-lg border bg-white p-6">
                        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                            Driver
                        </h2>

                        <p className="mb-4 text-sm">
                            {assignedDriver ? (
                                <>
                                    Currently assigned to{" "}
                                    <span className="font-medium text-slate-800">
                                        {assignedDriver.firstName} {assignedDriver.lastName}
                                    </span>
                                </>
                            ) : bus.assignedDriverId ? (
                                <span className="text-amber-700">
                                    Assigned to a driver who is no longer active — reassign below.
                                </span>
                            ) : (
                                <span className="text-slate-500">No driver assigned yet.</span>
                            )}
                        </p>

                        {bus.status === "ACTIVE" && (
                            <div className="flex flex-wrap items-center gap-3">
                                <select
                                    value={selectedDriverId}
                                    onChange={(e) => setSelectedDriverId(e.target.value)}
                                    className="min-w-80 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                                >
                                    <option value="">Choose a driver…</option>
                                    {activeDrivers.map((d) => {
                                        const expired = d.licenseExpiryDate < today
                                        return (
                                            <option key={d.id} value={d.id} disabled={expired}>
                                                {d.firstName} {d.lastName} · {d.licenseNumber}
                                                {expired ? " · licence expired" : ""}
                                            </option>
                                        )
                                    })}
                                </select>

                                <Button
                                    className="bg-sky-700 hover:bg-sky-800"
                                    disabled={!selectedDriverId || assigning}
                                    onClick={onAssign}
                                >
                                    {assigning ? "Assigning…" : "Assign driver"}
                                </Button>
                            </div>
                        )}

                        {activeDrivers.length === 0 && (
                            <p className="mt-3 text-sm text-slate-500">
                                No active drivers yet — add one on the Drivers page first.
                            </p>
                        )}
                    </section>

                    {bus.status === "ACTIVE" && (
                        <section className="mt-6 rounded-lg border border-red-200 bg-white p-6">
                            <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                Danger zone
                            </h2>
                            <p className="mb-4 text-sm text-slate-600">
                                Deactivating keeps the bus in the records for the audit trail. It stops
                                appearing in the fleet and cannot be assigned to a route.
                            </p>
                            <Button variant="destructive" onClick={() => setConfirmOpen(true)}>
                                Deactivate bus
                            </Button>

                            <ConfirmDialog
                                open={confirmOpen}
                                destructive
                                busy={deactivating}
                                title={`Deactivate ${bus.registrationNumber}?`}
                                description="The bus stays in the records for the audit trail, but stops appearing in the fleet and cannot be assigned to a route."
                                confirmLabel="Deactivate"
                                onConfirm={onDeactivate}
                                onCancel={() => setConfirmOpen(false)}
                            />
                        </section>
                    )}
                </>
            )}
        </DashboardLayout>
    )
}