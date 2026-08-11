package com.saferide.bus.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CreateBusRequest(
        @NotBlank @Size(max = 32) String registrationNumber,
        @NotBlank @Size(max = 100) String model,
        @Min(1) @Max(100) int capacity) {}
