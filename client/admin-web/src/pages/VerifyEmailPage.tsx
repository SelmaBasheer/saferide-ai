import { useEffect, useState } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import { MailCheck } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { ROUTES } from "@/routes/paths"
import { useVerifyEmailMutation, useResendVerificationMutation } from "@/features/auth/authApi"

const schema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    otp: z.string().regex(/^\d{6}$/, "Enter the 6-digit code"),
})
type FormValues = z.infer<typeof schema>

export default function VerifyEmailPage() {
    const navigate = useNavigate()
    // Email handed over from the register page (may be absent on direct visits)
    const location = useLocation()
    const presetEmail = (location.state as { email?: string } | null)?.email ?? ""

    const [verifyEmail, { isLoading, error }] = useVerifyEmailMutation()
    const [resend, { isLoading: resending }] = useResendVerificationMutation()
    const [cooldown, setCooldown] = useState(0)
    const [resent, setResent] = useState(false)

    // Tick the resend cooldown down once per second
    useEffect(() => {
        if (cooldown <= 0) return
        const t = setTimeout(() => setCooldown((c) => c - 1), 1000)
        return () => clearTimeout(t)
    }, [cooldown])

    const { register, handleSubmit, getValues, formState: { errors } } =
        useForm<FormValues>({
            resolver: zodResolver(schema),
            defaultValues: { email: presetEmail, otp: "" },
        })

    const onSubmit = async (values: FormValues) => {
        try {
            await verifyEmail(values).unwrap()
            navigate(ROUTES.login, { state: { verified: true } })
        } catch { /* error rendered below */ }
    }

    const onResend = async () => {
        const email = getValues("email")
        if (!email) return
        try {
            await resend({ email }).unwrap()
            setResent(true)
            setCooldown(60)
        } catch { /* silent-by-design backend; nothing useful to show */ }
    }

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4">
            <div className="w-full max-w-md rounded-2xl bg-white p-8 shadow-xl">
                <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-sky-100">
                    <MailCheck className="h-6 w-6 text-sky-700" />
                </div>
                <h1 className="text-center text-xl font-semibold text-slate-800">Verify your email</h1>
                <p className="mt-2 text-center text-sm text-slate-500">
                    We sent a 6-digit code to your email. Enter it below to activate your account.
                </p>

                <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-4">
                    <FormField label="Email" error={errors.email?.message}
                        type="email" placeholder="you@school.edu.in" {...register("email")} />

                    <FormField label="Verification code" error={errors.otp?.message}
                        inputMode="numeric" maxLength={6} placeholder="123456" {...register("otp")} />

                    {error && (
                        <p className="text-sm text-red-600">
                            Invalid or expired code. Check the code or request a new one.
                        </p>
                    )}
                    {resent && cooldown > 0 && (
                        <p className="text-sm text-emerald-600">If the account exists, a new code was sent.</p>
                    )}

                    <Button type="submit" disabled={isLoading} className="w-full bg-sky-700 hover:bg-sky-800">
                        {isLoading ? "Verifying…" : "Verify email"}
                    </Button>
                </form>

                <button type="button" onClick={onResend}
                    disabled={resending || cooldown > 0}
                    className="mt-4 w-full text-sm text-sky-700 disabled:text-slate-400">
                    {cooldown > 0 ? `Resend code in ${cooldown}s` : "Resend code"}
                </button>
            </div>
        </div>
    )
}