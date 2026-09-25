import { useState } from "react"
import { Plus, X, Pencil } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { superAdminNav } from "@/components/layout/superAdminNav"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
    useGetPlansQuery,
    useCreatePlanMutation,
    useUpdatePlanMutation,
    useDeactivatePlanMutation,
    formatRupees,
    type Plan,
} from "@/features/subscriptions/subscriptionApi"

function errorMessage(e: unknown, fallback: string) {
    const data = (e as { data?: { error?: { message?: string } } })?.data
    return data?.error?.message ?? fallback
}

/** One form for both jobs. A plan means edit; no plan means create. */
function PlanForm({ plan, onDone }: { plan?: Plan; onDone: () => void }) {
    const [name, setName] = useState(plan?.name ?? "")
    const [description, setDescription] = useState(plan?.description ?? "")
    const [rupees, setRupees] = useState(plan ? String(plan.priceInPaise / 100) : "")
    const [busLimit, setBusLimit] = useState(plan?.busLimit != null ? String(plan.busLimit) : "")
    const [months, setMonths] = useState(String(plan?.durationMonths ?? 12))

    const [createPlan, create] = useCreatePlanMutation()
    const [updatePlan, update] = useUpdatePlanMutation()

    const isLoading = create.isLoading || update.isLoading
    const error = create.error ?? update.error

    const submit = async (e: React.FormEvent) => {
        e.preventDefault()

        const body = {
            name: name.trim(),
            description: description.trim() || null,
            // Whole rupees only, so the conversion is an integer multiply and no
            // decimal ever exists in JavaScript.
            priceInPaise: Number.parseInt(rupees, 10) * 100,
            busLimit: busLimit.trim() === "" ? null : Number.parseInt(busLimit, 10),
            durationMonths: Number.parseInt(months, 10),
        }

        try {
            if (plan) {
                await updatePlan({ id: plan.id, ...body }).unwrap()
            } else {
                await createPlan(body).unwrap()
            }
            onDone()
        } catch {
            // Shown below.
        }
    }

    return (
        <form onSubmit={submit} className="rounded-lg border bg-white p-5">
            <p className="text-sm text-slate-500">
                {plan
                    ? "Changes apply to schools who subscribe from now on. Existing subscriptions keep the price and bus limit they were bought at."
                    : "A new plan schools can choose."}
            </p>

            <div className="mt-4 grid gap-4 sm:grid-cols-2">
                <label className="text-sm">
                    <span className="text-slate-600">Name</span>
                    <Input className="mt-1" value={name} onChange={(e) => setName(e.target.value)}
                        placeholder="Standard" required />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Description</span>
                    <Input className="mt-1" value={description} onChange={(e) => setDescription(e.target.value)}
                        placeholder="For growing fleets" />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Price in rupees</span>
                    <Input className="mt-1" type="number" min={0} step={1} value={rupees}
                        onChange={(e) => setRupees(e.target.value)} placeholder="49999" required />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Bus limit</span>
                    <Input className="mt-1" type="number" min={1} value={busLimit}
                        onChange={(e) => setBusLimit(e.target.value)} placeholder="Leave empty for unlimited" />
                </label>

                <label className="text-sm">
                    <span className="text-slate-600">Duration in months</span>
                    <Input className="mt-1" type="number" min={1} max={36} value={months}
                        onChange={(e) => setMonths(e.target.value)} required />
                </label>
            </div>

            {error && (
                <p className="mt-3 text-sm text-red-600">
                    {errorMessage(error, "Could not save the plan.")}
                </p>
            )}

            <div className="mt-4 flex gap-2">
                <Button type="submit" disabled={isLoading} className="bg-sky-700 hover:bg-sky-800">
                    {isLoading ? "Saving…" : plan ? "Save changes" : "Create plan"}
                </Button>
                <Button type="button" variant="outline" onClick={onDone}>
                    Cancel
                </Button>
            </div>
        </form>
    )
}

export default function SuperAdminPlansPage() {
    // null means closed, "new" means the create form, a Plan means editing it.
    const [editing, setEditing] = useState<Plan | "new" | null>(null)

    const { data: plans = [], isLoading } = useGetPlansQuery(true)
    const [deactivate] = useDeactivatePlanMutation()

    return (
        <DashboardLayout roleLabel="Super Admin" nav={superAdminNav("Plans")}>
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-slate-800">Plans</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        What schools can buy. Price and bus limit are copied onto a subscription
                        when it is bought, so editing a plan never alters an existing contract.
                    </p>
                </div>
                <Button
                    className="bg-sky-700 hover:bg-sky-800"
                    onClick={() => setEditing((e) => (e === null ? "new" : null))}
                >
                    {editing === null ? <Plus className="mr-1 h-4 w-4" /> : <X className="mr-1 h-4 w-4" />}
                    {editing === null ? "New plan" : "Close"}
                </Button>
            </div>

            {editing !== null && (
                <div className="mt-6">
                    <PlanForm
                        key={editing === "new" ? "new" : editing.id}
                        plan={editing === "new" ? undefined : editing}
                        onDone={() => setEditing(null)}
                    />
                </div>
            )}

            <div className="mt-6 overflow-hidden rounded-lg border bg-white">
                <table className="w-full text-sm">
                    <thead className="border-b bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-400">
                        <tr>
                            <th className="px-4 py-3">Name</th>
                            <th className="px-4 py-3">Price</th>
                            <th className="px-4 py-3">Buses</th>
                            <th className="px-4 py-3">Duration</th>
                            <th className="px-4 py-3">Status</th>
                            <th className="px-4 py-3 text-right">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y">
                        {isLoading ? (
                            <tr><td colSpan={6} className="px-4 py-6 text-slate-500">Loading…</td></tr>
                        ) : plans.length === 0 ? (
                            <tr><td colSpan={6} className="px-4 py-6 text-slate-500">No plans yet.</td></tr>
                        ) : (
                            plans.map((p) => (
                                <tr key={p.id} className={p.isActive ? "" : "bg-slate-50 text-slate-400"}>
                                    <td className="px-4 py-3 font-medium text-slate-800">{p.name}</td>
                                    <td className="px-4 py-3">{formatRupees(p.priceInPaise)}</td>
                                    <td className="px-4 py-3">
                                        {p.busLimit === null ? "Unlimited" : p.busLimit}
                                    </td>
                                    <td className="px-4 py-3">{p.durationMonths} months</td>
                                    <td className="px-4 py-3">
                                        <span className={`rounded-full px-2 py-0.5 text-xs font-medium
                                            ${p.isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>
                                            {p.isActive ? "Active" : "Retired"}
                                        </span>
                                    </td>
                                    <td className="px-4 py-3 text-right">
                                        <div className="flex justify-end gap-2">
                                            <Button size="sm" variant="outline" onClick={() => setEditing(p)}>
                                                <Pencil className="mr-1 h-3.5 w-3.5" /> Edit
                                            </Button>
                                            {p.isActive && (
                                                <Button size="sm" variant="outline" onClick={() => deactivate(p.id)}>
                                                    Retire
                                                </Button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            <p className="mt-3 text-sm text-slate-400">
                Plans are retired, never deleted — every subscription and payment points at one.
            </p>
        </DashboardLayout>
    )
}