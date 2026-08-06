import { useState } from "react"
import { Plus, X } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { DataTable, type Column } from "@/components/ui/data-table"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { useDebounce } from "@/lib/useDebounce"
import { useGetDriversQuery, type DriverListItem } from "@/features/drivers/driverApi"
import CreateDriverForm from "@/features/drivers/CreateDriverForm"

const PAGE_SIZE = 10

function formatDate(iso: string): string {
    const [y, m, d] = iso.split("-").map(Number)
    return new Date(y, m - 1, d).toLocaleDateString()
}

const columns: Column<DriverListItem>[] = [
    { header: "Name", cell: (d) => <span className="font-medium text-slate-800">{d.firstName} {d.lastName}</span> },
    { header: "Email", cell: (d) => d.email },
    { header: "Phone", cell: (d) => d.phone },
    { header: "Licence", cell: (d) => d.licenseNumber },
    { header: "Expiry", cell: (d) => formatDate(d.licenseExpiryDate) },
    {
        header: "Status",
        cell: (d) => (
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium
                ${d.status === "Active" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>
                {d.status}
            </span>
        ),
    },
]

export default function DriversPage() {
    const [search, setSearch] = useState("")
    const [page, setPage] = useState(1)
    const [showForm, setShowForm] = useState(false)
    const debouncedSearch = useDebounce(search, 400)

    const { data, isLoading, isError } = useGetDriversQuery({
        search: debouncedSearch, page, pageSize: PAGE_SIZE,
    })

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Drivers")}>
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-slate-800">Drivers</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Drivers you add here receive an email invitation to set their own password.
                    </p>
                </div>
                <Button className="bg-sky-700 hover:bg-sky-800" onClick={() => setShowForm((s) => !s)}>
                    {showForm ? <X className="mr-1 h-4 w-4" /> : <Plus className="mr-1 h-4 w-4" />}
                    {showForm ? "Close" : "Add driver"}
                </Button>
            </div>

            {showForm && (
                <div className="mt-6">
                    <CreateDriverForm onCreated={() => setShowForm(false)} />
                </div>
            )}

            <div className="mt-6 space-y-4">
                <Input placeholder="Search by name, email or licence…" className="max-w-sm bg-white"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(d) => d.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage="No drivers yet — add your first driver."
                    page={page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}