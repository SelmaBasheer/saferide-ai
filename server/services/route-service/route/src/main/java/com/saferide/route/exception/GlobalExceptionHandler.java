package com.saferide.route.exception;

import com.saferide.route.dto.ApiResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.web.ErrorResponseException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {

    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    private static final String REGISTRATION_CONSTRAINT = "uk_bus_school_registration";

    @ExceptionHandler(com.saferide.route.exception.AppException.class)
    ResponseEntity<ApiResponse<Object>> handleApp(com.saferide.route.exception.AppException ex) {
        log.warn("Handled: {}", ex.getCode());
        return ResponseEntity.status(ex.getStatusCode()).body(ApiResponse.fail(ex.getCode(), ex.getMessage()));
    }

    @ExceptionHandler(AccessDeniedException.class)
    ResponseEntity<ApiResponse<Object>> handleAccessDenied(AccessDeniedException ex) {
        log.warn("Handled: Auth.Forbidden");
        return ResponseEntity.status(403)
                .body(ApiResponse.fail("Auth.Forbidden", "You do not have access to this resource."));
    }

    @ExceptionHandler(ErrorResponseException.class)
    ResponseEntity<ApiResponse<Object>> handleErrorResponse(ErrorResponseException ex) {
        return ResponseEntity.status(ex.getStatusCode()).body(ApiResponse.fail("Http.Error", ex.getMessage()));
    }

    @ExceptionHandler(DataIntegrityViolationException.class)
    ResponseEntity<ApiResponse<Object>> handleIntegrity(DataIntegrityViolationException ex) {
        String cause = ex.getMostSpecificCause().getMessage();
        String lower = cause == null ? "" : cause.toLowerCase(java.util.Locale.ROOT);

        if (lower.contains(REGISTRATION_CONSTRAINT) || lower.contains("duplicate entry")) {
            log.warn("Handled: Db.Duplicate");
            return ResponseEntity.status(409)
                    .body(ApiResponse.fail("Db.Duplicate", "A record with the same value already exists."));
        }

        log.warn("Handled: Db.ConstraintViolation", ex);
        return ResponseEntity.status(400)
                .body(ApiResponse.fail("Db.ConstraintViolation", "The request violates a data constraint."));
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    ResponseEntity<ApiResponse<Object>> handleValidation(MethodArgumentNotValidException ex) {
        var first = ex.getBindingResult().getFieldErrors().stream()
                .findFirst()
                .map(f -> f.getField() + ": " + f.getDefaultMessage())
                .orElse("Validation failed.");
        return ResponseEntity.badRequest().body(ApiResponse.fail("Validation.Error", first));
    }

    @ExceptionHandler(Exception.class)
    ResponseEntity<ApiResponse<Object>> handleUnknown(Exception ex) {
        log.error("Unhandled", ex);
        return ResponseEntity.status(500).body(ApiResponse.fail("Server.Error", "An unexpected error occurred."));
    }

    @ExceptionHandler(org.springframework.dao.DuplicateKeyException.class)
    ResponseEntity<ApiResponse<Object>> handleDuplicateKey(org.springframework.dao.DuplicateKeyException ex) {
        log.warn("Handled: Db.Duplicate");
        return ResponseEntity.status(409)
                .body(ApiResponse.fail("Db.Duplicate", "A record with the same value already exists."));
    }
}
