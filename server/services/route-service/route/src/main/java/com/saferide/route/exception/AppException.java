package com.saferide.route.exception;

import lombok.Getter;

@Getter
public class AppException extends RuntimeException {
    private final String code;
    private final int statusCode;

    public AppException(String code, String message, int statusCode) {
        super(message);
        this.code = code;
        this.statusCode = statusCode;
    }

    public static class ConflictException extends AppException {
        public ConflictException(String message) {
            super("Resource.Conflict", message, 409);
        }
    }

    public static class ForbiddenException extends AppException {
        public ForbiddenException(String message) {
            super("Auth.Forbidden", message, 403);
        }
    }

    public static class NotFoundException extends AppException {
        public NotFoundException(String message) {
            super("Resource.NotFound", message, 404);
        }
    }
}
