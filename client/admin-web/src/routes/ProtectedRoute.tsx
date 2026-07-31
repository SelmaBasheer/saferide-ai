import { Navigate } from "react-router-dom"
import { useAppSelector } from "@/app/hooks"
import { ROUTES } from "@/routes/paths"
import type { UserRole } from "@/features/auth/authTypes"

export default function ProtectedRoute({
    children, roles,
}: { children: React.ReactNode; roles?: UserRole[] }) {
    const user = useAppSelector((state) => state.auth.user)
    if (!user) return <Navigate to={ROUTES.login} replace />
    if (roles && !roles.includes(user.role)) return <Navigate to={ROUTES.dashboard} replace />
    return <>{children}</>
}