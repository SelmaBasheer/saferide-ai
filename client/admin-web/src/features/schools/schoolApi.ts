import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"

export type SchoolStatus = "PendingApproval" | "Approved" | "Suspended"

export interface School {
    id: string
    name: string
    city: string
    district: string
    state: string
    pincode: string
    adminName: string
    adminEmail: string
    status: SchoolStatus
    createdAtUtc: string
}

export const schoolApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getSchools: builder.query<School[], void>({
            query: () => "/schools",
            transformResponse: (r: ApiResponse<School[]>) => r.data,
            providesTags: ["Schools"],
        }),
        approveSchool: builder.mutation<unknown, string>({
            query: (id) => ({ url: `/schools/${id}/approve`, method: "POST" }),
            invalidatesTags: ["Schools"],
        }),
        suspendSchool: builder.mutation<unknown, string>({
            query: (id) => ({ url: `/schools/${id}/suspend`, method: "POST" }),
            invalidatesTags: ["Schools"],
        }),
    }),
})

export const { useGetSchoolsQuery, useApproveSchoolMutation, useSuspendSchoolMutation } = schoolApi