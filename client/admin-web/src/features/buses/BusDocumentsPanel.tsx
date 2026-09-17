import { useState } from "react"
import { Download, Upload } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
    useGetBusDocumentsQuery,
    useLazyGetDocumentLinkQuery,
    useUploadBusDocumentMutation,
    type BusDocument,
    type BusDocumentType,
} from "@/features/buses/busApi"

const DOCUMENT_TYPES: { value: BusDocumentType; label: string }[] = [
    { value: "RC", label: "Registration certificate" },
    { value: "INSURANCE", label: "Insurance" },
    { value: "POLLUTION", label: "Pollution certificate" },
]

const MAX_SIZE_BYTES = 5 * 1024 * 1024

const labelFor = (type: BusDocumentType) =>
    DOCUMENT_TYPES.find((t) => t.value === type)?.label ?? type

const formatSize = (bytes: number) =>
    bytes < 1024 * 1024 ? `${Math.round(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`

const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" })

function apiErrorMessage(error: unknown): string | undefined {
    return (error as { data?: { error?: { message?: string } } } | undefined)?.data?.error?.message
}

export default function BusDocumentsPanel({
    busId,
    canUpload,
}: {
    busId: string
    canUpload: boolean
}) {
    const { data: documents = [], isLoading } = useGetBusDocumentsQuery(busId, { skip: !busId })
    const [uploadDocument, { isLoading: uploading }] = useUploadBusDocumentMutation()
    const [fetchLink] = useLazyGetDocumentLinkQuery()

    const [type, setType] = useState<BusDocumentType>("RC")
    const [expiresOn, setExpiresOn] = useState("")
    const [file, setFile] = useState<File | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [showHistory, setShowHistory] = useState(false)

    // The list arrives newest first, so the first of each type is the current one.
    // Older uploads are superseded, not deleted — an expired certificate still
    // answers "was this bus insured in March?"
    const current = new Map<BusDocumentType, BusDocument>()
    for (const document of documents) {
        if (!current.has(document.type)) current.set(document.type, document)
    }

    const currentIds = new Set([...current.values()].map((d) => d.id))
    const superseded = documents.filter((d) => !currentIds.has(d.id))

    const currentDocuments = DOCUMENT_TYPES.map(({ value }) => current.get(value)).filter(
        (d): d is BusDocument => d !== undefined
    )

    const tomorrow = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)

    const onUpload = async () => {
        setError(null)

        if (!file) {
            setError("Choose a file first.")
            return
        }

        if (file.size > MAX_SIZE_BYTES) {
            setError("The file must be 5 MB or smaller.")
            return
        }

        if (!expiresOn) {
            setError("Enter the expiry date.")
            return
        }

        try {
            await uploadDocument({ busId, type, expiresOn, file }).unwrap()
            setFile(null)
            setExpiresOn("")
        } catch (e) {
            setError(apiErrorMessage(e) ?? "Could not upload that document.")
        }
    }

    const onDownload = async (documentId: string) => {
        setError(null)

        try {
            // The link is fetched on demand and lasts five minutes, so nothing
            // long-lived is ever rendered into the page.
            const link = await fetchLink({ busId, documentId }).unwrap()
            window.open(link.url, "_blank", "noopener,noreferrer")
        } catch (e) {
            setError(apiErrorMessage(e) ?? "Could not open that document.")
        }
    }

    const row = (document: BusDocument, muted: boolean) => (
        <div
            key={document.id}
            className={`flex flex-wrap items-center gap-3 border-t border-slate-100 py-2 text-sm ${muted ? "text-slate-400" : ""
                }`}
        >
            <span className={`min-w-52 font-medium ${muted ? "text-slate-400" : "text-slate-700"}`}>
                {labelFor(document.type)}
            </span>
            <span className="flex-1 truncate">{document.fileName}</span>
            <span className="text-xs text-slate-400">{formatSize(document.sizeBytes)}</span>
            <span className={document.expired && !muted ? "text-xs text-red-600" : "text-xs text-slate-500"}>
                expires {formatDate(document.expiresOn)}
            </span>
            <Button variant="outline" onClick={() => onDownload(document.id)}>
                <Download className="mr-1 h-4 w-4" /> Open
            </Button>
        </div>
    )

    return (
        <section className="mt-6 rounded-lg border bg-white p-6">
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                Documents
            </h2>

            <div className="mb-5 grid gap-2 sm:grid-cols-3">
                {DOCUMENT_TYPES.map(({ value, label }) => {
                    const document = current.get(value)

                    return (
                        <div key={value} className="rounded-md border border-slate-200 p-3">
                            <div className="text-xs font-medium text-slate-500">{label}</div>

                            {!document ? (
                                <div className="mt-1 text-sm text-amber-700">Not uploaded</div>
                            ) : document.expired ? (
                                <div className="mt-1 text-sm text-red-700">
                                    Expired {formatDate(document.expiresOn)}
                                </div>
                            ) : (
                                <div className="mt-1 text-sm text-emerald-700">
                                    Valid until {formatDate(document.expiresOn)}
                                </div>
                            )}
                        </div>
                    )
                })}
            </div>

            {error && (
                <div
                    role="alert"
                    className="mb-4 cursor-pointer rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700"
                    onClick={() => setError(null)}
                >
                    {error}
                </div>
            )}

            {canUpload && (
                <div className="mb-6 flex flex-wrap items-end gap-3 rounded-md bg-slate-50 p-3">
                    <label className="text-xs text-slate-500">
                        Type
                        <select
                            value={type}
                            onChange={(e) => setType(e.target.value as BusDocumentType)}
                            className="mt-1 block rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
                        >
                            {DOCUMENT_TYPES.map(({ value, label }) => (
                                <option key={value} value={value}>
                                    {label}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label className="text-xs text-slate-500">
                        Expires on
                        <input
                            type="date"
                            value={expiresOn}
                            min={tomorrow}
                            onChange={(e) => setExpiresOn(e.target.value)}
                            className="mt-1 block rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
                        />
                    </label>

                    <label className="text-xs text-slate-500">
                        File
                        <input
                            type="file"
                            accept=".pdf,.jpg,.jpeg,.png"
                            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                            className="mt-1 block text-sm text-slate-800 file:mr-3 file:rounded-md file:border-0 file:bg-slate-200 file:px-3 file:py-2 file:text-sm"
                        />
                    </label>

                    <Button
                        className="bg-sky-700 hover:bg-sky-800"
                        disabled={uploading}
                        onClick={onUpload}
                    >
                        <Upload className="mr-1 h-4 w-4" />
                        {uploading ? "Uploading…" : "Upload"}
                    </Button>

                    <p className="w-full text-xs text-slate-500">
                        Uploading again replaces the current one. The previous version is kept.
                    </p>
                </div>
            )}

            {isLoading && <p className="text-sm text-slate-500">Loading…</p>}

            {!isLoading && documents.length === 0 && (
                <p className="text-sm text-slate-500">No documents uploaded yet.</p>
            )}

            {currentDocuments.map((document) => row(document, false))}

            {superseded.length > 0 && (
                <div className="mt-3">
                    <button
                        onClick={() => setShowHistory((v) => !v)}
                        className="text-xs text-sky-700 hover:underline"
                    >
                        {showHistory
                            ? "Hide previous versions"
                            : `Show ${superseded.length} previous version${superseded.length === 1 ? "" : "s"}`}
                    </button>

                    {showHistory && (
                        <div className="mt-2">{superseded.map((document) => row(document, true))}</div>
                    )}
                </div>
            )}
        </section>
    )
}