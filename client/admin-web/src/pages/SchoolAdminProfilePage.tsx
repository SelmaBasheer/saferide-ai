import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import ProfilePanel from "@/features/profile/ProfilePanel"

export default function SchoolAdminProfilePage() {
    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Profile")}>
            <ProfilePanel />
        </DashboardLayout>
    )
}