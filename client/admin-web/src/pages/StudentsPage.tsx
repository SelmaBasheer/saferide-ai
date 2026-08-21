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
import { useGetStudentsQuery, type StudentListItem } from "@/features/students/studentApi"
import CreateStudentForm from "@/features/students/CreateStudentForm"

const PAGE_SIZE = 10

const columns: Column<StudentListItem>[] = [
    {
        header: "Name",
        cell: (s) => (
            <Link
                to={ROUTES.schoolStudentDetail.replace(":id", s.id)}
                className="font-medium text-sky-700 hover:underline"
            >
                {s.firstName} {s.lastName}
            </Link>
        ),
    },
    { header: "Admission No", cell: (s) => s.admissionNumber },
    { header: "Grade", cell: (s) => s.grade },
    {
        header: "Route",
        cell: (s) =>
            s.routeId && s.pickupStopId ? (
                <span className="text-slate-700">Assigned</span>
            ) : (
                <span className="text-amber-700">Not assigned</span>
            ),
    },
    { header: "Parent", cell: (s) => `${s.parentFirstName} ${s.parentLastName}` },
    { header: "Parent email", cell: (s) => s.parentEmail },
    {
        header: "Status",
        cell: (s) => (
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium
                ${s.status === "ACTIVE" ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>
                {s.status === "ACTIVE" ? "Active" : "Inactive"}
            </span>
        ),
    },
]

export default function StudentsPage() {
    const [search, setSearch] = useState("")
    const [page, setPage] = useState(1)
    const [showForm, setShowForm] = useState(false)
    const debouncedSearch = useDebounce(search, 400)

    const { data, isLoading, isError } = useGetStudentsQuery({
        search: debouncedSearch, page, pageSize: PAGE_SIZE,
    })

    return (
        <DashboardLayout roleLabel="School Admin" nav={schoolAdminNav("Students")}>
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-slate-800">Students</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Each student's parent receives an email invitation to set their own password.
                    </p>
                </div>
                <Button className="bg-sky-700 hover:bg-sky-800" onClick={() => setShowForm((s) => !s)}>
                    {showForm ? <X className="mr-1 h-4 w-4" /> : <Plus className="mr-1 h-4 w-4" />}
                    {showForm ? "Close" : "Add student"}
                </Button>
            </div>

            {showForm && (
                <div className="mt-6">
                    <CreateStudentForm onCreated={() => setShowForm(false)} />
                </div>
            )}

            <div className="mt-6 space-y-4">
                <Input placeholder="Search by name, admission no or parent email…" className="max-w-sm bg-white"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
                <DataTable
                    columns={columns}
                    rows={data?.items ?? []}
                    rowKey={(s) => s.id}
                    isLoading={isLoading}
                    isError={isError}
                    emptyMessage="No students yet — add your first student."
                    page={page}
                    pageSize={PAGE_SIZE}
                    totalCount={data?.totalCount ?? 0}
                    onPageChange={setPage}
                />
            </div>
        </DashboardLayout>
    )
}