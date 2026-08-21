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

export interface CreateRouteRequest {
    code: string
    name: string
}

export interface StopInput {
    stopId: string | null
    name: string
    latitude: number
    longitude: number
    pickupTime: string
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

        getRoute: builder.query<RouteListItem, string>({
            query: (id) => ({ url: `/routes/${id}` }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            providesTags: ["Routes"],
        }),

        createRoute: builder.mutation<RouteListItem, CreateRouteRequest>({
            query: (body) => ({ url: "/routes", method: "POST", body }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            invalidatesTags: ["Routes"],
        }),

        assignBusToRoute: builder.mutation<RouteListItem, { id: string; busId: string }>({
            query: ({ id, busId }) => ({ url: `/routes/${id}/bus`, method: "PUT", body: { busId } }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            invalidatesTags: ["Routes"],
        }),

        deactivateRoute: builder.mutation<void, string>({
            query: (id) => ({ url: `/routes/${id}`, method: "DELETE" }),
            invalidatesTags: ["Routes"],
        }),

        updateRoute: builder.mutation<RouteListItem, { id: string; code: string; name: string }>({
            query: ({ id, ...body }) => ({ url: `/routes/${id}`, method: "PUT", body }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            invalidatesTags: ["Routes"],
        }),

        replaceStops: builder.mutation<RouteListItem, { id: string; stops: StopInput[] }>({
            query: ({ id, stops }) => ({ url: `/routes/${id}/stops`, method: "PUT", body: { stops } }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            invalidatesTags: ["Routes"],
        }),

        replacePath: builder.mutation<RouteListItem, { id: string; points: RouteGeoPoint[] }>({
            query: ({ id, points }) => ({ url: `/routes/${id}/path`, method: "PUT", body: { points } }),
            transformResponse: (r: ApiResponse<RouteListItem>) => r.data,
            invalidatesTags: ["Routes"],
        }),
    }),
})

export const {
    useGetRoutesQuery,
    useGetRouteQuery,
    useCreateRouteMutation,
    useAssignBusToRouteMutation,
    useDeactivateRouteMutation,
    useUpdateRouteMutation,
    useReplaceStopsMutation,
    useReplacePathMutation
} = routeApi