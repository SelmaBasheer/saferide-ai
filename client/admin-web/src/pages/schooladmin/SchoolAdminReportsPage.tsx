import { useState } from "react"
import { Download } from "lucide-react"
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
    useGetSchoolReportQuery,
    useDownloadSchoolReportMutation,
    type SchoolReportSection,
} from "@/features/reports/reportsApi"

function errorMessage(e: unknown, fallback: string) {
    const data = (e as { data?: { error?: { message?: string } } })?.data
    return data?.error?.message ?? fallback
}

/** Local date, not UTC — toISOString() would return yesterday all evening. */
function isoDate(d: Date): string {
    const year = d.getFullYear()
    const month = String(d.getMonth() + 1).padStart(2, "0")
    const day = String(d.getDate()).padStart(2, "0")
    return `${year}-${month}-${day}`
}

function defaultRange() {
    const to = new Date()
    const from = new Date()
    from.setDate(from.getDate() - 30)
    return { from: isoDate(from), to: isoDate(to) }
}

function time(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
}

function duration(startIso: string, endIso: string | null): string {
    if (!endIso) return "—"
    const minutes = Math.round(
        (new Date(endIso).getTime() - new Date(startIso).getTime()) / 60000
    )
    if (minutes < 60) return `${minutes}m`
    return `${Math.floor(minutes / 60)}h ${minutes % 60}m`
}

function Stat({ label, value }: { label: string; value: string | number }) {
    return (
        <div className="rounded-lg border bg-white p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
            <p className="mt-1 text-2xl font-semibold text-slate-800">{value}</p>
        </div>
    )
}

// Green for boarded, red for absent, grey for unmarked — unmarked is not a
// third outcome, it is the absence of one, and the colour should say so.
const SLICE_COLOURS = ["#059669", "#dc2626", "#94a3b8"]

