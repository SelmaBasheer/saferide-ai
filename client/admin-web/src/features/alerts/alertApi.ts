import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type AlertStatus = "Detected" | "Classified" | "Approved" | "Dismissed"

export interface Alert {
    id: string
    tripId: string
    routeCode: string
    routeName: string
    type: string
    status: AlertStatus
    detectedAtUtc: string
    classification: string | null
    reasoning: string | null
    draftMessage: string | null
    confidence: number | null
    classifiedByModel: boolean
    resolvedAtUtc: string | null
}

export interface AlertsQueryArgs {
    status?: string
    page: number
    pageSize: number
}

export const alertApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getAlerts: builder.query<PagedResult<Alert>, AlertsQueryArgs>({
            query: ({ status, page, pageSize }) => ({
                url: "/alerts",
                params: { status: status || undefined, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<Alert>>) => r.data,
            providesTags: ["Alerts"],
        }),

        approveAlert: builder.mutation<void, string>({
            query: (id) => ({ url: `/alerts/${id}/approve`, method: "POST" }),
            invalidatesTags: ["Alerts"],
        }),

        dismissAlert: builder.mutation<void, string>({
            query: (id) => ({ url: `/alerts/${id}/dismiss`, method: "POST" }),
            invalidatesTags: ["Alerts"],
        }),
    }),
})

export const { useGetAlertsQuery, useApproveAlertMutation, useDismissAlertMutation } = alertApi