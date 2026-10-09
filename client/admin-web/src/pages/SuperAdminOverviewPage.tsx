import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import {
    School, Clock, CheckCircle2, Ban, CreditCard, IndianRupee,
    CalendarClock, AlertTriangle, XCircle,
} from "lucide-react"
import {
    Bar, BarChart, Cell, Legend, Pie, PieChart, ResponsiveContainer,
    Tooltip, XAxis, YAxis,
} from "recharts"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ROUTES } from "@/routes/paths"
import { useGetSchoolsQuery } from "@/features/schools/schoolApi"
import { formatRupees } from "@/features/subscriptions/subscriptionApi"
import {
    useGetSuperAdminDashboardQuery,
    type DashboardArgs,
} from "@/features/dashboard/superAdminDashboardApi"

const COUNT_ONLY = { page: 1, pageSize: 1 }

const SCHOOL_COLOURS = ["#059669", "#dc2626", "#94a3b8"]
const SUBSCRIPTION_COLOURS = ["#059669", "#f97316", "#dc2626", "#94a3b8"]
const PLAN_COLOURS = ["#0369a1", "#0891b2", "#6366f1", "#8b5cf6", "#a855f7"]

const PRESETS = [
    { label: "This month", months: 1 },
    { label: "Last 3 months", months: 3 },
    { label: "Last 6 months", months: 6 },
    { label: "Last 12 months", months: 12 },
]

interface Slice {
    name: string
    value: number
}

function Stat({ icon, label, value, sub, to, tone = "slate" }: {
    icon: React.ReactNode
    label: string
    value: React.ReactNode
    sub?: string
    to?: string
    tone?: "slate" | "amber" | "red" | "sky"
}) {
    const tones = {
        slate: { box: "bg-white", label: "text-slate-500", value: "text-slate-800", sub: "text-slate-400" },
        sky: { box: "border-sky-200 bg-sky-50", label: "text-sky-700", value: "text-sky-900", sub: "text-sky-600" },
        amber: { box: "border-amber-200 bg-amber-50", label: "text-amber-700", value: "text-amber-800", sub: "text-amber-600" },
        red: { box: "border-red-200 bg-red-50", label: "text-red-700", value: "text-red-800", sub: "text-red-600" },
    }[tone]

    const body = (
        <div className={`h-full rounded-lg border p-4 ${tones.box} ${to ? "transition hover:border-sky-300" : ""}`}>
            <div className={`flex items-center gap-1.5 text-xs ${tones.label}`}>
                {icon}
                {label}
            </div>
            <p className={`mt-1 text-xl font-semibold ${tones.value}`}>{value}</p>
            {sub && <p className={`mt-0.5 text-[11px] leading-tight ${tones.sub}`}>{sub}</p>}
        </div>
    )

    return to ? <Link to={to}>{body}</Link> : body
}

/**
 * The hole is the reason these are donuts rather than pies: the total sits in
 * it, so one shape answers "how many" and "split how" at once.
 */
function Donut({ title, slices, colours, unit, format }: {
    title: string
    slices: Slice[]
    colours: string[]
    unit: string
    format?: (total: number) => string
}) {
    const total = slices.reduce((sum, s) => sum + s.value, 0)

    return (
        <div className="rounded-lg border bg-white p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">{title}</p>

            {slices.length === 0 ? (
                <p className="mt-10 text-center text-sm text-slate-400">Nothing yet.</p>
            ) : (
                <div className="mt-2 h-60">
                    <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                            <Pie data={slices} dataKey="value" nameKey="name"
                                innerRadius={48} outerRadius={78} paddingAngle={2}>
                                {slices.map((s, i) => (
                                    <Cell key={s.name} fill={colours[i % colours.length]} />
                                ))}
                            </Pie>

                            {/* Two lines straddling the centre so they read as one block. */}
                            <text x="50%" y="44%" textAnchor="middle"
                                className="fill-slate-800 text-lg font-semibold">
                                {format ? format(total) : total}
                            </text>
                            <text x="50%" y="53%" textAnchor="middle"
                                className="fill-slate-400 text-[10px]">
                                {unit}
                            </text>

                            <Tooltip />
                            <Legend verticalAlign="bottom" height={28} iconSize={8}
                                wrapperStyle={{ fontSize: 11 }} />
                        </PieChart>
                    </ResponsiveContainer>
                </div>
            )}
        </div>
    )
}

