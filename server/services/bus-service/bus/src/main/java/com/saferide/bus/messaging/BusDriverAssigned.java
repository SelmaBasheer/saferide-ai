package com.saferide.bus.messaging;

import java.time.Instant;
import java.util.UUID;

public record BusDriverAssigned(UUID busId, UUID schoolId, UUID driverId, Instant occurredAtUtc) {}
