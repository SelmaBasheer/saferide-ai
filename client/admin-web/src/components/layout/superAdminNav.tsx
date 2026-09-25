import { LayoutDashboard, School, Tags, CreditCard } from "lucide-react"
import { ROUTES } from "@/routes/paths"
import type { NavItem } from "@/components/layout/DashboardLayout"

export function superAdminNav(
    active: "Overview" | "Schools" | "Plans" | "Subscriptions"
): NavItem[] {
    return [
        { label: "Overview", icon: LayoutDashboard, to: ROUTES.superAdmin, active: active === "Overview" },
        { label: "Schools", icon: School, to: ROUTES.superAdminSchools, active: active === "Schools" },
        { label: "Plans", icon: Tags, to: ROUTES.superAdminPlans, active: active === "Plans" },
        { label: "Subscriptions", icon: CreditCard, to: ROUTES.superAdminSubscriptions, active: active === "Subscriptions" },
    ]
}