/** Request/response shapes — mirror the Identity API contract exactly. */

export interface LoginRequest {
    email: string
    password: string
}

export interface RegisterRequest {
    firstName: string
    lastName: string
    email: string
    phone: string
    password: string
    schoolName: string
    schoolAddress: string
    city: string
    district: string
    state: string
    pincode: string
}

/** Both login and refresh return this. `expiresIn` is an ISO UTC timestamp. */
export interface AuthResponse {
    accessToken: string
    refreshToken: string
    expiresIn: string
}

export interface RegisterResponse {
    userId: string
}

export type UserRole = "SuperAdmin" | "SchoolAdmin" | "Driver" | "Parent"

/** Decoded from the JWT — the app's idea of the current user. */
export interface AuthUser {
    userId: string
    email: string
    role: UserRole
    schoolId?: string
}

/** The standard envelope every endpoint now returns. */
export interface ApiResponse<T> {
    success: boolean
    data: T
    message: string | null
    error: { code: string; message: string } | null
}

export interface ForgotPasswordRequest {
    email: string
}

export interface ResendOtpRequest {
    email: string
}

export interface ResetPasswordRequest {
    email: string;
    otp: string;
    newPassword: string
}