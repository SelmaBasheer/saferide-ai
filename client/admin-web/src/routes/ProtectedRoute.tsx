import { Navigate } from "react-router-dom"
import { useAppSelector } from "@/app/hooks"

const ADMIN_ROLES = ["SuperAdmin", "SchoolAdmin"]

/** Requires a valid session AND an admin role to view the page. */
export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
    const user = useAppSelector((state) => state.auth.user)

    if (!user || !ADMIN_ROLES.includes(user.role)) {
        return <Navigate to="/login" replace />
    }

    return <>{children}</>
}