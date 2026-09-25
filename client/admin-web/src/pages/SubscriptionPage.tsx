import { useEffect, useState } from "react"
import { AlertTriangle, Bus, Calendar, CheckCircle2, CreditCard } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import {
    useGetMySubscriptionQuery,
    useGetPlansQuery,
    useStartCheckoutMutation,
    formatRupees,
    type Plan,
    type Subscription,
} from "@/features/subscriptions/subscriptionApi"
import { loadRazorpay } from "@/features/subscriptions/razorpay"

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
                        {subscription.busLimit === null
                            ? "Unlimited"
                            : `Up to ${subscription.busLimit}`}
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

function PayButton({ planId, onPaid }: { planId: string; onPaid: () => void }) {
    const [startCheckout, { isLoading }] = useStartCheckoutMutation()
    const [error, setError] = useState<string | null>(null)

    const pay = async () => {
        setError(null)

        try {
            const session = await startCheckout({ planId }).unwrap()
            await loadRazorpay()

            const checkout = new window.Razorpay!({
                key: session.publicKey,
                amount: session.amountInPaise,
                currency: session.currency,
                name: "SafeRide AI",
                description: session.planName,
                order_id: session.orderId,
                prefill: {
                    name: session.schoolName,
                    email: session.adminEmail,
                    contact: session.adminPhone,
                },
                theme: { color: "#0369a1" },

                // Razorpay tells the browser the payment worked. That is not what
                // activates anything — the webhook is. All this does is start
                // looking for the subscription the webhook will create.
                handler: onPaid,
            })

            checkout.open()
        } catch {
            setError("Could not start checkout. Please try again.")
        }
    }

    return (
        <div className="mt-4">
            <Button
                onClick={pay}
                disabled={isLoading}
                className="w-full bg-sky-700 hover:bg-sky-800"
            >
                {isLoading ? "Opening…" : "Pay and activate"}
            </Button>
            {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
        </div>
    )
}

function PlanCard({
    plan,
    current,
    payable,
    onPaid,
}: {
    plan: Plan
    current: boolean
    payable: boolean
    onPaid: () => void
}) {
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

            {payable && <PayButton planId={plan.id} onPaid={onPaid} />}
        </div>
    )
}

export default function SubscriptionPage() {
    // While confirming, poll. The subscription appears when the webhook lands,
    // a second or two after the browser thinks it is finished — because the
    // browser's opinion is not what creates it.
    const [confirming, setConfirming] = useState(false)

    const {
        data: subscription,
        isLoading,
        isError,
    } = useGetMySubscriptionQuery(undefined, {
        pollingInterval: confirming ? 2000 : 0,
    })

    const { data: plans = [] } = useGetPlansQuery(false)

    const live = subscription?.status === "Active" || subscription?.status === "InGrace"

    useEffect(() => {
        if (confirming && live) setConfirming(false)
    }, [confirming, live])

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
                            Choose a plan below to activate your school.
                        </p>
                    </div>
                ) : (
                    <>
                        <Notice subscription={subscription} />
                        <CurrentPlan subscription={subscription} />
                    </>
                )}

                {confirming && !live && (
                    <div className="rounded-lg border border-sky-200 bg-sky-50 p-4 text-sm text-sky-800">
                        Payment received. Waiting for confirmation from the payment gateway — this
                        usually takes a few seconds.
                    </div>
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
                                    payable={!live}
                                    onPaid={() => setConfirming(true)}
                                />
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </DashboardLayout>
    )
}