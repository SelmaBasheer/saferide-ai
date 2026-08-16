package com.saferide.route.dto;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

public record RouteResponse(
        UUID id,
        UUID schoolId,
        String code,
        String name,
        String status,
        UUID assignedBusId,
        List<StopDto> stops,
        List<GeoPointDto> path,
        Instant createdAt,
        Instant updatedAt) {}
