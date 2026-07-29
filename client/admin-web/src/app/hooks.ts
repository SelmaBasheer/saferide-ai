import { useDispatch, useSelector } from "react-redux"
import type { RootState, AppDispatch } from "./store"

/**
 * Typed versions of the Redux hooks.
 * Use these throughout the app instead of the plain react-redux hooks
 * so state and dispatch are fully type-checked.
 */
export const useAppDispatch = () => useDispatch<AppDispatch>()
export const useAppSelector = useSelector.withTypes<RootState>()