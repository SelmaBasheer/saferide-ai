import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"

export interface Profile {
    id: string
    email: string
    firstName: string
    lastName: string
    phone: string
    role: string
    status: string
    schoolId: string | null
    mustChangePassword: boolean
    /** A signed link, valid for an hour. Null when no photo has been uploaded. */
    photoUrl: string | null
}

export interface UpdateProfileRequest {
    firstName: string
    lastName: string
    phone: string
}

export interface ChangePasswordRequest {
    currentPassword: string
    newPassword: string
}

export const profileApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getProfile: builder.query<Profile, void>({
            query: () => ({ url: "/auth/me" }),
            transformResponse: (r: ApiResponse<Profile>) => r.data,
            providesTags: ["Profile"],
        }),

        updateProfile: builder.mutation<Profile, UpdateProfileRequest>({
            query: (body) => ({ url: "/auth/me", method: "PUT", body }),
            transformResponse: (r: ApiResponse<Profile>) => r.data,
            invalidatesTags: ["Profile"],
        }),

        changePassword: builder.mutation<void, ChangePasswordRequest>({
            query: (body) => ({ url: "/auth/change-password", method: "POST", body }),
        }),

        uploadPhoto: builder.mutation<Profile, File>({
            query: (file) => {
                const form = new FormData()
                form.append("file", file)

                // No Content-Type header. The browser has to set it so it can
                // include the multipart boundary, and setting it by hand
                // silently breaks the upload.
                return { url: "/auth/me/photo", method: "POST", body: form }
            },
            transformResponse: (r: ApiResponse<Profile>) => r.data,
            invalidatesTags: ["Profile"],
        }),

        removePhoto: builder.mutation<Profile, void>({
            query: () => ({ url: "/auth/me/photo", method: "DELETE" }),
            transformResponse: (r: ApiResponse<Profile>) => r.data,
            invalidatesTags: ["Profile"],
        }),
    }),
})

export const {
    useGetProfileQuery,
    useUpdateProfileMutation,
    useChangePasswordMutation,
    useUploadPhotoMutation,
    useRemovePhotoMutation,
} = profileApi