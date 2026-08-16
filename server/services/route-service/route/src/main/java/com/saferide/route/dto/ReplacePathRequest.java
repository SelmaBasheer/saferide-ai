package com.saferide.route.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.util.List;

public record ReplacePathRequest(@NotNull @Size(min = 2, max = 5000) List<@NotNull @Valid GeoPointDto> points) {}
