import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { useCreateStudentMutation } from "@/features/students/studentApi"

const schema = z.object({
    firstName: z.string().trim().min(1, "First name is required").max(75),
    lastName: z.string().trim().min(1, "Last name is required").max(75),
    admissionNumber: z.string().trim().min(1, "Admission number is required").max(30),
    grade: z.string().trim().min(1, "Grade is required").max(20),
    parentFirstName: z.string().trim().min(1, "Parent first name is required").max(75),
    parentLastName: z.string().trim().min(1, "Parent last name is required").max(75),
    parentEmail: z.string().email("Enter a valid email"),
    parentPhone: z.string().regex(/^\+?[0-9]{10,15}$/, "Enter a valid phone number"),
})
type FormValues = z.infer<typeof schema>

// The envelope's error message, if the server sent one (e.g. duplicate admission number 409).
function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function CreateStudentForm({ onCreated }: { onCreated?: () => void }) {
    const [createStudent, { isLoading, error }] = useCreateStudentMutation()
    const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
    })

    const onSubmit = async (values: FormValues) => {
        await createStudent(values).unwrap()
        reset()
        onCreated?.()
    }

    return (
        <form onSubmit={handleSubmit((v) => onSubmit(v).catch(() => { }))}
            className="rounded-lg border bg-white p-6">
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Add student
            </h2>
            <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="First name" error={errors.firstName?.message} {...register("firstName")} />
                <FormField label="Last name" error={errors.lastName?.message} {...register("lastName")} />
                <FormField label="Admission number" placeholder="GV-2026-001" error={errors.admissionNumber?.message} {...register("admissionNumber")} />
                <FormField label="Grade / class" placeholder="3A" error={errors.grade?.message} {...register("grade")} />
            </div>

            <p className="mt-6 mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Parent details
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="Parent first name" error={errors.parentFirstName?.message} {...register("parentFirstName")} />
                <FormField label="Parent last name" error={errors.parentLastName?.message} {...register("parentLastName")} />
                <FormField label="Parent email" type="email" error={errors.parentEmail?.message} {...register("parentEmail")} />
                <FormField label="Parent phone" placeholder="+919876543210" error={errors.parentPhone?.message} {...register("parentPhone")} />
            </div>

            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {apiErrorMessage(error) ?? "Could not create the student. Please try again."}
                </p>
            )}
            <Button className="mt-4 bg-sky-700 hover:bg-sky-800" disabled={isLoading}>
                {isLoading ? "Saving…" : "Add student"}
            </Button>
        </form>
    )
}