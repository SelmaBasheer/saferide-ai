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
        </Routes>
    )
}