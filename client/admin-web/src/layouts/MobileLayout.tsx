import { Outlet } from "react-router-dom"

export function MobileLayout() {
    return (
        <div className="min-h-dvh bg-slate-100 flex justify-center">
            <div className="w-full max-w-md min-h-dvh bg-white flex flex-col shadow-lg">
                <Outlet />
            </div>
        </div>
    )
}