import { useEffect, useState } from "react"

// Returns `value`, but only after it has stopped changing for `delayMs`.
export function useDebounce<T>(value: T, delayMs = 400): T {
    const [debounced, setDebounced] = useState(value)
    useEffect(() => {
        const t = setTimeout(() => setDebounced(value), delayMs)
        return () => clearTimeout(t)   // typing again cancels the pending update
    }, [value, delayMs])
    return debounced
}