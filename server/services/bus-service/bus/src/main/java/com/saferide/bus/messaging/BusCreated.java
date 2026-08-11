package com.saferide.bus.messaging;

import java.time.Instant;
import java.util.UUID;

public record BusCreated(
        UUID busId, UUID schoolId, String registrationNumber, String model, int capacity, Instant occurredAtUtc) {}
