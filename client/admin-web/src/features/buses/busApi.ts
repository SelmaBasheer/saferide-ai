import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type BusStatus = "ACTIVE" | "INACTIVE"

export type BusDocumentType = "RC" | "INSURANCE" | "POLLUTION"

export interface BusListItem {
    id: string
    schoolId: string
    registrationNumber: string
    model: string
    capacity: number
    status: BusStatus
    assignedDriverId: string | null
    createdAt: string
    updatedAt: string
}

export interface BusDocument {
    id: string
    type: BusDocumentType
    fileName: string
    sizeBytes: number
    expiresOn: string
    expired: boolean
    uploadedAt: string
}

export interface DocumentLink {
    url: string
    expiresInSeconds: number
}

export interface CreateBusRequest {
    registrationNumber: string
    model: string
    capacity: number
}

export interface BusesQueryArgs {
    search?: string
    includeInactive?: boolean
    page: number
    pageSize: number
}

export interface UploadDocumentArgs {
    busId: string
    type: BusDocumentType
    expiresOn: string
    file: File
}

export const busApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getBus: builder.query<BusListItem, string>({
            query: (id) => ({ url: `/buses/${id}` }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            providesTags: ["Buses"],
        }),

        getBuses: builder.query<PagedResult<BusListItem>, BusesQueryArgs>({
            query: ({ search, includeInactive, page, pageSize }) => ({
                url: "/buses",
                params: { search: search || undefined, includeInactive, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<BusListItem>>) => r.data,
            providesTags: ["Buses"],
        }),

        createBus: builder.mutation<BusListItem, CreateBusRequest>({
            query: (body) => ({ url: "/buses", method: "POST", body }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        updateBus: builder.mutation<BusListItem, { id: string } & CreateBusRequest>({
            query: ({ id, ...body }) => ({ url: `/buses/${id}`, method: "PUT", body }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        assignDriver: builder.mutation<BusListItem, { id: string; driverId: string }>({
            query: ({ id, driverId }) => ({
                url: `/buses/${id}/driver`,
                method: "PUT",
                body: { driverId },
            }),
            transformResponse: (r: ApiResponse<BusListItem>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        deactivateBus: builder.mutation<void, string>({
            query: (id) => ({ url: `/buses/${id}`, method: "DELETE" }),
            invalidatesTags: ["Buses"],
        }),

        getBusDocuments: builder.query<BusDocument[], string>({
            query: (busId) => ({ url: `/buses/${busId}/documents` }),
            transformResponse: (r: ApiResponse<BusDocument[]>) => r.data,
            providesTags: ["Buses"],
        }),

        uploadBusDocument: builder.mutation<BusDocument, UploadDocumentArgs>({
            query: ({ busId, type, expiresOn, file }) => {
                const body = new FormData()
                body.append("file", file)

                return {
                    url: `/buses/${busId}/documents`,
                    method: "POST",
                    // type and expiresOn are request params on the server, so they
                    // belong in the query string rather than the form body.
                    params: { type, expiresOn },
                    // No Content-Type here on purpose. The browser sets it, because
                    // only the browser knows the multipart boundary it generated.
                    body,
                }
            },
            transformResponse: (r: ApiResponse<BusDocument>) => r.data,
            invalidatesTags: ["Buses"],
        }),

        getDocumentLink: builder.query<DocumentLink, { busId: string; documentId: string }>({
            query: ({ busId, documentId }) => ({
                url: `/buses/${busId}/documents/${documentId}/link`,
            }),
            transformResponse: (r: ApiResponse<DocumentLink>) => r.data,
        }),
    }),
})

export const {
    useGetBusQuery,
    useGetBusesQuery,
    useCreateBusMutation,
    useUpdateBusMutation,
    useAssignDriverMutation,
    useDeactivateBusMutation,
    useGetBusDocumentsQuery,
    useUploadBusDocumentMutation,
    useLazyGetDocumentLinkQuery,
} = busApi