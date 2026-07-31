import { useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useNavigate, useLocation, Link } from "react-router-dom"
import { useResetPasswordMutation, useResendOtpMutation } from "@/features/auth/authApi"
import { ROUTES } from "@/routes/paths"
import { FormField } from "@/components/ui/form-field"
import { Button } from "@/components/ui/button"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"

const schema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    otp: z.string().min(4, "Enter the OTP"),
    newPassword: z.string().min(8, "At least 8 characters"),
    confirmPassword: z.string(),
}).refine((v) => v.newPassword === v.confirmPassword, {
    message: "Passwords do not match", path: ["confirmPassword"],
})
type FormValues = z.infer<typeof schema>

export default function ResetPasswordPage() {
    const navigate = useNavigate()
    const location = useLocation()
    const prefillEmail = (location.state as { email?: string } | null)?.email ?? ""
    const [resetPassword, { isLoading }] = useResetPasswordMutation()
    const [resendOtp, { isLoading: resending }] = useResendOtpMutation()
    const [banner, setBanner] = useState<string | null>(null)

    const { register, handleSubmit, getValues, formState: { errors } } =
        useForm<FormValues>({
            resolver: zodResolver(schema),
            defaultValues: { email: prefillEmail, otp: "", newPassword: "", confirmPassword: "" },
        })

    const onSubmit = async (values: FormValues) => {
        setBanner(null)
        try {
            await resetPassword({ email: values.email, otp: values.otp, newPassword: values.newPassword }).unwrap()
            navigate(ROUTES.login)
        } catch (e) {
            const msg = (e as { data?: { error?: { message?: string } } })?.data?.error?.message
            setBanner(msg ?? "Invalid or expired OTP.")
        }
    }

    const handleResend = async () => {
        setBanner(null)
        try {
            await resendOtp({ email: getValues("email") }).unwrap()
            setBanner("A new OTP has been sent.")
        } catch (e) {
            const msg = (e as { data?: { error?: { message?: string } } })?.data?.error?.message
            setBanner(msg ?? "Please wait before requesting another OTP.")
        }
    }

    return (
        <div className="flex min-h-screen items-center justify-center p-4">
            <Card className="w-full max-w-sm">
                <CardHeader><CardTitle>Reset password</CardTitle></CardHeader>
                <CardContent>
                    {banner && <p className="mb-3 text-center text-sm">{banner}</p>}
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                        <FormField label="Email" type="email" {...register("email")} error={errors.email?.message} />
                        <FormField label="OTP" inputMode="numeric" {...register("otp")} error={errors.otp?.message} />
                        <FormField label="New password" type="password" {...register("newPassword")} error={errors.newPassword?.message} />
                        <FormField label="Confirm password" type="password" {...register("confirmPassword")} error={errors.confirmPassword?.message} />
                        <Button type="submit" className="w-full" disabled={isLoading}>
                            {isLoading ? "Resetting…" : "Reset password"}
                        </Button>
                        <Button type="button" variant="outline" className="w-full" onClick={handleResend} disabled={resending}>
                            {resending ? "Sending…" : "Resend OTP"}
                        </Button>
                        <p className="text-center text-sm">
                            <Link to={ROUTES.login} className="underline">Back to login</Link>
                        </p>
                    </form>
                </CardContent>
            </Card>
        </div>
    )
}