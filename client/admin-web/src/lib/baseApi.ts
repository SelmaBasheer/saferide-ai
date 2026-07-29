import { createApi, fetchBaseQuery } from "@reduxjs/toolkit/query/react"
import type { RootState } from "@/app/store"

/**
 * Base API for all backend calls. Feature APIs inject their endpoints here.
 * baseUrl "/api" is forwarded to the Identity service by the Vite dev proxy.
 */
export const baseApi = createApi({
    reducerPath: "api",
    baseQuery: fetchBaseQuery({
        baseUrl: "/api",
        prepareHeaders: (headers, { getState }) => {
            // Attach the JWT to every request once logged in.
            const token = (getState() as RootState).auth.accessToken
            if (token) headers.set("Authorization", `Bearer ${token}`)
            return headers
        },
    }),
    endpoints: () => ({}),
})