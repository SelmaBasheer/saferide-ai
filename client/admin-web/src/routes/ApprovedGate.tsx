import { Navigate } from "react-router-dom"
import { ROUTES } from "@/routes/paths"
import { useGetMySchoolQuery } from "@/features/schools/schoolApi"

/**
 * For pages that need an approved school but not a live subscription —
 * currently just the subscription page itself, which cannot be wrapped in
 * SubscriptionGate without redirecting to itself.
 *
 * Without this, typing /school-admin/subscription while still under review
 * shows plan cards whose buttons the server will refuse.
 */
export default function ApprovedGate({ children }: { children: React.ReactNode }) {
    const { data: school, isLoading } = useGetMySchoolQuery()

    if (isLoading) {
        return null
    }

    if (school && school.status !== "Approved") {
        return <Navigate to={ROUTES.schoolAdmin} replace />
    }

    return <>{children}</>
}