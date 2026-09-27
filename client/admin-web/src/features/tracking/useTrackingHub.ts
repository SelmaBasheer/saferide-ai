import { useCallback, useEffect, useRef, useState } from "react"
import * as signalR from "@microsoft/signalr"
import { useAppSelector } from "@/app/hooks"

export interface PositionUpdate {
    tripId: string
    busId: string
    routeCode: string
    latitude: number
    longitude: number
    speedKmh: number | null
    recordedAt: string
    source: string
}

export interface StopReachedNotification {
    tripId: string
    stopId: string
    stopName: string
    sequence: number
    at: string
}

export interface ApproachingStopNotification {
    tripId: string
    routeCode: string
    stopId: string
    stopName: string
    sequence: number
    stopsAway: number
}

export interface StudentBoardedNotification {
    tripId: string
    studentId: string
    studentName: string
    stopName: string
    status: string
    at: string
}

export interface TripLifecycleNotification {
    tripId: string
    busId: string
    routeCode: string
    routeName: string
    at: string
}

export interface RouteDeviationNotification {
    tripId: string
    busId: string
    routeCode: string
    latitude: number
    longitude: number
    metresOffRoute: number
    at: string
}

export interface TrackingHandlers {
    onPosition?: (u: PositionUpdate) => void
    onStopReached?: (n: StopReachedNotification) => void
    onApproachingStop?: (n: ApproachingStopNotification) => void
    onStudentBoarded?: (n: StudentBoardedNotification) => void
    onTripStarted?: (n: TripLifecycleNotification) => void
    onTripEnded?: (n: TripLifecycleNotification) => void
    onRouteDeviation?: (n: RouteDeviationNotification) => void
}

export interface StopEta {
    stopId: string
    etaAt: string
}

export interface PositionUpdate {
    tripId: string
    busId: string
    routeCode: string
    latitude: number
    longitude: number
    speedKmh: number | null
    recordedAt: string
    source: string
    etas: StopEta[]
}

export type HubStatus = "idle" | "connecting" | "connected" | "reconnecting" | "disconnected"

export function useTrackingHub(handlers: TrackingHandlers) {
    const token = useAppSelector((s) => s.auth.accessToken)

    const connectionRef = useRef<signalR.HubConnection | null>(null)
    const handlersRef = useRef(handlers)
    const [status, setStatus] = useState<HubStatus>("idle")

    useEffect(() => {
        handlersRef.current = handlers
    })

    useEffect(() => {
        if (!token) return

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/tracking", { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build()

        // Development only: lets to drive a single position from the browser
        // console for testing geofence and deviation without the simulator.
        if (import.meta.env.DEV) {
            (window as unknown as { hub?: typeof connection }).hub = connection
        }

        connection.on("PositionUpdated", (u: PositionUpdate) => handlersRef.current.onPosition?.(u))
        connection.on("StopReached", (n: StopReachedNotification) => handlersRef.current.onStopReached?.(n))
        connection.on("ApproachingStop", (n: ApproachingStopNotification) => handlersRef.current.onApproachingStop?.(n))
        connection.on("StudentBoarded", (n: StudentBoardedNotification) => handlersRef.current.onStudentBoarded?.(n))
        connection.on("TripStarted", (n: TripLifecycleNotification) => handlersRef.current.onTripStarted?.(n))
        connection.on("TripEnded", (n: TripLifecycleNotification) => handlersRef.current.onTripEnded?.(n))
        connection.on("RouteDeviation", (n: RouteDeviationNotification) => handlersRef.current.onRouteDeviation?.(n))

        connection.onreconnecting(() => setStatus("reconnecting"))
        connection.onreconnected(() => setStatus("connected"))
        connection.onclose(() => setStatus("disconnected"))

        connectionRef.current = connection
        setStatus("connecting")

        connection
            .start()
            .then(() => setStatus("connected"))
            .catch(() => setStatus("disconnected"))

        return () => {
            connectionRef.current = null
            void connection.stop()
        }
    }, [token])

    const invokeIfConnected = useCallback((method: string, ...args: unknown[]) => {
        const connection = connectionRef.current

        // The connection can be mid-negotiation or already stopping — React's
        // strict mode mounts twice in development, and a driver closing the app
        // does the same thing in production.
        if (connection?.state !== signalR.HubConnectionState.Connected) {
            return Promise.resolve()
        }

        return connection.invoke(method, ...args)
    }, [])

    const joinTrip = useCallback((tripId: string) => invokeIfConnected("JoinTrip", tripId), [invokeIfConnected])
    const leaveTrip = useCallback((tripId: string) => invokeIfConnected("LeaveTrip", tripId), [invokeIfConnected])
    const joinFleet = useCallback(() => invokeIfConnected("JoinSchoolFleet"), [invokeIfConnected])

    const sendPosition = useCallback(
        (tripId: string, latitude: number, longitude: number, speedKmh: number | null, source: string) =>
            invokeIfConnected("SendPosition", tripId, latitude, longitude, speedKmh, source),
        [invokeIfConnected]
    )

    return { status, joinTrip, leaveTrip, joinFleet, sendPosition }
}