import { School, Bus as BusIcon, Route as RouteIcon, Users, ClipboardList } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { useAppSelector } from "@/app/hooks"

export default function SchoolAdminDashboardPage() {
    const user = useAppSelector((s) => s.auth.user)
    return (
        <DashboardLayout roleLabel="School Admin" nav={[
            { label: "Overview", icon: School, active: true },
            { label: "Buses", icon: BusIcon },
            { label: "Routes", icon: RouteIcon },
            { label: "Drivers", icon: Users },
            { label: "Students", icon: ClipboardList },
        ]}>
            <h1 className="text-2xl font-semibold text-slate-800">Welcome</h1>
            <p className="mt-1 text-sm text-slate-500">{user?.email}</p>
            <div className="mt-6 grid gap-4 sm:grid-cols-3">
                {["Buses", "Routes", "Drivers"].map((k) => (
                    <div key={k} className="rounded-lg border bg-white p-5">
                        <p className="text-sm text-slate-500">{k}</p>
                        <p className="mt-1 text-2xl font-semibold text-slate-800">—</p>
                    </div>
                ))}
            </div>
            <p className="mt-6 text-sm text-slate-400">
                Manage your school's buses, routes, drivers and students here.
            </p>
        </DashboardLayout>
    )
}