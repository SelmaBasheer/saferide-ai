import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"

export interface DashboardRange {
    from: string
    to: string
}

export interface SubscriptionCounts {
    active: number
    expiringSoon: number
    inGrace: number
    expired: number
    cancelled: number
}

export interface SchoolCounts {
    onboarded: number
    approved: number
    suspended: number
    withExpiredSubscription: number
    inRangeOnboarded: number
    inRangeApproved: number
    inRangeSuspended: number
}

export interface RevenueTotals {
    totalPaise: number
    thisMonthPaise: number
    thisYearPaise: number
    paymentCount: number
    inRangePaise: number
    inRangeCount: number
}

export interface MonthlyRevenue {
    month: string
    revenuePaise: number
    paymentCount: number
}

export interface PlanRevenue {
    planName: string
    paymentCount: number
    revenuePaise: number
}

export interface RecentSchool {
    name: string
    city: string | null
    planName: string | null
    onboardedAtUtc: string
    status: string
}

export interface RecentPurchase {
    paymentDate: string
    schoolName: string
    planName: string
    amountPaise: number
    status: string
}

export interface SuperAdminDashboard {
    range: DashboardRange
    subscriptions: SubscriptionCounts
    schools: SchoolCounts
    revenue: RevenueTotals
    revenueByMonth: MonthlyRevenue[]
    plans: PlanRevenue[]
    recentSchools: RecentSchool[]
    recentPurchases: RecentPurchase[]
}

/** Both optional: the first load sends neither and lets the server pick the
 *  default window, so the rule lives in one place rather than two. */
export interface DashboardArgs {
    from?: string
    to?: string
}

export const superAdminDashboardApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getSuperAdminDashboard: builder.query<SuperAdminDashboard, DashboardArgs>({
            query: ({ from, to }) => ({
                url: "/analytics/dashboard/super-admin",
                params: { from, to },
            }),
            transformResponse: (r: ApiResponse<SuperAdminDashboard>) => r.data,
        }),
    }),
})

export const { useGetSuperAdminDashboardQuery } = superAdminDashboardApi