package com.saferide.student.infrastructure.adapter.in.web.dto;

import java.util.UUID;

public record StudentResponse(
        UUID id,
        String firstName,
        String lastName,
        String admissionNumber,
        String grade,
        String parentFirstName,
        String parentLastName,
        String parentEmail,
        String parentPhone,
        String status,
        UUID routeId,
        UUID pickupStopId,
        UUID dropStopId) {}
