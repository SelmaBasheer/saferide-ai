export const ROUTES = {
    home: "/",
    login: "/login",
    register: "/register",
    forgotPassword: "/forgot-password",
    resetPassword: "/reset-password",
    dashboard: "/dashboard",
    superAdmin: "/super-admin",       // SuperAdmin → schools
    schoolAdmin: "/school-admin",     // SchoolAdmin → their school
    verifyEmail: "/verify-email",
    superAdminSchool: "/super-admin/schools/:id",
    schoolDrivers: "/school-admin/drivers",
    schoolStudents: "/school-admin/students",
} as const