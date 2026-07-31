import { useState } from "react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import {
    useGetSchoolsQuery, useApproveSchoolMutation, useSuspendSchoolMutation,
    type SchoolStatus,
} from "@/features/schools/schoolApi"
import { Button } from "@/components/ui/button"
import { School, Users, BarChart3 } from "lucide-react"


const FILTERS: { label: string; value: SchoolStatus | "All" }[] = [
    { label: "All", value: "All" },
    { label: "Pending", value: "PendingApproval" },
    { label: "Approved", value: "Approved" },
    { label: "Suspended", value: "Suspended" },
]

const badge: Record<SchoolStatus, string> = {
    PendingApproval: "bg-amber-100 text-amber-700",
    Approved: "bg-green-100 text-green-700",
    Suspended: "bg-red-100 text-red-700",
}

export default function DashboardPage() {
    const { data: schools = [], isLoading, isError } = useGetSchoolsQuery()
    const [approve, { isLoading: approving }] = useApproveSchoolMutation()
    const [suspend, { isLoading: suspending }] = useSuspendSchoolMutation()
    const [filter, setFilter] = useState<SchoolStatus | "All">("All")

    const rows = filter === "All" ? schools : schools.filter((s) => s.status === filter)

    return (
        <DashboardLayout roleLabel="Super Admin" nav={[
            { label: "Schools", icon: School, active: true },
            { label: "Users", icon: Users },
            { label: "Reports", icon: BarChart3 },
        ]}>
            <h1 className="text-2xl font-semibold text-slate-800">Schools</h1>
            <p className="mt-1 text-sm text-slate-500">Approve or suspend registered schools.</p>

            <div className="mt-6 flex gap-2">
                {FILTERS.map((f) => (
                    <button key={f.value} onClick={() => setFilter(f.value)}
                        className={`rounded-full px-4 py-1.5 text-sm ${filter === f.value ? "bg-sky-700 text-white" : "bg-white text-slate-600 border"}`}>
                        {f.label}
                    </button>
                ))}
            </div>

            <div className="mt-4 overflow-hidden rounded-lg border bg-white">
                {isLoading ? (
                    <p className="p-6 text-sm text-slate-500">Loading schools…</p>
                ) : isError ? (
                    <p className="p-6 text-sm text-red-600">Failed to load schools.</p>
                ) : rows.length === 0 ? (
                    <p className="p-6 text-sm text-slate-500">No schools found.</p>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-slate-50 text-left text-slate-500">
                            <tr>
                                <th className="px-4 py-3 font-medium">School</th>
                                <th className="px-4 py-3 font-medium">Admin</th>
                                <th className="px-4 py-3 font-medium">Location</th>
                                <th className="px-4 py-3 font-medium">Status</th>
                                <th className="px-4 py-3 font-medium text-right">Action</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y">
                            {rows.map((s) => (
                                <tr key={s.id} className="hover:bg-slate-50">
                                    <td className="px-4 py-3 font-medium text-slate-800">{s.name}</td>
                                    <td className="px-4 py-3 text-slate-600">
                                        <div>{s.adminName}</div>
                                        <div className="text-xs text-slate-400">{s.adminEmail}</div>
                                    </td>
                                    <td className="px-4 py-3 text-slate-600">{s.city}, {s.district}, {s.state}</td>
                                    <td className="px-4 py-3">
                                        <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${badge[s.status]}`}>
                                            {s.status === "PendingApproval" ? "Pending" : s.status}
                                        </span>
                                    </td>
                                    <td className="px-4 py-3 text-right">
                                        {s.status === "PendingApproval" && (
                                            <Button size="sm" disabled={approving}
                                                onClick={() => approve(s.id)}>Approve</Button>
                                        )}
                                        {s.status === "Approved" && (
                                            <Button size="sm" variant="destructive" disabled={suspending}
                                                onClick={() => suspend(s.id)}>Suspend</Button>
                                        )}
                                        {s.status === "Suspended" && <span className="text-xs text-slate-400">—</span>}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </DashboardLayout>
    )
}