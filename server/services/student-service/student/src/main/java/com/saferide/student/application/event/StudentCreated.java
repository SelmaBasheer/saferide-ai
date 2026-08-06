package com.saferide.student.application.event;

import java.time.Instant;
import java.util.UUID;

public record StudentCreated(
        UUID studentId,
        UUID schoolId,
        String firstName,
        String lastName,
        String parentFirstName,
        String parentLastName,
        String parentEmail,
        String parentPhone,
        Instant occurredAt) {}
