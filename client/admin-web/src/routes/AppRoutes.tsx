import { Routes, Route } from "react-router-dom"
import { ROUTES } from "@/routes/paths"
import LandingPage from "@/pages/LandingPage"
import LoginPage from "@/pages/LoginPage"
import RegisterPage from "@/pages/RegisterPage"
import DashboardPage from "@/pages/DashboardPage"
import ProtectedRoute from "@/routes/ProtectedRoute"
import ForgotPasswordPage from "@/pages/ForgotPasswordPage"
import ResetPasswordPage from "@/pages/ResetPasswordPage"
import DashboardHome from "@/routes/DashboardHome"
import SchoolAdminDashboardPage from "@/pages/SchoolAdminDashboardPage"
import VerifyEmailPage from "@/pages/VerifyEmailPage"
import SchoolDetailPage from "@/pages/SchoolDetailPage"
import DriversPage from "@/pages/DriversPage"
import StudentsPage from "@/pages/StudentsPage"
import StudentDetailPage from "@/pages/StudentDetailPage"
import { MobileLayout } from "@/layouts/MobileLayout"
import DriverHomePage from "@/pages/driver/DriverHomePage"
import DriverTripPage from "@/pages/driver/DriverTripPage"
import ParentHomePage from "@/pages/parent/ParentHomePage"
import ParentTripPage from "@/pages/parent/ParentTripPage"
import { Users, UserCircle } from "lucide-react"
import ParentChildrenPage from "@/pages/parent/ParentChildrenPage"
import BusesPage from "@/pages/BusesPage"
import BusDetailPage from "@/pages/BusDetailPage"
import RoutesPage from "@/pages/RoutesPage"
import RouteDetailPage from "@/pages/RouteDetailPage"
import TripsPage from "@/pages/TripsPage"
import TripDetailPage from "@/pages/TripDetailPage"
import MobileTripsPage from "@/pages/MobileTripsPage"
import AlertsPage from "@/pages/AlertsPage"
import SubscriptionPage from "@/pages/SubscriptionPage"
import SuperAdminOverviewPage from "@/pages/SuperAdminOverviewPage"
import SuperAdminPlansPage from "@/pages/SuperAdminPlansPage"
import SuperAdminSubscriptionsPage from "@/pages/SuperAdminSubscriptionsPage"
import SchoolAdminProfilePage from "@/pages/SchoolAdminProfilePage"
import SuperAdminProfilePage from "@/pages/SuperAdminProfilePage"
import MobileProfilePage from "@/pages/MobileProfilePage"
import SuperAdminReportsPage from "@/pages/superadmin/SuperAdminReportsPage"
import SchoolAdminReportsPage from "@/pages/schooladmin/SchoolAdminReportsPage"
import SubscriptionGate from "@/routes/SubscriptionGate"

export default function AppRoutes() {
    return (
        <Routes>
            <Route path={ROUTES.home} element={<LandingPage />} />
            <Route path={ROUTES.login} element={<LoginPage />} />
            <Route path={ROUTES.register} element={<RegisterPage />} />
            <Route path={ROUTES.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={ROUTES.resetPassword} element={<ResetPasswordPage />} />
            <Route path={ROUTES.verifyEmail} element={<VerifyEmailPage />} />

            <Route path={ROUTES.dashboard} element={<ProtectedRoute><DashboardHome /></ProtectedRoute>} />

            {/* ---------- Super admin ---------- */}

            <Route path={ROUTES.superAdmin} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SuperAdminOverviewPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminSchools} element={
                <ProtectedRoute roles={["SuperAdmin"]}><DashboardPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminSchool} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SchoolDetailPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminPlans} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SuperAdminPlansPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminSubscriptions} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SuperAdminSubscriptionsPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminReports} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SuperAdminReportsPage /></ProtectedRoute>} />
            <Route path={ROUTES.superAdminProfile} element={
                <ProtectedRoute roles={["SuperAdmin"]}><SuperAdminProfilePage /></ProtectedRoute>} />

            {/* ---------- School admin: always reachable ---------- */}
            {/* Overview, subscription and profile stay open, or a lapsed school
                has nowhere to land and the gate below would loop. */}

            <Route path={ROUTES.schoolAdmin} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><SchoolAdminDashboardPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolSubscription} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><SubscriptionPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolProfile} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><SchoolAdminProfilePage /></ProtectedRoute>} />

            {/* ---------- School admin: needs a live subscription ---------- */}
            {/* Detail pages are gated too, or a bounced admin could still open
                one by pasting its URL. */}

            <Route path={ROUTES.schoolBuses} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><BusesPage /></SubscriptionGate>
                </ProtectedRoute>} />
            <Route path={ROUTES.schoolBusDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><BusDetailPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolRoutes} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><RoutesPage /></SubscriptionGate>
                </ProtectedRoute>} />
            <Route path={ROUTES.schoolRouteDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><RouteDetailPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolDrivers} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><DriversPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolStudents} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><StudentsPage /></SubscriptionGate>
                </ProtectedRoute>} />
            <Route path={ROUTES.schoolStudentDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><StudentDetailPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolTrips} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><TripsPage /></SubscriptionGate>
                </ProtectedRoute>} />
            <Route path={ROUTES.schoolTripDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><TripDetailPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolAlerts} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><AlertsPage /></SubscriptionGate>
                </ProtectedRoute>} />

            <Route path={ROUTES.schoolReports} element={
                <ProtectedRoute roles={["SchoolAdmin"]}>
                    <SubscriptionGate><SchoolAdminReportsPage /></SubscriptionGate>
                </ProtectedRoute>} />

            {/* ---------- Driver ---------- */}

            <Route element={
                <ProtectedRoute roles={["Driver"]}>
                    <MobileLayout
                        tripsPath={ROUTES.driverTrips}
                        items={[{ label: "Profile", icon: UserCircle, to: ROUTES.driverProfile }]}
                    />
                </ProtectedRoute>}>
                <Route path={ROUTES.driver} element={<DriverHomePage />} />
                <Route path={ROUTES.driverProfile} element={<MobileProfilePage />} />
                <Route path={ROUTES.driverTrip} element={<DriverTripPage />} />
                <Route path={ROUTES.driverTrips} element={
                    <MobileTripsPage title="Past trips" homePath={ROUTES.driver} detailPath={ROUTES.driverTrip} />} />
            </Route>

            {/* ---------- Parent ---------- */}

            <Route element={
                <ProtectedRoute roles={["Parent"]}>
                    <MobileLayout
                        tripsPath={ROUTES.parentTrips}
                        items={[
                            { label: "Your children", icon: Users, to: ROUTES.parentChildren },
                            { label: "Profile", icon: UserCircle, to: ROUTES.parentProfile },
                        ]}
                    />
                </ProtectedRoute>}>
                <Route path={ROUTES.parent} element={<ParentHomePage />} />
                <Route path={ROUTES.parentChildren} element={<ParentChildrenPage />} />
                <Route path={ROUTES.parentProfile} element={<MobileProfilePage />} />
                <Route path={ROUTES.parentTrip} element={<ParentTripPage />} />
                <Route path={ROUTES.parentTrips} element={
                    <MobileTripsPage title="Past trips" homePath={ROUTES.parent} detailPath={ROUTES.parentTrip} />} />
            </Route>
        </Routes>
    )
}