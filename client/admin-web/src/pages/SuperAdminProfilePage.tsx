import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import ProfilePanel from "@/features/profile/ProfilePanel"

export default function SuperAdminProfilePage() {
    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Profile")}>
            <ProfilePanel />
        </DashboardLayout>
    )
}