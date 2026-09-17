package com.saferide.bus.dto;

import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;

public record BusDocumentResponse(
        UUID id,
        String type,
        String fileName,
        long sizeBytes,
        LocalDate expiresOn,
        boolean expired,
        Instant uploadedAt) {}