export default function SchoolAdminReportsPage() {
    const initial = defaultRange()

    const [draft, setDraft] = useState(initial)
    const [range, setRange] = useState(initial)
    const [section, setSection] = useState<SchoolReportSection>("Attendance")

    const { data, isFetching, isError, error } = useGetSchoolReportQuery({ ...range, section })
    const [download, { isLoading: isDownloading, error: downloadError, reset }] =
        useDownloadSchoolReportMutation()

    const save = async () => {
        try {
            const blob = await download({ ...range, section, format: "csv" }).unwrap()

            const url = URL.createObjectURL(blob)
            const link = document.createElement("a")
            link.href = url
            link.download = `saferide-${section.toLowerCase()}-${range.from}-to-${range.to}.csv`
            link.click()

            URL.revokeObjectURL(url)
            reset()
        } catch {
            // Shown below.
        }
    }

    const slices = data
        ? [
            { name: "Boarded", value: data.summary.boarded },
            { name: "Absent", value: data.summary.absent },
            { name: "Unmarked", value: data.summary.unmarked },
        ].filter((s) => s.value > 0)
        : []

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Reports")}>
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Reports</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Attendance and trip history for your school. Built from completed trips, so a
                    trip appears once the driver ends it.
                </p>
            </div>

            <form
                className="mt-6 flex flex-wrap items-end gap-3 rounded-lg border bg-white p-4"
                onSubmit={(e) => {
                    e.preventDefault()
                    setRange(draft)
                }}
            >
                <label className="text-sm">
                    <span className="text-slate-600">Report</span>
                    <select
                        className="mt-1 block h-9 rounded-md border border-slate-200 bg-white px-3 text-sm"
                        value={section}
                        onChange={(e) => setSection(e.target.value as SchoolReportSection)}
                    >
                        <option value="Attendance">Attendance</option>
                        <option value="Trips">Trip history</option>
                    </select>
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">From</span>
                    <Input
                        type="date"
                        className="mt-1"
                        value={draft.from}
                        max={draft.to}
                        onChange={(e) => setDraft({ ...draft, from: e.target.value })}
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">To</span>
                    <Input
                        type="date"
                        className="mt-1"
                        value={draft.to}
                        min={draft.from}
                        onChange={(e) => setDraft({ ...draft, to: e.target.value })}
                    />
                </label>

                <Button type="submit" className="bg-sky-700 hover:bg-sky-800">
                    Apply
                </Button>

                <Button
                    type="button"
                    variant="outline"
                    disabled={!data || isDownloading}
                    onClick={save}
                >
                    <Download className="mr-1 h-4 w-4" />
                    {isDownloading ? "Preparing…" : "Download CSV"}
                </Button>
            </form>

            {isError && (
                <p className="mt-4 text-sm text-red-600">
                    {errorMessage(error, "Could not load the report.")}
                </p>
            )}

            {downloadError && (
                <p className="mt-4 text-sm text-red-600">
                    {errorMessage(downloadError, "Could not download the report.")}
                </p>
            )}

            {isFetching && <p className="mt-4 text-sm text-slate-500">Loading…</p>}

            {data && (
                <>
                    <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        <Stat label="Trips" value={data.summary.trips} />
                        <Stat label="Boarded" value={data.summary.boarded} />
                        <Stat label="Absent" value={data.summary.absent} />
                        <Stat
                            label="Attendance rate"
                            value={`${data.summary.attendanceRate}%`}
                        />
                    </div>

                    <div className="mt-6 grid gap-6 lg:grid-cols-3">
                        <div className="rounded-lg border bg-white p-4">
                            <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                                Boarding breakdown
                            </h2>

                            {slices.length === 0 ? (
                                <p className="mt-6 text-sm text-slate-400">
                                    Nothing recorded in this range.
                                </p>
                            ) : (
                                <div className="mt-2 h-56">
                                    <ResponsiveContainer width="100%" height="100%">
                                        <PieChart>
                                            <Pie
                                                data={slices}
                                                dataKey="value"
                                                nameKey="name"
                                                innerRadius={45}
                                                outerRadius={75}
                                                paddingAngle={2}
                                            >
                                                {slices.map((s, i) => (
                                                    <Cell
                                                        key={s.name}
                                                        fill={SLICE_COLOURS[i % SLICE_COLOURS.length]}
                                                    />
                                                ))}
                                            </Pie>
                                            <Tooltip />
                                            <Legend verticalAlign="bottom" height={24} />
                                        </PieChart>
                                    </ResponsiveContainer>
                                </div>
                            )}

                            <p className="mt-2 text-xs text-slate-400">
                                The attendance rate counts boarded against absent only. Unmarked
                                students are excluded rather than assumed present.
                            </p>
                        </div>

                        <div className="overflow-hidden rounded-lg border bg-white lg:col-span-2">
                            <h2 className="border-b bg-slate-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                                {section === "Attendance" ? "Attendance" : "Trip history"}
                            </h2>

                            <div className="max-h-[32rem] overflow-auto">
                                {section === "Attendance" ? (
                                    <table className="w-full text-sm">
                                        <thead className="sticky top-0 border-b bg-white text-left text-xs uppercase tracking-wide text-slate-400">
                                            <tr>
                                                <th className="px-4 py-3">Date</th>
                                                <th className="px-4 py-3">Route</th>
                                                <th className="px-4 py-3">Student</th>
                                                <th className="px-4 py-3">Stop</th>
                                                <th className="px-4 py-3">Status</th>
                                                <th className="px-4 py-3">Marked</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y">
                                            {data.attendance.length === 0 ? (
                                                <tr>
                                                    <td
                                                        colSpan={6}
                                                        className="px-4 py-6 text-slate-500"
                                                    >
                                                        No completed trips in this range.
                                                    </td>
                                                </tr>
                                            ) : (
                                                data.attendance.map((r, i) => (
                                                    <tr key={`${r.tripDate}-${r.studentName}-${i}`}>
                                                        <td className="px-4 py-2 text-slate-800">
                                                            {r.tripDate}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {r.routeCode ?? "—"}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-800">
                                                            {r.studentName}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {r.stopName ?? "—"}
                                                        </td>
                                                        <td className="px-4 py-2">
                                                            <span
                                                                className={
                                                                    r.status === "Boarded"
                                                                        ? "text-emerald-700"
                                                                        : r.status === "Absent"
                                                                            ? "text-red-600"
                                                                            : "text-slate-400"
                                                                }
                                                            >
                                                                {r.status}
                                                            </span>
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {time(r.markedAtUtc)}
                                                        </td>
                                                    </tr>
                                                ))
                                            )}
                                        </tbody>
                                    </table>
                                ) : (
                                    <table className="w-full text-sm">
                                        <thead className="sticky top-0 border-b bg-white text-left text-xs uppercase tracking-wide text-slate-400">
                                            <tr>
                                                <th className="px-4 py-3">Date</th>
                                                <th className="px-4 py-3">Route</th>
                                                <th className="px-4 py-3">Bus</th>
                                                <th className="px-4 py-3">Started</th>
                                                <th className="px-4 py-3">Duration</th>
                                                <th className="px-4 py-3">Boarded</th>
                                                <th className="px-4 py-3">Absent</th>
                                                <th className="px-4 py-3">Unmarked</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y">
                                            {data.trips.length === 0 ? (
                                                <tr>
                                                    <td
                                                        colSpan={8}
                                                        className="px-4 py-6 text-slate-500"
                                                    >
                                                        No completed trips in this range.
                                                    </td>
                                                </tr>
                                            ) : (
                                                data.trips.map((t, i) => (
                                                    <tr key={`${t.tripDate}-${t.startedAtUtc}-${i}`}>
                                                        <td className="px-4 py-2 text-slate-800">
                                                            {t.tripDate}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {t.routeCode ?? "—"}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {t.busRegistration ?? "—"}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {time(t.startedAtUtc)}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-500">
                                                            {duration(t.startedAtUtc, t.endedAtUtc)}
                                                        </td>
                                                        <td className="px-4 py-2 text-emerald-700">
                                                            {t.boardedCount}
                                                        </td>
                                                        <td className="px-4 py-2 text-red-600">
                                                            {t.absentCount}
                                                        </td>
                                                        <td className="px-4 py-2 text-slate-400">
                                                            {t.unmarkedCount}
                                                        </td>
                                                    </tr>
                                                ))
                                            )}
                                        </tbody>
                                    </table>
                                )}
                            </div>
                        </div>
                    </div>
                </>
            )}
        </DashboardLayout>
    )
}