import { useMemo } from "react"
import { useGetBusesQuery } from "@/features/buses/busApi"
import { useGetStudentsQuery } from "@/features/students/studentApi"
import { useGetRoutesQuery } from "@/features/routes/routeApi"
import { useGetDriversQuery } from "@/features/drivers/driverApi"
import { useGetAlertsQuery } from "@/features/alerts/alertApi"
import { useGetTripsQuery, useGetActiveTripsQuery } from "@/features/tracking/trackingApi"

// We only want totalCount from most of these, so ask for the smallest page
// the server will give us. One row travels; the count is what we read.
const COUNT_ONLY = { page: 1, pageSize: 1 }

// Routes are different: we need every route's stops to count them, so we
// fetch the whole list. A school has single-digit routes, so this is cheap.
const ROUTE_PAGE = 100

// Seven days of completed trips. ~24 trips a day means ~170 rows.
const TRIP_HISTORY_PAGE = 250
const DAYS = 7

// A bus that hasn't reported in three minutes has lost signal.
const STALE_MS = 3 * 60 * 1000

// Local calendar day, not UTC. toISOString() would put an early-morning
// trip in yesterday's bucket for anyone east of Greenwich.
function dayKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`
}

export interface DayBucket {
    date: string
    label: string
    count: number
}

export function useSchoolOverview() {
    const allBuses = useGetBusesQuery({ ...COUNT_ONLY, includeInactive: true })
    const activeBuses = useGetBusesQuery({ ...COUNT_ONLY, includeInactive: false })
    const students = useGetStudentsQuery(COUNT_ONLY)
    const drivers = useGetDriversQuery(COUNT_ONLY)
    const routes = useGetRoutesQuery({ page: 1, pageSize: ROUTE_PAGE, includeInactive: false })

    const openAlerts = useGetAlertsQuery({ status: "Classified", ...COUNT_ONLY })
    const recentAlerts = useGetAlertsQuery({ page: 1, pageSize: 5 })

    // Polling is the floor, not the ceiling — SignalR moves the buses between
    // polls. This is here so a trip that starts or ends still shows up if the
    // hub connection dropped.
    const activeTrips = useGetActiveTripsQuery(undefined, { pollingInterval: 30_000 })

    // Computed once at mount. A dashboard left open overnight will show a
    // stale window, which is a fair trade for not refetching every render.
    const window = useMemo(() => {
        const to = new Date()
        to.setHours(23, 59, 59, 999)
        const from = new Date()
        from.setDate(from.getDate() - (DAYS - 1))
        from.setHours(0, 0, 0, 0)
        return { from: from.toISOString(), to: to.toISOString() }
    }, [])

    const history = useGetTripsQuery({
        status: "Completed",
        from: window.from,
        to: window.to,
        page: 1,
        pageSize: TRIP_HISTORY_PAGE,
    })

    const stopCount = useMemo(
        () => (routes.data?.items ?? []).reduce((n, r) => n + r.stops.length, 0),
        [routes.data],
    )

    const tripsPerDay = useMemo<DayBucket[]>(() => {
        const days: DayBucket[] = []
        const index = new Map<string, number>()

        for (let i = DAYS - 1; i >= 0; i--) {
            const d = new Date()
            d.setDate(d.getDate() - i)
            const key = dayKey(d)
            index.set(key, days.length)
            days.push({
                date: key,
                label: d.toLocaleDateString(undefined, { weekday: "short" }),
                count: 0,
            })
        }

        for (const trip of history.data?.items ?? []) {
            const at = index.get(dayKey(new Date(trip.startedAt)))
            if (at !== undefined) days[at].count += 1
        }

        return days
    }, [history.data])

    const active = activeTrips.data ?? []

    const silentCount = active.filter((t) => {
        if (!t.lastPosition) return true
        return Date.now() - new Date(t.lastPosition.recordedAt).getTime() > STALE_MS
    }).length

    return {
        buses: { total: allBuses.data?.totalCount ?? 0, active: activeBuses.data?.totalCount ?? 0 },
        studentCount: students.data?.totalCount ?? 0,
        driverCount: drivers.data?.totalCount ?? 0,
        routeCount: routes.data?.totalCount ?? 0,
        stopCount,

        // True when a school outgrew ROUTE_PAGE, so the stop total is a floor
        // rather than a fact. Better to say so than to quietly undercount.
        stopCountPartial: (routes.data?.totalCount ?? 0) > ROUTE_PAGE,

        openAlertCount: openAlerts.data?.totalCount ?? 0,
        recentAlerts: recentAlerts.data?.items ?? [],

        activeTrips: active,
        silentCount,
        refetchActiveTrips: activeTrips.refetch,

        tripsPerDay,
        tripHistoryPartial: (history.data?.totalCount ?? 0) > TRIP_HISTORY_PAGE,

        isLoading:
            allBuses.isLoading || students.isLoading || routes.isLoading ||
            drivers.isLoading || activeTrips.isLoading,
        isError:
            allBuses.isError || students.isError || routes.isError ||
            drivers.isError || activeTrips.isError,
    }
}