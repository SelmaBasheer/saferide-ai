/**
 * Razorpay's checkout is a script tag, not a package. Loaded on demand rather
 * than in index.html so a school that never opens this page never downloads it.
 */
const SRC = "https://checkout.razorpay.com/v1/checkout.js"

let loading: Promise<void> | null = null

export function loadRazorpay(): Promise<void> {
    if (window.Razorpay) return Promise.resolve()

    // Cached, so two clicks don't add two script tags.
    loading ??= new Promise<void>((resolve, reject) => {
        const script = document.createElement("script")
        script.src = SRC
        script.onload = () => resolve()
        script.onerror = () => {
            loading = null
            reject(new Error("Could not load Razorpay checkout."))
        }
        document.body.appendChild(script)
    })

    return loading
}

export interface RazorpayOptions {
    key: string
    amount: number
    currency: string
    name: string
    description: string
    order_id: string
    prefill: { name?: string; email?: string; contact?: string }
    theme?: { color?: string }
    handler: () => void
    modal?: { ondismiss?: () => void }
}

declare global {
    interface Window {
        Razorpay?: new (options: RazorpayOptions) => { open: () => void }
    }
}