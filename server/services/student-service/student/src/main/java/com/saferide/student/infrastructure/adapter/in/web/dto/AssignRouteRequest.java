package com.saferide.student.infrastructure.adapter.in.web.dto;

import jakarta.validation.constraints.NotNull;
import java.util.UUID;

public record AssignRouteRequest(@NotNull UUID routeId, @NotNull UUID pickupStopId, UUID dropStopId) {}
