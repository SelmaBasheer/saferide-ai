import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"

export type SchoolStatus = "Draft" | "Submitted" | "Approved" | "Rejected" | "Suspended"

export interface SchoolDocument {
    id: string
    type: string
    fileName: string
    contentType: string
    fileSizeBytes: number
    uploadedAtUtc: string
}

export interface SchoolDetail {
    id: string
    name: string
    address: string
    city: string
    district: string
    state: string
    pincode: string
    legalName: string | null
    board: string | null
    registrationNumber: string | null
    authorizedPersonName: string | null
    authorizedPersonDesignation: string | null
    officialPhone: string | null
    officialEmail: string | null
    busCount: string | null
    studentCount: string | null
    status: SchoolStatus
    rejectionReason: string | null
    submittedAtUtc: string | null
    rejectedAtUtc: string | null
    approvedAtUtc: string | null
    documents: SchoolDocument[]
    missingRequirements: string[]
}

// The PUT body — mirrors UpdateProfileRequest on the server.
export interface UpdateProfileRequest {
    name: string
    address: string
    city: string
    district: string
    state: string
    pincode: string
    legalName: string | null
    board: string | null
    registrationNumber: string | null
    authorizedPersonName: string | null
    authorizedPersonDesignation: string | null
    officialPhone: string | null
    officialEmail: string | null
    busCount: string | null
    studentCount: string | null
}

// List row (SuperAdmin queue) — matches the existing SchoolDto
export interface SchoolListItem {
    id: string
    name: string
    city: string
    district: string
    state: string
    pincode: string
    adminName: string
    adminEmail: string
    status: SchoolStatus
    createdAtUtc: string
}

// ---- Endpoints ----
export const schoolApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getMySchool: builder.query<SchoolDetail, void>({
            query: () => "/schools/me",
            transformResponse: (r: ApiResponse<SchoolDetail>) => r.data,
            providesTags: ["MySchool"],
        }),

        updateMyProfile: builder.mutation<unknown, UpdateProfileRequest>({
            query: (body) => ({ url: "/schools/me/profile", method: "PUT", body }),
            invalidatesTags: ["MySchool"],
        }),

        uploadDocument: builder.mutation<unknown, { file: File; documentType: string }>({
            query: ({ file, documentType }) => {
                // FormData is plain JS — multipart upload, same as Swagger's file picker
                const form = new FormData()
                form.append("file", file)
                form.append("documentType", documentType)
                return { url: "/schools/me/documents", method: "POST", body: form }
            },
            invalidatesTags: ["MySchool"],
        }),

        submitSchool: builder.mutation<unknown, void>({
            query: () => ({ url: "/schools/me/submit", method: "POST" }),
            invalidatesTags: ["MySchool"],
        }),

        // ---- SuperAdmin ----
        getSchools: builder.query<SchoolListItem[], SchoolStatus | undefined>({
            query: (status) => status ? `/schools?status=${status}` : "/schools",
            transformResponse: (r: ApiResponse<SchoolListItem[]>) => r.data,
            providesTags: ["Schools"],
        }),

        getSchoolById: builder.query<SchoolDetail, string>({
            query: (id) => `/schools/${id}`,
            transformResponse: (r: ApiResponse<SchoolDetail>) => r.data,
            providesTags: ["Schools"],
        }),

        getDocumentDownloadUrl: builder.mutation<string, { schoolId: string; documentId: string }>({
            query: ({ schoolId, documentId }) =>
                `/schools/${schoolId}/documents/${documentId}/download`,
            transformResponse: (r: ApiResponse<{ url: string }>) => r.data.url,
        }),

        approveSchool: builder.mutation<unknown, string>({
            query: (id) => ({ url: `/schools/${id}/approve`, method: "POST" }),
            invalidatesTags: ["Schools"],
        }),

        rejectSchool: builder.mutation<unknown, { id: string; reason: string }>({
            query: ({ id, reason }) => ({
                url: `/schools/${id}/reject`, method: "POST", body: { reason },
            }),
            invalidatesTags: ["Schools"],
        }),

        suspendSchool: builder.mutation<unknown, string>({
            query: (id) => ({ url: `/schools/${id}/suspend`, method: "POST" }),
            invalidatesTags: ["Schools"],
        }),
    }),
})

export const {
    useGetMySchoolQuery, useUpdateMyProfileMutation, useUploadDocumentMutation,
    useSubmitSchoolMutation, useGetSchoolsQuery, useGetSchoolByIdQuery,
    useGetDocumentDownloadUrlMutation, useApproveSchoolMutation,
    useRejectSchoolMutation, useSuspendSchoolMutation,
} = schoolApi