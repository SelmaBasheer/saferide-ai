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

/** No email: it is the login identity, so it is not editable here. */
export interface UpdateDriverRequest {
    firstName: string
    lastName: string
    phone: string
    licenseNumber: string
    licenseExpiryDate: string
}

export type LicenceFilter = "valid" | "expiring" | "expired"

export interface DriversQueryArgs {
    search?: string
    status?: DriverStatus
    licence?: LicenceFilter
    page: number
    pageSize: number
}

export const driverApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getDrivers: builder.query<PagedResult<DriverListItem>, DriversQueryArgs>({
            query: ({ search, status, licence, page, pageSize }) => ({
                url: "/drivers",
                params: {
                    search: search || undefined,
                    status: status || undefined,
                    licence: licence || undefined,
                    page,
                    pageSize,
                },
            }),
            transformResponse: (r: ApiResponse<PagedResult<DriverListItem>>) => r.data,
            providesTags: ["Drivers"],
        }),

        createDriver: builder.mutation<DriverListItem, CreateDriverRequest>({
            query: (body) => ({ url: "/drivers", method: "POST", body }),
            transformResponse: (r: ApiResponse<DriverListItem>) => r.data,
            invalidatesTags: ["Drivers"],
        }),

        updateDriver: builder.mutation<DriverListItem, { id: string } & UpdateDriverRequest>({
            query: ({ id, ...body }) => ({ url: `/drivers/${id}`, method: "PUT", body }),
            transformResponse: (r: ApiResponse<DriverListItem>) => r.data,
            invalidatesTags: ["Drivers"],
        }),

        // Buses show the assigned driver, so a status change makes a cached
        // bus list wrong as well.
        deactivateDriver: builder.mutation<DriverListItem, string>({
            query: (id) => ({ url: `/drivers/${id}/deactivate`, method: "POST" }),
            transformResponse: (r: ApiResponse<DriverListItem>) => r.data,
            invalidatesTags: ["Drivers", "Buses"],
        }),

        reactivateDriver: builder.mutation<DriverListItem, string>({
            query: (id) => ({ url: `/drivers/${id}/reactivate`, method: "POST" }),
            transformResponse: (r: ApiResponse<DriverListItem>) => r.data,
            invalidatesTags: ["Drivers", "Buses"],
        }),
    }),
})

export const {
    useGetDriversQuery,
    useCreateDriverMutation,
    useUpdateDriverMutation,
    useDeactivateDriverMutation,
    useReactivateDriverMutation,
} = driverApi