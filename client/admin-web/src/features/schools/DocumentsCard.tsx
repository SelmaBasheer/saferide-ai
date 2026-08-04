import { useRef, useState } from "react"
import { FileText, Upload, CheckCircle2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useUploadDocumentMutation, type SchoolDocument } from "@/features/schools/schoolApi"

// One row per document type — mirrors the backend's DocumentType enum.
// `required` mirrors the completeness rule (RegistrationCertificate + AdminIdProof).
const DOC_TYPES = [
    { type: "RegistrationCertificate", label: "Registration certificate", required: true },
    { type: "AdminIdProof", label: "Admin ID proof", required: true },
    { type: "AffiliationCertificate", label: "Affiliation certificate", required: false },
    { type: "AddressProof", label: "Address proof", required: false },
] as const

const MAX_BYTES = 5 * 1024 * 1024
const ALLOWED = ["application/pdf", "image/jpeg", "image/png"]

const formatSize = (bytes: number) =>
    bytes >= 1024 * 1024 ? `${(bytes / (1024 * 1024)).toFixed(1)} MB` : `${Math.ceil(bytes / 1024)} KB`

function DocumentRow({ docType, label, required, uploaded }: {
    docType: string
    label: string
    required: boolean
    uploaded: SchoolDocument | undefined
}) {
    const [upload, { isLoading }] = useUploadDocumentMutation()
    const [localError, setLocalError] = useState<string | null>(null)
    // Ref to the hidden <input type="file"> so the styled button can open it
    const fileInput = useRef<HTMLInputElement>(null)

    const onFilePicked = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0]
        e.target.value = ""              // allow re-picking the same file later
        if (!file) return

        // Client-side pre-checks mirror the server rules — instant feedback,
        // but the server remains the real enforcer.
        if (!ALLOWED.includes(file.type)) {
            setLocalError("Only PDF, JPEG, or PNG files are allowed.")
            return
        }
        if (file.size > MAX_BYTES) {
            setLocalError("File must be 5 MB or smaller.")
            return
        }

        setLocalError(null)
        try {
            await upload({ file, documentType: docType }).unwrap()
        } catch {
            setLocalError("Upload failed. Please try again.")
        }
    }

    return (
        <div className="flex items-center justify-between gap-4 py-3">
            <div className="flex items-center gap-3">
                {uploaded
                    ? <CheckCircle2 className="h-5 w-5 shrink-0 text-emerald-600" />
                    : <FileText className="h-5 w-5 shrink-0 text-slate-300" />}
                <div>
                    <p className="text-sm font-medium text-slate-700">
                        {label}
                        {required && <span className="ml-1 text-red-500">*</span>}
                    </p>
                    {uploaded ? (
                        <p className="text-xs text-slate-400">
                            {uploaded.fileName} · {formatSize(uploaded.fileSizeBytes)}
                        </p>
                    ) : (
                        <p className="text-xs text-slate-400">Not uploaded yet</p>
                    )}
                    {localError && <p className="text-xs text-red-600">{localError}</p>}
                </div>
            </div>

            <input ref={fileInput} type="file" className="hidden"
                accept=".pdf,.jpg,.jpeg,.png" onChange={onFilePicked} />
            <Button type="button" variant="outline" size="sm" disabled={isLoading}
                onClick={() => fileInput.current?.click()}>
                <Upload className="mr-1 h-4 w-4" />
                {isLoading ? "Uploading…" : uploaded ? "Replace" : "Upload"}
            </Button>
        </div>
    )
}

export default function DocumentsCard({ documents }: { documents: SchoolDocument[] }) {
    return (
        <div className="rounded-lg border bg-white p-6">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                Verification documents
            </h2>
            <p className="mt-1 text-xs text-slate-400">
                PDF, JPEG, or PNG · max 5 MB · <span className="text-red-500">*</span> required for submission
            </p>
            <div className="mt-2 divide-y">
                {DOC_TYPES.map((d) => (
                    <DocumentRow key={d.type} docType={d.type} label={d.label} required={d.required}
                        uploaded={documents.find((doc) => doc.type === d.type)} />
                ))}
            </div>
        </div>
    )
}