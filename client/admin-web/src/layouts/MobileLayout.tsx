import { useState } from "react"
import { Outlet, useNavigate } from "react-router-dom"
import { Bus, History, LogOut, Menu } from "lucide-react"
import { useAppDispatch } from "@/app/hooks"
import { logout } from "@/features/auth/authSlice"
import { ROUTES } from "@/routes/paths"

export function MobileLayout({ tripsPath }: { tripsPath: string }) {
    const dispatch = useAppDispatch()
    const navigate = useNavigate()
    const [open, setOpen] = useState(false)

    const handleLogout = () => {
        setOpen(false)
        dispatch(logout())
        navigate(ROUTES.login)
    }

    return (
        <div className="flex min-h-dvh justify-center bg-slate-100">
            <div className="flex min-h-dvh w-full max-w-md flex-col bg-white shadow-lg">
                <header className="relative flex items-center justify-between border-b border-slate-200 px-3 py-2">
                    <div className="flex items-center gap-2">
                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-300">
                            <Bus className="h-4 w-4 text-amber-900" />
                        </div>
                        <span className="font-semibold text-slate-800">SafeRide</span>
                    </div>

                    <button
                        onClick={() => setOpen((v) => !v)}
                        aria-label="Menu"
                        aria-expanded={open}
                        className="rounded-lg p-2 text-slate-600 hover:bg-slate-100"
                    >
                        <Menu className="h-5 w-5" />
                    </button>

                    {open && (
                        <>
                            <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
                            <div className="absolute right-2 top-12 z-20 w-44 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-lg">
                                <button
                                    onClick={() => {
                                        setOpen(false)
                                        navigate(tripsPath)
                                    }}
                                    className="flex w-full items-center gap-2 px-3 py-3 text-sm hover:bg-slate-50"
                                >
                                    <History className="h-4 w-4 text-slate-500" /> Past trips
                                </button>
                                <button
                                    onClick={handleLogout}
                                    className="flex w-full items-center gap-2 border-t border-slate-100 px-3 py-3 text-sm text-red-600 hover:bg-slate-50"
                                >
                                    <LogOut className="h-4 w-4" /> Log out
                                </button>
                            </div>
                        </>
                    )}
                </header>

                <main className="flex min-h-0 flex-1 flex-col">
                    <Outlet />
                </main>
            </div>
        </div>
    )
}