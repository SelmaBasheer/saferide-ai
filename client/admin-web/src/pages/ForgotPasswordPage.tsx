import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useNavigate, Link } from "react-router-dom"
import { useForgotPasswordMutation } from "@/features/auth/authApi"
import { ROUTES } from "@/routes/paths"
import { FormField } from "@/components/ui/form-field"
import { Button } from "@/components/ui/button"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"

const schema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
})
type FormValues = z.infer<typeof schema>

export default function ForgotPasswordPage() {
    const navigate = useNavigate()
    const [forgotPassword, { isLoading }] = useForgotPasswordMutation()
    const { register, handleSubmit, formState: { errors } } =
        useForm<FormValues>({ resolver: zodResolver(schema) })

    const onSubmit = async (values: FormValues) => {
        await forgotPassword(values).unwrap()               // always 200 (anti-enumeration)
        navigate(ROUTES.resetPassword, { state: { email: values.email } })
    }

    return (
        <div className="flex min-h-screen items-center justify-center p-4">
            <Card className="w-full max-w-sm">
                <CardHeader><CardTitle>Forgot password</CardTitle></CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                        <FormField label="Email" type="email" placeholder="you@example.com"
                            {...register("email")} error={errors.email?.message} />
                        <Button type="submit" className="w-full" disabled={isLoading}>
                            {isLoading ? "Sending…" : "Send OTP"}
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