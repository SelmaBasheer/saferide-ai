package com.saferide.bus.dto;

import java.time.Instant;
import java.util.UUID;

public record BusResponse(
        UUID id,
        UUID schoolId,
        String registrationNumber,
        String model,
        int capacity,
        String status,
        UUID assignedDriverId,
        Instant createdAt,
        Instant updatedAt) {}
