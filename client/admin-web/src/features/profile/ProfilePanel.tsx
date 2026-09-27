import { useEffect, useRef, useState } from "react"
import { Camera, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useGetMySchoolQuery } from "@/features/schools/schoolApi"
import { useGetMyChildrenQuery } from "@/features/students/studentApi"
import {
    useGetProfileQuery,
    useUpdateProfileMutation,
    useChangePasswordMutation,
    useUploadPhotoMutation,
    useRemovePhotoMutation,
    type Profile,
} from "@/features/profile/profileApi"

const MAX_PHOTO_BYTES = 2 * 1024 * 1024

function errorMessage(e: unknown, fallback: string) {
    const data = (e as { data?: { error?: { message?: string } } })?.data
    return data?.error?.message ?? fallback
}

function initials(p: Profile) {
    return `${p.firstName[0] ?? ""}${p.lastName[0] ?? ""}`.toUpperCase()
}

/** Nobody can change their own email here, but who to ask differs by role —
 *  and telling a school admin to contact their school administrator is absurd. */
function emailHint(role: string) {
    switch (role) {
        case "SuperAdmin":
            return "Your sign-in email can't be changed."
        case "SchoolAdmin":
            return "Contact SafeRide support to change this."
        default:
            return "Contact your school administrator to change this."
    }
}

function PhotoField({ profile }: { profile: Profile }) {
    const fileInput = useRef<HTMLInputElement>(null)
    const [tooLarge, setTooLarge] = useState(false)

    const [uploadPhoto, upload] = useUploadPhotoMutation()
    const [removePhoto, remove] = useRemovePhotoMutation()

    const busy = upload.isLoading || remove.isLoading

    return (
        <div className="flex flex-wrap items-center gap-4">
            {profile.photoUrl ? (
                <img src={profile.photoUrl} alt="" className="h-20 w-20 rounded-full object-cover" />
            ) : (
                <div className="flex h-20 w-20 items-center justify-center rounded-full bg-sky-100 text-xl font-semibold text-sky-700">
                    {initials(profile)}
                </div>
            )}

            <div>
                <div className="flex gap-2">
                    <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={busy}
                        onClick={() => fileInput.current?.click()}
                    >
                        <Camera className="mr-1 h-3.5 w-3.5" />
                        {profile.photoUrl ? "Change" : "Add photo"}
                    </Button>

                    {profile.photoUrl && (
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={busy}
                            onClick={() => removePhoto()}
                        >
                            <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                    )}
                </div>

                <p className="mt-1 text-xs text-slate-400">JPEG, PNG or WebP, up to 2 MB.</p>

                {tooLarge && (
                    <p className="mt-1 text-xs text-red-600">That image is larger than 2 MB.</p>
                )}

                {upload.isError && (
                    <p className="mt-1 text-xs text-red-600">
                        {errorMessage(upload.error, "Could not upload that image.")}
                    </p>
                )}
            </div>

            <input
                ref={fileInput}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={(e) => {
                    const file = e.target.files?.[0]
                    e.target.value = ""

                    if (!file) return

                    // The server checks this too — that is the one that counts.
                    // This just saves someone a slow upload before being told no.
                    if (file.size > MAX_PHOTO_BYTES) {
                        setTooLarge(true)
                        return
                    }

                    setTooLarge(false)
                    uploadPhoto(file)
                }}
            />
        </div>
    )
}

function DetailsForm({ profile }: { profile: Profile }) {
    const [firstName, setFirstName] = useState(profile.firstName)
    const [lastName, setLastName] = useState(profile.lastName)
    const [phone, setPhone] = useState(profile.phone)
    const [saved, setSaved] = useState(false)

    const [updateProfile, { isLoading, error }] = useUpdateProfileMutation()

    // The query refetches after a save, so the props change under us.
    useEffect(() => {
        setFirstName(profile.firstName)
        setLastName(profile.lastName)
        setPhone(profile.phone)
    }, [profile.firstName, profile.lastName, profile.phone])

    const submit = async (e: React.FormEvent) => {
        e.preventDefault()
        setSaved(false)

        try {
            await updateProfile({ firstName, lastName, phone }).unwrap()
            setSaved(true)
        } catch {
            // Shown below.
        }
    }

    return (
        <form onSubmit={submit} className="rounded-lg border bg-white p-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                Your details
            </h2>

            <div className="mt-4">
                <PhotoField profile={profile} />
            </div>

            <div className="mt-5 grid gap-4 sm:grid-cols-2">
                <label className="text-sm">
                    <span className="text-slate-600">First name</span>
                    <Input
                        className="mt-1"
                        value={firstName}
                        onChange={(e) => setFirstName(e.target.value)}
                        required
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Last name</span>
                    <Input
                        className="mt-1"
                        value={lastName}
                        onChange={(e) => setLastName(e.target.value)}
                        required
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Phone</span>
                    <Input
                        className="mt-1"
                        value={phone}
                        onChange={(e) => setPhone(e.target.value)}
                        required
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Email</span>
                    <Input className="mt-1 bg-slate-50" value={profile.email} disabled />
                    <span className="mt-1 block text-xs text-slate-400">
                        {emailHint(profile.role)}
                    </span>
                </label>
            </div>

            <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button type="submit" disabled={isLoading} className="bg-sky-700 hover:bg-sky-800">
                    {isLoading ? "Saving…" : "Save changes"}
                </Button>

                {saved && <span className="text-sm text-emerald-600">Saved</span>}
                {error && (
                    <span className="text-sm text-red-600">
                        {errorMessage(error, "Could not save your details.")}
                    </span>
                )}
            </div>
        </form>
    )
}

