package com.saferide.student.infrastructure.adapter.in.web.dto;

import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

public record MyChildResponse(
        UUID id,
        String firstName,
        String lastName,
        String grade,
        UUID routeId,
        UUID pickupStopId,
        List<LocalDate> upcomingLeaves) {}
