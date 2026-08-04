import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { FormField } from "@/components/ui/form-field"
import { Label } from "@/components/ui/label"
import { useUpdateMyProfileMutation, type SchoolDetail } from "@/features/schools/schoolApi"

// Required fields mirror the backend's completeness rule; extended fields are
// optional here because a Draft is ALLOWED to be incomplete — the Submit
// checklist (not this form) is what enforces completeness.
const schema = z.object({
    name: z.string().min(1, "School name is required"),
    address: z.string().min(1, "Address is required"),
    city: z.string().min(1, "City is required"),
    district: z.string().min(1, "District is required"),
    state: z.string().min(1, "State is required"),
    pincode: z.string().regex(/^\d{6}$/, "Enter a valid 6-digit pincode"),
    legalName: z.string(),
    board: z.string(),
    registrationNumber: z.string(),
    authorizedPersonName: z.string(),
    authorizedPersonDesignation: z.string(),
    officialPhone: z.string(),
    officialEmail: z.string().email("Enter a valid email").or(z.literal("")),
    busCount: z.string(),
    studentCount: z.string(),
})
type FormValues = z.infer<typeof schema>

const BOARDS = [
    { value: "", label: "Select board…" },
    { value: "CBSE", label: "CBSE" },
    { value: "ICSE", label: "ICSE" },
    { value: "StateBoard", label: "State Board" },
    { value: "Other", label: "Other" },
]
const BUS_COUNTS = [
    { value: "", label: "Select range…" },
    { value: "UpTo5", label: "Up to 5" },
    { value: "From6To15", label: "6 – 15" },
    { value: "From16To50", label: "16 – 50" },
    { value: "Above50", label: "More than 50" },
]
const STUDENT_COUNTS = [
    { value: "", label: "Select range…" },
    { value: "UpTo100", label: "Up to 100" },
    { value: "From101To500", label: "101 – 500" },
    { value: "From501To1000", label: "501 – 1000" },
    { value: "Above1000", label: "More than 1000" },
]

// Form uses "" for empty; the API wants null. Convert at the boundary.
const emptyToNull = (v: string) => (v.trim() === "" ? null : v)

export default function OnboardingProfileForm({ school }: { school: SchoolDetail }) {
    const [updateProfile, { isLoading, isSuccess, error }] = useUpdateMyProfileMutation()

    const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
        // Pre-fill from the server — API nulls become empty inputs.
        defaultValues: {
            name: school.name,
            address: school.address,
            city: school.city,
            district: school.district,
            state: school.state,
            pincode: school.pincode,
            legalName: school.legalName ?? "",
            board: school.board ?? "",
            registrationNumber: school.registrationNumber ?? "",
            authorizedPersonName: school.authorizedPersonName ?? "",
            authorizedPersonDesignation: school.authorizedPersonDesignation ?? "",
            officialPhone: school.officialPhone ?? "",
            officialEmail: school.officialEmail ?? "",
            busCount: school.busCount ?? "",
            studentCount: school.studentCount ?? "",
        },
    })

    const onSubmit = async (v: FormValues) => {
        try {
            await updateProfile({
                name: v.name, address: v.address, city: v.city,
                district: v.district, state: v.state, pincode: v.pincode,
                legalName: emptyToNull(v.legalName),
                board: emptyToNull(v.board),
                registrationNumber: emptyToNull(v.registrationNumber),
                authorizedPersonName: emptyToNull(v.authorizedPersonName),
                authorizedPersonDesignation: emptyToNull(v.authorizedPersonDesignation),
                officialPhone: emptyToNull(v.officialPhone),
                officialEmail: emptyToNull(v.officialEmail),
                busCount: emptyToNull(v.busCount),
                studentCount: emptyToNull(v.studentCount),
            }).unwrap()
        } catch { /* error shown below */ }
    }

    const selectCls =
        "h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm shadow-xs outline-none focus-visible:ring-2"

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 rounded-lg border bg-white p-6">
            <div>
                <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Basic details</h2>
                <div className="mt-4 grid gap-4 sm:grid-cols-2">
                    <FormField label="School name" {...register("name")} error={errors.name?.message} />
                    <FormField label="Legal name" placeholder="As on registration certificate" {...register("legalName")} error={errors.legalName?.message} />
                    <div className="sm:col-span-2">
                        <FormField label="Address" {...register("address")} error={errors.address?.message} />
                    </div>
                    <FormField label="City" {...register("city")} error={errors.city?.message} />
                    <FormField label="District" {...register("district")} error={errors.district?.message} />
                    <FormField label="State" {...register("state")} error={errors.state?.message} />
                    <FormField label="Pincode" inputMode="numeric" {...register("pincode")} error={errors.pincode?.message} />
                </div>
            </div>

            <div>
                <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Registration & contacts</h2>
                <div className="mt-4 grid gap-4 sm:grid-cols-2">
                    <div className="space-y-1.5">
                        <Label htmlFor="board">Affiliation board</Label>
                        <select id="board" className={selectCls} {...register("board")}>
                            {BOARDS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                        </select>
                    </div>
                    <FormField label="Registration number" {...register("registrationNumber")} error={errors.registrationNumber?.message} />
                    <FormField label="Authorized person" {...register("authorizedPersonName")} error={errors.authorizedPersonName?.message} />
                    <FormField label="Designation" placeholder="e.g. Principal" {...register("authorizedPersonDesignation")} error={errors.authorizedPersonDesignation?.message} />
                    <FormField label="Official phone" {...register("officialPhone")} error={errors.officialPhone?.message} />
                    <FormField label="Official email" type="email" {...register("officialEmail")} error={errors.officialEmail?.message} />
                    <div className="space-y-1.5">
                        <Label htmlFor="busCount">Number of buses</Label>
                        <select id="busCount" className={selectCls} {...register("busCount")}>
                            {BUS_COUNTS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                        </select>
                    </div>
                    <div className="space-y-1.5">
                        <Label htmlFor="studentCount">Number of students</Label>
                        <select id="studentCount" className={selectCls} {...register("studentCount")}>
                            {STUDENT_COUNTS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                        </select>
                    </div>
                </div>
            </div>

            {error && <p className="text-sm text-red-600">Could not save the profile. Please try again.</p>}
            {isSuccess && <p className="text-sm text-emerald-600">Profile saved.</p>}

            <Button type="submit" disabled={isLoading} className="bg-sky-700 hover:bg-sky-800">
                {isLoading ? "Saving…" : "Save profile"}
            </Button>
        </form>
    )
}