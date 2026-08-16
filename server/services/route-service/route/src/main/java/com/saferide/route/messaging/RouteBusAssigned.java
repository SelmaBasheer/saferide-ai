package com.saferide.route.messaging;

import java.time.Instant;
import java.util.UUID;

public record RouteBusAssigned(UUID routeId, UUID schoolId, UUID busId, Instant occurredAtUtc) {}
