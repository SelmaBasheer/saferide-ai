import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Bus, ArrowLeft } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { ROUTES } from "@/routes/paths"
import { useRegisterSchoolAdminMutation } from "@/features/auth/authApi"

// Validation rules + messages — one source of truth for the whole form.
const schema = z.object({
    firstName: z.string().min(1, "First name is required"),
    lastName: z.string().min(1, "Last name is required"),
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    phone: z.string().regex(/^[6-9]\d{9}$/, "Enter a valid 10-digit phone number"),
    password: z.string().min(8, "Password must be at least 8 characters"),
    schoolName: z.string().min(1, "School name is required"),
    schoolAddress: z.string().min(1, "Address is required"),
    city: z.string().min(1, "City is required"),
    district: z.string().min(1, "District is required"),
    state: z.string().min(1, "State is required"),
    pincode: z.string().regex(/^\d{6}$/, "Enter a valid 6-digit pincode"),
})
type FormValues = z.infer<typeof schema>

export default function RegisterPage() {
    const navigate = useNavigate()
    const [success, setSuccess] = useState(false)
    const [registerSchoolAdmin, { isLoading, error }] = useRegisterSchoolAdminMutation()

    const { register, handleSubmit, formState: { errors } } =
        useForm<FormValues>({ resolver: zodResolver(schema) })

    const onSubmit = async (values: FormValues) => {
        try {
            await registerSchoolAdmin(values).unwrap()
            setSuccess(true)
        } catch {
            /* API error shown below via `error` */
        }
    }

    if (success) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4">
                <div className="max-w-md rounded-2xl bg-white p-8 text-center shadow-xl">
                    <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100">
                        <Bus className="h-6 w-6 text-emerald-700" />
                    </div>
                    <h1 className="text-xl font-semibold text-slate-800">Registration submitted</h1>
                    <p className="mt-2 text-sm text-slate-500">
                        Your school admin account is <strong>pending approval</strong>. You'll be able to sign in
                        once SafeRide approves your school.
                    </p>
                    <Button onClick={() => navigate(ROUTES.login)} className="mt-6 w-full bg-sky-700 hover:bg-sky-800">
                        Go to sign in
                    </Button>
                </div>
            </div>
        )
    }

    return (
        <div className="min-h-screen bg-slate-100 py-10">
            <div className="mx-auto max-w-2xl px-4">
                <button onClick={() => navigate(ROUTES.home)}
                    className="mb-4 flex items-center gap-1 text-sm text-slate-500">
                    <ArrowLeft className="h-4 w-4" /> Back to home
                </button>

                <div className="overflow-hidden rounded-2xl bg-white shadow-xl">
                    <div className="flex items-center gap-2.5 bg-sky-700 px-8 py-6">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-300">
                            <Bus className="h-5 w-5 text-amber-900" />
                        </div>
                        <div>
                            <p className="font-semibold text-white">Register your school</p>
                            <p className="text-xs text-sky-200">Create a school admin account · approved by SafeRide</p>
                        </div>
                    </div>

                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-8 p-8">
                        <section>
                            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">Your details</h2>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <FormField label="First name" placeholder="Enter first name" {...register("firstName")} error={errors.firstName?.message} />
                                <FormField label="Last name" placeholder="Enter last name" {...register("lastName")} error={errors.lastName?.message} />
                                <FormField label="Email" type="email" placeholder="Enter email" {...register("email")} error={errors.email?.message} />
                                <FormField label="Phone" inputMode="numeric" placeholder="10-digit number" {...register("phone")} error={errors.phone?.message} />
                                <div className="sm:col-span-2">
                                    <FormField label="Password" type="password" placeholder="At least 8 characters" {...register("password")} error={errors.password?.message} />
                                </div>
                            </div>
                        </section>

                        <section>
                            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">School details</h2>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="sm:col-span-2">
                                    <FormField label="School name" placeholder="Enter school name" {...register("schoolName")} error={errors.schoolName?.message} />
                                </div>
                                <div className="sm:col-span-2">
                                    <FormField label="Address" placeholder="Enter school address" {...register("schoolAddress")} error={errors.schoolAddress?.message} />
                                </div>
                                <FormField label="City" placeholder="Enter city" {...register("city")} error={errors.city?.message} />
                                <FormField label="District" placeholder="Enter district" {...register("district")} error={errors.district?.message} />
                                <FormField label="State" placeholder="Enter state" {...register("state")} error={errors.state?.message} />
                                <FormField label="Pincode" inputMode="numeric" placeholder="6-digit pincode" {...register("pincode")} error={errors.pincode?.message} />
                            </div>
                        </section>

                        {error && (
                            <p className="text-sm text-red-600">Registration failed. Please check your details and try again.</p>
                        )}

                        <Button type="submit" disabled={isLoading} className="w-full bg-sky-700 hover:bg-sky-800">
                            {isLoading ? "Creating account..." : "Create account"}
                        </Button>

                        <p className="text-center text-sm text-slate-500">
                            Already have an account?{" "}
                            <Link to={ROUTES.login} className="font-medium text-sky-700">Sign in</Link>
                        </p>
                    </form>
                </div>
            </div>
        </div>
    )
}