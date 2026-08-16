package com.saferide.route.dto;

import jakarta.validation.constraints.*;
import java.util.UUID;

public record StopInput(
        UUID stopId, // null for a new stop; existing id to keep it
        @NotBlank @Size(max = 120) String name,
        @DecimalMin("-90.0") @DecimalMax("90.0") double latitude,
        @DecimalMin("-180.0") @DecimalMax("180.0") double longitude,
        @NotNull @Pattern(regexp = "^([01]\\d|2[0-3]):[0-5]\\d$", message = "must be HH:mm") String pickupTime) {}
