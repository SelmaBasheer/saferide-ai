import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { ROUTES } from "@/routes/paths"
import { useGetMyTripsQuery } from "@/features/tracking/trackingApi"

export default function ParentHomePage() {
    const navigate = useNavigate()
    const { data, isLoading, isError, refetch } = useGetMyTripsQuery({
        status: "Active",
        page: 1,
        pageSize: 50,
    })

    const trips = data?.items ?? []

    useEffect(() => {
        if (trips.length === 1) {
            navigate(ROUTES.parentTrip.replace(":id", trips[0].id), { replace: true })
        }
    }, [trips, navigate])

    if (isError) {
        return (
            <div className="p-6 text-center">
                <p className="text-slate-600">Could not reach the server.</p>
                <button
                    onClick={() => refetch()}
                    className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm"
                >
                    Try again
                </button>
            </div>
        )
    }

    if (isLoading) return <div className="p-6 text-slate-500">Looking for your bus…</div>

    if (trips.length === 0) {
        return (
            <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
                <div className="text-4xl">🚌</div>
                <div className="font-medium">No bus is running right now</div>
                <p className="text-sm text-slate-500">
                    You'll be able to follow the bus here once the driver starts the trip.
                </p>
            </div>
        )
    }

    return (
        <div className="flex flex-col gap-3 p-4">
            <h1 className="text-xl font-semibold">Your child's bus</h1>
            {trips.map((t) => (
                <button
                    key={t.id}
                    onClick={() => navigate(ROUTES.parentTrip.replace(":id", t.id))}
                    className="rounded-xl border border-slate-200 p-4 text-left"
                >
                    <div className="text-lg font-semibold">{t.routeCode}</div>
                    <div className="text-slate-600">{t.routeName}</div>
                </button>
            ))}
        </div>
    )
}