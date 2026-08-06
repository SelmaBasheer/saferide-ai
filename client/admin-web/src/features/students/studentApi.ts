import { baseApi } from "@/lib/baseApi"
import type { ApiResponse } from "@/features/auth/authTypes"
import type { PagedResult } from "@/features/schools/schoolApi"

export type StudentStatus = "ACTIVE" | "INACTIVE"

export interface StudentListItem {
    id: string
    firstName: string
    lastName: string
    admissionNumber: string
    grade: string
    parentFirstName: string
    parentLastName: string
    parentEmail: string
    parentPhone: string
    status: StudentStatus
}

export interface CreateStudentRequest {
    firstName: string
    lastName: string
    admissionNumber: string
    grade: string
    parentFirstName: string
    parentLastName: string
    parentEmail: string
    parentPhone: string
}

export interface StudentsQueryArgs {
    search?: string
    page: number
    pageSize: number
}

export const studentApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getStudents: builder.query<PagedResult<StudentListItem>, StudentsQueryArgs>({
            query: ({ search, page, pageSize }) => ({
                url: "/students",
                params: { search: search || undefined, page, pageSize },
            }),
            transformResponse: (r: ApiResponse<PagedResult<StudentListItem>>) => r.data,
            providesTags: ["Students"],
        }),

        createStudent: builder.mutation<StudentListItem, CreateStudentRequest>({
            query: (body) => ({ url: "/students", method: "POST", body }),
            transformResponse: (r: ApiResponse<StudentListItem>) => r.data,
            invalidatesTags: ["Students"],
        }),
    }),
})

export const { useGetStudentsQuery, useCreateStudentMutation } = studentApi