import type { ReactNode } from "react"
import { Button } from "@/components/ui/button"


export interface Column<T> {
    header: string
    cell: (row: T) => ReactNode
    className?: string
}

interface DataTableProps<T> {
    columns: Column<T>[]
    rows: T[]
    rowKey: (row: T) => string
    isLoading?: boolean
    isError?: boolean
    emptyMessage?: string
    // pagination
    page: number
    pageSize: number
    totalCount: number
    onPageChange: (page: number) => void
}

export function DataTable<T>({
    columns, rows, rowKey, isLoading, isError,
    emptyMessage = "No records found.",
    page, pageSize, totalCount, onPageChange,
}: DataTableProps<T>) {
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

    return (
        <div className="overflow-hidden rounded-lg border bg-white">
            {isLoading ? (
                <p className="p-6 text-sm text-slate-500">Loading…</p>
            ) : isError ? (
                <p className="p-6 text-sm text-red-600">Failed to load data.</p>
            ) : rows.length === 0 ? (
                <p className="p-6 text-sm text-slate-500">{emptyMessage}</p>
            ) : (
                <table className="w-full text-sm">
                    <thead className="bg-slate-50 text-left text-slate-500">
                        <tr>
                            {columns.map((c) => (
                                <th key={c.header} className={`px-4 py-3 font-medium ${c.className ?? ""}`}>
                                    {c.header}
                                </th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="divide-y">
                        {rows.map((row) => (
                            <tr key={rowKey(row)} className="hover:bg-slate-50">
                                {columns.map((c) => (
                                    <td key={c.header} className={`px-4 py-3 ${c.className ?? ""}`}>
                                        {c.cell(row)}
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            <div className="flex items-center justify-between border-t bg-slate-50 px-4 py-2">
                <p className="text-xs text-slate-500">
                    {totalCount} result{totalCount === 1 ? "" : "s"} · page {page} of {totalPages}
                </p>
                <div className="flex gap-1">
                    <Button size="sm" variant="outline" disabled={page <= 1 || isLoading}
                        onClick={() => onPageChange(page - 1)}>Previous</Button>
                    <Button size="sm" variant="outline" disabled={page >= totalPages || isLoading}
                        onClick={() => onPageChange(page + 1)}>Next</Button>
                </div>
            </div>
        </div>
    )
}