import { useNavigate } from "react-router-dom"
import { Bus, MapPin, Map, Bell, ClipboardCheck, ArrowRight } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function LandingPage() {
    const navigate = useNavigate()

    return (
        <div className="min-h-screen bg-white">
            {/* Nav */}
            <header className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
                <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sky-700">
                        <Bus className="h-4 w-4 text-white" />
                    </div>
                    <span className="font-semibold text-slate-800">SafeRide AI</span>
                </div>
                <div className="flex items-center gap-3 sm:gap-5">
                    <a href="#features" className="hidden text-sm text-slate-600 sm:block">Features</a>
                    <button
                        onClick={() => navigate("/login")}
                        className="text-sm font-medium text-sky-700"
                    >
                        Sign in
                    </button>
                    <Button
                        onClick={() => navigate("/login")}
                        className="bg-sky-700 hover:bg-sky-800"
                    >
                        Get started
                    </Button>
                </div>
            </header>

            {/* Hero */}
            <section className="mx-auto max-w-2xl px-6 py-20 text-center">
                <div className="mb-5 inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-3 py-1.5">
                    <MapPin className="h-3.5 w-3.5 text-amber-800" />
                    <span className="text-xs text-amber-800">Real-time GPS for school buses</span>
                </div>
                <h1 className="text-4xl font-semibold leading-tight text-slate-900 sm:text-5xl">
                    Every child's ride, tracked in real time.
                </h1>
                <p className="mx-auto mt-5 max-w-md text-slate-500">
                    Live tracking, instant arrival alerts, and attendance — one platform
                    for schools, drivers, and parents.
                </p>
                <div className="mt-8 flex justify-center gap-3">
                    {/* TODO: point to /register once the registration page is built */}
                    <Button
                        onClick={() => navigate("/register")}
                        className="bg-sky-700 hover:bg-sky-800"
                    >
                        Register your school
                        <ArrowRight className="ml-1 h-4 w-4" />
                    </Button>
                    <Button variant="outline" onClick={() => navigate("/login")}>
                        Sign in
                    </Button>
                </div>
            </section>

            {/* Features */}
            <section id="features" className="mx-auto max-w-4xl px-6 pb-24">
                <div className="grid gap-4 sm:grid-cols-3">
                    <FeatureCard
                        icon={<Map className="h-5 w-5 text-sky-700" />}
                        tint="bg-sky-100"
                        title="Live tracking"
                        desc="See the bus move on a map, updated every few seconds."
                    />
                    <FeatureCard
                        icon={<Bell className="h-5 w-5 text-amber-800" />}
                        tint="bg-amber-100"
                        title="Instant alerts"
                        desc="Parents get a notification when the bus is arriving."
                    />
                    <FeatureCard
                        icon={<ClipboardCheck className="h-5 w-5 text-emerald-700" />}
                        tint="bg-emerald-100"
                        title="Attendance"
                        desc="Automatic boarding and drop-off records per student."
                    />
                </div>
            </section>

            {/* Footer */}
            <footer className="border-t border-slate-200 px-6 py-6 text-center text-sm text-slate-400">
                © 2026 SafeRide AI · Built for schools across India
            </footer>
        </div>
    )
}

/** A single feature card in the landing grid. */
function FeatureCard({
    icon,
    tint,
    title,
    desc,
}: {
    icon: React.ReactNode
    tint: string
    title: string
    desc: string
}) {
    return (
        <div className="rounded-xl border border-slate-200 p-5">
            <div className={`mb-3 flex h-9 w-9 items-center justify-center rounded-lg ${tint}`}>
                {icon}
            </div>
            <p className="mb-1 font-medium text-slate-800">{title}</p>
            <p className="text-sm text-slate-500">{desc}</p>
        </div>
    )
}