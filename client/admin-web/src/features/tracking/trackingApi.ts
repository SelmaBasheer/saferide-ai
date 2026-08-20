import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type TripStatus = "Scheduled" | "Active" | "Completed" | "Cancelled"
export type BoardingStatus = "Unmarked" | "Boarded" | "Absent"
export type PositionSource = "Gps" | "Simulated"

export interface GeoPoint {
    latitude: number
    longitude: number
}

export interface TripStop {
    stopId: string
    sequence: number
    name: string
    latitude: number
    longitude: number
    pickupTime: string
    reachedAt: string | null
}

export interface TripRosterEntry {
    studentId: string
    name: string
    pickupStopId: string
    boardingStatus: BoardingStatus
    markedAt: string | null
}

export interface LastPosition {
    latitude: number
    longitude: number
    recordedAt: string
    speedKmh: number | null
    source: PositionSource
}

export interface Trip {
    id: string
    schoolId: string
    routeId: string
    busId: string
    driverId: string
    status: TripStatus
    startedAt: string
    endedAt: string | null
    routeCode: string
    routeName: string
    stops: TripStop[]
    path: GeoPoint[]
    roster: TripRosterEntry[]
    lastPosition: LastPosition | null
    unmarkedCount: number
}

export interface TripSummary {
    id: string
    routeId: string
    busId: string
    driverId: string
    status: TripStatus
    startedAt: string
    endedAt: string | null
    routeCode: string
    routeName: string
    lastPosition: LastPosition | null
    studentCount: number
    unmarkedCount: number
}

export interface MarkBoardingArgs {
    tripId: string
    studentId: string
    status: Exclude<BoardingStatus, "Unmarked">
}

export interface MyTripsArgs {
    status?: string
    page: number
    pageSize: number
}

export const trackingApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getTrip: builder.query<Trip, string>({
            query: (id) => ({ url: `/trips/${id}` }),
            transformResponse: (r: ApiResponse<Trip>) => r.data,
            providesTags: ["Trips"],
        }),

        getMyTrips: builder.query<PagedResult<TripSummary>, MyTripsArgs>({
            query: ({ status, page, pageSize }) => ({
                url: "/trips",
                params: { status: status || undefined, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<TripSummary>>) => r.data,
            providesTags: ["Trips"],
        }),

        getActiveTrips: builder.query<TripSummary[], void>({
            query: () => ({ url: "/trips/active" }),
            transformResponse: (r: ApiResponse<TripSummary[]>) => r.data,
            providesTags: ["Trips"],
        }),

        startTrip: builder.mutation<Trip, { routeId: string }>({
            query: (body) => ({ url: "/trips/start", method: "POST", body }),
            transformResponse: (r: ApiResponse<Trip>) => r.data,
            invalidatesTags: ["Trips"],
        }),

        endTrip: builder.mutation<Trip, string>({
            query: (id) => ({ url: `/trips/${id}/end`, method: "POST" }),
            transformResponse: (r: ApiResponse<Trip>) => r.data,
            invalidatesTags: ["Trips"],
        }),

        markBoarding: builder.mutation<Trip, MarkBoardingArgs>({
            query: ({ tripId, studentId, status }) => ({
                url: `/trips/${tripId}/boarding`,
                method: "POST",
                body: { studentId, status },
            }),
            transformResponse: (r: ApiResponse<Trip>) => r.data,
            invalidatesTags: ["Trips"],
        }),
    }),
})

export const {
    useGetTripQuery,
    useGetMyTripsQuery,
    useGetActiveTripsQuery,
    useStartTripMutation,
    useEndTripMutation,
    useMarkBoardingMutation,
} = trackingApi