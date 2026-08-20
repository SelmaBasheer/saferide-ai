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
import { MobileLayout } from "@/layouts/MobileLayout"
import DriverHomePage from "@/pages/driver/DriverHomePage"
import DriverTripPage from "@/pages/driver/DriverTripPage"
import ParentHomePage from "@/pages/parent/ParentHomePage"
import ParentTripPage from "@/pages/parent/ParentTripPage"
import BusesPage from "@/pages/BusesPage"
import BusDetailPage from "@/pages/BusDetailPage"
import RoutesPage from "@/pages/RoutesPage"
import RouteDetailPage from "@/pages/RouteDetailPage"

export default function AppRoutes() {
    return (
        <Routes>
            <Route path={ROUTES.home} element={<LandingPage />} />
            <Route path={ROUTES.login} element={<LoginPage />} />
            <Route path={ROUTES.register} element={<RegisterPage />} />
            <Route path={ROUTES.dashboard} element={<ProtectedRoute><DashboardHome /></ProtectedRoute>} />
            <Route path={ROUTES.superAdmin} element={
                <ProtectedRoute roles={["SuperAdmin"]}><DashboardPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolAdmin} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><SchoolAdminDashboardPage /></ProtectedRoute>} />
            <Route path={ROUTES.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={ROUTES.resetPassword} element={<ResetPasswordPage />} />
            <Route path={ROUTES.verifyEmail} element={<VerifyEmailPage />} />
            <Route path={ROUTES.superAdminSchool} element={<SchoolDetailPage />} />
            <Route path={ROUTES.schoolDrivers} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><DriversPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolStudents} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><StudentsPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolBuses} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><BusesPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolBusDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><BusDetailPage /></ProtectedRoute>} />
            <Route element={
                <ProtectedRoute roles={["Driver"]}><MobileLayout /></ProtectedRoute>}>
                <Route path={ROUTES.driver} element={<DriverHomePage />} />
                <Route path={ROUTES.driverTrip} element={<DriverTripPage />} />            </Route>
            <Route element={
                <ProtectedRoute roles={["Parent"]}><MobileLayout /></ProtectedRoute>}>
                <Route path={ROUTES.parent} element={<ParentHomePage />} />
                <Route path={ROUTES.parentTrip} element={<ParentTripPage />} />
            </Route>
            <Route path={ROUTES.schoolRoutes} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><RoutesPage /></ProtectedRoute>} />
            <Route path={ROUTES.schoolRouteDetail} element={
                <ProtectedRoute roles={["SchoolAdmin"]}><RouteDetailPage /></ProtectedRoute>} />
        </Routes>
    )
}