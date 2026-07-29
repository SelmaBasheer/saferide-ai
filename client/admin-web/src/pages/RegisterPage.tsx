import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Bus, ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useRegisterSchoolAdminMutation } from "@/features/auth/authApi"

/** All fields the register/school-admin API expects, in one object. */
const initialForm = {
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    password: "",
    schoolName: "",
    schoolAddress: "",
    city: "",
    district: "",
    state: "",
    pincode: "",
}

export default function RegisterPage() {
    const navigate = useNavigate()
    const [form, setForm] = useState(initialForm)
    const [success, setSuccess] = useState(false)
    const [registerSchoolAdmin, { isLoading, error }] = useRegisterSchoolAdminMutation()

    // One handler updates whichever field changed, by its `name`.
    function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
        const { name, value } = e.target
        setForm((prev) => ({ ...prev, [name]: value }))
    }

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault()
        try {
            // `form` already has exactly the 11 fields the API wants.
            await registerSchoolAdmin(form).unwrap()
            setSuccess(true)
        } catch {
            // error from the hook is shown in the form below
        }
    }

    // Success screen — shown after the account is created.
    if (success) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4">
                <div className="max-w-md rounded-2xl bg-white p-8 text-center shadow-xl">
                    <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100">
                        <Bus className="h-6 w-6 text-emerald-700" />
                    </div>
                    <h1 className="text-xl font-semibold text-slate-800">Registration submitted</h1>
                    <p className="mt-2 text-sm text-slate-500">
                        Your school admin account is <strong>pending approval</strong>. You'll be
                        able to sign in once SafeRide approves your school.
                    </p>
                    <Button
                        onClick={() => navigate("/login")}
                        className="mt-6 w-full bg-sky-700 hover:bg-sky-800"
                    >
                        Go to sign in
                    </Button>
                </div>
            </div>
        )
    }

    return (
        <div className="min-h-screen bg-slate-100 py-10">
            <div className="mx-auto max-w-2xl px-4">
                {/* Back link */}
                <button
                    onClick={() => navigate("/")}
                    className="mb-4 flex items-center gap-1 text-sm text-slate-500"
                >
                    <ArrowLeft className="h-4 w-4" />
                    Back to home
                </button>

                <div className="overflow-hidden rounded-2xl bg-white shadow-xl">
                    {/* Header */}
                    <div className="flex items-center gap-2.5 bg-sky-700 px-8 py-6">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-300">
                            <Bus className="h-5 w-5 text-amber-900" />
                        </div>
                        <div>
                            <p className="font-semibold text-white">Register your school</p>
                            <p className="text-xs text-sky-200">
                                Create a school admin account · approved by SafeRide
                            </p>
                        </div>
                    </div>

                    <form onSubmit={handleSubmit} className="space-y-8 p-8">
                        {/* Section 1 */}
                        <section>
                            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                Your details
                            </h2>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <Field label="First name" name="firstName" value={form.firstName} onChange={handleChange} placeholder="Enter firstName.." />
                                <Field label="Last name" name="lastName" value={form.lastName} onChange={handleChange} placeholder="Enter lastName.." />
                                <Field label="Email" name="email" type="email" value={form.email} onChange={handleChange} placeholder="Enter email.." />
                                <Field label="Phone" name="phone" value={form.phone} onChange={handleChange} placeholder="Enter phone.." />
                                <div className="sm:col-span-2">
                                    <Field label="Password" name="password" type="password" value={form.password} onChange={handleChange} placeholder="At least 8 characters" />
                                </div>
                            </div>
                        </section>

                        {/* Section 2 */}
                        <section>
                            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                                School details
                            </h2>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="sm:col-span-2">
                                    <Field label="School name" name="schoolName" value={form.schoolName} onChange={handleChange} placeholder="Enter school name.." />
                                </div>
                                <div className="sm:col-span-2">
                                    <Field label="Address" name="schoolAddress" value={form.schoolAddress} onChange={handleChange} placeholder="Enter school address.." />
                                </div>
                                <Field label="City" name="city" value={form.city} onChange={handleChange} placeholder="Enter city.." />
                                <Field label="District" name="district" value={form.district} onChange={handleChange} placeholder="Enter district.." />
                                <Field label="State" name="state" value={form.state} onChange={handleChange} placeholder="Enter state.." />
                                <Field label="Pincode" name="pincode" value={form.pincode} onChange={handleChange} placeholder="Enter pincode.." />
                            </div>
                        </section>

                        {error && (
                            <p className="text-sm text-red-600">
                                Registration failed. Please check your details and try again.
                            </p>
                        )}

                        <Button type="submit" disabled={isLoading} className="w-full bg-sky-700 hover:bg-sky-800">
                            {isLoading ? "Creating account..." : "Create account"}
                        </Button>

                        <p className="text-center text-sm text-slate-500">
                            Already have an account?{" "}
                            <Link to="/login" className="font-medium text-sky-700">
                                Sign in
                            </Link>
                        </p>
                    </form>
                </div>
            </div>
        </div>
    )
}

/** One labelled text input. Reused for every field so the form stays clean. */
function Field({
    label,
    name,
    value,
    onChange,
    type = "text",
    placeholder,
}: {
    label: string
    name: string
    value: string
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => void
    type?: string
    placeholder?: string
}) {
    return (
        <div className="space-y-1.5">
            <Label htmlFor={name}>{label}</Label>
            <Input id={name} name={name} type={type} value={value} onChange={onChange} placeholder={placeholder} required />
        </div>
    )
}