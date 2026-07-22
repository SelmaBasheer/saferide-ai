import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { Bus, Mail, Lock, Eye, EyeOff, ArrowRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAppDispatch } from "@/app/hooks"
import { useLoginMutation } from "@/features/auth/authApi"
import { setCredentials } from "@/features/auth/authSlice"

export default function LoginPage() {
    const navigate = useNavigate()
    const dispatch = useAppDispatch()
    const [login, { isLoading, error }] = useLoginMutation()

    const [email, setEmail] = useState("")
    const [password, setPassword] = useState("")
    const [showPassword, setShowPassword] = useState(false)

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault()
        try {
            // Call the API. .unwrap() returns the data or throws on failure.
            const result = await login({ email, password }).unwrap()
            dispatch(
                setCredentials({
                    accessToken: result.accessToken,
                    refreshToken: result.refreshToken,
                })
            )
            navigate("/dashboard")
        } catch {
            // On failure, the `error` from the hook is shown in the form below.
        }
    }

    return (
        <div className="flex min-h-screen bg-slate-100">
            {/* Left brand panel — desktop only */}
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
                    <div>
                        <p className="text-lg font-semibold text-white">1.5M+</p>
                        <p className="text-xs text-sky-300">schools</p>
                    </div>
                    <div>
                        <p className="text-lg font-semibold text-white">Live</p>
                        <p className="text-xs text-sky-300">GPS tracking</p>
                    </div>
                </div>
            </aside>

            {/* Right form panel — always visible */}
            <main className="flex w-full items-center justify-center p-6 lg:w-1/2">
                <div className="w-full max-w-sm">
                    {/* Compact brand — mobile only */}
                    <div className="mb-8 flex items-center gap-2.5 lg:hidden">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-sky-700">
                            <Bus className="h-5 w-5 text-white" />
                        </div>
                        <p className="font-semibold text-slate-800">SafeRide AI</p>
                    </div>

                    <h1 className="text-xl font-semibold text-slate-800">Sign in</h1>
                    <p className="mb-6 mt-1 text-sm text-slate-500">
                        Welcome back. Enter your credentials.
                    </p>

                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="space-y-1.5">
                            <Label htmlFor="email">Email</Label>
                            <div className="relative">
                                <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                                <Input
                                    id="email"
                                    type="email"
                                    placeholder="admin@saferide.ai"
                                    className="pl-9"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    required
                                />
                            </div>
                        </div>

                        <div className="space-y-1.5">
                            <Label htmlFor="password">Password</Label>
                            <div className="relative">
                                <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                                <Input
                                    id="password"
                                    type={showPassword ? "text" : "password"}
                                    placeholder="••••••••"
                                    className="pl-9 pr-9"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    required
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowPassword(!showPassword)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400"
                                >
                                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                </button>
                            </div>
                        </div>

                        {error && (
                            <p className="text-sm text-red-600">Invalid email or password.</p>
                        )}

                        <Button
                            type="submit"
                            disabled={isLoading}
                            className="w-full bg-sky-700 hover:bg-sky-800"
                        >
                            {isLoading ? "Signing in..." : "Sign in"}
                            {!isLoading && <ArrowRight className="ml-1 h-4 w-4" />}
                        </Button>

                        <p className="text-center text-sm text-slate-500">
                            New school?{" "}
                            <span
                                onClick={() => navigate("/register")}
                                className="cursor-pointer font-medium text-sky-700"
                            >
                                Register here
                            </span>
                        </p>
                    </form>
                </div>
            </main>
        </div>
    )
}