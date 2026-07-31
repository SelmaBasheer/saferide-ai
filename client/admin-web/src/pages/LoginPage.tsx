import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Bus, Mail, Lock, Eye, EyeOff, ArrowRight } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAppDispatch } from "@/app/hooks"
import { useLoginMutation } from "@/features/auth/authApi"
import { setCredentials } from "@/features/auth/authSlice"
import { ROUTES } from "@/routes/paths"
import type { ApiResponse } from "@/features/auth/authTypes"

const schema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    password: z.string().min(1, "Password is required"),
})
type FormValues = z.infer<typeof schema>

export default function LoginPage() {
    const navigate = useNavigate()
    const dispatch = useAppDispatch()
    const [login, { isLoading }] = useLoginMutation()
    const [statusMsg, setStatusMsg] = useState<{ text: string; pending: boolean } | null>(null)
    const [showPassword, setShowPassword] = useState(false)

    const { register, handleSubmit, formState: { errors } } =
        useForm<FormValues>({ resolver: zodResolver(schema) })

    const onSubmit = async (values: FormValues) => {
        setStatusMsg(null)
        try {
            const result = await login(values).unwrap()
            dispatch(setCredentials({ accessToken: result.accessToken, refreshToken: result.refreshToken }))
            navigate(ROUTES.dashboard)
        } catch (err) {
            const apiError = (err as { data?: ApiResponse<unknown> })?.data?.error
            if (apiError?.code === "Auth.AccountNotActive") {
                setStatusMsg({ text: "Your account is pending approval. You'll be able to sign in once an administrator approves it.", pending: true })
            } else {
                setStatusMsg({ text: apiError?.message ?? "Invalid email or password.", pending: false })
            }
        }
    }

    return (
        <div className="flex min-h-screen bg-slate-100">
            {/* Left brand panel — unchanged */}
            <aside className="hidden w-1/2 flex-col justify-between bg-sky-700 p-12 lg:flex">
                <div className="flex items-center gap-2.5">
                    <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-300">
                        <Bus className="h-5 w-5 text-amber-900" />
                    </div>
                    <div>
                        <p className="font-semibold text-white">SafeRide AI</p>
                        <p className="text-xs text-sky-200">Admin console</p>
                    </div>
                </div>
                <div>
                    <h2 className="text-2xl font-semibold leading-snug text-white">
                        Every child, every ride, tracked in real time.
                    </h2>
                    <p className="mt-3 text-sm text-sky-200">
                        Manage schools, buses, routes, and drivers from one dashboard.
                    </p>
                </div>
                <div className="flex gap-8">
                    <div><p className="text-lg font-semibold text-white">1.5M+</p><p className="text-xs text-sky-300">schools</p></div>
                    <div><p className="text-lg font-semibold text-white">Live</p><p className="text-xs text-sky-300">GPS tracking</p></div>
                </div>
            </aside>

            {/* Right form panel */}
            <main className="flex w-full items-center justify-center p-6 lg:w-1/2">
                <div className="w-full max-w-sm">
                    <div className="mb-8 flex items-center gap-2.5 lg:hidden">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-sky-700">
                            <Bus className="h-5 w-5 text-white" />
                        </div>
                        <p className="font-semibold text-slate-800">SafeRide AI</p>
                    </div>

                    <h1 className="text-xl font-semibold text-slate-800">Sign in</h1>
                    <p className="mb-6 mt-1 text-sm text-slate-500">Welcome back. Enter your credentials.</p>

                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                        {/* Email */}
                        <div className="space-y-1.5">
                            <Label htmlFor="email">Email</Label>
                            <div className="relative">
                                <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                                <Input id="email" type="email" placeholder="admin@saferide.ai"
                                    className="pl-9" {...register("email")} />
                            </div>
                            {errors.email && <p className="text-sm text-red-500">{errors.email.message}</p>}
                        </div>

                        {/* Password */}
                        <div className="space-y-1.5">
                            <div className="flex items-center justify-between">
                                <Label htmlFor="password">Password</Label>
                                <Link to={ROUTES.forgotPassword} className="text-sm font-medium text-sky-700">
                                    Forgot password?
                                </Link>
                            </div>
                            <div className="relative">
                                <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                                <Input id="password" type={showPassword ? "text" : "password"} placeholder="••••••••"
                                    className="pl-9 pr-9" {...register("password")} />
                                <button type="button" onClick={() => setShowPassword(!showPassword)}
                                    aria-label={showPassword ? "Hide password" : "Show password"}
                                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                </button>
                            </div>
                            {errors.password && <p className="text-sm text-red-500">{errors.password.message}</p>}
                        </div>

                        {statusMsg && (
                            <p className={statusMsg.pending ? "text-sm text-amber-600" : "text-sm text-red-600"}>
                                {statusMsg.text}
                            </p>
                        )}

                        <Button type="submit" disabled={isLoading} className="w-full bg-sky-700 hover:bg-sky-800">
                            {isLoading ? "Signing in..." : "Sign in"}
                            {!isLoading && <ArrowRight className="ml-1 h-4 w-4" />}
                        </Button>

                        <p className="text-center text-sm text-slate-500">
                            New school?{" "}
                            <Link to={ROUTES.register} className="font-medium text-sky-700">Register here</Link>
                        </p>
                    </form>
                </div>
            </main>
        </div>
    )
}