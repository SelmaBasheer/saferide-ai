package com.saferide.route.messaging;

import java.time.Instant;
import java.util.UUID;

public record RouteCreated(UUID routeId, UUID schoolId, String code, String name, Instant occurredAtUtc) {}
