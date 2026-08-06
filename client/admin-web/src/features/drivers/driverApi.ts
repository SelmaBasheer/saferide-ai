import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type DriverStatus = "Active" | "Inactive"

export interface DriverListItem {
    id: string
    firstName: string
    lastName: string
    email: string
    phone: string
    licenseNumber: string
    licenseExpiryDate: string   // "2029-12-31"
    status: DriverStatus
}

export interface CreateDriverRequest {
    firstName: string
    lastName: string
    email: string
    phone: string
    licenseNumber: string
    licenseExpiryDate: string
}

export interface DriversQueryArgs {
    search?: string
    page: number
    pageSize: number
}

export const driverApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getDrivers: builder.query<PagedResult<DriverListItem>, DriversQueryArgs>({
            query: ({ search, page, pageSize }) => ({
                url: "/drivers",
                params: { search: search || undefined, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<DriverListItem>>) => r.data,
            providesTags: ["Drivers"],
        }),

        createDriver: builder.mutation<DriverListItem, CreateDriverRequest>({
            query: (body) => ({ url: "/drivers", method: "POST", body }),
            transformResponse: (r: ApiResponse<DriverListItem>) => r.data,
            invalidatesTags: ["Drivers"],
        }),
    }),
})

export const { useGetDriversQuery, useCreateDriverMutation } = driverApi