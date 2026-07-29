import type { AuthUser, UserRole } from "@/features/auth/authTypes"

const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

/** Decode + validate the JWT. Returns null if expired or missing required claims. */
export function decodeToken(token: string): AuthUser | null {
    try {
        const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")
        const payload = JSON.parse(atob(base64)) as Record<string, unknown>

        // Reject expired tokens (exp is seconds since epoch).
        const exp = payload["exp"] as number | undefined
        if (!exp || exp * 1000 <= Date.now()) return null

        const rawRole = payload[ROLE_CLAIM] ?? payload["role"] ?? payload["roles"]
        const role = (Array.isArray(rawRole) ? rawRole[0] : rawRole) as UserRole | undefined
        const email = (payload[EMAIL_CLAIM] ?? payload["email"]) as string | undefined
        const userId = (payload["sub"] ?? payload["nameid"]) as string | undefined

        // Require the claims the app depends on.
        if (!userId || !email || !role) return null

        return { userId, email, role, schoolId: payload["schoolId"] as string | undefined }
    } catch {
        return null
    }
}