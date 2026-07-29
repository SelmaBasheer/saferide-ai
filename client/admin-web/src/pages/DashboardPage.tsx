import { useNavigate } from "react-router-dom"
import { Bus, LogOut } from "lucide-react"
import { useAppSelector, useAppDispatch } from "@/app/hooks"
import { logout } from "@/features/auth/authSlice"
import { Button } from "@/components/ui/button"

export default function DashboardPage() {
    const navigate = useNavigate()
    const dispatch = useAppDispatch()
    const user = useAppSelector((state) => state.auth.user)

    function handleLogout() {
        dispatch(logout())
        navigate("/login", { replace: true })
    }

    const initials =
        user?.email?.slice(0, 2).toUpperCase() ?? "SR"

    return (
        <div className="min-h-screen bg-slate-100">
            {/* Top bar */}
            <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
                <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sky-700">
                        <Bus className="h-4 w-4 text-white" />
                    </div>
                    <span className="font-semibold text-slate-800">SafeRide AI</span>
                </div>
                <Button variant="outline" onClick={handleLogout}>
                    <LogOut className="mr-1 h-4 w-4" />
                    Sign out
                </Button>
            </header>

            {/* Content */}
            <main className="mx-auto max-w-4xl px-6 py-10">
                <div className="rounded-2xl bg-white p-6 shadow-sm">
                    <div className="flex items-center gap-4">
                        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-sky-100 text-lg font-semibold text-sky-700">
                            {initials}
                        </div>
                        <div>
                            <h1 className="text-xl font-semibold text-slate-800">
                                Welcome back
                            </h1>
                            <p className="text-sm text-slate-500">{user?.email}</p>
                        </div>
                        <span className="ml-auto rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-800">
                            {user?.role}
                        </span>
                    </div>
                </div>

                <div className="mt-6 grid gap-4 sm:grid-cols-3">
                    <StatCard label="Schools" value="—" note="Total schools" />
                    <StatCard label="Active" value="—" note="Active schools" />
                    <StatCard label="Suspended" value="—" note="Suspended schools" />
                </div>
            </main>
        </div>
    )
}

function StatCard({ label, value, note }: { label: string; value: string; note: string }) {
    return (
        <div className="rounded-xl border border-slate-200 bg-white p-5">
            <p className="text-sm text-slate-500">{label}</p>
            <p className="mt-1 text-2xl font-semibold text-slate-800">{value}</p>
            <p className="mt-1 text-xs text-slate-400">{note}</p>
        </div>
    )
}