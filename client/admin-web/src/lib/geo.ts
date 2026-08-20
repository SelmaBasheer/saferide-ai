export type LatLng = [number, number]

export function metres(a: LatLng, b: LatLng): number {
    const R = 6371000
    const toRad = (d: number) => (d * Math.PI) / 180
    const dLat = toRad(b[0] - a[0])
    const dLon = toRad(b[1] - a[1])
    const h =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(toRad(a[0])) * Math.cos(toRad(b[0])) * Math.sin(dLon / 2) ** 2
    return R * 2 * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h))
}

/** Fills in points along a path so a simulated bus moves smoothly. */
export function densify(points: LatLng[], stepMetres: number): LatLng[] {
    if (points.length < 2) return points
    const out: LatLng[] = []
    for (let i = 0; i < points.length - 1; i++) {
        const a = points[i]
        const b = points[i + 1]
        const steps = Math.max(1, Math.round(metres(a, b) / stepMetres))
        for (let s = 0; s < steps; s++) {
            const t = s / steps
            out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])
        }
    }
    out.push(points[points.length - 1])
    return out
}