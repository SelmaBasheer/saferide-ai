import { useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { ArrowLeft, FileText, ExternalLink, CheckCircle2, XCircle, Ban } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { School as SchoolIcon, Users, BarChart3 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import {
    useGetSchoolByIdQuery, useGetDocumentDownloadUrlMutation,
    useApproveSchoolMutation, useRejectSchoolMutation, useSuspendSchoolMutation,
    type SchoolStatus,
} from "@/features/schools/schoolApi"

const badge: Record<SchoolStatus, string> = {
    Draft: "bg-slate-100 text-slate-600",
    Submitted: "bg-amber-100 text-amber-700",
    Approved: "bg-green-100 text-green-700",
    Rejected: "bg-red-100 text-red-700",
    Suspended: "bg-slate-200 text-slate-500",
}

// Small display helper: label + value (— for empty)
function Field({ label, value }: { label: string; value: string | null | undefined }) {
    return (
        <div>
            <p className="text-xs text-slate-400">{label}</p>
            <p className="text-sm text-slate-700">{value || "—"}</p>
        </div>
    )
}

export default function SchoolDetailPage() {
    const { id = "" } = useParams()
    const navigate = useNavigate()

    const { data: school, isLoading, isError } = useGetSchoolByIdQuery(id)
    const [getUrl] = useGetDocumentDownloadUrlMutation()
    const [approve, { isLoading: approving }] = useApproveSchoolMutation()
    const [reject, { isLoading: rejecting }] = useRejectSchoolMutation()
    const [suspend, { isLoading: suspending }] = useSuspendSchoolMutation()

    const [showReject, setShowReject] = useState(false)
    const [reason, setReason] = useState("")

    const viewDocument = async (documentId: string) => {
        try {
            const url = await getUrl({ schoolId: id, documentId }).unwrap()
            window.open(url, "_blank", "noopener")   // browser fetches straight from blob storage
        } catch { /* could add a toast here */ }
    }

    const onReject = async () => {
        if (!reason.trim()) return
        await reject({ id, reason: reason.trim() }).unwrap().catch(() => { })
        setShowReject(false)
        setReason("")
    }

    return (
        <DashboardLayout roleLabel="Super Admin" nav={[
            { label: "Schools", icon: SchoolIcon, active: true },
            { label: "Users", icon: Users },
            { label: "Reports", icon: BarChart3 },
        ]}>
            <button onClick={() => navigate(ROUTES.superAdmin)}
                className="mb-4 flex items-center gap-1 text-sm text-slate-500">
                <ArrowLeft className="h-4 w-4" /> Back to schools
            </button>

            {isLoading ? (
                <p className="text-sm text-slate-500">Loading school…</p>
            ) : isError || !school ? (
                <p className="text-sm text-red-600">Could not load this school.</p>
            ) : (
                <div className="space-y-6">
                    {/* Header */}
                    <div className="flex flex-wrap items-center justify-between gap-4">
                        <div>
                            <div className="flex items-center gap-3">
                                <h1 className="text-2xl font-semibold text-slate-800">{school.name}</h1>
                                <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${badge[school.status]}`}>
                                    {school.status}
                                </span>
                            </div>
                            {school.legalName && (
                                <p className="mt-0.5 text-sm text-slate-500">{school.legalName}</p>
                            )}
                        </div>

                        {/* Actions depend on state — same transitions as the backend */}
                        <div className="flex gap-2">
                            {school.status === "Submitted" && (
                                <>
                                    <Button disabled={approving} onClick={() => approve(id)}
                                        className="bg-emerald-600 hover:bg-emerald-700">
                                        <CheckCircle2 className="mr-1 h-4 w-4" /> Approve
                                    </Button>
                                    <Button variant="destructive" disabled={rejecting}
                                        onClick={() => setShowReject(true)}>
                                        <XCircle className="mr-1 h-4 w-4" /> Reject
                                    </Button>
                                </>
                            )}
                            {school.status === "Approved" && (
                                <Button variant="destructive" disabled={suspending} onClick={() => suspend(id)}>
                                    <Ban className="mr-1 h-4 w-4" /> Suspend
                                </Button>
                            )}
                        </div>
                    </div>

                    {/* Rejection reason input */}
                    {showReject && (
                        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
                            <p className="text-sm font-medium text-red-700">Reason for rejection</p>
                            <p className="mt-0.5 text-xs text-red-500">
                                The school admin sees this text — be specific about what to fix.
                            </p>
                            <textarea value={reason} onChange={(e) => setReason(e.target.value)}
                                rows={3} placeholder="e.g. Registration certificate is not readable — please upload a clearer scan."
                                className="mt-2 w-full rounded-md border border-red-200 bg-white p-2 text-sm outline-none focus:ring-2 focus:ring-red-300" />
                            <div className="mt-2 flex gap-2">
                                <Button size="sm" variant="destructive"
                                    disabled={!reason.trim() || rejecting} onClick={onReject}>
                                    {rejecting ? "Rejecting…" : "Confirm rejection"}
                                </Button>
                                <Button size="sm" variant="outline"
                                    onClick={() => { setShowReject(false); setReason("") }}>
                                    Cancel
                                </Button>
                            </div>
                        </div>
                    )}

                    {school.status === "Rejected" && school.rejectionReason && (
                        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                            <strong>Rejected:</strong> {school.rejectionReason}
                        </div>
                    )}

                    {/* Profile details */}
                    <div className="rounded-lg border bg-white p-6">
                        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Profile</h2>
                        <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                            <Field label="Address" value={`${school.address}, ${school.city}`} />
                            <Field label="District / State" value={`${school.district}, ${school.state}`} />
                            <Field label="Pincode" value={school.pincode} />
                            <Field label="Board" value={school.board} />
                            <Field label="Registration number" value={school.registrationNumber} />
                            <Field label="Authorized person"
                                value={school.authorizedPersonName &&
                                    `${school.authorizedPersonName} (${school.authorizedPersonDesignation ?? "—"})`} />
                            <Field label="Official phone" value={school.officialPhone} />
                            <Field label="Official email" value={school.officialEmail} />
                            <Field label="Buses / Students" value={`${school.busCount ?? "—"} / ${school.studentCount ?? "—"}`} />
                        </div>
                        {school.missingRequirements.length > 0 && (
                            <p className="mt-4 text-xs text-amber-600">
                                Incomplete: {school.missingRequirements.join(", ")}
                            </p>
                        )}
                    </div>

                    {/* Documents */}
                    <div className="rounded-lg border bg-white p-6">
                        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Documents</h2>
                        {school.documents.length === 0 ? (
                            <p className="mt-3 text-sm text-slate-400">No documents uploaded.</p>
                        ) : (
                            <div className="mt-2 divide-y">
                                {school.documents.map((d) => (
                                    <div key={d.id} className="flex items-center justify-between py-3">
                                        <div className="flex items-center gap-3">
                                            <FileText className="h-5 w-5 text-slate-300" />
                                            <div>
                                                <p className="text-sm font-medium text-slate-700">{d.type}</p>
                                                <p className="text-xs text-slate-400">{d.fileName}</p>
                                            </div>
                                        </div>
                                        <Button size="sm" variant="outline" onClick={() => viewDocument(d.id)}>
                                            <ExternalLink className="mr-1 h-4 w-4" /> View
                                        </Button>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            )}
        </DashboardLayout>
    )
}