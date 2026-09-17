package com.saferide.bus.messaging;

import java.time.Instant;
import java.util.UUID;

/**
 * A fat event on purpose: a consumer can answer "may this bus be assigned?"
 * without calling back. occurredAtUtc lets a projection ignore a replayed event
 * that is older than what it already holds.
 */
public record BusStatusChanged(
        UUID busId,
        UUID schoolId,
        String registrationNumber,
        boolean active,
        boolean documentsValid,
        Instant occurredAtUtc) {}
