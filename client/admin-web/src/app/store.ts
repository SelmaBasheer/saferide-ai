import { configureStore } from "@reduxjs/toolkit"
import { baseApi } from "@/lib/baseApi"
import authReducer from "@/features/auth/authSlice"

export const store = configureStore({
    reducer: {
        [baseApi.reducerPath]: baseApi.reducer,
        auth: authReducer,
    },
    middleware: (getDefault) => getDefault().concat(baseApi.middleware),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch