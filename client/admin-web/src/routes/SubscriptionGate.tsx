import { Navigate } from "react-router-dom"
import { ROUTES } from "@/routes/paths"
import { useGetMySubscriptionQuery } from "@/features/subscriptions/subscriptionApi"

/**
 * Keeps a school without a live subscription out of the pages that depend on
 * one, by sending them where they can fix it.
 *
 * Convenience, not security. The Bus service refuses to create or assign a bus
 * without a subscription regardless of what the browser does — a guard that
 * lived only here would be bypassed by anyone with Swagger.
 *
 * Grace counts as live, matching the server: the service keeps working until
 * the grace period ends, and locking a school out early would contradict what
 * their own subscription page tells them.
 */
export default function SubscriptionGate({ children }: { children: React.ReactNode }) {
    const { data: subscription, isLoading, isError } = useGetMySubscriptionQuery()

    // Render nothing rather than the page while we find out. Showing a page and
    // then yanking it away is worse than a blank moment.
    if (isLoading) {
        return null
    }

    const live = subscription?.status === "Active" || subscription?.status === "InGrace"

    // isError covers "no subscription exists", which the endpoint reports as a
    // failure rather than as an empty result.
    if (isError || !live) {
        return <Navigate to={ROUTES.schoolSubscription} replace />
    }

    return <>{children}</>
}