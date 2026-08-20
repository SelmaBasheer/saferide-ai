import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type RouteStatus = "ACTIVE" | "INACTIVE"

export interface RouteStop {
    stopId: string
    sequence: number
    name: string
    latitude: number
    longitude: number
    pickupTime: string
}

export interface RouteGeoPoint {
    latitude: number
    longitude: number
}

export interface RouteListItem {
    id: string
    schoolId: string
    code: string
    name: string
    status: RouteStatus
    assignedBusId: string | null
    stops: RouteStop[]
    path: RouteGeoPoint[]
}

export interface RoutesQueryArgs {
    search?: string
    includeInactive?: boolean
    page: number
    pageSize: number
}

export const routeApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getRoutes: builder.query<PagedResult<RouteListItem>, RoutesQueryArgs>({
            query: ({ search, includeInactive, page, pageSize }) => ({
                url: "/routes",
                params: { search: search || undefined, includeInactive, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<RouteListItem>>) => r.data,
            providesTags: ["Routes"],
        }),
    }),
})

export const { useGetRoutesQuery } = routeApi