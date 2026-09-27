import {
    createApi, fetchBaseQuery,
    type BaseQueryFn, type FetchArgs, type FetchBaseQueryError,
} from "@reduxjs/toolkit/query/react"
import { setCredentials, logout } from "@/features/auth/authSlice"

/**
 * Only the slice this file reads, declared locally rather than imported from
 * the store. store.ts imports this file, so importing RootState back from it
 * makes a cycle — which TypeScript resolves to `unknown` once the API's
 * inferred types grow past a certain size.
 */
interface AuthState {
    auth: {
        accessToken: string | null
        refreshToken: string | null
    }
}

// Raw client — attaches the JWT to every request ("request interceptor").
const rawBaseQuery = fetchBaseQuery({
    baseUrl: "/api",
    prepareHeaders: (headers, { getState }) => {
        const token = (getState() as AuthState).auth.accessToken
        if (token) headers.set("Authorization", `Bearer ${token}`)
        return headers
    },
})

// Wrapper — the "response interceptor": on 401, refresh once, then retry the original request.
const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> =
    async (args, api, extraOptions) => {
        let result = await rawBaseQuery(args, api, extraOptions)

        if (result.error?.status === 401) {
            const refreshToken = (api.getState() as AuthState).auth.refreshToken
            if (!refreshToken) {
                api.dispatch(logout())
                return result
            }

            // Try to get a fresh token pair.
            const refreshResult = await rawBaseQuery(
                { url: "/auth/refresh", method: "POST", body: { refreshToken } },
                api, extraOptions,
            )

            const data = (refreshResult.data as
                { data?: { accessToken: string; refreshToken: string } } | undefined)?.data

            if (data?.accessToken) {
                // Save new tokens, then retry the original request.
                api.dispatch(setCredentials({ accessToken: data.accessToken, refreshToken: data.refreshToken }))
                result = await rawBaseQuery(args, api, extraOptions)
            } else {
                api.dispatch(logout())   // refresh failed - session is over
            }
        }

        return result
    }

export const baseApi = createApi({
    reducerPath: "api",
    tagTypes: ["Schools", "MySchool", "Drivers", "Students", "Trips", "Routes", "Buses", "Alerts", "Plans", "Subscription", "Profile"],
    baseQuery: baseQueryWithReauth,   //wrapper instead of raw fetchBaseQuery
    endpoints: () => ({}),
})