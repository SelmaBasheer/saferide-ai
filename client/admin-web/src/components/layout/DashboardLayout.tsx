import type { ReactNode } from "react"
import type { LucideIcon } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { Bus, LogOut } from "lucide-react"
import { useAppDispatch } from "@/app/hooks"
import { logout } from "@/features/auth/authSlice"
import { ROUTES } from "@/routes/paths"
import { Button } from "@/components/ui/button"

export interface NavItem { label: string; icon: LucideIcon; active?: boolean }

export default function DashboardLayout({
    roleLabel, nav, children,
}: { roleLabel: string; nav: NavItem[]; children: ReactNode }) {
    const dispatch = useAppDispatch()
    const navigate = useNavigate()
    const handleLogout = () => { dispatch(logout()); navigate(ROUTES.login) }

    return (
        <div className="flex min-h-screen bg-slate-100">
            <aside className="flex w-60 flex-col justify-between bg-sky-800 p-4 text-sky-100">
                <div>
                    <div className="mb-8 flex items-center gap-2.5 px-2 pt-2">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-300">
                            <Bus className="h-5 w-5 text-amber-900" />
                        </div>
                        <div>
                            <p className="font-semibold text-white">SafeRide AI</p>
                            <p className="text-xs text-sky-300">{roleLabel}</p>
                        </div>
                    </div>
                    <nav className="space-y-1">
                        {nav.map(({ label, icon: Icon, active }) => (
                            <button key={label} disabled={!active}
                                className={`flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm
                  ${active ? "bg-sky-700 text-white" : "cursor-not-allowed text-sky-300/70"}`}>
                                <Icon className="h-4 w-4" /> {label}
                            </button>
                        ))}
                    </nav>
                </div>
                <Button variant="ghost" onClick={handleLogout}
                    className="w-full justify-start gap-3 text-sky-100 hover:bg-sky-700 hover:text-white">
                    <LogOut className="h-4 w-4" /> Logout
                </Button>
            </aside>
            <main className="flex-1 overflow-auto p-8">{children}</main>
        </div>
    )
}