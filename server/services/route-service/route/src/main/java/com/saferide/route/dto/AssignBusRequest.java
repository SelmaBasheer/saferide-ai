package com.saferide.route.dto;

import jakarta.validation.constraints.NotNull;
import java.util.UUID;

public record AssignBusRequest(@NotNull UUID busId) {}
