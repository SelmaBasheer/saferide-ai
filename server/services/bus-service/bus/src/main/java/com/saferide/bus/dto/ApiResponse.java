package com.saferide.bus.dto;

public record ApiResponse<T>(boolean success, T data, String message, ApiError error) {

    public record ApiError(String code, String message) {}

    public static <T> ApiResponse<T> ok(T data, String message) {
        return new ApiResponse<>(true, data, message, null);
    }

    public static <T> ApiResponse<T> ok(T data) {
        return ok(data, null);
    }

    public static <T> ApiResponse<T> fail(String code, String message) {
        return new ApiResponse<>(false, null, null, new ApiError(code, message));
    }
}
