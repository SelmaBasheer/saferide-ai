import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { useUpdateRouteMutation, type RouteListItem } from "@/features/routes/routeApi"

const schema = z.object({
    code: z.string().trim().min(1, "Route code is required").max(20),
    name: z.string().trim().min(1, "Route name is required").max(120),
})
type FormValues = z.infer<typeof schema>

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function EditRouteForm({
    route,
    onSaved,
}: {
    route: RouteListItem
    onSaved?: () => void
}) {
    const [updateRoute, { isLoading, error }] = useUpdateRouteMutation()
    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: { code: route.code, name: route.name },
    })

    const onSubmit = async (values: FormValues) => {
        await updateRoute({ id: route.id, ...values }).unwrap()
        onSaved?.()
    }

    return (
        <form
            onSubmit={handleSubmit((v) => onSubmit(v).catch(() => { }))}
            className="rounded-lg border bg-white p-6"
        >
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Edit route
            </h2>

            <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="Route code" error={errors.code?.message} {...register("code")} />
                <FormField label="Route name" error={errors.name?.message} {...register("name")} />
            </div>

            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {apiErrorMessage(error) ?? "Could not save the route."}
                </p>
            )}

            <Button className="mt-4 bg-sky-700 hover:bg-sky-800" disabled={isLoading}>
                {isLoading ? "Saving…" : "Save changes"}
            </Button>
        </form>
    )
}