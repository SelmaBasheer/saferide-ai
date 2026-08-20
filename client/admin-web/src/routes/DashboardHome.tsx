import { Navigate } from "react-router-dom"
import { useAppSelector } from "@/app/hooks"
import { ROUTES } from "@/routes/paths"

export default function DashboardHome() {
    const user = useAppSelector((s) => s.auth.user)
    if (user?.role === "SuperAdmin") return <Navigate to={ROUTES.superAdmin} replace />
    if (user?.role === "SchoolAdmin") return <Navigate to={ROUTES.schoolAdmin} replace />
    if (user?.role === "Driver") return <Navigate to={ROUTES.driver} replace />
    if (user?.role === "Parent") return <Navigate to={ROUTES.parent} replace />
    return <Navigate to={ROUTES.login} replace />
}