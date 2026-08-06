package com.saferide.student.application.exception;

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
}
