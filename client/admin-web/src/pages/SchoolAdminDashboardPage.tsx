import {
    School, Bus as BusIcon, Route as RouteIcon, Users, ClipboardList,
    Clock, AlertTriangle, CheckCircle2, Ban, Send
} from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { useAppSelector } from "@/app/hooks"
import { Button } from "@/components/ui/button"
import { useGetMySchoolQuery, useSubmitSchoolMutation, type SchoolDetail } from "@/features/schools/schoolApi"
import OnboardingProfileForm from "@/features/schools/OnboardingProfileForm"
import DocumentsCard from "@/features/schools/DocumentsCard"

// ---------- Onboarding (Draft / Rejected) ----------

function SubmitCard({ school }: { school: SchoolDetail }) {
    const [submit, { isLoading, error }] = useSubmitSchoolMutation()
    const ready = school.missingRequirements.length === 0

    return (
        <div className="rounded-lg border bg-white p-6">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Submit for approval</h2>
            {ready ? (
                <p className="mt-2 text-sm text-emerald-600">
                    Everything's complete — you can submit your school for review.
                </p>
            ) : (
                <>
                    <p className="mt-2 text-sm text-slate-500">Still missing:</p>
                    <ul className="mt-2 space-y-1">
                        {school.missingRequirements.map((m) => (
                            <li key={m} className="flex items-center gap-2 text-sm text-slate-600">
                                <span className="h-1.5 w-1.5 rounded-full bg-amber-400" /> {m}
                            </li>
                        ))}
                    </ul>
                </>
            )}
            {error && <p className="mt-2 text-sm text-red-600">Submission failed. Please try again.</p>}
            <Button className="mt-4 bg-sky-700 hover:bg-sky-800"
                disabled={!ready || isLoading} onClick={() => submit()}>
                <Send className="mr-1 h-4 w-4" />
                {isLoading ? "Submitting…" : "Submit for approval"}
            </Button>
        </div>
    )
}

function OnboardingView({ school }: { school: SchoolDetail }) {
    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Complete your school profile</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Fill in the details, upload the required documents, then submit for approval.
                </p>
            </div>

            {school.status === "Rejected" && (
                <div className="flex gap-3 rounded-lg border border-red-200 bg-red-50 p-4">
                    <AlertTriangle className="h-5 w-5 shrink-0 text-red-500" />
                    <div>
                        <p className="text-sm font-medium text-red-700">Your submission was not approved</p>
                        <p className="mt-1 text-sm text-red-600">{school.rejectionReason}</p>
                        <p className="mt-1 text-xs text-red-400">
                            Update your profile or documents below and submit again.
                        </p>
                    </div>
                </div>
            )}

            <OnboardingProfileForm school={school} />
            <DocumentsCard documents={school.documents} />
            <SubmitCard school={school} />
        </div>
    )
}

// ---------- Waiting / blocked states ----------

function StatusCard({ icon, title, children }: {
    icon: React.ReactNode
    title: string
    children: React.ReactNode
}) {
    return (
        <div className="mx-auto mt-16 max-w-md rounded-2xl border bg-white p-8 text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100">
                {icon}
            </div>
            <h1 className="text-xl font-semibold text-slate-800">{title}</h1>
            <div className="mt-2 text-sm text-slate-500">{children}</div>
        </div>
    )
}

// ---------- Approved: the real dashboard ----------

function ApprovedDashboard({ school }: { school: SchoolDetail }) {
    return (
        <>
            <div className="flex items-center gap-2">
                <h1 className="text-2xl font-semibold text-slate-800">{school.name}</h1>
                <CheckCircle2 className="h-5 w-5 text-emerald-600" />
            </div>
            <p className="mt-1 text-sm text-slate-500">Your school is approved and active.</p>
            <div className="mt-6 grid gap-4 sm:grid-cols-3">
                {["Buses", "Routes", "Drivers"].map((k) => (
                    <div key={k} className="rounded-lg border bg-white p-5">
                        <p className="text-sm text-slate-500">{k}</p>
                        <p className="mt-1 text-2xl font-semibold text-slate-800">—</p>
                    </div>
                ))}
            </div>
            <p className="mt-6 text-sm text-slate-400">
                Manage your school's buses, routes, drivers and students here.
            </p>
        </>
    )
}

// ---------- The page: one component, four faces ----------

export default function SchoolAdminDashboardPage() {
    const user = useAppSelector((s) => s.auth.user)
    const { data: school, isLoading, isError } = useGetMySchoolQuery()

    return (
        <DashboardLayout roleLabel="School Admin" nav={[
            { label: "Overview", icon: School, active: true },
            { label: "Buses", icon: BusIcon },
            { label: "Routes", icon: RouteIcon },
            { label: "Drivers", icon: Users },
            { label: "Students", icon: ClipboardList },
        ]}>
            {isLoading ? (
                <p className="text-sm text-slate-500">Loading your school…</p>
            ) : isError || !school ? (
                <p className="text-sm text-red-600">
                    Could not load your school. Please refresh or contact support. ({user?.email})
                </p>
            ) : school.status === "Draft" || school.status === "Rejected" ? (
                <OnboardingView school={school} />
            ) : school.status === "Submitted" ? (
                <StatusCard icon={<Clock className="h-6 w-6 text-amber-500" />} title="Under review">
                    <p>
                        <strong>{school.name}</strong> was submitted
                        {school.submittedAtUtc && <> on {new Date(school.submittedAtUtc).toLocaleDateString()}</>}.
                        We'll email you as soon as it's reviewed.
                    </p>
                </StatusCard>
            ) : school.status === "Suspended" ? (
                <StatusCard icon={<Ban className="h-6 w-6 text-red-500" />} title="School suspended">
                    <p>Your school's access has been suspended. Contact SafeRide support for details.</p>
                </StatusCard>
            ) : (
                <ApprovedDashboard school={school} />
            )}
        </DashboardLayout>
    )
}