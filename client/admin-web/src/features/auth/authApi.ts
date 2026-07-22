import { baseApi } from "@/lib/baseApi"
import type {
    LoginRequest,
    RegisterRequest,
    AuthResponse,
    RegisterResponse,
} from "./authTypes"

/** Auth endpoints, injected into the base API. */
export const authApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        login: builder.mutation<AuthResponse, LoginRequest>({
            query: (body) => ({ url: "/auth/login", method: "POST", body }),
        }),
        registerSchoolAdmin: builder.mutation<RegisterResponse, RegisterRequest>({
            query: (body) => ({ url: "/auth/register/school-admin", method: "POST", body }),
        }),
    }),
})

// RTK Query auto-generates these hooks from the endpoints above.
export const { useLoginMutation, useRegisterSchoolAdminMutation } = authApi