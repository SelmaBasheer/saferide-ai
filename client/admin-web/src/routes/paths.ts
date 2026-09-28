export const ROUTES = {
    home: "/",
    login: "/login",
    register: "/register",
    forgotPassword: "/forgot-password",
    resetPassword: "/reset-password",
    dashboard: "/dashboard",
    superAdmin: "/super-admin",       // SuperAdmin → overview
    schoolAdmin: "/school-admin",     // SchoolAdmin → their school
    verifyEmail: "/verify-email",
    superAdminSchools: "/super-admin/schools",
    superAdminSchool: "/super-admin/schools/:id",
    superAdminPlans: "/super-admin/plans",
    superAdminSubscriptions: "/super-admin/subscriptions",
    schoolDrivers: "/school-admin/drivers",
    schoolStudents: "/school-admin/students",
    // driver (mobile)
    driver: "/driver",
    driverTrip: "/driver/trip/:id",
    driverTrips: "/driver/trips",

    // parent (mobile)
    parent: "/parent",
    parentTrip: "/parent/trip/:id",
    parentTrips: "/parent/trips",
    parentChildren: "/parent/children",

    // school admin (web)
    schoolFleet: "/school-admin/fleet",
    schoolBuses: "/school-admin/buses",
    schoolBusDetail: "/school-admin/buses/:id",
    schoolRoutes: "/school-admin/routes",
    schoolRouteDetail: "/school-admin/routes/:id",
    schoolStudentDetail: "/school-admin/students/:id",
    schoolTrips: "/school-admin/trips",
    schoolTripDetail: "/school-admin/trips/:id",
    schoolAlerts: "/school-admin/alerts",
    schoolSubscription: "/school-admin/subscription",

    // profile settings
    superAdminProfile: "/super-admin/profile",
    schoolProfile: "/school-admin/profile",
    driverProfile: "/driver/profile",
    parentProfile: "/parent/profile",

    //reports
    superAdminReports: "/super-admin/reports",
} as const