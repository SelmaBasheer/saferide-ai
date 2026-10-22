import http from 'k6/http'
import { check, sleep } from 'k6'

const BASE = __ENV.BASE_URL || 'https://saferide-ai.centralindia.cloudapp.azure.com'

export const options = {
    cloud: {
        name: 'SafeRide — super admin dashboard',
        // Spread load across two regions so you are not measuring one
        // datacentre's route to Mumbai. Both are reasonably close to
        // centralindia.
        distribution: {
            mumbai: { loadZone: 'amazon:in:mumbai', percent: 100 },
        },
    },

    // Ramp up, hold, ramp down. The hold is where the numbers come from — the
    // ramp is just getting there without a thundering herd.
    stages: [
        { duration: '1m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '2m', target: 100 },
        { duration: '30s', target: 0 },
    ],

    // k6 exits non-zero if these are breached, which is what makes it usable
    // in CI later rather than a thing you eyeball.
    thresholds: {
        http_req_failed: ['rate<0.05'],      // under 1% errors
        http_req_duration: ['p(95)<3000'],    // 95% of requests under 800ms
    },
}

// Runs once before the test. Logging in per-iteration would hit the auth rate
// limiter and measure that instead of the thing we care about.
export function setup() {
    const res = http.post(
        `${BASE}/api/auth/login`,
        JSON.stringify({ email: __ENV.EMAIL, password: __ENV.PASSWORD }),
        { headers: { 'Content-Type': 'application/json' } },
    )

    check(res, { 'login succeeded': (r) => r.status === 200 })

    if (res.status !== 200) {
        throw new Error(`Login failed: ${res.status} ${res.body}`)
    }

    return { token: res.json('data.accessToken') }
}

// Runs repeatedly, once per virtual user per iteration.
export default function (data) {
    const params = { headers: { Authorization: `Bearer ${data.token}` } }

    // The super admin dashboard — seven SQL queries behind one request, so the
    // heaviest read in the system and the right thing to put under load.
    const res = http.get(`${BASE}/api/analytics/dashboard/super-admin`, params)

    check(res, {
        'dashboard 200': (r) => r.status === 200,
    })

    // Think time. Without it, each user hammers as fast as the network allows,
    // which is a stress test rather than a load test.
    sleep(0.1)
}