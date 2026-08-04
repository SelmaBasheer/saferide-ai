import { baseApi } from "@/lib/baseApi"
import type {
    LoginRequest,
    RegisterRequest,
    AuthResponse,
    RegisterResponse,
    ApiResponse,
    ForgotPasswordRequest,
    ResendOtpRequest,
    ResetPasswordRequest,
    VerifyEmailRequest
} from "./authTypes"

/** Auth endpoints, injected into the base API. */
export const authApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        login: builder.mutation<AuthResponse, LoginRequest>({
            query: (body) => ({ url: "/auth/login", method: "POST", body }),
            // unwrap the envelope → components still receive a plain AuthResponse
            transformResponse: (r: ApiResponse<AuthResponse>) => r.data,
        }),
        registerSchoolAdmin: builder.mutation<RegisterResponse, RegisterRequest>({
            query: (body) => ({ url: "/auth/register/school-admin", method: "POST", body }),
            transformResponse: (r: ApiResponse<RegisterResponse>) => r.data,
        }),
        forgotPassword: builder.mutation<null, ForgotPasswordRequest>({
            query: (body) => ({ url: "/auth/forgot-password", method: "POST", body }),
            transformResponse: (r: ApiResponse<null>) => r.data,
        }),
        resendOtp: builder.mutation<null, ResendOtpRequest>({
            query: (body) => ({ url: "/auth/resend-otp", method: "POST", body }),
            transformResponse: (r: ApiResponse<null>) => r.data,
        }),
        resetPassword: builder.mutation<null, ResetPasswordRequest>({
            query: (body) => ({ url: "/auth/reset-password", method: "POST", body }),
            transformResponse: (r: ApiResponse<null>) => r.data,
        }),
        verifyEmail: builder.mutation<null, VerifyEmailRequest>({
            query: (body) => ({ url: "/auth/verify-email", method: "POST", body }),
            transformResponse: (r: ApiResponse<null>) => r.data,
        }),
        resendVerification: builder.mutation<null, ResendOtpRequest>({
            query: (body) => ({ url: "/auth/resend-verification", method: "POST", body }),
            transformResponse: (r: ApiResponse<null>) => r.data,
        }),
    }),
})

// RTK Query auto-generates these hooks from the endpoints above.
export const { useLoginMutation,
    useRegisterSchoolAdminMutation,
    useForgotPasswordMutation,
    useResendOtpMutation,
    useResetPasswordMutation,
    useVerifyEmailMutation,
    useResendVerificationMutation } = authApi