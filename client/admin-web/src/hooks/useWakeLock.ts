import { useEffect, useRef } from "react"

export function useWakeLock(active: boolean) {
    const sentinelRef = useRef<WakeLockSentinel | null>(null)

    useEffect(() => {
        if (!active || !("wakeLock" in navigator)) return

        let cancelled = false

        const request = async () => {
            try {
                const sentinel = await navigator.wakeLock.request("screen")
                if (cancelled) {
                    void sentinel.release()
                    return
                }
                sentinelRef.current = sentinel
            } catch {
                // denied, or the tab is hidden — nothing to do
            }
        }

        void request()

        // browsers release the lock when the tab is hidden and do not give it back
        const onVisible = () => {
            if (document.visibilityState === "visible") void request()
        }
        document.addEventListener("visibilitychange", onVisible)

        return () => {
            cancelled = true
            document.removeEventListener("visibilitychange", onVisible)
            void sentinelRef.current?.release()
            sentinelRef.current = null
        }
    }, [active])
}