function PasswordForm() {
    const [currentPassword, setCurrentPassword] = useState("")
    const [newPassword, setNewPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [mismatch, setMismatch] = useState(false)
    const [done, setDone] = useState(false)

    const [changePassword, { isLoading, error }] = useChangePasswordMutation()

    const submit = async (e: React.FormEvent) => {
        e.preventDefault()
        setDone(false)

        if (newPassword !== confirmPassword) {
            setMismatch(true)
            return
        }

        setMismatch(false)

        try {
            await changePassword({ currentPassword, newPassword }).unwrap()
            setCurrentPassword("")
            setNewPassword("")
            setConfirmPassword("")
            setDone(true)
        } catch {
            // Shown below.
        }
    }

    return (
        <form onSubmit={submit} className="rounded-lg border bg-white p-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                Security
            </h2>

            <div className="mt-4 grid gap-4 sm:grid-cols-3">
                <label className="text-sm">
                    <span className="text-slate-600">Current password</span>
                    <Input
                        className="mt-1"
                        type="password"
                        autoComplete="current-password"
                        value={currentPassword}
                        onChange={(e) => setCurrentPassword(e.target.value)}
                        required
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">New password</span>
                    <Input
                        className="mt-1"
                        type="password"
                        autoComplete="new-password"
                        minLength={8}
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        required
                    />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Confirm new password</span>
                    <Input
                        className="mt-1"
                        type="password"
                        autoComplete="new-password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                    />
                </label>
            </div>

            <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button type="submit" disabled={isLoading} className="bg-sky-700 hover:bg-sky-800">
                    {isLoading ? "Changing…" : "Change password"}
                </Button>

                {done && <span className="text-sm text-emerald-600">Password changed</span>}
                {mismatch && (
                    <span className="text-sm text-red-600">The new passwords don't match.</span>
                )}
                {error && (
                    <span className="text-sm text-red-600">
                        {errorMessage(error, "Could not change your password.")}
                    </span>
                )}
            </div>

            {/* Worth the user knowing, and worth a reviewer seeing that it was a
                decision rather than an oversight. */}
            <p className="mt-3 text-xs text-slate-400">
                Your current password is required even though you're signed in, so that a stolen
                session cannot lock you out of your own account.
            </p>
        </form>
    )
}

/** School name and address, read-only. The school's own details are edited on
 *  the school settings page — this is context, not a second place to change it. */
function SchoolContext() {
    const { data: school } = useGetMySchoolQuery()

    if (!school) return null

    return (
        <div className="rounded-lg border bg-white p-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                Your school
            </h2>
            <p className="mt-3 font-medium text-slate-800">{school.name}</p>
            <p className="text-sm text-slate-500">
                {school.city}, {school.district}, {school.state}
            </p>
            <p className="mt-1 text-sm text-slate-400">{school.status}</p>
        </div>
    )
}

/** A parent's children, read-only. Their bus and route are one screen away on
 *  the live trip; duplicating them here would mean two places to keep correct. */
function ChildrenContext() {
    const { data: children = [] } = useGetMyChildrenQuery()

    if (children.length === 0) return null

    return (
        <div className="rounded-lg border bg-white p-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                Your children
            </h2>
            <div className="mt-3 divide-y">
                {children.map((c) => (
                    <div key={c.id} className="flex items-baseline justify-between py-2">
                        <span className="text-sm text-slate-800">
                            {c.firstName} {c.lastName}
                        </span>
                        <span className="text-sm text-slate-500">{c.grade}</span>
                    </div>
                ))}
            </div>
            <p className="mt-2 text-xs text-slate-400">Contact your school to change any of this.</p>
        </div>
    )
}

export default function ProfilePanel() {
    const { data: profile, isLoading, isError } = useGetProfileQuery()

    if (isLoading) return <p className="text-sm text-slate-500">Loading…</p>

    if (isError || !profile) {
        return <p className="text-sm text-red-600">Could not load your profile.</p>
    }

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-slate-800">Profile</h1>
                <p className="mt-1 text-sm text-slate-500">
                    Signed in as {profile.role} · {profile.status}
                </p>
            </div>

            <DetailsForm profile={profile} />

            {profile.role === "SchoolAdmin" && <SchoolContext />}
            {profile.role === "Parent" && <ChildrenContext />}

            <PasswordForm />
        </div>
    )
}