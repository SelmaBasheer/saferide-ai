package com.saferide.student.infrastructure.adapter.in.web.dto;

import java.util.UUID;

public record RosterEntryResponse(
        UUID studentId, String firstName, String lastName, String parentEmail, UUID pickupStopId) {}
