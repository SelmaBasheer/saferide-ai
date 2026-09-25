import { AlertTriangle, Bus, Calendar, CheckCircle2, CreditCard } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import {
    useGetMySubscriptionQuery,
    useGetPlansQuery,
    formatRupees,
    type Plan,
    type Subscription,
} from "@/features/subscriptions/subscriptionApi"

const statusStyles: Record<Subscription["status"], string> = {
    Active: "bg-emerald-50 text-emerald-700",
    InGrace: "bg-amber-50 text-amber-700",
    Expired: "bg-red-50 text-red-700",
    Cancelled: "bg-slate-100 text-slate-500",
}

function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, {
        day: "numeric",
        month: "long",
        year: "numeric",
    })
}

/** Everything the school needs to know at a glance, in one sentence. */
function Notice({ subscription }: { subscription: Subscription }) {
    if (subscription.status === "Expired") {
        return (
            <div className="flex gap-3 rounded-lg border border-red-200 bg-red-50 p-4">
                <AlertTriangle className="h-5 w-5 shrink-0 text-red-500" />
                <div>
                    <p className="text-sm font-medium text-red-700">Your subscription has ended</p>
                    <p className="mt-1 text-sm text-red-600">
                        Tracking, routes and the parent app are stopped until it is renewed.
                    </p>
                </div>
            </div>
        )
    }

    if (subscription.status === "InGrace") {
        return (
            <div className="flex gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4">
                <AlertTriangle className="h-5 w-5 shrink-0 text-amber-500" />
                <div>
                    <p className="text-sm font-medium text-amber-800">
                        Your subscription ended on {formatDate(subscription.endsOn)}
                    </p>
                    <p className="mt-1 text-sm text-amber-700">
                        Everything keeps working until {formatDate(subscription.graceEndsOn)}. After
                        that, the service stops.
                    </p>
                </div>
            </div>
        )
    }

    if (subscription.daysRemaining <= 7) {
        return (
            <div className="flex gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4">
                <AlertTriangle className="h-5 w-5 shrink-0 text-amber-500" />
                <p className="text-sm text-amber-800">
                    Your subscription ends in {subscription.daysRemaining}{" "}
                    {subscription.daysRemaining === 1 ? "day" : "days"}, on{" "}
                    {formatDate(subscription.endsOn)}.
                </p>
            </div>
        )
    }

    return null
}

function CurrentPlan({ subscription }: { subscription: Subscription }) {
    return (
        <div className="rounded-lg border bg-white p-6">
            <div className="flex items-start justify-between">
                <div>
                    <p className="text-sm text-slate-500">Current plan</p>
                    <p className="mt-1 text-2xl font-semibold text-slate-800">
                        {subscription.planName}
                    </p>
                </div>
                <span
                    className={`rounded-full px-3 py-1 text-xs font-medium ${statusStyles[subscription.status]}`}
                >
                    {subscription.status === "InGrace" ? "Grace period" : subscription.status}
                </span>
            </div>

            <div className="mt-6 grid gap-4 sm:grid-cols-3">
                <div>
                    <p className="flex items-center gap-1.5 text-xs text-slate-500">
                        <CreditCard className="h-3.5 w-3.5" /> Price
                    </p>
                    <p className="mt-1 text-sm font-medium text-slate-800">
                        {formatRupees(subscription.priceInPaise)}
                    </p>
                </div>
                <div>
                    <p className="flex items-center gap-1.5 text-xs text-slate-500">
                        <Bus className="h-3.5 w-3.5" /> Buses
                    </p>
                    <p className="mt-1 text-sm font-medium text-slate-800">
                        {subscription.busLimit === null ? "Unlimited" : `Up to ${subscription.busLimit}`}
                    </p>
                </div>
                <div>
                    <p className="flex items-center gap-1.5 text-xs text-slate-500">
                        <Calendar className="h-3.5 w-3.5" /> Ends
                    </p>
                    <p className="mt-1 text-sm font-medium text-slate-800">
                        {formatDate(subscription.endsOn)}
                    </p>
                </div>
            </div>

            {subscription.status === "Active" && (
                <p className="mt-6 border-t pt-4 text-sm text-slate-500">
                    {subscription.daysRemaining} days remaining · started{" "}
                    {formatDate(subscription.startsOn)}
                </p>
            )}
        </div>
    )
}

function PlanCard({ plan, current }: { plan: Plan; current: boolean }) {
    return (
        <div
            className={`rounded-lg border bg-white p-5 ${current ? "border-sky-300 ring-1 ring-sky-100" : ""}`}
        >
            <div className="flex items-center gap-2">
                <p className="font-medium text-slate-800">{plan.name}</p>
                {current && <CheckCircle2 className="h-4 w-4 text-sky-600" />}
            </div>

            <p className="mt-2 text-2xl font-semibold text-slate-800">
                {formatRupees(plan.priceInPaise)}
            </p>
            <p className="text-xs text-slate-400">
                per {plan.durationMonths === 12 ? "year" : `${plan.durationMonths} months`}
            </p>

            <p className="mt-4 text-sm text-slate-600">
                {plan.busLimit === null ? "Unlimited buses" : `Up to ${plan.busLimit} buses`}
            </p>
        </div>
    )
}

export default function SubscriptionPage() {
    const { data: subscription, isLoading, isError } = useGetMySubscriptionQuery()
    const { data: plans = [] } = useGetPlansQuery(false)

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Subscription")}>
            <h1 className="text-2xl font-semibold text-slate-800">Subscription</h1>
            <p className="mt-1 text-sm text-slate-500">
                Your plan, what it includes, and when it renews.
            </p>

            <div className="mt-6 space-y-6">
                {isLoading ? (
                    <p className="text-sm text-slate-500">Loading…</p>
                ) : isError || !subscription ? (
                    // The endpoint returns an error when no subscription exists.
                    // That is not a failure worth a red box — it is the normal
                    // state of a school that has not subscribed yet.
                    <div className="rounded-lg border border-dashed p-6 text-center">
                        <p className="text-sm font-medium text-slate-700">No active subscription</p>
                        <p className="mt-1 text-sm text-slate-500">
                            Contact SafeRide to choose a plan and activate your school.
                        </p>
                    </div>
                ) : (
                    <>
                        <Notice subscription={subscription} />
                        <CurrentPlan subscription={subscription} />
                    </>
                )}

                {plans.length > 0 && (
                    <div>
                        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                            Plans
                        </h2>
                        <div className="mt-3 grid gap-4 sm:grid-cols-3">
                            {plans.map((p) => (
                                <PlanCard
                                    key={p.id}
                                    plan={p}
                                    current={p.name === subscription?.planName}
                                />
                            ))}
                        </div>

                        {/* Said plainly rather than shown as a dead button. The UI
                            does not claim something that does not happen. */}
                        <p className="mt-3 text-sm text-slate-400">
                            Online payment is not available yet. Contact SafeRide to change or renew
                            your plan.
                        </p>
                    </div>
                )}
            </div>
        </DashboardLayout>
    )
}