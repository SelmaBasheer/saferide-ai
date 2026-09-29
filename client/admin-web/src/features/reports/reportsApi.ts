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

export type SchoolReportSection = "Attendance" | "Trips"

export interface SchoolReportSummary {
    trips: number
    boarded: number
    absent: number
    unmarked: number
    attendanceRate: number
}

export interface AttendanceReportRow {
    tripDate: string
    routeCode: string | null
    routeName: string | null
    studentName: string
    stopName: string | null
    status: string
    markedAtUtc: string | null
}

export interface TripReportRow {
    tripDate: string
    routeCode: string | null
    routeName: string | null
    busRegistration: string | null
    startedAtUtc: string
    endedAtUtc: string | null
    studentCount: number
    boardedCount: number
    absentCount: number
    unmarkedCount: number
}

export interface SchoolReport {
    range: ReportRange
    section: SchoolReportSection
    summary: SchoolReportSummary
    attendance: AttendanceReportRow[]
    trips: TripReportRow[]
}

export interface SchoolReportArgs extends ReportRange {
    section: SchoolReportSection
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

        getSchoolReport: builder.query<SchoolReport, SchoolReportArgs>({
            query: ({ from, to, section }) => ({
                url: "/analytics/reports/school",
                params: { from, to, section, format: "json" },
            }),
            transformResponse: (r: ApiResponse<SchoolReport>) => r.data,
        }),

        downloadSchoolReport: builder.mutation<Blob, SchoolReportArgs & { format: string }>({
            query: ({ from, to, section, format }) => ({
                url: "/analytics/reports/school",
                params: { from, to, section, format },
                responseHandler: (response) => response.blob(),
                cache: "no-cache",
            }),
        }),
    }),
})

export const {
    useGetSuperAdminReportQuery,
    useDownloadSuperAdminReportMutation,
    useGetSchoolReportQuery,
    useDownloadSchoolReportMutation,
} = reportsApi