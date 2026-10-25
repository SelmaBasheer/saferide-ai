import { useState, type ReactNode } from "react"
import type { LucideIcon } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { Bus, LogOut, Menu, X } from "lucide-react"
import { useAppDispatch } from "@/app/hooks"
import { logout } from "@/features/auth/authSlice"
import { ROUTES } from "@/routes/paths"
import { Button } from "@/components/ui/button"

export interface NavItem { label: string; icon: LucideIcon; active?: boolean; to?: string }

export default function DashboardLayout({
    roleLabel, nav, children,
}: { roleLabel: string; nav: NavItem[]; children: ReactNode }) {
    const dispatch = useAppDispatch()
    const navigate = useNavigate()
    const [open, setOpen] = useState(false)

    const handleLogout = () => { dispatch(logout()); navigate(ROUTES.login) }

    const go = (to: string) => { setOpen(false); navigate(to) }

    const brand = (
        <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-amber-300">
                <Bus className="h-5 w-5 text-amber-900" />
            </div>
            <div className="min-w-0">
                <p className="truncate font-semibold text-white">SafeRide AI</p>
                <p className="truncate text-xs text-sky-300">{roleLabel}</p>
            </div>
        </div>
    )

    return (
        <div className="min-h-screen bg-slate-100 lg:flex">
            {/* Mobile bar. On lg the sidebar carries the brand, so this disappears. */}
            <header className="flex items-center justify-between bg-sky-800 px-4 py-2.5 lg:hidden">
                {brand}
                <button
                    onClick={() => setOpen(true)}
                    aria-label="Open menu"
                    className="rounded-lg p-2 text-sky-100 hover:bg-sky-700"
                >
                    <Menu className="h-5 w-5" />
                </button>
            </header>

            {open && (
                <div
                    className="fixed inset-0 z-30 bg-slate-900/50 lg:hidden"
                    onClick={() => setOpen(false)}
                />
            )}

            {/* One sidebar for both breakpoints: a slide-over below lg, static above.
                Two separate copies would drift apart the first time a nav item changes. */}
            <aside
                className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col justify-between bg-sky-800 p-4
                            text-sky-100 transition-transform duration-200
                            lg:static lg:w-60 lg:translate-x-0
                            ${open ? "translate-x-0" : "-translate-x-full"}`}
            >
                <div>
                    <div className="mb-8 flex items-start justify-between px-2 pt-2">
                        {brand}
                        <button
                            onClick={() => setOpen(false)}
                            aria-label="Close menu"
                            className="rounded-lg p-1.5 text-sky-200 hover:bg-sky-700 lg:hidden"
                        >
                            <X className="h-5 w-5" />
                        </button>
                    </div>

                    <nav className="space-y-1">
                        {nav.map(({ label, icon: Icon, active, to }) => (
                            <button key={label} disabled={!active && !to}
                                onClick={() => to && go(to)}
                                className={`flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm
                                        ${active ? "bg-sky-700 text-white"
                                        : to ? "text-sky-100 hover:bg-sky-700/60"
                                            : "cursor-not-allowed text-sky-300/70"}`}>
                                <Icon className="h-4 w-4 shrink-0" /> {label}
                            </button>
                        ))}
                    </nav>
                </div>

                <Button variant="ghost" onClick={handleLogout}
                    className="w-full justify-start gap-3 text-sky-100 hover:bg-sky-700 hover:text-white">
                    <LogOut className="h-4 w-4" /> Logout
                </Button>
            </aside>

            <main className="min-w-0 flex-1 overflow-auto p-4 sm:p-6 lg:p-8">{children}</main>
        </div>
    )
}