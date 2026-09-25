import { Link } from "react-router-dom"
import { School, Clock, CheckCircle2, Ban, CreditCard, IndianRupee, CalendarClock } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import { ROUTES } from "@/routes/paths"
import { useGetSchoolsQuery } from "@/features/schools/schoolApi"
import { useGetSubscriptionsQuery, formatRupees } from "@/features/subscriptions/subscriptionApi"

const COUNT_ONLY = { page: 1, pageSize: 1 }

function Stat({ icon, label, value, sub, to, tone = "slate" }: {
    icon: React.ReactNode
    label: string
    value: React.ReactNode
    sub?: string
    to?: string
    tone?: "slate" | "amber"
}) {
    const body = (
        <div className={`rounded-lg border p-5 ${tone === "amber" ? "border-amber-200 bg-amber-50" : "bg-white"}
            ${to ? "transition hover:border-sky-300" : ""}`}>
            <div className={`flex items-center gap-2 text-sm ${tone === "amber" ? "text-amber-700" : "text-slate-500"}`}>
                {icon}
                {label}
            </div>
            <p className={`mt-1 text-2xl font-semibold ${tone === "amber" ? "text-amber-800" : "text-slate-800"}`}>
                {value}
            </p>
            {sub && <p className={`mt-0.5 text-xs ${tone === "amber" ? "text-amber-600" : "text-slate-400"}`}>{sub}</p>}
        </div>
    )

    return to ? <Link to={to}>{body}</Link> : body
}

export default function SuperAdminOverviewPage() {
    const all = useGetSchoolsQuery(COUNT_ONLY)
    const submitted = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Submitted" })
    const approved = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Approved" })
    const suspended = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Suspended" })

    // The five most recent schools waiting for review — the one thing a super
    // admin comes here to do.
    const pending = useGetSchoolsQuery({ status: "Submitted", page: 1, pageSize: 5 })

    // Every subscription in one page. Revenue and renewal counts are sums over
    // this list rather than endpoints of their own: at a few hundred schools
    // that is one request instead of three.
    const subs = useGetSubscriptionsQuery({ page: 1, pageSize: 200 })
    const subscriptions = subs.data?.items ?? []

    const active = subscriptions.filter((s) => s.status === "Active" || s.status === "InGrace")
    const revenue = active.reduce((sum, s) => sum + s.priceInPaise, 0)
    const renewalsDue = active.filter((s) => s.daysRemaining <= 30).length

    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Overview")}>
            <h1 className="text-2xl font-semibold text-slate-800">Overview</h1>
            <p className="mt-1 text-sm text-slate-500">Every school on SafeRide, and what they pay.</p>

            <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                <Stat
                    icon={<School className="h-4 w-4" />} label="Schools"
                    value={all.data?.totalCount ?? "—"}
                    to={ROUTES.superAdminSchools}
                />
                <Stat
                    icon={<Clock className="h-4 w-4" />} label="Pending review"
                    value={submitted.data?.totalCount ?? "—"}
                    sub="waiting on you"
                    to={ROUTES.superAdminSchools}
                    tone={(submitted.data?.totalCount ?? 0) > 0 ? "amber" : "slate"}
                />
                <Stat
                    icon={<CheckCircle2 className="h-4 w-4" />} label="Approved"
                    value={approved.data?.totalCount ?? "—"}
                />
                <Stat
                    icon={<Ban className="h-4 w-4" />} label="Suspended"
                    value={suspended.data?.totalCount ?? "—"}
                />
                <Stat
                    icon={<CreditCard className="h-4 w-4" />} label="Live subscriptions"
                    value={subs.isLoading ? "—" : active.length}
                    to={ROUTES.superAdminSubscriptions}
                />
                <Stat
                    icon={<IndianRupee className="h-4 w-4" />} label="Contracted revenue"
                    value={subs.isLoading ? "—" : formatRupees(revenue)}
                    sub="sum of live subscriptions"
                />
                <Stat
                    icon={<CalendarClock className="h-4 w-4" />} label="Renewals due"
                    value={subs.isLoading ? "—" : renewalsDue}
                    sub="within 30 days"
                    to={ROUTES.superAdminSubscriptions}
                    tone={renewalsDue > 0 ? "amber" : "slate"}
                />
            </div>

            <div className="mt-8 rounded-lg border bg-white p-5">
                <div className="flex items-center justify-between">
                    <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                        Waiting for review
                    </h2>
                    <Link to={ROUTES.superAdminSchools} className="text-sm text-sky-700 hover:underline">
                        All schools
                    </Link>
                </div>

                {pending.isLoading ? (
                    <p className="mt-3 text-sm text-slate-500">Loading…</p>
                ) : (pending.data?.items.length ?? 0) === 0 ? (
                    <p className="mt-3 text-sm text-slate-500">Nothing waiting. All caught up.</p>
                ) : (
                    <div className="mt-2 divide-y">
                        {pending.data?.items.map((s) => (
                            <Link
                                key={s.id}
                                to={ROUTES.superAdminSchool.replace(":id", s.id)}
                                className="flex items-center justify-between py-3 hover:bg-slate-50"
                            >
                                <div>
                                    <p className="text-sm font-medium text-slate-800">{s.name}</p>
                                    <p className="text-xs text-slate-400">
                                        {s.city}, {s.district} · {s.adminEmail}
                                    </p>
                                </div>
                                <span className="text-sm text-sky-700">Review →</span>
                            </Link>
                        ))}
                    </div>
                )}
            </div>
        </DashboardLayout>
    )
}