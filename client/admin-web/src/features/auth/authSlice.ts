import { createSlice, type PayloadAction } from "@reduxjs/toolkit"
import { decodeToken } from "@/lib/jwt"
import type { AuthUser } from "./authTypes"

const TOKEN_KEY = "saferide_access_token"
const REFRESH_KEY = "saferide_refresh_token"

interface AuthState {
    accessToken: string | null
    refreshToken: string | null
    user: AuthUser | null
}

// Restore session only if the saved token is still valid; otherwise clear it.
const savedToken = localStorage.getItem(TOKEN_KEY)
const savedUser = savedToken ? decodeToken(savedToken) : null
if (savedToken && !savedUser) {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
}

const initialState: AuthState = {
    accessToken: savedUser ? savedToken : null,
    refreshToken: savedUser ? localStorage.getItem(REFRESH_KEY) : null,
    user: savedUser,
}

const authSlice = createSlice({
    name: "auth",
    initialState,
    reducers: {
        setCredentials: (
            state,
            action: PayloadAction<{ accessToken: string; refreshToken: string }>
        ) => {
            const user = decodeToken(action.payload.accessToken)
            if (!user) return // reject invalid tokens
            state.accessToken = action.payload.accessToken
            state.refreshToken = action.payload.refreshToken
            state.user = user
            localStorage.setItem(TOKEN_KEY, action.payload.accessToken)
            localStorage.setItem(REFRESH_KEY, action.payload.refreshToken)
        },
        logout: (state) => {
            state.accessToken = null
            state.refreshToken = null
            state.user = null
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem(REFRESH_KEY)
        },
    },
})

export const { setCredentials, logout } = authSlice.actions
export default authSlice.reducer