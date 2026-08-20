import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { useCreateBusMutation } from "@/features/buses/busApi"

const schema = z.object({
    registrationNumber: z.string().trim().min(1, "Registration number is required").max(32),
    model: z.string().trim().min(1, "Model is required").max(100),
    capacity: z
        .string()
        .trim()
        .min(1, "Capacity is required")
        .refine((v) => /^\d+$/.test(v), "Enter a whole number")
        .refine((v) => Number(v) >= 1 && Number(v) <= 100, "Capacity must be between 1 and 100"),
})
type FormValues = z.infer<typeof schema>

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function CreateBusForm({ onCreated }: { onCreated?: () => void }) {
    const [createBus, { isLoading, error }] = useCreateBusMutation()
    const {
        register,
        handleSubmit,
        reset,
        formState: { errors },
    } = useForm<FormValues>({ resolver: zodResolver(schema) })

    const onSubmit = async (values: FormValues) => {
        await createBus({
            registrationNumber: values.registrationNumber,
            model: values.model,
            capacity: Number(values.capacity),
        }).unwrap()
        reset()
        onCreated?.()
    }

    return (
        <form
            onSubmit={handleSubmit((v) => onSubmit(v).catch(() => { }))}
            className="rounded-lg border bg-white p-6"
        >
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Add bus
            </h2>

            <div className="grid gap-4 sm:grid-cols-3">
                <FormField
                    label="Registration number"
                    placeholder="KL-07-AB-1234"
                    error={errors.registrationNumber?.message}
                    {...register("registrationNumber")}
                />
                <FormField
                    label="Model"
                    placeholder="Tata Starbus Ultra"
                    error={errors.model?.message}
                    {...register("model")}
                />
                <FormField
                    label="Capacity"
                    type="number"
                    placeholder="42"
                    error={errors.capacity?.message}
                    {...register("capacity")}
                />
            </div>

            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {apiErrorMessage(error) ?? "Could not create the bus. Please try again."}
                </p>
            )}

            <Button className="mt-4 bg-sky-700 hover:bg-sky-800" disabled={isLoading}>
                {isLoading ? "Saving…" : "Add bus"}
            </Button>
        </form>
    )
}