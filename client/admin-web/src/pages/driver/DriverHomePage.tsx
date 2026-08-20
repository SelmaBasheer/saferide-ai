import { useNavigate } from "react-router-dom"
import { ROUTES } from "@/routes/paths"
import { useGetRoutesQuery } from "@/features/routes/routeApi"
import { useGetMyTripsQuery, useStartTripMutation } from "@/features/tracking/trackingApi"

export default function DriverHomePage() {
    const navigate = useNavigate()

    const {
        data: active,
        isLoading: loadingActive,
        isError: activeFailed,
        refetch: refetchActive,
    } = useGetMyTripsQuery({ status: "Active", page: 1, pageSize: 1 })

    const {
        data: routes,
        isLoading: loadingRoutes,
        isError: routesFailed,
        refetch: refetchRoutes,
    } = useGetRoutesQuery({ page: 1, pageSize: 50 })

    const [startTrip, { isLoading: starting }] = useStartTripMutation()

    const activeTrip = active?.items?.[0]

    const onStart = async (routeId: string) => {
        try {
            const trip = await startTrip({ routeId }).unwrap()
            navigate(ROUTES.driverTrip.replace(":id", trip.id))
        } catch (e) {
            const message =
                (e as { data?: { error?: { message?: string } } })?.data?.error?.message ??
                "Could not start the trip."
            alert(message)
        }
    }

    if (activeFailed || routesFailed) {
        return (
            <div className="p-6 text-center">
                <p className="text-slate-600">Could not reach the server.</p>
                <button
                    onClick={() => {
                        void refetchActive()
                        void refetchRoutes()
                    }}
                    className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm"
                >
                    Try again
                </button>
            </div>
        )
    }

    if (loadingActive || loadingRoutes) {
        return <div className="p-6 text-slate-500">Loading…</div>
    }

    return (
        <div className="flex flex-col gap-4 p-4">
            <h1 className="text-xl font-semibold">Today's trip</h1>

            {activeTrip && (
                <button
                    onClick={() => navigate(ROUTES.driverTrip.replace(":id", activeTrip.id))}
                    className="rounded-xl bg-emerald-600 p-4 text-left text-white"
                >
                    <div className="text-sm opacity-90">Trip in progress</div>
                    <div className="text-lg font-semibold">
                        {activeTrip.routeCode} — {activeTrip.routeName}
                    </div>
                    <div className="mt-1 text-sm opacity-90">Tap to continue</div>
                </button>
            )}

            {!activeTrip && (
                <>
                    <p className="text-sm text-slate-500">Choose your route to begin.</p>

                    {routes?.items
                        ?.filter((r) => r.status === "ACTIVE")
                        .map((route) => (
                            <button
                                key={route.id}
                                disabled={starting || !route.assignedBusId}
                                onClick={() => onStart(route.id)}
                                className="rounded-xl border border-slate-200 p-4 text-left disabled:opacity-50"
                            >
                                <div className="text-lg font-semibold">{route.code}</div>
                                <div className="text-slate-600">{route.name}</div>
                                <div className="mt-1 text-sm text-slate-500">
                                    {route.stops.length} stops
                                    {!route.assignedBusId && " · no bus assigned"}
                                </div>
                            </button>
                        ))}
                </>
            )}
        </div>
    )
}