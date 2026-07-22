import { Navigate } from "react-router-dom"
import { useAppSelector } from "@/app/hooks"

/** Wraps pages that require login. Redirects to /login if there's no token. */
export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
    const token = useAppSelector((state) => state.auth.accessToken)

    if (!token) {
        return <Navigate to="/login" replace />
    }

    return <>{children}</>
}