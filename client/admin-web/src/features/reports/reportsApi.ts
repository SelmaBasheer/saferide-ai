import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"

export interface ReportRange {
    from: string
    to: string
}

export interface SuperAdminSummary {
    totalSchools: number
    approvedSchools: number
    suspendedSchools: number
    revenuePaise: number
    paymentCount: number
    failedPaymentCount: number
}

export interface SchoolReportRow {
    name: string
    city: string | null
    status: string
    planName: string | null
    subscriptionStatus: string
    subscriptionEndsOn: string | null
    busLimit: number | null
    busesInUse: number
}

export interface RevenueReportRow {
    paymentDate: string
    schoolName: string
    planName: string
    amountPaise: number
    status: string
}

export interface PlanReportRow {
    planName: string
    paymentCount: number
    revenuePaise: number
}

export interface SuperAdminReport {
    range: ReportRange
    summary: SuperAdminSummary
    schools: SchoolReportRow[]
    revenue: RevenueReportRow[]
    plans: PlanReportRow[]
}

/** Paise are integers everywhere; rupees exist only at the moment of display. */
export function formatRupees(paise: number): string {
    return new Intl.NumberFormat("en-IN", {
        style: "currency",
        currency: "INR",
        maximumFractionDigits: 0,
    }).format(paise / 100)
}

export const reportsApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getSuperAdminReport: builder.query<SuperAdminReport, ReportRange>({
            query: ({ from, to }) => ({
                url: "/analytics/reports/super-admin",
                params: { from, to, format: "json" },
            }),
            transformResponse: (r: ApiResponse<SuperAdminReport>) => r.data,
        }),

        // A mutation rather than a query: it isn't cacheable, and it must be
        // triggered by a click rather than by rendering. responseHandler gives
        // us the raw blob while prepareHeaders still attaches the token, so no
        // part of this has to know how authentication works.
        downloadSuperAdminReport: builder.mutation<Blob, ReportRange & { format: string }>({
            query: ({ from, to, format }) => ({
                url: "/analytics/reports/super-admin",
                params: { from, to, format },
                responseHandler: (response) => response.blob(),
                cache: "no-cache",
            }),
        }),
    }),
})

export const { useGetSuperAdminReportQuery, useDownloadSuperAdminReportMutation } = reportsApi