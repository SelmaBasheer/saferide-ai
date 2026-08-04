import { useState } from "react"
import { useNavigate } from "react-router-dom"
import DashboardLayout from "@/components/layout/DashboardLayout"
import { DataTable, type Column } from "@/components/ui/data-table"
import { useDebounce } from "@/lib/useDebounce"
import { Button } from "@/components/ui/button"
import { ROUTES } from "@/routes/paths"
import { useGetSchoolsQuery, type SchoolStatus, type SchoolListItem } from "@/features/schools/schoolApi"
import { School, Users, BarChart3, Search } from "lucide-react"

const FILTERS: { label: string; value: SchoolStatus | "All" }[] = [
    { label: "Pending review", value: "Submitted" },
    { label: "All", value: "All" },
    { label: "Draft", value: "Draft" },
    { label: "Approved", value: "Approved" },
    { label: "Rejected", value: "Rejected" },
    { label: "Suspended", value: "Suspended" },
]

const badge: Record<SchoolStatus, string> = {
    Draft: "bg-slate-100 text-slate-600",
    Submitted: "bg-amber-100 text-amber-700",
    Approved: "bg-green-100 text-green-700",
    Rejected: "bg-red-100 text-red-700",
    Suspended: "bg-slate-200 text-slate-500",
}

const PAGE_SIZE = 5

export default function DashboardPage() {
    const navigate = useNavigate()
    const [filter, setFilter] = useState<SchoolStatus | "All">("Submitted")
    const [search, setSearch] = useState("")
    const [page, setPage] = useState(1)
    const debouncedSearch = useDebounce(search)

    const { data, isLoading, isError, isFetching } = useGetSchoolsQuery({
        status: filter === "All" ? undefined : filter,
        search: debouncedSearch,
        page,
        pageSize: PAGE_SIZE,
    })

    // Any change of filter/search should restart from page 1
    const changeFilter = (f: SchoolStatus | "All") => { setFilter(f); setPage(1) }
    const changeSearch = (s: string) => { setSearch(s); setPage(1) }

    const columns: Column<SchoolListItem>[] = [
        {
            header: "School",
            cell: (s) => <span className="font-medium text-slate-800">{s.name}</span>,
        },
        {
            header: "Admin",
            cell: (s) => (
                <div className="text-slate-600">
                    <div>{s.adminName}</div>
                    <div className="text-xs text-slate-400">{s.adminEmail}</div>
                </div>
            ),
        },
        {
            header: "Location",
            cell: (s) => <span className="text-slate-600">{s.city}, {s.district}, {s.state}</span>,
        },
        {
            header: "Status",
            cell: (s) => (
                <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${badge[s.status]}`}>
                    {s.status}
                </span>
            ),
        },
        {
            header: "Action",
            className: "text-right",
            cell: (s) => (
                <Button size="sm" variant="outline"
                    onClick={() => navigate(ROUTES.superAdminSchool.replace(":id", s.id))}>
                    Details
                </Button>
            ),
        },
    ]

    return (
        <DashboardLayout roleLabel="Super Admin" nav={[
            { label: "Schools", icon: School, active: true },
            { label: "Users", icon: Users },
            { label: "Reports", icon: BarChart3 },
        ]}>
            <h1 className="text-2xl font-semibold text-slate-800">Schools</h1>
            <p className="mt-1 text-sm text-slate-500">Review, approve, or suspend registered schools.</p>

            <div className="mt-6 flex flex-wrap items-center gap-2">
                {FILTERS.map((f) => (
                    <button key={f.value} onClick={() => changeFilter(f.value)}
                        className={`rounded-full px-4 py-1.5 text-sm ${filter === f.value ? "bg-sky-700 text-white" : "bg-white text-slate-600 border"}`}>
                        {f.label}
                    </button>
                ))}
                <div className="relative ml-auto">
                    <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
                    <input value={search} onChange={(e) => changeSearch(e.target.value)}
                        placeholder="Search name, city, or email…"
                        className="h-9 w-64 rounded-md border bg-white pl-8 pr-3 text-sm outline-none focus:ring-2 focus:ring-sky-200" />
                </div>
            </div>

            <div className="mt-4">
                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(s) => s.id}
                    isLoading={isLoading || isFetching}
                    isError={isError}
                    emptyMessage="No schools match."
                    page={data?.page ?? page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}