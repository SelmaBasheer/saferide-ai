import {
    School,
    Bus as BusIcon,
    Route as RouteIcon,
    Users,
    ClipboardList,
    History,
    Bell,
    CreditCard,
    UserCircle,
    FileText
} from "lucide-react"
import { ROUTES } from "@/routes/paths"
import type { NavItem } from "@/components/layout/DashboardLayout"

export function schoolAdminNav(
    active:
        | "Overview"
        | "Drivers"
        | "Students"
        | "Buses"
        | "Routes"
        | "Trips"
        | "Alerts"
        | "Subscription"
        | "Profile"
        | "Reports"
): NavItem[] {
    return [
        { label: "Overview", icon: School, to: ROUTES.schoolAdmin, active: active === "Overview" },
        { label: "Buses", icon: BusIcon, to: ROUTES.schoolBuses, active: active === "Buses" },
        { label: "Routes", icon: RouteIcon, to: ROUTES.schoolRoutes, active: active === "Routes" },
        { label: "Drivers", icon: Users, to: ROUTES.schoolDrivers, active: active === "Drivers" },
        { label: "Students", icon: ClipboardList, to: ROUTES.schoolStudents, active: active === "Students" },
        { label: "Trips", icon: History, to: ROUTES.schoolTrips, active: active === "Trips" },
        { label: "Alerts", icon: Bell, to: ROUTES.schoolAlerts, active: active === "Alerts" },
        { label: "Subscription", icon: CreditCard, to: ROUTES.schoolSubscription, active: active === "Subscription" },
        { label: "Reports", icon: FileText, to: ROUTES.schoolReports, active: active === "Reports" },
        { label: "Profile", icon: UserCircle, to: ROUTES.schoolProfile, active: active === "Profile" },
    ]
}