package com.saferide.route.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CreateRouteRequest(@NotBlank @Size(max = 20) String code, @NotBlank @Size(max = 120) String name) {}
