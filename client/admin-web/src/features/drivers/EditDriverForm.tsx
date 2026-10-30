import { useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import ConfirmDialog from "@/components/ui/confirm-dialog"
import {
    useDeactivateDriverMutation,
    useReactivateDriverMutation,
    useUpdateDriverMutation,
    type DriverListItem,
} from "@/features/drivers/driverApi"

/**
 * Unlike the create form, there is no "must not be expired" rule. An admin
 * correcting a record has to be able to enter a date that has already passed —
 * refusing it would leave the system holding a licence date it knows is wrong.
 * The form warns instead, and the driver simply can't be assigned to a bus.
 */
const schema = z.object({
    firstName: z.string().trim().min(1, "First name is required").max(75),
    lastName: z.string().trim().min(1, "Last name is required").max(75),
    phone: z.string().regex(/^\+?[0-9]{10,15}$/, "Enter a valid phone number"),
    licenseNumber: z.string().trim().min(1, "Licence number is required").max(50),
    licenseExpiryDate: z.string().min(1, "Licence expiry is required"),
})
type FormValues = z.infer<typeof schema>

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

function isPast(iso: string): boolean {
    if (!iso) return false
    const [y, m, d] = iso.split("-").map(Number)
    const expiry = new Date(y, m - 1, d)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return expiry < today
}

export default function EditDriverForm({
    driver,
    onSaved,
    onCancel,
    onStatusChanged,
}: {
    driver: DriverListItem
    onSaved?: () => void
    onCancel?: () => void
    onStatusChanged?: (message: string) => void
}) {
    const [updateDriver, { isLoading, error }] = useUpdateDriverMutation()
    const [deactivateDriver, { isLoading: deactivating }] = useDeactivateDriverMutation()
    const [reactivateDriver, { isLoading: reactivating }] = useReactivateDriverMutation()

    const [confirmOpen, setConfirmOpen] = useState(false)
    const [statusError, setStatusError] = useState<string | null>(null)

    const { register, handleSubmit, watch, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: {
            firstName: driver.firstName,
            lastName: driver.lastName,
            phone: driver.phone,
            licenseNumber: driver.licenseNumber,
            licenseExpiryDate: driver.licenseExpiryDate,
        },
    })

    const expiryWarning = isPast(watch("licenseExpiryDate"))
    const isActive = driver.status === "Active"
    const fullName = `${driver.firstName} ${driver.lastName}`

    const onSubmit = async (values: FormValues) => {
        await updateDriver({ id: driver.id, ...values }).unwrap()
        onSaved?.()
    }

    const onConfirmStatus = async () => {
        setStatusError(null)
        try {
            if (isActive) {
                await deactivateDriver(driver.id).unwrap()
                onStatusChanged?.(`${fullName} deactivated.`)
            } else {
                await reactivateDriver(driver.id).unwrap()
                onStatusChanged?.(`${fullName} reactivated.`)
            }
            setConfirmOpen(false)
        } catch (e) {
            setConfirmOpen(false)
            setStatusError(apiErrorMessage(e) ?? "Could not change this driver's status.")
        }
    }

    return (
        <div className="rounded-lg border bg-white p-4 sm:p-6">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
                <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                    Edit {fullName}
                </h2>
                <span className={`rounded-full px-2 py-0.5 text-xs font-medium
                    ${isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>
                    {driver.status}
                </span>
            </div>

            <form onSubmit={handleSubmit((v) => onSubmit(v).catch(() => { }))}>
                <div className="grid gap-4 sm:grid-cols-2">
                    <FormField label="First name" error={errors.firstName?.message} {...register("firstName")} />
                    <FormField label="Last name" error={errors.lastName?.message} {...register("lastName")} />
                    <FormField label="Phone" placeholder="+919876543210" error={errors.phone?.message} {...register("phone")} />
                    <FormField label="Licence number" error={errors.licenseNumber?.message} {...register("licenseNumber")} />
                    <FormField label="Licence expiry" type="date" error={errors.licenseExpiryDate?.message} {...register("licenseExpiryDate")} />

                    <div className="sm:col-span-2">
                        <p className="text-xs text-slate-500">
                            Email is {driver.email} and cannot be changed here — it is the driver's
                            sign-in address.
                        </p>
                    </div>
                </div>

                {expiryWarning && (
                    <p className="mt-3 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                        This licence has already expired. The driver stays on your list but cannot be
                        assigned to a bus or start a trip until it is renewed.
                    </p>
                )}

                {error && (
                    <p className="mt-3 text-sm text-red-600">
                        {apiErrorMessage(error) ?? "Could not save the changes. Please try again."}
                    </p>
                )}

                <div className="mt-4 flex flex-wrap gap-2">
                    <Button className="bg-sky-700 hover:bg-sky-800" disabled={isLoading}>
                        {isLoading ? "Saving…" : "Save changes"}
                    </Button>
                    <Button type="button" variant="outline" onClick={onCancel}>
                        Cancel
                    </Button>
                </div>
            </form>

            {/* Employment status sits apart from the detail fields on purpose.
                It is not a value you type and save with the rest — it is a
                decision with its own confirmation, and mixing it into the form
                would make it something you could change by accident. */}
            <div className="mt-6 border-t border-slate-200 pt-5">
                <h3 className="text-sm font-semibold text-slate-700">Employment status</h3>

                <p className="mt-1 text-sm text-slate-500">
                    {isActive
                        ? "This driver can sign in, be assigned to a bus, and start trips — provided their licence is valid."
                        : "This driver cannot sign in to drive, be assigned to a bus, or start a trip."}
                </p>

                {statusError && <p className="mt-2 text-sm text-red-600">{statusError}</p>}

                <Button
                    type="button"
                    variant={isActive ? "destructive" : "outline"}
                    className={`mt-3 ${isActive ? "" : "border-emerald-300 text-emerald-700 hover:bg-emerald-50"}`}
                    disabled={deactivating || reactivating}
                    onClick={() => setConfirmOpen(true)}
                >
                    {isActive
                        ? deactivating ? "Deactivating…" : "Deactivate driver"
                        : reactivating ? "Reactivating…" : "Reactivate driver"}
                </Button>
            </div>

            <ConfirmDialog
                open={confirmOpen}
                destructive={isActive}
                busy={deactivating || reactivating}
                title={isActive ? `Deactivate ${fullName}?` : `Reactivate ${fullName}?`}
                description={
                    isActive
                        ? "They stay in your records, but cannot sign in to drive, be assigned to a bus, or start a trip. If they are currently assigned to a bus, assign a replacement on the bus page."
                        : "They can sign in and be assigned to a bus again, provided their licence is still valid."
                }
                confirmLabel={isActive ? "Deactivate" : "Reactivate"}
                onConfirm={onConfirmStatus}
                onCancel={() => setConfirmOpen(false)}
            />
        </div>
    )
}