/** Local-date ISO string. toISOString() converts to UTC first and hands back
 *  yesterday for anyone east of Greenwich — which is every user of this app. */
function isoDate(d: Date): string {
    const month = `${d.getMonth() + 1}`.padStart(2, "0")
    const day = `${d.getDate()}`.padStart(2, "0")
    return `${d.getFullYear()}-${month}-${day}`
}

/** A window of whole months ending today: 1 is the current month so far, 3 is
 *  this month plus the two before it. Day-of-month never enters the From date,
 *  so "last 3 months" means the same thing on the 1st as on the 31st. */
function monthsBack(months: number): { from: string; to: string } {
    const now = new Date()
    return {
        from: isoDate(new Date(now.getFullYear(), now.getMonth() - (months - 1), 1)),
        to: isoDate(now),
    }
}

/** "2026-05-01" → "May". Built from the parts rather than a Date, which would
 *  shift the month for anyone behind UTC. */
function monthLabel(iso: string): string {
    const [year, month] = iso.split("-").map(Number)
    return new Date(year, month - 1, 1).toLocaleDateString(undefined, { month: "short" })
}

/** For date-only strings from the API. Passing "2026-10-01" to the Date
 *  constructor parses it as UTC midnight, which renders as 30 September in any
 *  timezone behind Greenwich — so build it from the parts instead. */
function shortLocalDate(iso: string): string {
    const [year, month, day] = iso.split("-").map(Number)
    return new Date(year, month - 1, day).toLocaleDateString(undefined, {
        day: "numeric",
        month: "short",
        year: "numeric",
    })
}

/** For real timestamps, where the instant is the point and the browser should
 *  render it in local time. */
function shortDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, {
        day: "numeric",
        month: "short",
        year: "numeric",
    })
}

/** Recharts hands the formatter a loosely typed value, so narrow it once here
 *  rather than fighting the signature at each call site. Chart data is in
 *  rupees; formatRupees wants paise. */
function rupeeTooltip(value: unknown): string {
    return formatRupees(Number(value ?? 0) * 100)
}

