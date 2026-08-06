import { School, Bus as BusIcon, Route as RouteIcon, Users, ClipboardList } from "lucide-react"
import { ROUTES } from "@/routes/paths"
import type { NavItem } from "@/components/layout/DashboardLayout"

export function schoolAdminNav(active: "Overview" | "Drivers" | "Students"): NavItem[] {
    return [
        { label: "Overview", icon: School, to: ROUTES.schoolAdmin, active: active === "Overview" },
        { label: "Buses", icon: BusIcon },
        { label: "Routes", icon: RouteIcon },
        { label: "Drivers", icon: Users, to: ROUTES.schoolDrivers, active: active === "Drivers" },
        { label: "Students", icon: ClipboardList, to: ROUTES.schoolStudents, active: active === "Students" },
    ]
}