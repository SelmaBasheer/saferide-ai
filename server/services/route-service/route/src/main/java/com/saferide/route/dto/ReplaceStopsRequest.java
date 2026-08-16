package com.saferide.route.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.util.List;

public record ReplaceStopsRequest(@NotNull @Valid @Size(max = 100) List<StopInput> stops) {}
