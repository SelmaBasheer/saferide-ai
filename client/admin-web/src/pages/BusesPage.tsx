import { useState } from "react"
import { Link } from "react-router-dom"
import { Plus, X } from "lucide-react"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { schoolAdminNav } from "@/components/layout/schoolAdminNav"
import { DataTable, type Column } from "@/components/ui/data-table"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { useDebounce } from "@/lib/useDebounce"
import { ROUTES } from "@/routes/paths"
import { useGetBusesQuery, type BusListItem } from "@/features/buses/busApi"
import CreateBusForm from "@/features/buses/CreateBusForm"

const PAGE_SIZE = 10

const columns: Column<BusListItem>[] = [
    {
        header: "Registration",
        cell: (b) => (
            <Link
                to={ROUTES.schoolBusDetail.replace(":id", b.id)}
                className="font-medium text-sky-700 hover:underline"
            >
                {b.registrationNumber}
            </Link>
        ),
    },
    { header: "Model", cell: (b) => b.model },
    { header: "Capacity", cell: (b) => b.capacity },
    {
        header: "Driver",
        cell: (b) =>
            b.assignedDriverId ? (
                <span className="text-slate-700">Assigned</span>
            ) : (
                <span className="text-slate-400">Not assigned</span>
            ),
    },
    {
        header: "Status",
        cell: (b) => (
            <span
                className={`rounded-full px-2 py-0.5 text-xs font-medium
                ${b.status === "ACTIVE" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}
            >
                {b.status === "ACTIVE" ? "Active" : "Inactive"}
            </span>
        ),
    },
]

export default function BusesPage() {
    const [search, setSearch] = useState("")
    const [page, setPage] = useState(1)
    const [showForm, setShowForm] = useState(false)
    const [includeInactive, setIncludeInactive] = useState(false)
    const debouncedSearch = useDebounce(search, 400)

    const { data, isLoading, isError } = useGetBusesQuery({
        search: debouncedSearch,
        includeInactive,
        page,
        pageSize: PAGE_SIZE,
    })

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Buses")}>
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-slate-800">Buses</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Your fleet. Select a bus to assign a driver or deactivate it.
                    </p>
                </div>
                <Button className="bg-sky-700 hover:bg-sky-800" onClick={() => setShowForm((s) => !s)}>
                    {showForm ? <X className="mr-1 h-4 w-4" /> : <Plus className="mr-1 h-4 w-4" />}
                    {showForm ? "Close" : "Add bus"}
                </Button>
            </div>

            {showForm && (
                <div className="mt-6">
                    <CreateBusForm onCreated={() => setShowForm(false)} />
                </div>
            )}

            <div className="mt-6 space-y-4">
                <div className="flex items-center gap-4">
                    <Input
                        placeholder="Search by registration or model…"
                        className="max-w-sm bg-white"
                        value={search}
                        onChange={(e) => {
                            setSearch(e.target.value)
                            setPage(1)
                        }}
                    />
                    <label className="flex items-center gap-2 text-sm text-slate-600">
                        <input
                            type="checkbox"
                            checked={includeInactive}
                            onChange={(e) => {
                                setIncludeInactive(e.target.checked)
                                setPage(1)
                            }}
                        />
                        Show inactive
                    </label>
                </div>

                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(b) => b.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage="No buses yet — add your first bus."
                    page={page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}