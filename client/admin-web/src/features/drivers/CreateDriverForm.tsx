import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { useCreateDriverMutation } from "@/features/drivers/driverApi"

const schema = z.object({
    firstName: z.string().min(1, "First name is required").max(75),
    lastName: z.string().min(1, "Last name is required").max(75),
    email: z.string().email("Enter a valid email"),
    phone: z.string().regex(/^\+?[0-9]{10,15}$/, "Enter a valid phone number"),
    licenseNumber: z.string().min(1, "Licence number is required").max(50),
    licenseExpiryDate: z.string().refine(
        (d) => new Date(d) > new Date(), "Licence must not be expired"),
})
type FormValues = z.infer<typeof schema>

// The envelope's error message, if the server sent one (e.g. duplicate email 409).
function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function CreateDriverForm({ onCreated }: { onCreated?: () => void }) {
    const [createDriver, { isLoading, error }] = useCreateDriverMutation()
    const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
    })

    const onSubmit = async (values: FormValues) => {
        await createDriver(values).unwrap()
        reset()
        onCreated?.()
    }

    return (
        <form onSubmit={handleSubmit((v) => onSubmit(v).catch(() => { }))}
            className="rounded-lg border bg-white p-6">
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Add driver
            </h2>
            <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="First name" error={errors.firstName?.message} {...register("firstName")} />
                <FormField label="Last name" error={errors.lastName?.message} {...register("lastName")} />
                <FormField label="Email" type="email" error={errors.email?.message} {...register("email")} />
                <FormField label="Phone" placeholder="+919876543210" error={errors.phone?.message} {...register("phone")} />
                <FormField label="Licence number" error={errors.licenseNumber?.message} {...register("licenseNumber")} />
                <FormField label="Licence expiry" type="date" error={errors.licenseExpiryDate?.message} {...register("licenseExpiryDate")} />
            </div>
            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {apiErrorMessage(error) ?? "Could not create the driver. Please try again."}
                </p>
            )}
            <Button className="mt-4 bg-sky-700 hover:bg-sky-800" disabled={isLoading}>
                {isLoading ? "Saving…" : "Add driver"}
            </Button>
        </form>
    )
}