import type { AuthUser, UserRole } from "@/features/auth/authTypes"

const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

interface JwtPayload {
    sub: string
    [EMAIL_CLAIM]: string
    [ROLE_CLAIM]: string
    schoolId?: string
    exp: number
}

/** Decode the JWT payload to read the user's identity and role. */
export function decodeToken(token: string): AuthUser | null {
    try {
        const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")
        const payload = JSON.parse(atob(base64)) as JwtPayload
        return {
            userId: payload.sub,
            email: payload[EMAIL_CLAIM],
            role: payload[ROLE_CLAIM] as UserRole,
            schoolId: payload.schoolId,
        }
    } catch {
        return null
    }
}