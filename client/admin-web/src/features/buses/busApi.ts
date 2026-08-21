import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type BusStatus = "ACTIVE" | "INACTIVE"

export interface BusListItem {
    id: string
    schoolId: string
    registrationNumber: string
    model: string
    capacity: number
    status: BusStatus
    assignedDriverId: string | null
    createdAt: string
    updatedAt: string
}

export interface CreateBusRequest {
    registrationNumber: string
    model: string
    capacity: number
}

export interface BusesQueryArgs {
    search?: string
    includeInactive?: boolean
    page: number
    pageSize: number
}

export const busApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getBus: builder.query<BusListItem, string>({
            query: (id) => ({ url: `/buses/${id}` }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            providesTags: ["Buses"],
        }),

        getBuses: builder.query<PagedResult<BusListItem>, BusesQueryArgs>({
            query: ({ search, includeInactive, page, pageSize }) => ({
                url: "/buses",
                params: { search: search || undefined, includeInactive, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<BusListItem>>) => r.data,
            providesTags: ["Buses"],
        }),

        createBus: builder.mutation<BusListItem, CreateBusRequest>({
            query: (body) => ({ url: "/buses", method: "POST", body }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        updateBus: builder.mutation<BusListItem, { id: string } & CreateBusRequest>({
            query: ({ id, ...body }) => ({ url: `/buses/${id}`, method: "PUT", body }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        assignDriver: builder.mutation<BusListItem, { id: string; driverId: string }>({
            query: ({ id, driverId }) => ({
                url: `/buses/${id}/driver`,
                method: "PUT",
                body: { driverId },
            }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        deactivateBus: builder.mutation<void, string>({
            query: (id) => ({ url: `/buses/${id}`, method: "DELETE" }),
            invalidatesTags: ["Buses"],
        }),
    }),
})

export const {
    useGetBusQuery,
    useGetBusesQuery,
    useCreateBusMutation,
    useUpdateBusMutation,
    useAssignDriverMutation,
    useDeactivateBusMutation,
} = busApi