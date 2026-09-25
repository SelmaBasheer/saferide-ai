import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type SubscriptionStatus = "Active" | "InGrace" | "Expired" | "Cancelled"

export interface Plan {
    id: string
    name: string
    description: string | null
    priceInPaise: number
    busLimit: number | null
    durationMonths: number
    isActive: boolean
}

export interface Subscription {
    id: string
    schoolId: string
    schoolName: string | null
    planName: string
    priceInPaise: number
    busLimit: number | null
    startsOn: string
    endsOn: string
    graceEndsOn: string
    status: SubscriptionStatus
    daysRemaining: number
}

export interface CreatePlanRequest {
    name: string
    description: string | null
    priceInPaise: number
    busLimit: number | null
    durationMonths: number
}

export interface ActivateSubscriptionRequest {
    schoolId: string
    planId: string
    startsOn?: string | null
}

export interface SubscriptionsQueryArgs {
    status?: SubscriptionStatus
    page: number
    pageSize: number
}

/**
 * Prices travel as whole paise, the same integer the database and Razorpay use.
 * Dividing happens here and nowhere else — the moment a rupee value with a
 * decimal point exists in JavaScript, arithmetic on it stops being exact.
 */
export function formatRupees(paise: number): string {
    return new Intl.NumberFormat("en-IN", {
        style: "currency",
        currency: "INR",
        maximumFractionDigits: 0,
    }).format(paise / 100)
}

export const subscriptionApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getPlans: builder.query<Plan[], boolean>({
            query: (includeInactive) => ({ url: "/plans", params: { includeInactive } }),
            transformResponse: (r: ApiResponse<Plan[]>) => r.data,
            providesTags: ["Plans"],
        }),

        createPlan: builder.mutation<{ id: string }, CreatePlanRequest>({
            query: (body) => ({ url: "/plans", method: "POST", body }),
            transformResponse: (r: ApiResponse<{ id: string }>) => r.data,
            invalidatesTags: ["Plans"],
        }),

        updatePlan: builder.mutation<void, { id: string } & CreatePlanRequest>({
            query: ({ id, ...body }) => ({ url: `/plans/${id}`, method: "PUT", body }),
            invalidatesTags: ["Plans"],
        }),

        deactivatePlan: builder.mutation<void, string>({
            query: (id) => ({ url: `/plans/${id}/deactivate`, method: "POST" }),
            invalidatesTags: ["Plans"],
        }),

        getMySubscription: builder.query<Subscription, void>({
            query: () => ({ url: "/subscriptions/me" }),
            transformResponse: (r: ApiResponse<Subscription>) => r.data,
            providesTags: ["Subscription"],
        }),

        getSubscriptions: builder.query<PagedResult<Subscription>, SubscriptionsQueryArgs>({
            query: ({ status, page, pageSize }) => ({
                url: "/subscriptions",
                params: { status: status || undefined, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<Subscription>>) => r.data,
            providesTags: ["Subscription"],
        }),

        activateSubscription: builder.mutation<{ id: string }, ActivateSubscriptionRequest>({
            query: (body) => ({ url: "/subscriptions/activate", method: "POST", body }),
            transformResponse: (r: ApiResponse<{ id: string }>) => r.data,
            invalidatesTags: ["Subscription"],
        }),
    }),
})

export const {
    useGetPlansQuery,
    useCreatePlanMutation,
    useUpdatePlanMutation,
    useDeactivatePlanMutation,
    useGetMySubscriptionQuery,
    useGetSubscriptionsQuery,
    useActivateSubscriptionMutation,
} = subscriptionApi