export default function SuperAdminOverviewPage() {
    // School statuses come from the School service, which owns them. Money and
    // subscription lifecycle come from Analytics. Asking each for what it owns
    // means the two can never disagree on screen.
    const all = useGetSchoolsQuery(COUNT_ONLY)
    const submitted = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Submitted" })
    const approved = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Approved" })
    const suspended = useGetSchoolsQuery({ ...COUNT_ONLY, status: "Suspended" })

    // The five most recent schools waiting for review — the one thing a super
    // admin comes here to do, so it stays above everything else.
    const pending = useGetSchoolsQuery({ status: "Submitted", page: 1, pageSize: 5 })

    // The first request sends no dates and lets the server choose the window,
    // so the default lives in one place. The inputs are then seeded from what
    // came back.
    const [range, setRange] = useState<DashboardArgs>({})
    const [draft, setDraft] = useState({ from: "", to: "" })

    const { data, isFetching, isError } = useGetSuperAdminDashboardQuery(range)

    // Every field below is optional-chained rather than assumed present. A
    // service running an older image answers with a subset of the shape, and a
    // dashboard that white-screens on a missing field is worse than one showing
    // a dash.
    useEffect(() => {
        if (data?.range && !draft.from) {
            setDraft({ from: data.range.from, to: data.range.to })
        }
    }, [data, draft.from])

    const pendingCount = submitted.data?.totalCount ?? 0
    const approvedCount = approved.data?.totalCount ?? 0
    const suspendedCount = suspended.data?.totalCount ?? 0

    const subs = data?.subscriptions
    const revenue = data?.revenue
    const schools = data?.schools
    const lapsed = schools?.withExpiredSubscription ?? 0

    // Highlight against what the server actually returned rather than the draft
    // inputs, so the active chip always describes what is on screen.
    const applied = data?.range

    const isActivePreset = (months: number) => {
        const preset = monthsBack(months)
        return applied?.from === preset.from && applied?.to === preset.to
    }

    const applyPreset = (months: number) => {
        const preset = monthsBack(months)
        setDraft(preset)
        setRange(preset)
    }

    // The donut answers "who came on board in this range, and where do they
    // stand today" — an event filtered by date, broken down by current status.
    // Schools still under review are absent by construction: nothing publishes
    // an event for them, so they never reach dim_school.
    const inRangeOther = Math.max(
        0,
        (schools?.inRangeOnboarded ?? 0)
        - (schools?.inRangeApproved ?? 0)
        - (schools?.inRangeSuspended ?? 0),
    )

    const schoolSlices: Slice[] = [
        { name: "Approved", value: schools?.inRangeApproved ?? 0 },
        { name: "Suspended", value: schools?.inRangeSuspended ?? 0 },
        { name: "Other", value: inRangeOther },
    ].filter((s) => s.value > 0)

    // Expiring soon is deliberately absent — it is a subset of Active, and a
    // donut whose slices overlap would add to more than the whole.
    const subscriptionSlices: Slice[] = [
        { name: "Active", value: subs?.active ?? 0 },
        { name: "In grace", value: subs?.inGrace ?? 0 },
        { name: "Expired", value: subs?.expired ?? 0 },
        { name: "Cancelled", value: subs?.cancelled ?? 0 },
    ].filter((s) => s.value > 0)

    const planSlices: Slice[] = (data?.plans ?? [])
        .filter((p) => p.revenuePaise > 0)
        .map((p) => ({ name: p.planName, value: p.revenuePaise / 100 }))

    const monthly = (data?.revenueByMonth ?? []).map((m) => ({
        label: monthLabel(m.month),
        rupees: m.revenuePaise / 100,
    }))

    const recentSchools = data?.recentSchools ?? []
    const recentPurchases = data?.recentPurchases ?? []

    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Overview")}>
            <h1 className="text-2xl font-semibold text-slate-800">Overview</h1>
            <p className="mt-1 text-sm text-slate-500">Every school on SafeRide, and what they pay.</p>

            {isError && (
                <p className="mt-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                    Revenue and subscription figures could not be loaded. School counts below are unaffected.
                </p>
            )}

            {/* ---------- Waiting for review ---------- */}

            <div className="mt-6 rounded-lg border bg-white p-5">
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
                ) : (pending.data?.items?.length ?? 0) === 0 ? (
                    <p className="mt-3 text-sm text-slate-500">Nothing waiting. All caught up.</p>
                ) : (
                    <div className="mt-2 divide-y">
                        {pending.data?.items?.map((s) => (
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

            {/* ---------- Date range ---------- */}

            <div className="mt-6 rounded-lg border bg-white p-4">
                <div className="flex flex-wrap items-center gap-2">
                    {PRESETS.map((p) => (
                        <button
                            key={p.months}
                            type="button"
                            onClick={() => applyPreset(p.months)}
                            disabled={isFetching}
                            className={`rounded-full border px-3 py-1 text-xs transition ${isActivePreset(p.months)
                                ? "border-sky-600 bg-sky-600 text-white"
                                : "border-slate-200 text-slate-600 hover:border-sky-300 hover:text-sky-700"
                                }`}
                        >
                            {p.label}
                        </button>
                    ))}

                    <span className="text-xs text-slate-300">|</span>

                    <form
                        className="flex flex-wrap items-end gap-2"
                        onSubmit={(e) => {
                            e.preventDefault()
                            setRange({ from: draft.from, to: draft.to })
                        }}
                    >
                        <Input type="date" className="h-8 w-auto text-xs" value={draft.from}
                            max={draft.to} aria-label="From"
                            onChange={(e) => setDraft({ ...draft, from: e.target.value })} />
                        <span className="pb-1.5 text-xs text-slate-400">to</span>
                        <Input type="date" className="h-8 w-auto text-xs" value={draft.to}
                            min={draft.from} aria-label="To"
                            onChange={(e) => setDraft({ ...draft, to: e.target.value })} />
                        <Button type="submit" size="sm" variant="outline"
                            className="h-8 text-xs" disabled={isFetching}>
                            {isFetching ? "Loading…" : "Apply"}
                        </Button>
                    </form>
                </div>

                <p className="mt-2 text-[11px] leading-tight text-slate-400">
                    Showing{" "}
                    {applied ? `${shortLocalDate(applied.from)} – ${shortLocalDate(applied.to)}` : "…"}.
                    The range filters onboardings, purchases, revenue and both breakdowns. The
                    subscription lifecycle counts describe today, because they are derived from
                    each subscription's end date rather than stored per day.
                </p>
            </div>

            {/* ---------- Schools ---------- */}

            <h2 className="mt-8 text-sm font-semibold uppercase tracking-wide text-slate-400">Schools</h2>

            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-6">
                <Stat icon={<School className="h-3.5 w-3.5" />} label="Total"
                    value={all.data?.totalCount ?? "—"} to={ROUTES.superAdminSchools} />
                <Stat icon={<School className="h-3.5 w-3.5" />} label="Onboarded in range"
                    value={isFetching ? "—" : schools?.inRangeOnboarded ?? 0}
                    sub="approved inside the dates" tone="sky" />
                <Stat icon={<Clock className="h-3.5 w-3.5" />} label="Pending review"
                    value={submitted.isLoading ? "—" : pendingCount} sub="waiting on you"
                    to={ROUTES.superAdminSchools} tone={pendingCount > 0 ? "amber" : "slate"} />
                <Stat icon={<CheckCircle2 className="h-3.5 w-3.5" />} label="Approved"
                    value={approved.isLoading ? "—" : approvedCount} />
                <Stat icon={<Ban className="h-3.5 w-3.5" />} label="Suspended"
                    value={suspended.isLoading ? "—" : suspendedCount}
                    tone={suspendedCount > 0 ? "amber" : "slate"} />
                <Stat icon={<XCircle className="h-3.5 w-3.5" />} label="Lapsed"
                    value={isFetching ? "—" : lapsed}
                    sub="paid once, now expired" to={ROUTES.superAdminSubscriptions}
                    tone={lapsed > 0 ? "red" : "slate"} />
            </div>

            {/* ---------- Subscriptions ---------- */}

            <h2 className="mt-8 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Subscriptions
                <span className="font-normal normal-case tracking-normal text-slate-400">
                    {" "}— as of today
                </span>
            </h2>

            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
                <Stat icon={<CreditCard className="h-3.5 w-3.5" />} label="Active"
                    value={isFetching ? "—" : subs?.active ?? 0} to={ROUTES.superAdminSubscriptions} />
                <Stat icon={<CalendarClock className="h-3.5 w-3.5" />} label="Expiring soon"
                    value={isFetching ? "—" : subs?.expiringSoon ?? 0} sub="within 30 days"
                    to={ROUTES.superAdminSubscriptions}
                    tone={(subs?.expiringSoon ?? 0) > 0 ? "amber" : "slate"} />
                <Stat icon={<AlertTriangle className="h-3.5 w-3.5" />} label="In grace"
                    value={isFetching ? "—" : subs?.inGrace ?? 0} sub="past end date, still working"
                    tone={(subs?.inGrace ?? 0) > 0 ? "amber" : "slate"} />
                <Stat icon={<XCircle className="h-3.5 w-3.5" />} label="Expired"
                    value={isFetching ? "—" : subs?.expired ?? 0}
                    tone={(subs?.expired ?? 0) > 0 ? "red" : "slate"} />
                <Stat icon={<Ban className="h-3.5 w-3.5" />} label="Cancelled"
                    value={isFetching ? "—" : subs?.cancelled ?? 0} />
            </div>

            {/* ---------- Revenue ---------- */}

            <h2 className="mt-8 text-sm font-semibold uppercase tracking-wide text-slate-400">Revenue</h2>

            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
                <Stat icon={<IndianRupee className="h-3.5 w-3.5" />} label="In selected range"
                    value={isFetching ? "—" : formatRupees(revenue?.inRangePaise ?? 0)}
                    sub={`${revenue?.inRangeCount ?? 0} payments`} tone="sky" />
                <Stat icon={<IndianRupee className="h-3.5 w-3.5" />} label="This month"
                    value={isFetching ? "—" : formatRupees(revenue?.thisMonthPaise ?? 0)} />
                <Stat icon={<IndianRupee className="h-3.5 w-3.5" />} label="This year"
                    value={isFetching ? "—" : formatRupees(revenue?.thisYearPaise ?? 0)} />
                <Stat icon={<IndianRupee className="h-3.5 w-3.5" />} label="All time"
                    value={isFetching ? "—" : formatRupees(revenue?.totalPaise ?? 0)} />
                <Stat icon={<CreditCard className="h-3.5 w-3.5" />} label="Payments"
                    value={isFetching ? "—" : revenue?.paymentCount ?? 0} sub="all time" />
            </div>

            {/* ---------- Charts ---------- */}

            <div className="mt-6 grid gap-4 lg:grid-cols-3">
                <Donut title="Onboarded in range, by status today" slices={schoolSlices}
                    colours={SCHOOL_COLOURS} unit="schools" />
                <Donut title="Subscriptions today" slices={subscriptionSlices}
                    colours={SUBSCRIPTION_COLOURS} unit="subscriptions" />
                <Donut title="Revenue by plan" slices={planSlices} colours={PLAN_COLOURS}
                    unit="in range" format={(total) => formatRupees(total * 100)} />
            </div>

            <div className="mt-4 rounded-lg border bg-white p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Monthly revenue</p>

                {monthly.length === 0 ? (
                    <p className="mt-10 text-center text-sm text-slate-400">
                        No payments in this range.
                    </p>
                ) : (
                    <div className="mt-4 h-64">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={monthly}>
                                <XAxis dataKey="label" tickLine={false} axisLine={false}
                                    tick={{ fontSize: 12, fill: "#94a3b8" }} />
                                <YAxis tickLine={false} axisLine={false}
                                    tick={{ fontSize: 12, fill: "#94a3b8" }}
                                    tickFormatter={(v: number) => `₹${v / 1000}k`} />
                                <Tooltip
                                    formatter={(value: unknown) =>
                                        [rupeeTooltip(value), "Revenue"] as [string, string]
                                    }
                                />
                                <Bar dataKey="rupees" fill="#0369a1" radius={[4, 4, 0, 0]} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                )}
            </div>

            {/* ---------- Detail tables ---------- */}

            <div className="mt-4 grid gap-4 lg:grid-cols-2">
                <div className="overflow-hidden rounded-lg border bg-white">
                    <h2 className="border-b bg-slate-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                        Recently onboarded
                    </h2>

                    {recentSchools.length === 0 ? (
                        <p className="px-4 py-6 text-sm text-slate-400">
                            No schools came on board in this range.
                        </p>
                    ) : (
                        <table className="w-full text-sm">
                            <tbody className="divide-y">
                                {recentSchools.map((s) => (
                                    <tr key={`${s.name}-${s.onboardedAtUtc}`}>
                                        <td className="px-4 py-2">
                                            <p className="font-medium text-slate-800">{s.name}</p>
                                            <p className="text-xs text-slate-400">
                                                {s.city ?? "—"} · {s.planName ?? "no plan"}
                                            </p>
                                        </td>
                                        <td className="px-4 py-2 text-right text-xs text-slate-500">
                                            {shortDate(s.onboardedAtUtc)}
                                            <span className="mt-0.5 block text-slate-400">{s.status}</span>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>

                <div className="overflow-hidden rounded-lg border bg-white">
                    <div className="flex items-center justify-between border-b bg-slate-50 px-4 py-3">
                        <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                            Subscription purchases
                        </h2>
                        <Link to={ROUTES.superAdminReports} className="text-xs text-sky-700 hover:underline">
                            Full report
                        </Link>
                    </div>

                    {recentPurchases.length === 0 ? (
                        <p className="px-4 py-6 text-sm text-slate-400">
                            No purchases in this range.
                        </p>
                    ) : (
                        <table className="w-full text-sm">
                            <tbody className="divide-y">
                                {recentPurchases.map((p, i) => (
                                    <tr key={`${p.paymentDate}-${p.schoolName}-${i}`}>
                                        <td className="px-4 py-2">
                                            <p className="font-medium text-slate-800">{p.schoolName}</p>
                                            <p className="text-xs text-slate-400">
                                                {p.planName} · {shortLocalDate(p.paymentDate)}
                                            </p>
                                        </td>
                                        <td className="px-4 py-2 text-right">
                                            <p className="font-medium text-slate-800">
                                                {formatRupees(p.amountPaise)}
                                            </p>
                                            <p className="text-xs text-slate-400">{p.status}</p>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>

            <p className="mt-3 text-xs text-slate-400">
                Subscription lifecycle counts are as of today, not filtered by the dates above:
                they are derived from each subscription's end date, and Analytics keeps no daily
                history to answer "how many were active in March". Everything else on this page —
                onboardings, purchases, revenue and both breakdowns — moves with the range.
            </p>
        </DashboardLayout>
    )
}