export function formatDuration(startedAt: string, endedAt: string | null): string {
    const end = endedAt ? new Date(endedAt).getTime() : Date.now()
    const ms = end - new Date(startedAt).getTime()
    if (ms < 0) return "—"
    const minutes = Math.floor(ms / 60000)
    const hours = Math.floor(minutes / 60)
    return hours > 0 ? `${hours}h ${minutes % 60}m` : `${minutes}m`
}

export function formatTime(iso: string | null): string {
    return iso ? new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "—"
}

export function formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString([], { day: "2-digit", month: "short", year: "numeric" })
}