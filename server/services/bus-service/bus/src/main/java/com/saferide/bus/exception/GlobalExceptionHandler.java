package com.saferide.bus.exception;

import com.saferide.bus.dto.ApiResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {

    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(com.saferide.bus.exception.AppException.class)
    ResponseEntity<ApiResponse<Object>> handleApp(com.saferide.bus.exception.AppException ex) {
        log.warn("Handled: {}", ex.getCode());
        return ResponseEntity.status(ex.getStatusCode()).body(ApiResponse.fail(ex.getCode(), ex.getMessage()));
    }

    // DB-level safety net: unique-constraint race (same lesson as Driver's 23505 handling)
    @ExceptionHandler(DataIntegrityViolationException.class)
    ResponseEntity<ApiResponse<Object>> handleIntegrity(DataIntegrityViolationException ex) {
        log.warn("Handled: Db.ConstraintViolation");
        return ResponseEntity.status(409)
                .body(ApiResponse.fail("Db.Duplicate", "A record with the same value already exists."));
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
}
